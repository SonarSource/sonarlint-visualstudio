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

using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl
{
    [TestClass]
    public class IssueVisualizationViewModelTests
    {
        private const AnalysisIssueSeverity DefaultNullIssueSeverity = AnalysisIssueSeverity.Info;

        private Mock<IAnalysisIssueSelectionService> selectionEventsMock;
        private Mock<IVsImageService2> imageServiceMock;
        private Mock<IRuleHelpLinkProvider> helpLinkProviderMock;

        private IssueVisualizationViewModel testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            selectionEventsMock = new Mock<IAnalysisIssueSelectionService>();
            imageServiceMock = new Mock<IVsImageService2>();
            helpLinkProviderMock = new Mock<IRuleHelpLinkProvider>();
            testSubject = new IssueVisualizationViewModel(selectionEventsMock.Object, imageServiceMock.Object, helpLinkProviderMock.Object);
        }

        [TestMethod]
        public void Description_NoCurrentIssueVisualization_Null()
        {
            testSubject.CurrentIssue = null;

            testSubject.Description.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void Description_CurrentIssueVisualizationHasNoAnalysisIssue_Null()
        {
            testSubject.CurrentIssue = new AnalysisIssueVisualization(null, null);

            testSubject.Description.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void Description_CurrentIssueVisualizationHasAnalysisIssue_IssueMessage()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.Message).Returns("test message");

            testSubject.CurrentIssue = new AnalysisIssueVisualization(null, issue.Object);

            testSubject.Description.Should().Be("test message");
        }

        [TestMethod]
        public void RuleKey_NoCurrentIssueVisualization_Null()
        {
            testSubject.CurrentIssue = null;

            testSubject.RuleKey.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleKey_CurrentIssueVisualizationHasNoAnalysisIssue_Null()
        {
            testSubject.CurrentIssue = new AnalysisIssueVisualization(null, null);

            testSubject.RuleKey.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleKey_CurrentIssueVisualizationHasAnalysisIssue_IssueRuleKey()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.RuleKey).Returns("test RuleKey");

            testSubject.CurrentIssue = new AnalysisIssueVisualization(null, issue.Object);

            testSubject.RuleKey.Should().Be("test RuleKey");
        }

        [TestMethod]
        public void RuleHelpLink_NoCurrentIssueVisualization_Null()
        {
            testSubject.CurrentIssue = null;

            testSubject.RuleHelpLink.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleHelpLink_CurrentIssueVisualizationHasNoAnalysisIssue_Null()
        {
            testSubject.CurrentIssue = new AnalysisIssueVisualization(null, null);

            testSubject.RuleHelpLink.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleHelpLink_CurrentIssueVisualizationHasAnalysisIssue_RuleHelpLinkFromLinkProvider()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.RuleKey).Returns("test RuleKey");

            helpLinkProviderMock.Setup(x => x.GetHelpLink("test RuleKey")).Returns("test link");

            testSubject.CurrentIssue = new AnalysisIssueVisualization(null, issue.Object);

            testSubject.RuleHelpLink.Should().Be("test link");
        }

        [TestMethod]
        public void Severity_NoCurrentIssueVisualization_DefaultSeverity()
        {
            testSubject.CurrentIssue = null;

            testSubject.Severity.Should().Be(DefaultNullIssueSeverity);
        }

        [TestMethod]
        public void Severity_CurrentIssueVisualizationHasNoAnalysisIssue_DefaultSeverity()
        {
            testSubject.CurrentIssue = new AnalysisIssueVisualization(null, null);

            testSubject.Severity.Should().Be(DefaultNullIssueSeverity);
        }

        [TestMethod]
        public void Severity_CurrentIssueVisualizationHasAnalysisIssue_IssueSeverity()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.Severity).Returns(AnalysisIssueSeverity.Blocker);

            testSubject.CurrentIssue = new AnalysisIssueVisualization(null, issue.Object);

            testSubject.Severity.Should().Be(AnalysisIssueSeverity.Blocker);
        }
    }
}
