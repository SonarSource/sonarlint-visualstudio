/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using Constants = SonarLint.VisualStudio.IssueVisualization.Commands.Constants;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions
{
    [TestClass]
    public class SelectIssueVisualizationActionTests
    {
        private IVsUIShell vsUiShell;
        private IIssueSelectionService selectionService;
        private IAnalysisIssueVisualization issue;
        private ILightBulbBroker lightBulbBroker;
        private ITextView textView;
        private SelectIssueVisualizationAction testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            vsUiShell = Substitute.For<IVsUIShell>();
            selectionService = Substitute.For<IIssueSelectionService>();
            issue = Substitute.For<IAnalysisIssueVisualization>();
            lightBulbBroker = Substitute.For<ILightBulbBroker>();
            textView = Substitute.For<ITextView>();

            testSubject = new SelectIssueVisualizationAction(vsUiShell, selectionService, issue, lightBulbBroker, textView);
        }

        [TestMethod]
        public void Invoke_IssueIsSelected()
        {
            selectionService.DidNotReceive().SelectedIssue = Arg.Any<IAnalysisIssueVisualization>();

            testSubject.Invoke(CancellationToken.None);

            selectionService.Received(1).SelectedIssue = issue;
        }

        [TestMethod]
        public void Invoke_IssueVisualizationToolWindowOpened()
        {
            testSubject.Invoke(CancellationToken.None);

            var guid = Constants.CommandSetGuid;
            object inputArgs = 0;
            vsUiShell.Received(1).PostExecCommand(ref guid, Constants.ViewToolWindowCommandId, 0, ref inputArgs);
        }

        [TestMethod]
        public void Invoke_DismissesLightBulbSession()
        {
            lightBulbBroker.DidNotReceiveWithAnyArgs().DismissSession(default);

            testSubject.Invoke(CancellationToken.None);

            lightBulbBroker.Received(1).DismissSession(textView);
        }

        [TestMethod]
        public void DisplayText_UsesIssueRuleKey()
        {
            issue.SonarRuleId.Returns(new SonarCompositeRuleId("repo", "test rule id"));

            testSubject.DisplayText.Should().Contain("test rule id");
        }
    }
}
