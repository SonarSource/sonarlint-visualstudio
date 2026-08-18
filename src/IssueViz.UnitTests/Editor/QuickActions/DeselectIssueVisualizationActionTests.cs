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
using FluentAssertions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions
{
    [TestClass]
    public class DeselectIssueVisualizationActionTests
    {
        [TestMethod]
        public void Invoke_IssueIsDeselected()
        {
            var selectedIssueMock = new Mock<IAnalysisIssueVisualization>();
            selectedIssueMock.Setup(x => x.RuleId).Returns("test rule id");

            var selectionServiceMock = new Mock<IIssueSelectionService>();
            selectionServiceMock.SetupGet(x => x.SelectedIssue).Returns(selectedIssueMock.Object);
            selectionServiceMock.SetupSet(x => x.SelectedIssue = null);

            var testSubject = new DeselectIssueVisualizationAction(selectionServiceMock.Object, Mock.Of<ILightBulbBroker>(), Mock.Of<ITextView>());

            selectionServiceMock.VerifySet(x=> x.SelectedIssue = It.IsAny<IAnalysisIssueVisualization>(), Times.Never());

            testSubject.Invoke(CancellationToken.None);

            selectionServiceMock.VerifySet(x => x.SelectedIssue = null, Times.Once());
        }

        [TestMethod]
        public void Invoke_DismissesLightBulbSession()
        {
            var selectedIssueMock = new Mock<IAnalysisIssueVisualization>();
            selectedIssueMock.Setup(x => x.RuleId).Returns("test rule id");

            var selectionServiceMock = new Mock<IIssueSelectionService>();
            selectionServiceMock.SetupGet(x => x.SelectedIssue).Returns(selectedIssueMock.Object);

            var lightBulbBroker = new Mock<ILightBulbBroker>();
            var textView = Mock.Of<ITextView>();

            var testSubject = new DeselectIssueVisualizationAction(selectionServiceMock.Object, lightBulbBroker.Object, textView);

            lightBulbBroker.VerifyNoOtherCalls();

            testSubject.Invoke(CancellationToken.None);

            lightBulbBroker.Verify(x => x.DismissSession(textView), Times.Once);
        }

        [TestMethod]
        public void DisplayText_UsesCachedIssueRuleKey()
        {
            var selectedIssueMock = new Mock<IAnalysisIssueVisualization>();
            selectedIssueMock.Setup(x => x.RuleId).Returns("test rule id");

            var selectionServiceMock = new Mock<IIssueSelectionService>();
            selectionServiceMock.SetupGet(x => x.SelectedIssue).Returns(selectedIssueMock.Object);

            var testSubject = new DeselectIssueVisualizationAction(selectionServiceMock.Object, Mock.Of<ILightBulbBroker>(), Mock.Of<ITextView>());
            testSubject.DisplayText.Should().Contain("test rule id");

            selectionServiceMock.Reset();
            selectionServiceMock.SetupGet(x => x.SelectedIssue).Returns((IAnalysisIssueVisualization) null);

            testSubject.DisplayText.Should().Contain("test rule id");
        }
    }
}
