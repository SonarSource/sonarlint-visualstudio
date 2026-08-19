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
using Microsoft.VisualStudio.Text.Editor;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions
{
    [TestClass]
    public class DeselectIssueVisualizationActionTests
    {
        private IIssueSelectionService selectionService;
        private ILightBulbBroker lightBulbBroker;
        private ITextView textView;
        private DeselectIssueVisualizationAction testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            var selectedIssue = CreateSelectedIssue();
            selectionService = Substitute.For<IIssueSelectionService>();
            selectionService.SelectedIssue.Returns(selectedIssue);
            lightBulbBroker = Substitute.For<ILightBulbBroker>();
            textView = Substitute.For<ITextView>();

            testSubject = new DeselectIssueVisualizationAction(selectionService, lightBulbBroker, textView);
        }

        [TestMethod]
        public void Invoke_IssueIsDeselected()
        {
            selectionService.DidNotReceive().SelectedIssue = Arg.Any<IAnalysisIssueVisualization>();

            testSubject.Invoke(CancellationToken.None);

            selectionService.Received(1).SelectedIssue = null;
        }

        [TestMethod]
        public void Invoke_DismissesLightBulbSession()
        {
            lightBulbBroker.DidNotReceiveWithAnyArgs().DismissSession(default);

            testSubject.Invoke(CancellationToken.None);

            lightBulbBroker.Received(1).DismissSession(textView);
        }

        [TestMethod]
        public void DisplayText_UsesCachedIssueRuleKey()
        {
            testSubject.DisplayText.Should().Contain("test rule id");

            selectionService.SelectedIssue.Returns((IAnalysisIssueVisualization)null);

            testSubject.DisplayText.Should().Contain("test rule id");
        }

        private static IAnalysisIssueVisualization CreateSelectedIssue(string ruleKey = "test rule id")
        {
            var issue = Substitute.For<IAnalysisIssueVisualization>();
            issue.SonarRuleId.Returns(new SonarCompositeRuleId("repo", ruleKey));
            return issue;
        }
    }
}
