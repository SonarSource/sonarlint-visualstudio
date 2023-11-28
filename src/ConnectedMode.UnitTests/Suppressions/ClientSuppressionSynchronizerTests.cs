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
using System.Collections.Generic;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
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
                MefTestHelpers.CreateExport<IIssueLocationStoreAggregator>(),
                MefTestHelpers.CreateExport<ISuppressedIssueMatcher>());
        }

        [TestMethod]
        public void SynchronizeIssues_IssueGetsSuppressedOnServer_LocalIssueGetUpdated()
        {
            var issue = CreateIssue(isSuppressedLocally: false);

            var localIssues = new[] { issue };
            var clientIssueStore = CreateIssueStore(localIssues);
            var suppressedIssueMatcher = CreateSuppressedIssueMatcher(issue);
            
            var testSubject = new ClientSuppressionSynchronizer(clientIssueStore, suppressedIssueMatcher);

            issue.IsSuppressed.Should().BeFalse();

            testSubject.SynchronizeSuppressedIssues();

            issue.IsSuppressed.Should().BeTrue();
        }

        [TestMethod]
        public void SynchronizeIssues_IssueGetsUnsuppressedOnServer_LocalIssueGetUpdated()
        {
            var issue = CreateIssue(isSuppressedLocally: true);

            var localIssues = new[] { issue };
            var issueStore = CreateIssueStore(localIssues);
            var suppressedIssueMatcher = CreateSuppressedIssueMatcher();

            var testSubject = new ClientSuppressionSynchronizer(issueStore, suppressedIssueMatcher);

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
            var issueStore = CreateIssueStore(localIssues);
            var suppressedIssueMatcher = CreateSuppressedIssueMatcher(issue1);

            var testSubject = new ClientSuppressionSynchronizer(issueStore, suppressedIssueMatcher);

            issue1.IsSuppressed.Should().BeFalse();
            issue2.IsSuppressed.Should().BeTrue();
            issue3.IsSuppressed.Should().BeFalse();

            testSubject.SynchronizeSuppressedIssues();

            issue1.IsSuppressed.Should().BeTrue();
            issue2.IsSuppressed.Should().BeFalse();
            issue3.IsSuppressed.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(true, true)]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void Synchronize_EventRaisedIfSuppressionValueChanged(bool isSuppressedLocally, bool isSuppressedOnServer)
        {
            var localIssue = CreateIssue(isSuppressedLocally: isSuppressedLocally);
            var localIssues = new[] { localIssue };
            var clientIssueStore = CreateIssueStore(localIssues);

            // Issues in the matched list are treated as being suppressed
            var matches = new List<IAnalysisIssueVisualization>();
            if (isSuppressedOnServer)
            {
                matches.Add(localIssue);
            }
            var suppressedIssueMatcher = CreateSuppressedIssueMatcher(matches.ToArray());

            var testSubject = new ClientSuppressionSynchronizer(clientIssueStore, suppressedIssueMatcher);

            var eventMock = new Mock<EventHandler<LocalSuppressionsChangedEventArgs>>();
            testSubject.LocalSuppressionsChanged += eventMock.Object;

            testSubject.SynchronizeSuppressedIssues();

            // We only expect an event if the local and server values don't match
            var expectedEventCount = (isSuppressedLocally == isSuppressedOnServer) ? 0 : 1;
            eventMock.Verify(x => x(testSubject, It.IsAny<LocalSuppressionsChangedEventArgs>()), Times.Exactly(expectedEventCount));
        }

        [TestMethod]
        public void Synchronize_EventRaised_EventArgsContainsExpectedFiles()
        {
            var suppressedLocally_NotSuppressedOnServer = CreateIssue(isSuppressedLocally: true, filePath: "path 1");
            var suppressedLocally_SuppressedOnServer = CreateIssue(isSuppressedLocally: true, filePath: "path 2");
            var notSuppressedLocally_NotSuppressedOnServer = CreateIssue(isSuppressedLocally: false, filePath: "path 3");
            var notSuppressedLocally_SuppressedOnServer = CreateIssue(isSuppressedLocally: false, filePath: "path 4");

            var localIssues = new[]
            {
                suppressedLocally_NotSuppressedOnServer,
                suppressedLocally_SuppressedOnServer,
                notSuppressedLocally_NotSuppressedOnServer,
                notSuppressedLocally_SuppressedOnServer
            };
            var clientIssueStore = CreateIssueStore(localIssues);

            // Issues in the matched list are treated as being suppressed
            // => anything that should be treated as suppressed on the server should
            //    be in the list
            var matches = new List<IAnalysisIssueVisualization>
            {
                suppressedLocally_SuppressedOnServer,
                notSuppressedLocally_SuppressedOnServer
            };
            var suppressedIssueMatcher = CreateSuppressedIssueMatcher(matches.ToArray());

            var testSubject = new ClientSuppressionSynchronizer(clientIssueStore, suppressedIssueMatcher);

            var eventMock = new Mock<EventHandler<LocalSuppressionsChangedEventArgs>>();
            testSubject.LocalSuppressionsChanged += eventMock.Object;

            testSubject.SynchronizeSuppressedIssues();

            eventMock.Verify(x => x(testSubject, It.IsAny<LocalSuppressionsChangedEventArgs>()), Times.Once);

            eventMock.Invocations[0].Arguments[0].Should().BeSameAs(testSubject);
            var actualEventArgs = eventMock.Invocations[0].Arguments[1] as LocalSuppressionsChangedEventArgs;

            actualEventArgs.Should().NotBeNull();

            // We expect to see the paths where the local and server values don't match
            actualEventArgs.ChangedFiles.Should().BeEquivalentTo("path 1", "path 4");
        }

        private static IIssueLocationStoreAggregator CreateIssueStore(IEnumerable<IAnalysisIssueVisualization> localIssues)
        {
            var issuesStore = new Mock<IIssueLocationStoreAggregator>();

            issuesStore.Setup(x => x.GetIssues()).Returns(localIssues);

            return issuesStore.Object;
        }

        private static ISuppressedIssueMatcher CreateSuppressedIssueMatcher(params IFilterableIssue[] matches)
        {
            var suppressedIssuesMatcher = new Mock<ISuppressedIssueMatcher>();

            foreach (var match in matches)
            {
                suppressedIssuesMatcher.Setup(x => x.SuppressionExists(match)).Returns(true);
            }

            return suppressedIssuesMatcher.Object;
        }

        private static IAnalysisIssueVisualization CreateIssue(bool isSuppressedLocally, string filePath = "any")
        {
            var issue = new Mock<IAnalysisIssueVisualization>();
            issue.SetupProperty(x => x.IsSuppressed);
            issue.Setup(x => x.CurrentFilePath).Returns(filePath);

            issue.Object.IsSuppressed = isSuppressedLocally;

            return issue.Object;
        }
    }
}
