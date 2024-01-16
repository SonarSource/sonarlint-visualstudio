/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Selection
{
    [TestClass]
    public class IssueSelectionServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<IssueSelectionService, IIssueSelectionService>();
        }
      
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_IssueIsSet(bool isSetToNull)
        {
            var testSubject = CreateTestSubject();

            var issueViz = isSetToNull ? null : Mock.Of<IAnalysisIssueVisualization>();
            testSubject.SelectedIssue = issueViz;

            testSubject.SelectedIssue.Should().Be(issueViz);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_NoSubscribersToSelectionChanged_NoException(bool isSetToNull)
        {
            var testSubject = CreateTestSubject();

            var issueViz = isSetToNull ? null : Mock.Of<IAnalysisIssueVisualization>();

            Action act = () => testSubject.SelectedIssue = issueViz;
            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_SameValue_SubscribersNotNotified(bool isSetToNull)
        {
            var oldSelection = isSetToNull ? null : Mock.Of<IAnalysisIssueVisualization>();

            var testSubject = CreateTestSubject();
            
            testSubject.SelectedIssue = oldSelection;

            var callCount = 0;
            testSubject.SelectedIssueChanged += (sender, args) => { callCount++; };

            testSubject.SelectedIssue = oldSelection;

            callCount.Should().Be(0);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_DifferentValue_HasSubscribersToSelectionChanged_SubscribersNotified(bool isSetToNull)
        {
            var testSubject = CreateTestSubject();

            var oldSelection = isSetToNull ? Mock.Of<IAnalysisIssueVisualization>() : null;
            var newSelection = isSetToNull ? null : Mock.Of<IAnalysisIssueVisualization>();

            testSubject.SelectedIssue = oldSelection;

            var callCount = 0;
            testSubject.SelectedIssueChanged += (sender, args) => { callCount++; };
            
            testSubject.SelectedIssue = newSelection;

            callCount.Should().Be(1);
        }

        private IssueSelectionService CreateTestSubject()
        {
            return new IssueSelectionService();
        }
    }
}
