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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.Integration.UnitTests.ErrorList
{
    [TestClass]
    public class SonarErrorListDataSource_ClientIssueStoreTests
    {
        [TestMethod]
        public void GetIssues_ReturnsCorrectIssuesOneFactory()
        {
            var issue1 = Mock.Of<IAnalysisIssueVisualization>();
            var issue2 = Mock.Of<IAnalysisIssueVisualization>();
            var factory = SetUpFactoryWithIssues(issue1, issue2);

            var testSubject = CreateTestSubject();

            testSubject.GetIssues().Should().Equal();

            testSubject.AddFactory(factory);
            testSubject.GetIssues().Should().Equal(issue1, issue2);

            testSubject.RemoveFactory(factory);
            testSubject.GetIssues().Should().Equal();
        }

        [TestMethod]
        public void GetIssues_ReturnsCorrectIssuesTwoFactories()
        {
            var issue1 = Mock.Of<IAnalysisIssueVisualization>();
            var issue2 = Mock.Of<IAnalysisIssueVisualization>();
            var factory = SetUpFactoryWithIssues(issue1, issue2);

            var issue3 = Mock.Of<IAnalysisIssueVisualization>();
            var issue4 = Mock.Of<IAnalysisIssueVisualization>();
            var factory2 = SetUpFactoryWithIssues(issue3, issue4);

            var testSubject = CreateTestSubject();

            testSubject.GetIssues().Should().Equal();

            testSubject.AddFactory(factory);
            testSubject.GetIssues().Should().Equal(issue1, issue2);

            testSubject.AddFactory(factory2);
            testSubject.GetIssues().Should().Equal(issue1, issue2, issue3, issue4);

            testSubject.RemoveFactory(factory);
            testSubject.GetIssues().Should().Equal(issue3, issue4);
        }

        private SonarErrorListDataSource CreateTestSubject()
        {
            var tableManagerProvider = new Mock<ITableManagerProvider>();
            tableManagerProvider.Setup(x => x.GetTableManager(StandardTables.ErrorsTable)).Returns(Mock.Of<ITableManager>());

            return new SonarErrorListDataSource(tableManagerProvider.Object,
                Mock.Of<IFileRenamesEventSource>(),
                Mock.Of<IIssueSelectionService>());
        }

        private static IIssuesSnapshotFactory SetUpFactoryWithIssues(params IAnalysisIssueVisualization[] issues)
        {
            var snapshot = new Mock<IIssuesSnapshot>();
            snapshot.Setup(x => x.Issues).Returns(issues);

            var factory = new Mock<IIssuesSnapshotFactory>();
            factory.Setup(x => x.CurrentSnapshot).Returns(snapshot.Object);

            return factory.Object;
        }
    }
}
