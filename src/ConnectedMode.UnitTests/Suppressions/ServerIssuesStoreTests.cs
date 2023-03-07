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
using System.Linq;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions
{
    [TestClass]
    public class ServerIssuesStoreTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerIssuesStore, IServerIssuesStore>();
            MefTestHelpers.CheckTypeCanBeImported<ServerIssuesStore, IServerIssuesStoreWriter>();
        }

        [TestMethod]
        public void AddIssues_ListOfIssuesAsExpected()
        {
            var issue1 = CreateIssue("1", false);
            var issue2 = CreateIssue("2", false);
            var issue3 = CreateIssue("3", false);

            var testSubject = new ServerIssuesStore();

            var result = testSubject.Get();
            result.Count().Should().Be(0);

            testSubject.AddIssues(new List<SonarQubeIssue>() { issue1, issue2 }, clearAllExistingIssues: false);

            result = testSubject.Get();
            result.Count().Should().Be(2);
            result.Should().Contain(issue1);
            result.Should().Contain(issue2);

            testSubject.AddIssues(new List<SonarQubeIssue>() { issue3 }, clearAllExistingIssues: true);

            result = testSubject.Get();
            result.Count().Should().Be(1);
            result.Should().Contain(issue3);
        }

        [TestMethod]
        public void UpdateIssues_ListOfIssuesAsExpected()
        {
            var issue1 = CreateIssue("issue1", true);
            var issue2 = CreateIssue("issue2", false);

            var testSubject = new ServerIssuesStore();

            testSubject.AddIssues(new List<SonarQubeIssue>() { issue1, issue2 }, clearAllExistingIssues: false);

            var result = testSubject.Get();
            result.Count().Should().Be(2);
            result.Should().Contain(issue1);
            result.Should().Contain(issue2);
            issue1.IsResolved.Should().BeTrue();
            issue2.IsResolved.Should().BeFalse();

            testSubject.UpdateIssue("issue1", false);
            issue1.IsResolved.Should().BeFalse();

            testSubject.UpdateIssue("issue2", true);
            issue2.IsResolved.Should().BeTrue();

            // This test is to insure it doesn't just flip the state.
            testSubject.UpdateIssue("issue2", true);
            issue2.IsResolved.Should().BeTrue();
        }

        [TestMethod]
        public void AddOrUpdateIssues_EventIsInvoked()
        {
            var issue1 = CreateIssue("issue1", true);

            var testSubject = new ServerIssuesStore();
            var eventMock = new Mock<EventHandler>();
            testSubject.ServerIssuesChanged += eventMock.Object;

            testSubject.AddIssues(new List<SonarQubeIssue>() { issue1 }, clearAllExistingIssues: false);
            eventMock.Verify(x => x(testSubject, EventArgs.Empty), Times.Once);

            testSubject.UpdateIssue("issue1", false);
            eventMock.Verify(x => x(testSubject, EventArgs.Empty), Times.Exactly(2));
        }

        private static SonarQubeIssue CreateIssue(string key, bool isResolved)
        {
            var issue = new SonarQubeIssue(key, "", "", "", "", "", isResolved, SonarQubeIssueSeverity.Info, DateTimeOffset.MinValue, DateTimeOffset.MinValue, null, null);

            return issue;
        }
    }
}
