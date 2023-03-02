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

using System.Collections.Generic;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.IssueVisualization;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions
{
    [TestClass]
    public class ClientSuppressionSynchronizerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ClientSuppressionSynchronizer, IClientSuppressionSynchronizer>(
                MefTestHelpers.CreateExport<IClientIssueStore>(),
                MefTestHelpers.CreateExport<IIssuesFilter>());
        }

        [TestMethod]
        public void SynchronizeIssues_IssueGetsSuppressedOnServer_LocalIssueGetUpdated()
        {
            var issue = CreateIssue(isSuppressedLocally: false);

            var localIssues = new[] { issue };
            var clientIssueStore = CreateClientIssueStore(localIssues);

            var matches = new[] { issue };
            var issueFilter = CreateIssuesFilter(localIssues, matches);

            var testSubject = new ClientSuppressionSynchronizer(clientIssueStore, issueFilter);

            issue.IsSuppressed.Should().BeFalse();

            testSubject.SynchronizeSuppressedIssues();

            issue.IsSuppressed.Should().BeTrue();
        }

        [TestMethod]
        public void SynchronizeIssues_IssueGetsUnsuppressedOnServer_LocalIssueGetUpdated()
        {
            var issue = CreateIssue(isSuppressedLocally: true);

            var localIssues = new[] { issue };
            var clientIssueStore = CreateClientIssueStore(localIssues);
            var issueFilter = CreateIssuesFilter(localIssues, new List<IAnalysisIssueVisualization>());

            var testSubject = new ClientSuppressionSynchronizer(clientIssueStore, issueFilter);

            issue.IsSuppressed.Should().BeTrue();

            testSubject.SynchronizeSuppressedIssues();

            issue.IsSuppressed.Should().BeFalse();
        }

        [TestMethod]
        public void SynchronizeIssues_MultipleIssuesChanged_LocalIssuesGetUpdated()
        {
            var issue1 = CreateIssue(isSuppressedLocally: false);
            var issue2 = CreateIssue(isSuppressedLocally: true);
            var issue3 = CreateIssue(isSuppressedLocally: false);

            var localIssues = new[] { issue1, issue2, issue3 };
            var clientIssueStore = CreateClientIssueStore(localIssues);
            var matches = new[] { issue1 };
            var issueFilter = CreateIssuesFilter(localIssues, matches);

            var testSubject = new ClientSuppressionSynchronizer(clientIssueStore, issueFilter);

            issue1.IsSuppressed.Should().BeFalse();
            issue2.IsSuppressed.Should().BeTrue();
            issue3.IsSuppressed.Should().BeFalse();

            testSubject.SynchronizeSuppressedIssues();

            issue1.IsSuppressed.Should().BeTrue();
            issue2.IsSuppressed.Should().BeFalse();
            issue3.IsSuppressed.Should().BeFalse();
        }

        private static IClientIssueStore CreateClientIssueStore(IAnalysisIssueVisualization[] localIssues)
        {
            var clientIssueStore = new Mock<IClientIssueStore>();

            clientIssueStore.Setup(x => x.Get()).Returns(localIssues);

            return clientIssueStore.Object;
        }

        private static IIssuesFilter CreateIssuesFilter(IEnumerable<IAnalysisIssueVisualization> localIssues, IEnumerable<IFilterableIssue> matches = null)
        {
            var issueFilter = new Mock<IIssuesFilter>();
            issueFilter.Setup(x => x.GetMatches(localIssues)).Returns(matches);

            return issueFilter.Object;
        }

        private static IAnalysisIssueVisualization CreateIssue(bool isSuppressedLocally)
        {
            var issue = new Mock<IAnalysisIssueVisualization>();
            issue.SetupProperty(x => x.IsSuppressed);

            issue.Object.IsSuppressed = isSuppressedLocally;

            return issue.Object;
        }
    }
}
