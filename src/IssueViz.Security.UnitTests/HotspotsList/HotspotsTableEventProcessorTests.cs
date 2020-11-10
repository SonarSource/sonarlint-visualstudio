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
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource.Events;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotsTableEventProcessorTests
    {
        [TestMethod]
        public void PreprocessNavigate_NoSelectedEntry_NoNavigation()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = CreateTestSubject(locationNavigator.Object);
            testSubject.PreprocessNavigate(null, new TableEntryNavigateEventArgs(false));

            VerifyNoNavigation(locationNavigator);
        }

        [TestMethod]
        public void PreprocessNavigate_SelectedEntryIsNotIssueViz_NoNavigation()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = CreateTestSubject(locationNavigator.Object);
            testSubject.PreprocessNavigate(Mock.Of<ITableEntryHandle>(), new TableEntryNavigateEventArgs(false));

            VerifyNoNavigation(locationNavigator);
        }

        [TestMethod]
        public void PreprocessNavigate_SelectedEntryIsIssueViz_NavigationToIssue()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var issueViz = Mock.Of<IAnalysisIssueVisualization>();
            var entry = new Mock<ITableEntryHandle>();
            entry.Setup(x => x.Identity).Returns(issueViz);

            var testSubject = CreateTestSubject(locationNavigator.Object);
            testSubject.PreprocessNavigate(entry.Object, new TableEntryNavigateEventArgs(false));

            VerifyNavigation(locationNavigator, issueViz);
        }

        private ITableControlEventProcessor CreateTestSubject(ILocationNavigator locationNavigator)
            => new HotspotsTableEventProcessor(locationNavigator);

        private void VerifyNoNavigation(Mock<ILocationNavigator> locationNavigator)
        {
            locationNavigator.Invocations.Count.Should().Be(0);
        }

        private static void VerifyNavigation(Mock<ILocationNavigator> locationNavigator, IAnalysisIssueLocationVisualization issueViz)
        {
            locationNavigator.Verify(x => x.TryNavigate(issueViz), Times.Once);
            locationNavigator.VerifyNoOtherCalls();
        }
    }
}
