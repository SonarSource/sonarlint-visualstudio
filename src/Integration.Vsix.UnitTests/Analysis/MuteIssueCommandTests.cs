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

using System.ComponentModel.Design;
using System.Windows;
using Microsoft.VisualStudio.OLE.Interop;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.TestInfrastructure.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis;

[TestClass]
public class MuteIssueCommandTests
{
    private const int VisibleAndEnabled = (int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
    private const int VisibleButDisabled = (int)(OLECMDF.OLECMDF_SUPPORTED);
    private const int InvisibleAndDisabled = (int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE);

    private static readonly BindingConfiguration ConnectedModeBinding = new(null, SonarLintMode.Connected, null);

    private MuteIssueCommand testSubject;
    private MenuCommand testSubjectMenuCommand;
    private DummyMenuCommandService dummyMenuService;
    private IErrorListHelper errorListHelper;
    private IRoslynIssueLineHashCalculator roslynIssueLineHashCalculator;
    private IMuteIssuesService muteIssuesService;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private NoOpThreadHandler threadHandling;
    private IMessageBox messageBox;
    private TestLogger logger;
    private ILanguageProvider languageProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        dummyMenuService = new DummyMenuCommandService();
        errorListHelper = Substitute.For<IErrorListHelper>();
        roslynIssueLineHashCalculator = Substitute.For<IRoslynIssueLineHashCalculator>();
        muteIssuesService = Substitute.For<IMuteIssuesService>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        threadHandling = new NoOpThreadHandler();
        messageBox = Substitute.For<IMessageBox>();
        logger = new TestLogger();
        languageProvider = LanguageProvider.Instance;

        testSubject = new MuteIssueCommand(dummyMenuService, errorListHelper, roslynIssueLineHashCalculator, muteIssuesService, activeSolutionBoundTracker, threadHandling,
            messageBox, logger, languageProvider);

        testSubjectMenuCommand = dummyMenuService.AddedMenuCommands[0];

        activeSolutionBoundTracker.CurrentConfiguration.Returns(ConnectedModeBinding);
    }

    [TestMethod]
    public void Logger_HasCorrectContext()
    {
        var substituteLogger = Substitute.For<ILogger>();

        _ = new MuteIssueCommand(dummyMenuService, errorListHelper, roslynIssueLineHashCalculator, muteIssuesService, activeSolutionBoundTracker, threadHandling,
            messageBox, substituteLogger, languageProvider);

        substituteLogger.Received(1).ForContext("MuteIssueCommand");
    }

    [TestMethod]
    public void Ctor_RegistersCommand()
    {
        dummyMenuService.AddedMenuCommands.Count.Should().Be(1);
        testSubjectMenuCommand.CommandID.ID.Should().Be(MuteIssueCommand.CommandId);
        testSubjectMenuCommand.CommandID.Guid.Should().Be(MuteIssueCommand.CommandSet);
    }

    [TestMethod]
    public void QueryStatus_NotSonarIssue_Invisible()
    {
        errorListHelper.TryGetRuleIdAndSuppressionStateFromSelectedRow(out _, out _).Returns(false);
        var oleStatus = testSubjectMenuCommand.OleStatus;

        oleStatus.Should().Be(InvisibleAndDisabled);
        testSubjectMenuCommand.Visible.Should().BeFalse();
        testSubjectMenuCommand.Enabled.Should().BeFalse();
    }

    [TestMethod]
    public void QueryStatus_Exception_Invisible()
    {
        errorListHelper.TryGetRuleIdAndSuppressionStateFromSelectedRow(out _, out _).Throws(new Exception());
        var oleStatus = testSubjectMenuCommand.OleStatus;

        oleStatus.Should().Be(InvisibleAndDisabled);
        testSubjectMenuCommand.Visible.Should().BeFalse();
        testSubjectMenuCommand.Enabled.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow("java:S111")]
    public void QueryStatus_NotSupportedIssue_Invisible(string errorCode)
    {
        errorListHelper.TryGetRuleIdAndSuppressionStateFromSelectedRow(out _, out _).Returns(x =>
        {
            SonarCompositeRuleId.TryParse(errorCode, out var ruleId);
            x[0] = ruleId;
            x[1] = false;
            return true;
        });
        var oleStatus = testSubjectMenuCommand.OleStatus;

        oleStatus.Should().Be(InvisibleAndDisabled);
        testSubjectMenuCommand.Visible.Should().BeFalse();
        testSubjectMenuCommand.Enabled.Should().BeFalse();
    }

    [TestMethod]
    public void QueryStatus_SuppressedIssue_Invisible()
    {
        errorListHelper.TryGetRuleIdAndSuppressionStateFromSelectedRow(out _, out _).Returns(x =>
        {
            SonarCompositeRuleId.TryParse("javascript:S333", out var ruleId);
            x[0] = ruleId;
            x[1] = true;
            return true;
        });
        var oleStatus = testSubjectMenuCommand.OleStatus;

        oleStatus.Should().Be(InvisibleAndDisabled);
        testSubjectMenuCommand.Visible.Should().BeFalse();
        testSubjectMenuCommand.Enabled.Should().BeFalse();
    }

    [TestMethod]
    public void QueryStatus_NotInConnectedMode_VisibleButDisabled()
    {
        activeSolutionBoundTracker.CurrentConfiguration.Returns(BindingConfiguration.Standalone);
        errorListHelper.TryGetRuleIdAndSuppressionStateFromSelectedRow(out _, out _).Returns(x =>
        {
            SonarCompositeRuleId.TryParse("javascript:S333", out var ruleId);
            x[0] = ruleId;
            x[1] = false;
            return true;
        });
        var oleStatus = testSubjectMenuCommand.OleStatus;

        oleStatus.Should().Be(VisibleButDisabled);
        testSubjectMenuCommand.Visible.Should().BeTrue();
        testSubjectMenuCommand.Enabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("cpp:S111")]
    [DataRow("c:S222")]
    [DataRow("javascript:S333")]
    [DataRow("typescript:S444")]
    [DataRow("css:S555")]
    [DataRow("Web:S545")]
    [DataRow("csharpsquid:S555")]
    [DataRow("vbnet:S555")]
    [DataRow("tsql:S5556")]
    public void QueryStatus_ConnectedModeAndSupportedIssue_VisibleAndEnabled(string errorCode)
    {
        errorListHelper.TryGetRuleIdAndSuppressionStateFromSelectedRow(out _, out _).Returns(x =>
        {
            SonarCompositeRuleId.TryParse(errorCode, out var ruleId);
            x[0] = ruleId;
            x[1] = false;
            return true;
        });
        var oleStatus = testSubjectMenuCommand.OleStatus;

        oleStatus.Should().Be(VisibleAndEnabled);
        testSubjectMenuCommand.Visible.Should().BeTrue();
        testSubjectMenuCommand.Enabled.Should().BeTrue();
    }

    [TestMethod]
    public void Execute_WhenNoIssueSelected_DoesNothing()
    {
        errorListHelper.TryGetIssueFromSelectedRow(out _).Returns(false);
        errorListHelper.TryGetRoslynIssueFromSelectedRow(out _).Returns(false);

        testSubjectMenuCommand.Invoke();

        errorListHelper.Received(1).TryGetIssueFromSelectedRow(out Arg.Any<IFilterableIssue>());
        errorListHelper.Received(1).TryGetRoslynIssueFromSelectedRow(out Arg.Any<IFilterableRoslynIssue>());
        muteIssuesService.DidNotReceiveWithAnyArgs().ResolveIssueWithDialogAsync(Arg.Any<IFilterableIssue>());
    }

    [TestMethod]
    public void Execute_WithRoslynIssue_MutesIssue()
    {
        var roslynIssue = SetupRoslynIssue(out var sonarQubeIssue);

        testSubjectMenuCommand.Invoke();

        roslynIssueLineHashCalculator.Received(1).UpdateRoslynIssueWithLineHash(roslynIssue);
        muteIssuesService.Received(1).ResolveIssueWithDialogAsync(roslynIssue);
    }

    [TestMethod]
    public void Execute_WithNonRoslynIssue_MutesIssue()
    {
        var issue = SetupNonRoslynIssue();

        testSubjectMenuCommand.Invoke();

        muteIssuesService.Received(1).ResolveIssueWithDialogAsync(issue);
    }

    [TestMethod]
    public void Execute_WhenException_CatchesAndLogs()
    {
        errorListHelper.TryGetIssueFromSelectedRow(out Arg.Any<IFilterableIssue>()).Throws(new Exception("exception xxx"));

        var act = () => testSubjectMenuCommand.Invoke();

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists("exception xxx");
    }

    [TestMethod]
    public void Execute_CriticalExceptionNotCaught()
    {
        errorListHelper.TryGetIssueFromSelectedRow(out Arg.Any<IFilterableIssue>()).Throws(new DivideByZeroException("exception xxx"));

        var act = () => testSubjectMenuCommand.Invoke();

        act.Should().ThrowExactly<DivideByZeroException>();
        logger.AssertPartialOutputStringDoesNotExist("exception xxx");
    }

    [TestMethod]
    public void Execute_WithNonRoslynIssue_WhenMuteIssueException_CatchesAndShowsMessageBox()
    {
        SetupNonRoslynIssue();
        muteIssuesService.ResolveIssueWithDialogAsync(Arg.Any<IFilterableIssue>()).ThrowsAsync(new MuteIssueException("Error while muting"));

        var act = () => testSubjectMenuCommand.Invoke();

        act.Should().NotThrow();
        AssertMessageBoxShown("Error while muting");
    }

    [TestMethod]
    public void Execute_WithRoslynIssue_WhenMuteIssueException_CatchesAndShowsMessageBox()
    {
        SetupRoslynIssue(out _);
        muteIssuesService.ResolveIssueWithDialogAsync(Arg.Any<IFilterableIssue>()).ThrowsAsync(new MuteIssueException("Error while muting"));

        var act = () => testSubjectMenuCommand.Invoke();

        act.Should().NotThrow();
        AssertMessageBoxShown("Error while muting");
    }

    [TestMethod]
    public void Execute_WithNonRoslynIssue_WhenMuteIssueCommentFailedException_CatchesAndShowsMessageBox()
    {
        SetupNonRoslynIssue();
        muteIssuesService.ResolveIssueWithDialogAsync(Arg.Any<IFilterableIssue>()).ThrowsAsync(new MuteIssueException.MuteIssueCommentFailedException());

        var act = () => testSubjectMenuCommand.Invoke();

        act.Should().NotThrow();
        AssertCommentFailedMessageBoxShown();
    }

    [TestMethod]
    public void Execute_WithRoslynIssue_WhenMuteIssueCommentFailedException_CatchesAndShowsMessageBox()
    {
        SetupRoslynIssue(out _);
        muteIssuesService.ResolveIssueWithDialogAsync(Arg.Any<IFilterableIssue>()).ThrowsAsync(new MuteIssueException.MuteIssueCommentFailedException());

        var act = () => testSubjectMenuCommand.Invoke();

        act.Should().NotThrow();
        AssertCommentFailedMessageBoxShown();
    }

    [TestMethod]
    public void Execute_WithNonRoslynIssue_WhenMuteIssueCancelledException_Catches()
    {
        SetupNonRoslynIssue();
        muteIssuesService.ResolveIssueWithDialogAsync(Arg.Any<IFilterableIssue>()).ThrowsAsync(new MuteIssueException.MuteIssueCancelledException());

        var act = () => testSubjectMenuCommand.Invoke();

        act.Should().NotThrow();
        messageBox.DidNotReceiveWithAnyArgs().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public void Execute_WithRoslynIssue_WhenMuteIssueCancelledException_Catches()
    {
        SetupRoslynIssue(out _);
        muteIssuesService.ResolveIssueWithDialogAsync(Arg.Any<IFilterableIssue>()).ThrowsAsync(new MuteIssueException.MuteIssueCancelledException());

        var act = () => testSubjectMenuCommand.Invoke();

        act.Should().NotThrow();
        messageBox.DidNotReceiveWithAnyArgs().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public void SupportedRepos_AllKnownLanguagesAreSupported()
    {
        var supportedRepos = testSubject.SupportedRepos;

        supportedRepos.Should().HaveCount(languageProvider.AllKnownLanguages.Count);
    }

    private IFilterableRoslynIssue SetupRoslynIssue(out SonarQubeIssue sonarQubeIssue)
    {
        var roslynIssue = Substitute.For<IFilterableRoslynIssue>();
        sonarQubeIssue = DummySonarQubeIssueFactory.CreateServerIssue();
        errorListHelper.TryGetIssueFromSelectedRow(out _).Returns(x =>
        {
            x[0] = roslynIssue;
            return true;
        });
        return roslynIssue;
    }

    private IAnalysisIssueVisualization SetupNonRoslynIssue()
    {
        var issue = Substitute.For<IAnalysisIssueVisualization>();
        issue.Issue.Returns(new AnalysisIssue(Guid.NewGuid(), "ruleKey", "issueServerKey", false, AnalysisIssueSeverity.Major, AnalysisIssueType.Bug, null, Substitute.For<IAnalysisIssueLocation>(),
            []));

        errorListHelper.TryGetIssueFromSelectedRow(out _).Returns(x =>
        {
            x[0] = issue;
            return true;
        });

        return issue;
    }

    private void AssertMessageBoxShown(string message) => messageBox.Received(1).Show(message, AnalysisStrings.MuteIssue_FailureCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);

    private void AssertCommentFailedMessageBoxShown() =>
        messageBox.Received(1).Show(AnalysisStrings.MuteIssue_MessageBox_AddCommentFailed, AnalysisStrings.MuteIssue_WarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
}
