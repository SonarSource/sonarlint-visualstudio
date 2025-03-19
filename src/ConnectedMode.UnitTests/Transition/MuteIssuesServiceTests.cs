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
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Suppressions;
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
    private const string RoslynIssueServerKey = "roslynKey";
    private readonly IAnalysisIssueVisualization nonRoslynIssue = Substitute.For<IAnalysisIssueVisualization>();
    private readonly IFilterableRoslynIssue roslynIssue = Substitute.For<IFilterableRoslynIssue>();

    private MuteIssuesService testSubject;
    private IMuteIssuesWindowService muteIssuesWindowService;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private TestLogger logger;
    private IThreadHandling threadHandling;
    private IIssueSLCoreService issueSlCoreService;
    private IServerIssueFinder serverIssueFinder;
    private IRoslynSuppressionUpdater roslynSuppressionUpdater;
    private IAnalysisRequester analysisRequester;

    [TestInitialize]
    public void TestInitialize()
    {
        muteIssuesWindowService = Substitute.For<IMuteIssuesWindowService>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        serverIssueFinder = Substitute.For<IServerIssueFinder>();
        roslynSuppressionUpdater = Substitute.For<IRoslynSuppressionUpdater>();
        analysisRequester = Substitute.For<IAnalysisRequester>();
        logger = new TestLogger();
        threadHandling = new NoOpThreadHandler();
        issueSlCoreService = Substitute.For<IIssueSLCoreService>();
        testSubject = new MuteIssuesService(muteIssuesWindowService, activeConfigScopeTracker, slCoreServiceProvider, serverIssueFinder, roslynSuppressionUpdater, analysisRequester, logger, threadHandling);

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
            MefTestHelpers.CreateExport<IServerIssueFinder>(),
            MefTestHelpers.CreateExport<IRoslynSuppressionUpdater>(),
            MefTestHelpers.CreateExport<IAnalysisRequester>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<MuteIssuesService>();

    [TestMethod]
    public void Logger_HasCorrectContext()
    {
        var substituteLogger = Substitute.For<ILogger>();

        _ = new MuteIssuesService(muteIssuesWindowService, activeConfigScopeTracker, slCoreServiceProvider, serverIssueFinder, roslynSuppressionUpdater, analysisRequester, substituteLogger, threadHandling);

        substituteLogger.Received(1).ForContext("MuteIssuesService");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ResolveIssueWithDialogAsync_WhenIssueServerKeyIsNull_LogsAndThrows(string issueServerKey)
    {
        nonRoslynIssue.Issue.IssueServerKey.Returns(issueServerKey);

        var act = () => testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        act.Should().Throw<MuteIssueException>().WithMessage(Resources.MuteIssue_IssueNotFound);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_IssueNotFound);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void ResolveIssueWithDialogAsync_WhenRoslynIssueServerKeyIsNull_LogsAndThrows(string issueServerKey)
    {
        MockRoslynIssueOnServer(issueServerKey);

        var act = () => testSubject.ResolveIssueWithDialogAsync(roslynIssue);

        act.Should().Throw<MuteIssueException>().WithMessage(Resources.MuteIssue_IssueNotFound);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_IssueNotFound);
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenRoslynIssueServerIsAlreadyResolvedOnServer_LogsAndThrows()
    {
        var serverIssue = MockRoslynIssueOnServer(RoslynIssueServerKey);
        serverIssue.IsResolved = true;

        var act = () => testSubject.ResolveIssueWithDialogAsync(roslynIssue);

        act.Should().Throw<MuteIssueException>().WithMessage(Resources.MuteIssue_ErrorIssueAlreadyResolved);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_ErrorIssueAlreadyResolved);
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenConfigScopeIsNotSet_LogsAndThrows()
    {
        activeConfigScopeTracker.Current.ReturnsNull();

        var act = () => testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        act.Should().Throw<MuteIssueException>().WithMessage(Resources.MuteIssue_NotInConnectedMode);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_NotInConnectedMode);
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenNotInConnectedMode_LogsAndThrows()
    {
        NotInConnectedMode();

        var act = () => testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        act.Should().Throw<MuteIssueException>().WithMessage(Resources.MuteIssue_NotInConnectedMode);
        logger.AssertPartialOutputStrings(Resources.MuteIssue_NotInConnectedMode);
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenServiceProviderNotInitialized_LogsAndThrows()
    {
        ServiceProviderNotInitialized();

        var act = () => testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        act.Should().Throw<MuteIssueException>().WithMessage(SLCoreStrings.ServiceProviderNotInitialized);
        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenFailedToGetAllowedStatuses_LogsAndThrows()
    {
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).ThrowsAsync(_ => throw new Exception("Some error"));

        var act = () => testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        act.Should().Throw<MuteIssueException>().WithMessage("Some error");
        logger.AssertPartialOutputStrings("Some error");
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenNotPermitted_LogsAndThrowsWithReason()
    {
        var notPermittedResponse = new CheckStatusChangePermittedResponse(permitted: false, notPermittedReason: "Some reason", allowedStatuses: []);
        issueSlCoreService.CheckStatusChangePermittedAsync(Arg.Any<CheckStatusChangePermittedParams>()).Returns(notPermittedResponse);

        var act = () => testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        act.Should().Throw<MuteIssueException>().WithMessage("Some reason");
        logger.AssertPartialOutputStrings(string.Format(Resources.MuteIssue_NotPermitted, AnIssueServerKey, "Some reason"));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenValidationsArePassed_GetsAllowedStatuses()
    {
        MuteIssuePermitted();

        _ = testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        issueSlCoreService.Received().CheckStatusChangePermittedAsync(Arg.Is<CheckStatusChangePermittedParams>(x =>
            x.connectionId == "CONNECTION_ID"
            && x.issueKey == AnIssueServerKey));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseResultIsFalse_Cancels()
    {
        MuteIssuePermitted();
        CancelResolutionStatusWindow();

        var act = () => testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        act.Should().Throw<MuteIssueException.MuteIssueCancelledException>();
    }

    [TestMethod]
    [DataRow(ResolutionStatus.ACCEPT, SonarQubeIssueTransition.Accept)]
    [DataRow(ResolutionStatus.WONT_FIX, SonarQubeIssueTransition.WontFix)]
    [DataRow(ResolutionStatus.FALSE_POSITIVE, SonarQubeIssueTransition.FalsePositive)]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseResultIsTrue_ShouldMuteIssue(ResolutionStatus resolutionStatus, SonarQubeIssueTransition transition)
    {
        MuteIssuePermitted();
        muteIssuesWindowService.Show(Arg.Any<IEnumerable<SonarQubeIssueTransition>>()).Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = transition });

        _ = testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        issueSlCoreService.Received().ChangeStatusAsync(Arg.Is<ChangeIssueStatusParams>(x =>
            x.issueKey == AnIssueServerKey
            && x.newStatus == resolutionStatus
            && x.configurationScopeId == "CONFIG_SCOPE_ID"
            && !x.isTaintIssue));
    }

    [TestMethod]
    [DataRow(ResolutionStatus.ACCEPT, SonarQubeIssueTransition.Accept)]
    [DataRow(ResolutionStatus.WONT_FIX, SonarQubeIssueTransition.WontFix)]
    [DataRow(ResolutionStatus.FALSE_POSITIVE, SonarQubeIssueTransition.FalsePositive)]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseResultIsTrue_AndRoslynIssue_ShouldMuteIssue(ResolutionStatus resolutionStatus, SonarQubeIssueTransition transition)
    {
        MuteIssuePermitted();
        MockRoslynIssueOnServer(RoslynIssueServerKey);
        muteIssuesWindowService.Show(Arg.Any<IEnumerable<SonarQubeIssueTransition>>()).Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = transition });

        _ = testSubject.ResolveIssueWithDialogAsync(roslynIssue);

        issueSlCoreService.Received().ChangeStatusAsync(Arg.Is<ChangeIssueStatusParams>(x =>
            x.issueKey == RoslynIssueServerKey
            && x.newStatus == resolutionStatus
            && x.configurationScopeId == "CONFIG_SCOPE_ID"
            && !x.isTaintIssue));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseHasComment_ShouldAddComment()
    {
        MuteIssuePermitted();
        const string comment = "No you are not an issue, you are a feature";
        MockIssueAcceptedInWindow(comment);

        _ = testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

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
        MockIssueAcceptedInWindow(comment);
        issueSlCoreService.AddCommentAsync(Arg.Any<AddIssueCommentParams>()).ThrowsAsync(new Exception("Some error"));

        var act = () => testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        act.Should().Throw<MuteIssueException.MuteIssueCommentFailedException>();
        logger.AssertPartialOutputStringExists(string.Format(Resources.MuteIssue_AddCommentFailed, AnIssueServerKey, "Some error"));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseDoesNotHaveComment_ShouldMuteWithoutComment()
    {
        MuteIssuePermitted();
        MockIssueAcceptedInWindow();

        _ = testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        AssertMuteIssueWithoutComment();
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenWindowResponseHasEmptyComment_ShouldMuteWithoutComment()
    {
        MuteIssuePermitted();
        const string commentWithJustSpacesAndNewLine = " \n ";
        MockIssueAcceptedInWindow(commentWithJustSpacesAndNewLine);

        _ = testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        AssertMuteIssueWithoutComment();
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_WhenMuteIssueFails_LogsAndThrows()
    {
        MuteIssuePermitted();
        MockIssueAcceptedInWindow();
        issueSlCoreService.ChangeStatusAsync(Arg.Any<ChangeIssueStatusParams>()).Returns(call => throw new Exception("Some error"));

        var act = () => testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        act.Should().Throw<MuteIssueException>().WithMessage("Some error");
        logger.AssertPartialOutputStrings("Some error");
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_RoslynIssueMutedSuccessfully_CallsSuppressionsUpdater()
    {
        MuteIssuePermitted();
        MockRoslynIssueOnServer(RoslynIssueServerKey);
        MockIssueAcceptedInWindow();

        _ = testSubject.ResolveIssueWithDialogAsync(roslynIssue);

        roslynSuppressionUpdater.Received(1).UpdateSuppressedIssuesAsync(isResolved: true, Arg.Is<string[]>(x => x.SequenceEqual(new[] { RoslynIssueServerKey })), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_NonRoslynIssueMutedSuccessfully_DoesNotCallSuppressionsUpdater()
    {
        MuteIssuePermitted();
        MockIssueAcceptedInWindow();

        _ = testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        roslynSuppressionUpdater.DidNotReceiveWithAnyArgs().UpdateSuppressedIssuesAsync(default, default, default);
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_RoslynIssueMutedSuccessfully_RequestsAnalysis()
    {
        MuteIssuePermitted();
        MockRoslynIssueOnServer(RoslynIssueServerKey);
        MockIssueAcceptedInWindow();

        _ = testSubject.ResolveIssueWithDialogAsync(roslynIssue);

        analysisRequester.Received(1).RequestAnalysis(Arg.Is<AnalyzerOptions>(x => !x.IsOnOpen), Arg.Is<string[]>(x => x.SequenceEqual(new[] { roslynIssue.FilePath })));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_NonRoslynIssueMutedSuccessfully_RequestsAnalysis()
    {
        MuteIssuePermitted();
        MockIssueAcceptedInWindow();

        _ = testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        analysisRequester.Received(1).RequestAnalysis(Arg.Is<AnalyzerOptions>(x => !x.IsOnOpen), Arg.Is<string[]>(x => x.SequenceEqual(new[] { nonRoslynIssue.FilePath })));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_RoslynIssueMutedSuccessfullyButCommentFails_CallsSuppressionsUpdaterAndRequestsAnalysis()
    {
        MuteIssuePermitted();
        MockRoslynIssueOnServer(RoslynIssueServerKey);
        const string comment = "No you are not an issue, you are a feature";
        MockIssueAcceptedInWindow(comment);
        issueSlCoreService.AddCommentAsync(Arg.Any<AddIssueCommentParams>()).ThrowsAsync(new Exception("Some error"));

        _ = testSubject.ResolveIssueWithDialogAsync(roslynIssue);

        roslynSuppressionUpdater.Received(1).UpdateSuppressedIssuesAsync(isResolved: true, Arg.Is<string[]>(x => x.SequenceEqual(new[] { RoslynIssueServerKey })), Arg.Any<CancellationToken>());
        analysisRequester.Received(1).RequestAnalysis(Arg.Is<AnalyzerOptions>(x => !x.IsOnOpen), Arg.Is<string[]>(x => x.SequenceEqual(new[] { roslynIssue.FilePath })));
    }

    [TestMethod]
    public void ResolveIssueWithDialogAsync_NonRoslynIssueMutedSuccessfullyButCommentFails_RequestsAnalysis()
    {
        MuteIssuePermitted();
        const string comment = "No you are not an issue, you are a feature";
        MockIssueAcceptedInWindow(comment);
        issueSlCoreService.AddCommentAsync(Arg.Any<AddIssueCommentParams>()).ThrowsAsync(new Exception("Some error"));

        _ = testSubject.ResolveIssueWithDialogAsync(nonRoslynIssue);

        roslynSuppressionUpdater.DidNotReceive().UpdateSuppressedIssuesAsync(Arg.Any<bool>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>());
        analysisRequester.Received(1).RequestAnalysis(Arg.Is<AnalyzerOptions>(x => !x.IsOnOpen), Arg.Is<string[]>(x => x.SequenceEqual(new[] { nonRoslynIssue.FilePath })));
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

    private void MockNonRoslynIssue()
    {
        var analysisBase = Substitute.For<IAnalysisIssueBase>();
        analysisBase.IssueServerKey.Returns(AnIssueServerKey);
        nonRoslynIssue.Issue.Returns(analysisBase);
        nonRoslynIssue.FilePath.Returns("C:\\somePath.cs");
    }

    private SonarQubeIssue MockRoslynIssueOnServer(string issueServerKey)
    {
        roslynIssue.FilePath.Returns("C:\\someOtherPath.cs");
        var serverIssue = CreateServerIssue(issueServerKey);
        serverIssueFinder.FindServerIssueAsync(roslynIssue, Arg.Any<CancellationToken>()).Returns(serverIssue);
        return serverIssue;
    }

    public static SonarQubeIssue CreateServerIssue(string issueKey, bool isResolved = false) =>
        new(issueKey, default, default, default, default, default, isResolved, default, default, default, default, default);

    private void MockIssueAcceptedInWindow(string comment = null) =>
        muteIssuesWindowService.Show(Arg.Any<IEnumerable<SonarQubeIssueTransition>>())
            .Returns(new MuteIssuesWindowResponse { Result = true, IssueTransition = SonarQubeIssueTransition.Accept, Comment = comment });
}
