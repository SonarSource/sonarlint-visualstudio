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

using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Transition;
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

    private MuteIssuesService testSubject;
    private IMuteIssuesWindowService muteIssuesWindowService;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private TestLogger logger;
    private IThreadHandling threadHandling;
    private IIssueSLCoreService issueSlCoreService;

    [TestInitialize]
    public void TestInitialize()
    {
        muteIssuesWindowService = Substitute.For<IMuteIssuesWindowService>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        logger = new TestLogger();
        threadHandling = new NoOpThreadHandler();
        issueSlCoreService = Substitute.For<IIssueSLCoreService>();
        testSubject = new MuteIssuesService(muteIssuesWindowService, activeConfigScopeTracker, slCoreServiceProvider, logger, threadHandling);

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
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<MuteIssuesService>();

    [TestMethod]
    public void Logger_HasCorrectContext()
    {
        var substitureLogger = Substitute.For<ILogger>();

        _ = new MuteIssuesService(muteIssuesWindowService, activeConfigScopeTracker, slCoreServiceProvider, substitureLogger, threadHandling);

        substitureLogger.Received(1).ForContext("MuteIssuesService");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ResolveIssueWithDialogAsync_WhenIssueServerKeyIsNull_LogsAndThrows(string issueServerKey)
    {
        var act = () => testSubject.ResolveIssueWithDialogAsync(issueServerKey);

        act.Should().Throw<MuteIssueException>().WithMessage(Resources.MuteIssue_IssueNotFound);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_IssueNotFound);
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenConfigScopeIsNotSet_LogsAndThrows()
    {
        activeConfigScopeTracker.Current.ReturnsNull();

        var act = () => testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        act.Should().Throw<MuteIssueException>().WithMessage(Resources.MuteIssue_NotInConnectedMode);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_NotInConnectedMode);
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenNotInConnectedMode_LogsAndThrows()
    {
        NotInConnectedMode();

        var act = () => testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        act.Should().Throw<MuteIssueException>().WithMessage(Resources.MuteIssue_NotInConnectedMode);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_NotInConnectedMode);
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenServiceProviderNotInitialized_LogsAndThrows()
    {
        ServiceProviderNotInitialized();

        var act = () => testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        act.Should().Throw<MuteIssueException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenFailedToGetAllowedStatuses_LogsAndThrows()
    {
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).ThrowsAsync(_ => throw new Exception("Some error"));

        var act = () => testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        act.Should().Throw<MuteIssueException>().WithMessage("Some error");
        logger.AssertPartialOutputStrings("Some error");
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenNotPermitted_LogsAndThrowsWithReason()
    {
        var notPermittedResponse = new CheckStatusChangePermittedResponse(permitted: false, notPermittedReason: "Some reason", allowedStatuses: []);
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).Returns(notPermittedResponse);

        var act = () => testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        act.Should().Throw<MuteIssueException>().WithMessage("Some reason");
        logger.AssertPartialOutputStrings(string.Format(Resources.MuteIssue_NotPermitted, AnIssueServerKey, "Some reason"));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenValidationsArePassed_GetsAllowedStatuses()
    {
        MuteIssuePermitted();

        _ = testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        issueSlCoreService.Received().CheckStatusChangePermittedAsync(Arg.Is<CheckStatusChangePermittedParams>(x =>
            x.connectionId == "CONNECTION_ID"
            && x.issueKey == AnIssueServerKey));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseResultIsFalse_Cancels()
    {
        MuteIssuePermitted();
        CancelResolutionStatusWindow();

        var act = () => testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        act.Should().Throw<MuteIssueException.MuteIssueCancelledException>();
    }

    [TestMethod]
    [DataRow(ResolutionStatus.ACCEPT, SonarQubeIssueTransition.Accept)]
    [DataRow(ResolutionStatus.WONT_FIX, SonarQubeIssueTransition.WontFix)]
    [DataRow(ResolutionStatus.FALSE_POSITIVE, SonarQubeIssueTransition.FalsePositive)]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseResultIsTrue_ShouldMuteIssue(ResolutionStatus resolutionStatus, SonarQubeIssueTransition transition)
    {
        MuteIssuePermitted();
        muteIssuesWindowService.Show().Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = transition });

        _ = testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        issueSlCoreService.Received().ChangeStatusAsync(Arg.Is<ChangeIssueStatusParams>(x =>
            x.issueKey == AnIssueServerKey
            && x.newStatus == resolutionStatus
            && x.configurationScopeId == "CONFIG_SCOPE_ID"
            && !x.isTaintIssue));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseHasComment_ShouldAddComment()
    {
        MuteIssuePermitted();
        const string comment = "No you are not an issue, you are a feature";
        muteIssuesWindowService.Show().Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = SonarQubeIssueTransition.Accept, Comment = comment});

        _ = testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        Received.InOrder(() =>
        {
            issueSlCoreService.ChangeStatusAsync(Arg.Is<ChangeIssueStatusParams>(x => x.issueKey == AnIssueServerKey));
            issueSlCoreService.AddCommentAsync(Arg.Is<AddIssueCommentParams>(x => x.issueKey == AnIssueServerKey && x.text == comment));
        });
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseHasCommentButFails_LogsAndThrows()
    {
        MuteIssuePermitted();
        const string comment = "No you are not an issue, you are a feature";
        muteIssuesWindowService.Show().Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = SonarQubeIssueTransition.Accept, Comment = comment});
        issueSlCoreService.AddCommentAsync(Arg.Any<AddIssueCommentParams>()).ThrowsAsync(new Exception("Some error"));

        var act = () => testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        act.Should().Throw<MuteIssueException.MuteIssueCommentFailedException>();
        logger.AssertPartialOutputStringExists(string.Format(Resources.MuteIssue_AddCommentFailed, AnIssueServerKey, "Some error"));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseDoesNotHaveComment_ShouldMuteWithoutComment()
    {
        MuteIssuePermitted();
        muteIssuesWindowService.Show().Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = SonarQubeIssueTransition.Accept});

        _ = testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        AssertMuteIssueWithoutComment();
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseHasEmptyComment_ShouldMuteWithoutComment()
    {
        MuteIssuePermitted();
        const string commentWithJustSpacesAndNewLine = " \n ";
        muteIssuesWindowService.Show().Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = SonarQubeIssueTransition.Accept, Comment = commentWithJustSpacesAndNewLine});

        _ = testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        AssertMuteIssueWithoutComment();
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenMuteIssueFails_LogsAndThrows()
    {
        MuteIssuePermitted();
        muteIssuesWindowService.Show().Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = SonarQubeIssueTransition.Accept });
        issueSlCoreService.ChangeStatusAsync(Arg.Any<ChangeIssueStatusParams>()).Returns(call => throw new Exception("Some error"));

        var act = () => testSubject.ResolveIssueWithDialogAsync(AnIssueServerKey);

        act.Should().Throw<MuteIssueException>().WithMessage("Some error");
        logger.AssertPartialOutputStrings("Some error");
    }

    private void NotInConnectedMode() => activeConfigScopeTracker.Current.Returns(new Core.ConfigurationScope.ConfigurationScope("CONFIG_SCOPE_ID"));

    private void ServiceProviderNotInitialized() => slCoreServiceProvider.TryGetTransientService(out Arg.Any<ISLCoreService>()).ReturnsForAnyArgs(false);

    private void MuteIssuePermitted()
    {
        var notPermittedResponse = new CheckStatusChangePermittedResponse(permitted: true, notPermittedReason: null, allowedStatuses: [ResolutionStatus.ACCEPT, ResolutionStatus.FALSE_POSITIVE]);
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).Returns(notPermittedResponse);
    }

    private void CancelResolutionStatusWindow() => muteIssuesWindowService.Show().Returns(new MuteIssuesWindowResponse { Result = false });

    private void AssertMuteIssueWithoutComment()
    {
        issueSlCoreService.Received().ChangeStatusAsync(Arg.Is<ChangeIssueStatusParams>(x => x.issueKey == AnIssueServerKey));
        issueSlCoreService.DidNotReceiveWithAnyArgs().AddCommentAsync(Arg.Any<AddIssueCommentParams>());
    }
}
