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
using SonarLint.VisualStudio.ConnectedMode.Synchronization;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Synchronization
{
    [TestClass]
    public class ServerIssuesStoreTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerIssuesStore, IServerIssuesStore>(
                MefTestHelpers.CreateExport<ILogger>());

            MefTestHelpers.CheckTypeCanBeImported<ServerIssuesStore, IServerIssuesStoreWriter>(
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_Check_SameInstanceExported()
            => MefTestHelpers.CheckMultipleExportsReturnSameInstance<ServerIssuesStore, IServerIssuesStore, IServerIssuesStore>(
                MefTestHelpers.CreateExport<ILogger>());

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AddIssues_EmptyList_ResultContainsNewIssues(bool clearAllExistingIssues)
        {
            var issue1 = CreateIssue("1", false);
            var issue2 = CreateIssue("2", false);

            var testSubject = CreateTestSubject();

            var result = testSubject.Get();
            result.Count().Should().Be(0);

            testSubject.AddIssues(new List<SonarQubeIssue>() { issue1, issue2 }, clearAllExistingIssues: clearAllExistingIssues);

            result = testSubject.Get();
            result.Count().Should().Be(2);
            result.Should().Contain(issue1);
            result.Should().Contain(issue2);
        }

        [TestMethod]
        public void AddIssues_NonEmptyList_ClearExisting_OldIssuesCleared()
        {
            var issue1 = CreateIssue("1", false);
            var issue2 = CreateIssue("2", false);

            var testSubject = CreateTestSubject();

            var result = testSubject.Get();
            result.Count().Should().Be(0);

            testSubject.AddIssues(new List<SonarQubeIssue>() { issue1 }, clearAllExistingIssues: false);

            result = testSubject.Get();
            result.Count().Should().Be(1);
            result.Should().Contain(issue1);

            testSubject.AddIssues(new List<SonarQubeIssue>() { issue2 }, clearAllExistingIssues: true);

            result = testSubject.Get();
            result.Count().Should().Be(1);
            result.Should().Contain(issue2);
        }

        [TestMethod]
        public void AddIssues_ListOfAddIssues_NonEmptyList_DontClearExisting_OldIssuesAreRetained()
        {
            var issue1 = CreateIssue("1", false);
            var issue2 = CreateIssue("2", false);

            var testSubject = CreateTestSubject();

            var result = testSubject.Get();
            result.Count().Should().Be(0);

            testSubject.AddIssues(new List<SonarQubeIssue>() { issue1 }, clearAllExistingIssues: false);

            result = testSubject.Get();
            result.Count().Should().Be(1);
            result.Should().Contain(issue1);

            testSubject.AddIssues(new List<SonarQubeIssue>() { issue2 }, clearAllExistingIssues: false);

            result = testSubject.Get();
            result.Count().Should().Be(2);
            result.Should().Contain(issue1);
            result.Should().Contain(issue2);
        }

        [TestMethod]
        public void AddIssues_ExistingIssues_NewIssuesUpdateExistingIssues()
        {
            var testSubject = CreateTestSubject();

            var existing1 = CreateIssue("e1");
            var existing2 = CreateIssue("e2");

            // Set the initial list of items
            testSubject.AddIssues(new[] { existing1, existing2 }, true);
            var initialIssues = testSubject.Get();
            initialIssues.Should().HaveCount(2);
            initialIssues.Should().BeEquivalentTo(existing1, existing2);

            // Modify the items in the store
            // e1 is unchanged
            // e2 is replaced
            // n1 is added
            // E1 is added - different case from "e1" so should be treated as different
            var mod1 = CreateIssue("e2");
            var mod2 = CreateIssue("e3");
            var new1 = CreateIssue("n1");
            var new2 = CreateIssue("E1");

            testSubject.AddIssues(new[] { mod1, mod2, new1, new2 }, false);

            var actual = testSubject.Get();
            actual.Should().HaveCount(5);
            actual.Should().BeEquivalentTo(existing1, mod1, mod2, new1, new2);

            actual.Should().NotContain(existing2);
        }

        [TestMethod]
        public void AddIssues_AddNullIssues_EventIsNotInvoked()
        {
            var testSubject = CreateTestSubject();

            var eventMock = new Mock<EventHandler>();
            testSubject.ServerIssuesChanged += eventMock.Object;

            testSubject.AddIssues(null, false);

            eventMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void AddIssues_EventIsInvoked(bool clearAllExistingIssues)
        {
            var issue1 = CreateIssue("issue1", true);

            var testSubject = CreateTestSubject();
            var eventMock = new Mock<EventHandler>();
            testSubject.ServerIssuesChanged += eventMock.Object;

            testSubject.AddIssues(new List<SonarQubeIssue>() { issue1 }, clearAllExistingIssues);
            eventMock.Verify(x => x(testSubject, EventArgs.Empty), Times.Once);
        }

        [TestMethod]
        public void UpdateIssues_EmptyList_DoesNotInvokeEvent()
        {
            var testSubject = CreateTestSubject();

            var eventMock = new Mock<EventHandler>();
            testSubject.ServerIssuesChanged += eventMock.Object;

            var result = testSubject.Get();
            result.Count().Should().Be(0);
            testSubject.UpdateIssues(false, new[] { "issue1" });

            eventMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void UpdateIssues_NonEmptyList_NoMatch_DoesNotInvokeEvent()
        {
            var testSubject = CreateTestSubject();
            testSubject.AddIssues(new List<SonarQubeIssue>() { CreateIssue("issue1", true) }, clearAllExistingIssues: false);

            var eventMock = new Mock<EventHandler>();
            testSubject.ServerIssuesChanged += eventMock.Object;
            testSubject.UpdateIssues(false, new[] { "issue2" });

            eventMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void UpdateIssues_NonEmptyList_Match_InvokesEventAndPropertyIsChanged()
        {
            var issue1 = CreateIssue("issue1", true);
            var issue2 = CreateIssue("issue2", true); // this property should be changed
            var issue3 = CreateIssue("issue3", false);

            var testSubject = CreateTestSubject();
            testSubject.AddIssues(new List<SonarQubeIssue>() { issue1, issue2, issue3 }, clearAllExistingIssues: false);

            var eventMock = new Mock<EventHandler>();
            testSubject.ServerIssuesChanged += eventMock.Object;

            testSubject.UpdateIssues(false, new[] { "issue2", "issue3" });

            issue1.IsResolved.Should().BeTrue();
            issue2.IsResolved.Should().BeFalse();
            issue3.IsResolved.Should().BeFalse();

            // Changing a single property should be enough to trigger the event
            eventMock.Verify(x => x(testSubject, EventArgs.Empty), Times.Exactly(1));
        }

        [TestMethod]
        [DataRow(true, true)]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void UpdateIssues_IssueExists_InvokesEventIfPropertyDoesNotMatch(bool storeValue, bool newValue)
        {
            var issue1 = CreateIssue("issue1Key", storeValue);

            var testSubject = CreateTestSubject();
            testSubject.AddIssues(new List<SonarQubeIssue>() { issue1 }, clearAllExistingIssues: true);

            var eventMock = new Mock<EventHandler>();
            testSubject.ServerIssuesChanged += eventMock.Object;

            testSubject.UpdateIssues(newValue, new[] { "issue1Key" });

            issue1.IsResolved.Should().Be(newValue);

            var expectedEventCount = (storeValue == newValue) ? 0 : 1;
            eventMock.Verify(x => x(testSubject, EventArgs.Empty), Times.Exactly(expectedEventCount));
        }

        private static ServerIssuesStore CreateTestSubject(ILogger logger = null)
        {
            logger ??= new TestLogger(logToConsole: true);
            return new ServerIssuesStore(logger);
        }

        private static SonarQubeIssue CreateIssue(string key, bool isResolved = false)
        {
            var issue = new SonarQubeIssue(key, "", "", "", "", "", isResolved, SonarQubeIssueSeverity.Info, DateTimeOffset.MinValue, DateTimeOffset.MinValue, null, null, null);

            return issue;
        }
    }
}
