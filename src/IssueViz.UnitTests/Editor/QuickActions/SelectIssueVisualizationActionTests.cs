/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Commands;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions
{
    [TestClass]
    public class SelectIssueVisualizationActionTests
    {
        [TestMethod]
        public void Invoke_IssueIsSelected()
        {
            var selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            selectionServiceMock.SetupSet(x => x.SelectedIssue = null);

            var expectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            var testSubject = new SelectIssueVisualizationAction(Mock.Of<IVsUIShell>(), selectionServiceMock.Object, expectedIssue);

            selectionServiceMock.VerifySet(x => x.SelectedIssue = It.IsAny<IAnalysisIssueVisualization>(), Times.Never());

            testSubject.Invoke(CancellationToken.None);

            selectionServiceMock.VerifySet(x => x.SelectedIssue = expectedIssue, Times.Once());
        }

        [TestMethod]
        public void Invoke_IssueVisualizationToolWindowOpened()
        {
            var vsUiShell = new Mock<IVsUIShell>();
            var testSubject = new SelectIssueVisualizationAction(vsUiShell.Object, Mock.Of<IAnalysisIssueSelectionService>(), Mock.Of<IAnalysisIssueVisualization>());

            vsUiShell.VerifyNoOtherCalls();

            testSubject.Invoke(CancellationToken.None);

            object inputArgs = 0;
            var guid = IssueVisualizationToolWindowCommand.CommandSet;

            vsUiShell.Verify(x => x.PostExecCommand(
                    ref guid,
                    IssueVisualizationToolWindowCommand.ViewToolWindowCommandId,
                    0,
                    ref inputArgs),
                Times.Once);
        }

        [TestMethod]
        public void DisplayText_UsesIssueRuleKey()
        {
            var selectedIssueMock = new Mock<IAnalysisIssueVisualization>();
            selectedIssueMock.Setup(x => x.RuleId).Returns("test rule id");

            var testSubject = new SelectIssueVisualizationAction(Mock.Of<IVsUIShell>(), Mock.Of<IAnalysisIssueSelectionService>(), selectedIssueMock.Object);
            testSubject.DisplayText.Should().StartWith("test rule id");
        }
    }
}
