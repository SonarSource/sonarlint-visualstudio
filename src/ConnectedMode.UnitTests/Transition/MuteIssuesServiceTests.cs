/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Issue;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Transition;

[TestClass]
public class MuteIssuesServiceTests
{
    private const string AnIssueServerKey = "issueServerKey";
    private readonly IAnalysisIssueVisualization nonRoslynIssue = Substitute.For<IAnalysisIssueVisualization>();

    private MuteIssuesService testSubject;
    private IMuteIssuesWindowService muteIssuesWindowService;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private IMessageBox messageBox;
    private TestLogger logger;
    private IThreadHandling threadHandling;
    private IIssueSLCoreService issueSlCoreService;

    [TestInitialize]
    public void TestInitialize()
    {
        muteIssuesWindowService = Substitute.For<IMuteIssuesWindowService>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        logger = Substitute.ForPartsOf<TestLogger>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        messageBox = Substitute.For<IMessageBox>();
        issueSlCoreService = Substitute.For<IIssueSLCoreService>();
        testSubject = new MuteIssuesService(muteIssuesWindowService, activeConfigScopeTracker, slCoreServiceProvider, messageBox, logger,
            threadHandling);

        MockNonRoslynIssue();
        activeConfigScopeTracker.Current.Returns(new Core.ConfigurationScope.ConfigurationScope("CONFIG_SCOPE_ID", RootPath: "C:\\", ConnectionId: "CONNECTION_ID"));
        slCoreServiceProvider.TryGetTransientService(out IIssueSLCoreService _).Returns(call =>
        {
            call[0] = issueSlCoreService;
            return true;
        });
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<MuteIssuesService, IMuteIssuesService>(
            MefTestHelpers.CreateExport<IMuteIssuesWindowService>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
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
        nonRoslynIssue.Issue.IssueServerKey.Returns(issueServerKey);

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        AssertMessageBoxShown(Resources.MuteIssue_IssueNotFound);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_IssueNotFound);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenConfigScopeIsNotSet_LogsAndShowsMessageBox()
    {
        activeConfigScopeTracker.Current.ReturnsNull();

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        AssertMessageBoxShown(Resources.MuteIssue_NotInConnectedMode);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_NotInConnectedMode);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenNotInConnectedMode_LogsAndShowsMessageBox()
    {
        NotInConnectedMode();

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        AssertMessageBoxShown(Resources.MuteIssue_NotInConnectedMode);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_NotInConnectedMode);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenServiceProviderNotInitialized_LogsAndShowsMessageBox()
    {
        ServiceProviderNotInitialized();

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        AssertMessageBoxShown(SLCoreStrings.ServiceProviderNotInitialized);
        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenFailedToGetAllowedStatuses_LogsAndShowsMessageBox()
    {
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).ThrowsAsync(_ => throw new Exception("Some error"));

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        AssertMessageBoxShown("Some error");
        logger.AssertPartialOutputStrings("Some error");
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenNotPermitted_LogsAndShowsMessageBoxWithReason()
    {
        var notPermittedResponse = new CheckStatusChangePermittedResponse(permitted: false, notPermittedReason: "Some reason", allowedStatuses: []);
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).Returns(notPermittedResponse);

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        AssertMessageBoxShown("Some reason");
        logger.AssertPartialOutputStrings(string.Format(Resources.MuteIssue_NotPermitted, AnIssueServerKey, "Some reason"));
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenValidationsArePassed_GetsAllowedStatuses()
    {
        MuteIssuePermitted();

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        issueSlCoreService.Received().CheckStatusChangePermittedAsync(Arg.Is<CheckStatusChangePermittedParams>(x =>
            x.connectionId == "CONNECTION_ID"
            && x.issueKey == AnIssueServerKey));
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenWindowResponseResultIsFalse_Cancels()
    {
        MuteIssuePermitted();
        CancelResolutionStatusWindow();

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        issueSlCoreService.DidNotReceiveWithAnyArgs().ChangeStatusAsync(default);
        issueSlCoreService.DidNotReceiveWithAnyArgs().AddCommentAsync(default);
    }

    [TestMethod]
    [DataRow(ResolutionStatus.ACCEPT, SonarQubeIssueTransition.Accept)]
    [DataRow(ResolutionStatus.WONT_FIX, SonarQubeIssueTransition.WontFix)]
    [DataRow(ResolutionStatus.FALSE_POSITIVE, SonarQubeIssueTransition.FalsePositive)]
    public void ResolveIssueWithDialog_WhenWindowResponseResultIsTrue_ShouldMuteIssue(ResolutionStatus resolutionStatus, SonarQubeIssueTransition transition)
    {
        MuteIssuePermitted();
        muteIssuesWindowService.Show(Arg.Any<IEnumerable<SonarQubeIssueTransition>>()).Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = transition });

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        issueSlCoreService.Received().ChangeStatusAsync(Arg.Is<ChangeIssueStatusParams>(x =>
            x.issueKey == AnIssueServerKey
            && x.newStatus == resolutionStatus
            && x.configurationScopeId == "CONFIG_SCOPE_ID"
            && !x.isTaintIssue));
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenWindowResponseHasComment_ShouldAddComment()
    {
        MuteIssuePermitted();
        const string comment = "No you are not an issue, you are a feature";
        MockIssueAcceptedInWindow(comment);

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            issueSlCoreService.ChangeStatusAsync(Arg.Is<ChangeIssueStatusParams>(x => x.issueKey == AnIssueServerKey));
            issueSlCoreService.AddCommentAsync(Arg.Is<AddIssueCommentParams>(x => x.issueKey == AnIssueServerKey && x.text == comment));
        });
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenWindowResponseHasCommentButFails_LogsAndShowsCommentFailedMessageBox()
    {
        MuteIssuePermitted();
        const string comment = "No you are not an issue, you are a feature";
        MockIssueAcceptedInWindow(comment);
        issueSlCoreService.AddCommentAsync(Arg.Any<AddIssueCommentParams>()).ThrowsAsync(new Exception("Some error"));

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        AssertCommentFailedMessageBoxShown();
        logger.AssertPartialOutputStringExists(string.Format(Resources.MuteIssue_AddCommentFailed, AnIssueServerKey, "Some error"));
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenWindowResponseDoesNotHaveComment_ShouldMuteWithoutComment()
    {
        MuteIssuePermitted();
        MockIssueAcceptedInWindow();

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        AssertMuteIssueWithoutComment();
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenWindowResponseHasEmptyComment_ShouldMuteWithoutComment()
    {
        MuteIssuePermitted();
        const string commentWithJustSpacesAndNewLine = " \n ";
        MockIssueAcceptedInWindow(commentWithJustSpacesAndNewLine);

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        AssertMuteIssueWithoutComment();
    }

    [TestMethod]
    public void ResolveIssueWithDialog_WhenMuteIssueFails_LogsAndShowsMessageBox()
    {
        MuteIssuePermitted();
        MockIssueAcceptedInWindow();
        issueSlCoreService.ChangeStatusAsync(Arg.Any<ChangeIssueStatusParams>()).Returns(call => throw new Exception("Some error"));

        testSubject.ResolveIssueWithDialog(nonRoslynIssue);

        AssertMessageBoxShown("Some error");
        logger.AssertPartialOutputStrings("Some error");
    }

    private void NotInConnectedMode() => activeConfigScopeTracker.Current.Returns(new Core.ConfigurationScope.ConfigurationScope("CONFIG_SCOPE_ID"));

    private void ServiceProviderNotInitialized() => slCoreServiceProvider.TryGetTransientService(out Arg.Any<ISLCoreService>()).ReturnsForAnyArgs(false);

    private void MuteIssuePermitted()
    {
        var permittedResponse = new CheckStatusChangePermittedResponse(permitted: true, notPermittedReason: null, allowedStatuses: [ResolutionStatus.ACCEPT, ResolutionStatus.FALSE_POSITIVE]);
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).Returns(permittedResponse);
    }

    private void CancelResolutionStatusWindow() => muteIssuesWindowService.Show(Arg.Any<IEnumerable<SonarQubeIssueTransition>>()).Returns(new MuteIssuesWindowResponse { Result = false });

    private void AssertMuteIssueWithoutComment()
    {
        issueSlCoreService.Received().ChangeStatusAsync(Arg.Is<ChangeIssueStatusParams>(x => x.issueKey == AnIssueServerKey));
        issueSlCoreService.DidNotReceiveWithAnyArgs().AddCommentAsync(Arg.Any<AddIssueCommentParams>());
    }

    private void MockIssueAcceptedInWindow(string comment = null) =>
        muteIssuesWindowService.Show(Arg.Any<IEnumerable<SonarQubeIssueTransition>>())
            .Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = SonarQubeIssueTransition.Accept, Comment = comment });

    private void MockNonRoslynIssue()
    {
        var analysisBase = Substitute.For<IAnalysisIssueBase>();
        analysisBase.IssueServerKey.Returns(AnIssueServerKey);
        nonRoslynIssue.Issue.Returns(analysisBase);
        nonRoslynIssue.FilePath.Returns("C:\\somePath.cs");
    }

    private void AssertMessageBoxShown(string message) => messageBox.Received(1).Show(message, Resources.MuteIssue_FailureCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);

    private void AssertCommentFailedMessageBoxShown() =>
        messageBox.Received(1).Show(Resources.MuteIssue_MessageBox_AddCommentFailed, Resources.MuteIssue_WarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
}
