/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Windows;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.ConnectedMode.ReviewStatus;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Transition;

[TestClass]
public class MuteIssuesServiceTests
{
    private const string AnIssueServerKey = "issueServerKey";
    private readonly IAnalysisIssueVisualization nonRoslynIssue = Substitute.For<IAnalysisIssueVisualization>();

    private MuteIssuesService testSubject;
    private IChangeStatusWindowService changeStatusWindowService;
    private IReviewIssuesService reviewIssuesService;
    private IChangeIssueStatusViewModelFactory changeIssueStatusViewModelFactory;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IMessageBox messageBox;
    private TestLogger logger;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        changeStatusWindowService = Substitute.For<IChangeStatusWindowService>();
        reviewIssuesService = Substitute.For<IReviewIssuesService>();
        changeIssueStatusViewModelFactory = Substitute.For<IChangeIssueStatusViewModelFactory>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        logger = Substitute.ForPartsOf<TestLogger>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        messageBox = Substitute.For<IMessageBox>();
        testSubject = new MuteIssuesService(changeStatusWindowService, reviewIssuesService, changeIssueStatusViewModelFactory, activeConfigScopeTracker, messageBox, logger,
            threadHandling);

        MockNonRoslynIssue();
        activeConfigScopeTracker.Current.Returns(new Core.ConfigurationScope.ConfigurationScope("CONFIG_SCOPE_ID", RootPath: "C:\\", ConnectionId: "CONNECTION_ID"));
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<MuteIssuesService, IMuteIssuesService>(
            MefTestHelpers.CreateExport<IChangeStatusWindowService>(),
            MefTestHelpers.CreateExport<IReviewIssuesService>(),
            MefTestHelpers.CreateExport<IChangeIssueStatusViewModelFactory>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IMessageBox>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<MuteIssuesService>();

    [TestMethod]
    public void Logger_HasCorrectContext()
    {
        logger.Received(1).ForContext("MuteIssuesService");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ResolveIssueWithDialog_WhenIssueServerKeyIsNull_LogsAndShowsMessageBox(string issueServerKey)
    {
        testSubject.ResolveIssueWithDialog(issueServerKey, false);

        AssertMessageBoxShown(Resources.MuteIssue_IssueNotFound);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_IssueNotFound);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenConfigScopeIsNotSet_LogsAndShowsMessageBox()
    {
        activeConfigScopeTracker.Current.ReturnsNull();

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        AssertMessageBoxShown(Resources.MuteIssue_NotInConnectedMode);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_NotInConnectedMode);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenNotInConnectedMode_LogsAndShowsMessageBox()
    {
        NotInConnectedMode();

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        AssertMessageBoxShown(Resources.MuteIssue_NotInConnectedMode);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_NotInConnectedMode);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenServiceProviderNotInitialized_LogsAndShowsMessageBox()
    {
        reviewIssuesService.CheckReviewIssuePermittedAsync(AnIssueServerKey)
            .Returns(Task.FromResult<IReviewIssuePermissionArgs>(new ReviewIssueNotPermittedArgs(SLCoreStrings.ServiceProviderNotInitialized)));

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        AssertMessageBoxShown(SLCoreStrings.ServiceProviderNotInitialized);
        logger.AssertPartialOutputStringExists(string.Format(Resources.MuteIssue_NotPermitted, AnIssueServerKey, SLCoreStrings.ServiceProviderNotInitialized));
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenFailedToGetAllowedStatuses_LogsAndShowsMessageBox()
    {
        reviewIssuesService.CheckReviewIssuePermittedAsync(AnIssueServerKey)
            .Returns(Task.FromResult<IReviewIssuePermissionArgs>(new ReviewIssueNotPermittedArgs("Some error")));

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        AssertMessageBoxShown("Some error");
        logger.AssertPartialOutputStrings(string.Format(Resources.MuteIssue_NotPermitted, AnIssueServerKey, "Some error"));
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenNotPermitted_LogsAndShowsMessageBoxWithReason()
    {
        reviewIssuesService.CheckReviewIssuePermittedAsync(AnIssueServerKey)
            .Returns(Task.FromResult<IReviewIssuePermissionArgs>(new ReviewIssueNotPermittedArgs("Some reason")));

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        AssertMessageBoxShown("Some reason");
        logger.AssertPartialOutputStrings(string.Format(Resources.MuteIssue_NotPermitted, AnIssueServerKey, "Some reason"));
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenValidationsArePassed_GetsAllowedStatuses()
    {
        MuteIssuePermitted();

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        reviewIssuesService.Received().CheckReviewIssuePermittedAsync(AnIssueServerKey);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenWindowResponseResultIsFalse_Cancels()
    {
        MuteIssuePermitted();
        CancelResolutionStatusWindow();

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        reviewIssuesService.DidNotReceiveWithAnyArgs().ReviewIssueAsync(default, default, default, default);
    }

    [TestMethod]
    [DataRow(ResolutionStatus.ACCEPT)]
    [DataRow(ResolutionStatus.WONT_FIX)]
    [DataRow(ResolutionStatus.FALSE_POSITIVE)]
    public void ResolveIssueWithDialog_WhenWindowResponseResultIsTrue_ShouldMuteIssue(ResolutionStatus resolutionStatus)
    {
        MuteIssuePermitted();
        MockIssueStatusChangedInWindow(resolutionStatus);
        reviewIssuesService.ReviewIssueAsync(AnIssueServerKey, resolutionStatus, null, false).Returns(true);

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        reviewIssuesService.Received().ReviewIssueAsync(AnIssueServerKey, resolutionStatus, null, false);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenIsTaintIssue_ShouldPassTaintFlagToChangeStatus()
    {
        MuteIssuePermitted();
        MockIssueAcceptedInWindow();
        reviewIssuesService.ReviewIssueAsync(AnIssueServerKey, ResolutionStatus.ACCEPT, null, true).Returns(true);

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, true);

        reviewIssuesService.Received().ReviewIssueAsync(AnIssueServerKey, ResolutionStatus.ACCEPT, null, true);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenWindowResponseHasComment_ShouldAddComment()
    {
        MuteIssuePermitted();
        const string comment = "No you are not an issue, you are a feature";
        MockIssueAcceptedInWindow(comment);
        reviewIssuesService.ReviewIssueAsync(AnIssueServerKey, ResolutionStatus.ACCEPT, comment, false).Returns(true);

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        reviewIssuesService.Received().ReviewIssueAsync(AnIssueServerKey, ResolutionStatus.ACCEPT, comment, false);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenReviewIssueFails_LogsAndShowsMessageBox()
    {
        MuteIssuePermitted();
        const string comment = "No you are not an issue, you are a feature";
        MockIssueAcceptedInWindow(comment);
        reviewIssuesService.ReviewIssueAsync(AnIssueServerKey, ResolutionStatus.ACCEPT, comment, false).Returns(false);

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        AssertMessageBoxShown(Resources.MuteIssue_AnErrorOccurred);
        logger.AssertPartialOutputStrings(string.Format(Resources.MuteIssue_AnErrorOccurred, AnIssueServerKey, "See previous log entries"));
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenWindowResponseDoesNotHaveComment_ShouldMuteWithoutComment()
    {
        MuteIssuePermitted();
        MockIssueAcceptedInWindow();
        reviewIssuesService.ReviewIssueAsync(AnIssueServerKey, ResolutionStatus.ACCEPT, null, false).Returns(true);

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        reviewIssuesService.Received().ReviewIssueAsync(AnIssueServerKey, ResolutionStatus.ACCEPT, null, false);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenWindowResponseHasEmptyComment_ShouldMuteWithoutComment()
    {
        MuteIssuePermitted();
        const string commentWithJustSpacesAndNewLine = " \n ";
        MockIssueAcceptedInWindow(commentWithJustSpacesAndNewLine);
        reviewIssuesService.ReviewIssueAsync(AnIssueServerKey, ResolutionStatus.ACCEPT, null, false).Returns(true);

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        reviewIssuesService.Received().ReviewIssueAsync(AnIssueServerKey, ResolutionStatus.ACCEPT, null, false);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenMuteIssueFails_LogsAndShowsMessageBox()
    {
        MuteIssuePermitted();
        MockIssueAcceptedInWindow();
        reviewIssuesService.ReviewIssueAsync(AnIssueServerKey, ResolutionStatus.ACCEPT, null, false).Returns(false);

        testSubject.ResolveIssueWithDialog(AnIssueServerKey, false);

        AssertMessageBoxShown(Resources.MuteIssue_AnErrorOccurred);
        logger.AssertPartialOutputStrings(string.Format(Resources.MuteIssue_AnErrorOccurred, AnIssueServerKey, "See previous log entries"));
    }

    private void NotInConnectedMode() => activeConfigScopeTracker.Current.Returns(new Core.ConfigurationScope.ConfigurationScope("CONFIG_SCOPE_ID"));

    private void MuteIssuePermitted()
    {
        reviewIssuesService.CheckReviewIssuePermittedAsync(AnIssueServerKey)
            .Returns(Task.FromResult<IReviewIssuePermissionArgs>(
                new ReviewIssuePermittedArgs([ResolutionStatus.ACCEPT, ResolutionStatus.FALSE_POSITIVE])));

        var mockViewModel = Substitute.For<IChangeStatusViewModel>();
        changeIssueStatusViewModelFactory.CreateForIssue(Arg.Any<ResolutionStatus?>(), Arg.Any<IEnumerable<ResolutionStatus>>())
            .Returns(mockViewModel);
    }

    private void CancelResolutionStatusWindow()
    {
        changeStatusWindowService.Show(Arg.Any<IChangeStatusViewModel>())
            .Returns(new ChangeStatusWindowResponse { Result = false });
    }

    private void MockIssueAcceptedInWindow(string comment = null)
    {
        var statusViewModel = Substitute.For<IStatusViewModel>();
        statusViewModel.GetCurrentStatus<ResolutionStatus>().Returns(ResolutionStatus.ACCEPT);

        var viewModel = Substitute.For<IChangeStatusViewModel>();
        var normalizedComment = string.IsNullOrWhiteSpace(comment) ? null : comment;
        viewModel.GetNormalizedComment().Returns(normalizedComment);

        changeIssueStatusViewModelFactory.CreateForIssue(Arg.Any<ResolutionStatus?>(), Arg.Any<IEnumerable<ResolutionStatus>>())
            .Returns(viewModel);

        changeStatusWindowService.Show(Arg.Any<IChangeStatusViewModel>())
            .Returns(new ChangeStatusWindowResponse { Result = true, SelectedStatus = statusViewModel });
    }

    private void MockIssueStatusChangedInWindow(ResolutionStatus status)
    {
        var statusViewModel = Substitute.For<IStatusViewModel>();
        statusViewModel.GetCurrentStatus<ResolutionStatus>().Returns(status);

        var viewModel = Substitute.For<IChangeStatusViewModel>();
        viewModel.GetNormalizedComment().Returns((string)null);

        changeIssueStatusViewModelFactory.CreateForIssue(Arg.Any<ResolutionStatus?>(), Arg.Any<IEnumerable<ResolutionStatus>>())
            .Returns(viewModel);

        changeStatusWindowService.Show(Arg.Any<IChangeStatusViewModel>())
            .Returns(new ChangeStatusWindowResponse { Result = true, SelectedStatus = statusViewModel });
    }

    private void MockNonRoslynIssue()
    {
        var analysisBase = Substitute.For<IAnalysisIssueBase>();
        analysisBase.IssueServerKey.Returns(AnIssueServerKey);
        nonRoslynIssue.Issue.Returns(analysisBase);
        nonRoslynIssue.FilePath.Returns("C:\\somePath.cs");
    }

    private void AssertMessageBoxShown(string message) => messageBox.Received(1).Show(message, Resources.MuteIssue_FailureCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
}
