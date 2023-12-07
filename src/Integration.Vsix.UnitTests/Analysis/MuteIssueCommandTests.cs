/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FluentAssertions;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.ConnectedMode;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Core.Transition;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis;

[TestClass]
public class MuteIssueCommandTests
{
    private const int VisibleAndEnabled = (int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
    private const int VisibleButDisabled = (int)(OLECMDF.OLECMDF_SUPPORTED);
    private const int InvisibleAndDisabled = (int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE);

    private static readonly BindingConfiguration ConnectedModeBinding =
        new BindingConfiguration(default, SonarLintMode.Connected, default);

    [TestMethod]
    public void CommandRegistration()
    {
        var testSubject = CreateTestSubject(out _, out _, out _, out _, out _, out _, out _);

        testSubject.CommandID.ID.Should().Be(MuteIssueCommand.CommandId);
        testSubject.CommandID.Guid.Should().Be(MuteIssueCommand.CommandSet);
    }

    [TestMethod]
    public void QueryStatus_NotSonarIssue_Invisible()
    {
        var testSubject = CreateTestSubject(out var errorListHelperMock, out _, out _, out var activeSolutionBoundTrackerMock, out _, out _, out _);
        var rule = It.IsAny<SonarCompositeRuleId>();
        errorListHelperMock.Setup(x => x.TryGetRuleIdFromSelectedRow(out rule)).Returns(false);
        activeSolutionBoundTrackerMock
            .SetupGet(x => x.CurrentConfiguration)
            .Returns(ConnectedModeBinding);

        ThreadHelper.SetCurrentThreadAsUIThread();
        var oleStatus = testSubject.OleStatus;

        oleStatus.Should().Be(InvisibleAndDisabled);
        testSubject.Visible.Should().BeFalse();
        testSubject.Enabled.Should().BeFalse();
    }

    [TestMethod]
    public void QueryStatus_NotSupportedIssue_Invisible()
    {
        var testSubject = CreateTestSubject(out var errorListHelperMock, out _, out _, out var activeSolutionBoundTrackerMock, out _, out _, out _);
        SonarCompositeRuleId.TryParse("csharpsquid:S333", out SonarCompositeRuleId ruleId);
        errorListHelperMock.Setup(x => x.TryGetRuleIdFromSelectedRow(out ruleId)).Returns(true);
        activeSolutionBoundTrackerMock
            .SetupGet(x => x.CurrentConfiguration)
            .Returns(ConnectedModeBinding);

        ThreadHelper.SetCurrentThreadAsUIThread();
        var oleStatus = testSubject.OleStatus;

        oleStatus.Should().Be(InvisibleAndDisabled);
        testSubject.Visible.Should().BeFalse();
        testSubject.Enabled.Should().BeFalse();
    }

    [TestMethod]
    public void QueryStatus_NotInConnectedMode_VisibleButDisabled()
    {
        var testSubject = CreateTestSubject(out var errorListHelperMock, out _, out _, out var activeSolutionBoundTrackerMock, out _, out _, out _);
        SonarCompositeRuleId.TryParse("javascript:S333", out SonarCompositeRuleId ruleId);
        errorListHelperMock.Setup(x => x.TryGetRuleIdFromSelectedRow(out ruleId)).Returns(true);
        activeSolutionBoundTrackerMock
            .SetupGet(x => x.CurrentConfiguration)
            .Returns(BindingConfiguration.Standalone);

        ThreadHelper.SetCurrentThreadAsUIThread();
        var oleStatus = testSubject.OleStatus;

        oleStatus.Should().Be(VisibleButDisabled);
        testSubject.Visible.Should().BeTrue();
        testSubject.Enabled.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("cpp:S111")]
    [DataRow("c:S222")]
    [DataRow("javascript:S333")]
    [DataRow("typescript:S444")]
    [DataRow("secrets:S555")]
    [DataRow("css:S555")]
    public void QueryStatus_ConnectedModeAndSupportedIssue_VisibleAndEnabled(string errorCode)
    {
        var testSubject = CreateTestSubject(out var errorListHelperMock, out _, out _, out var activeSolutionBoundTrackerMock, out _, out _, out _);
        SonarCompositeRuleId.TryParse(errorCode, out SonarCompositeRuleId ruleId);
        errorListHelperMock.Setup(x => x.TryGetRuleIdFromSelectedRow(out ruleId)).Returns(true);
        activeSolutionBoundTrackerMock
            .SetupGet(x => x.CurrentConfiguration)
            .Returns(ConnectedModeBinding);

        ThreadHelper.SetCurrentThreadAsUIThread();
        var oleStatus = testSubject.OleStatus;

        oleStatus.Should().Be(VisibleAndEnabled);
        testSubject.Visible.Should().BeTrue();
        testSubject.Enabled.Should().BeTrue();
    }

    [TestMethod]
    public void Execute_DoesNothingWhenIssueCannotBeSelected()
    {
        // todo roslyn issues should be checked in this case, but it's not yet implemented
        var testSubject = CreateTestSubject(out var errorListHelperMock, out _, out _, out _, out _, out _, out _);
        var issue = Mock.Of<IFilterableIssue>();
        errorListHelperMock.Setup(x => x.TryGetIssueFromSelectedRow(out issue)).Returns(false);

        testSubject.Invoke();

        errorListHelperMock.Verify(x => x.TryGetIssueFromSelectedRow(out issue), Times.Once);
    }

    [TestMethod]
    public void Execute_DoesNotFindServerIssue_ShowsMessageBox()
    {
        var callSequence = new MockSequence();
        var testSubject = CreateTestSubject(out var errorListHelperMock,
            out var serverIssueFinderMock,
            out _,
            out _,
            out var threadHandlingMock,
            out var messageBoxMock,
            out _);
        var issue = Mock.Of<IFilterableIssue>();
        errorListHelperMock
            .InSequence(callSequence)
            .Setup(x => x.TryGetIssueFromSelectedRow(out issue))
            .Returns(true);
        threadHandlingMock
            .InSequence(callSequence)
            .Setup(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> action) => action());
        serverIssueFinderMock
            .InSequence(callSequence)
            .Setup(x => x.FindServerIssueAsync(issue, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SonarQubeIssue)null);

        testSubject.Invoke();

        threadHandlingMock.Verify(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<bool>>>()), Times.Once);
        serverIssueFinderMock.Verify(x => x.FindServerIssueAsync(issue, It.IsAny<CancellationToken>()), Times.Once);
        messageBoxMock.Verify(x => x.Show(AnalysisStrings.MuteIssue_IssueNotFoundText, AnalysisStrings.MuteIssue_IssueNotFoundCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation));
    }

    [TestMethod]
    public void Execute_MutesIssue()
    {
        var callSequence = new MockSequence();
        var testSubject = CreateTestSubject(out var errorListHelperMock,
            out var serverIssueFinderMock,
            out var muteIssueServiceMock,
            out _,
            out var threadHandlingMock,
            out _,
            out _);
        var issue = Mock.Of<IFilterableIssue>();
        var sonarQubeIssue = CreateSonarQubeIssue();
        errorListHelperMock
            .InSequence(callSequence)
            .Setup(x => x.TryGetIssueFromSelectedRow(out issue))
            .Returns(true);
        threadHandlingMock
            .InSequence(callSequence)
            .Setup(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> action) => action());
        serverIssueFinderMock
            .InSequence(callSequence)
            .Setup(x => x.FindServerIssueAsync(issue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sonarQubeIssue);
        muteIssueServiceMock
            .InSequence(callSequence)
            .Setup(x => x.Mute(sonarQubeIssue.IssueKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        testSubject.Invoke();

        errorListHelperMock.Verify(x => x.TryGetIssueFromSelectedRow(out issue), Times.Once);
        threadHandlingMock.Verify(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<bool>>>()), Times.Once);
        serverIssueFinderMock.Verify(x => x.FindServerIssueAsync(issue, It.IsAny<CancellationToken>()), Times.Once);
        muteIssueServiceMock.Verify(x => x.Mute(sonarQubeIssue.IssueKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public void Execute_ExceptionCaught()
    {
        var callSequence = new MockSequence();
        var testSubject = CreateTestSubject(out var errorListHelperMock, out _, out _, out _, out _, out _, out var logger);
        var issue = Mock.Of<IFilterableIssue>();
        errorListHelperMock
            .InSequence(callSequence)
            .Setup(x => x.TryGetIssueFromSelectedRow(out issue))
            .Throws(new Exception("exception xxx"));

        Action act = () => testSubject.Invoke();

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists("exception xxx");
    }

    [TestMethod]
    public void Execute_CriticalExceptionNotCaught()
    {
        var callSequence = new MockSequence();
        var testSubject = CreateTestSubject(out var errorListHelperMock, out _, out _, out _, out _, out _, out var logger);
        var issue = Mock.Of<IFilterableIssue>();
        errorListHelperMock
            .InSequence(callSequence)
            .Setup(x => x.TryGetIssueFromSelectedRow(out issue))
            .Throws(new DivideByZeroException("exception xxx"));

        Action act = () => testSubject.Invoke();

        act.Should().ThrowExactly<DivideByZeroException>();
        logger.AssertPartialOutputStringDoesNotExist("exception xxx");
    }

    private static SonarQubeIssue CreateSonarQubeIssue()
    {
        return new SonarQubeIssue("issueKey", default, default, default, default, default, default, default, default, default, default, default);
    }

    private MenuCommand CreateTestSubject(out Mock<IErrorListHelper> errorListHelperMock,
        out Mock<IServerIssueFinder> serverIssueFinderMock,
        out Mock<IMuteIssuesService> muteIssueServiceMock,
        out Mock<IActiveSolutionBoundTracker> activeSolutionBoundTrackerMock,
        out Mock<IThreadHandling> threadHandlingMock,
        out Mock<IMessageBox> messeageBox,
        out TestLogger logger)
    {
        var dummyMenuService = new DummyMenuCommandService();
        new MuteIssueCommand(dummyMenuService,
            (errorListHelperMock = new Mock<IErrorListHelper>(MockBehavior.Strict)).Object,
            (serverIssueFinderMock = new Mock<IServerIssueFinder>(MockBehavior.Strict)).Object,
            (muteIssueServiceMock = new Mock<IMuteIssuesService>(MockBehavior.Strict)).Object,
            (activeSolutionBoundTrackerMock = new Mock<IActiveSolutionBoundTracker>(MockBehavior.Strict)).Object,
            (threadHandlingMock = new Mock<IThreadHandling>()).Object,
            (messeageBox = new Mock<IMessageBox>()).Object,
            logger = new TestLogger());

        dummyMenuService.AddedMenuCommands.Count.Should().Be(1);
        return dummyMenuService.AddedMenuCommands[0];
    }
}
