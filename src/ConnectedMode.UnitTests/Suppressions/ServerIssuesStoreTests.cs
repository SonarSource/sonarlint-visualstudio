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

using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions;

[TestClass]
public class ServerIssuesStoreTests
{
    private readonly SonarQubeIssue resolvedIssue = CreateIssue("3", true);
    private readonly SonarQubeIssue resolvedIssue2 = CreateIssue("4", true);
    private readonly SonarQubeIssue unresolvedIssue = CreateIssue("1");
    private readonly SonarQubeIssue unresolvedIssue2 = CreateIssue("2");
    private ILogger logger;
    private ServerIssuesStore testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = new TestLogger();
        testSubject = new ServerIssuesStore(logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ServerIssuesStore, IServerIssuesStore>(
            MefTestHelpers.CreateExport<ILogger>());

        MefTestHelpers.CheckTypeCanBeImported<ServerIssuesStore, IServerIssuesStoreWriter>(
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_Check_SameInstanceExported() =>
        MefTestHelpers.CheckMultipleExportsReturnSameInstance<ServerIssuesStore, IServerIssuesStore, IServerIssuesStore>(
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void TryGetIssue_HasIssueWithSameKey_ReturnsTrue()
    {
        InitializeStoreWithIssue(unresolvedIssue);

        testSubject.TryGetIssue(unresolvedIssue.IssueKey, out var storedIssue).Should().BeTrue();

        storedIssue.Should().BeSameAs(unresolvedIssue);
    }

    [TestMethod]
    public void TryGetIssue_NoMatchingIssue_ReturnsFalse()
    {
        InitializeStoreWithIssue(unresolvedIssue);

        testSubject.TryGetIssue("NOTMATCHINGKEY", out _).Should().BeFalse();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AddIssues_EmptyList_ResultContainsNewIssues(bool clearAllExistingIssues)
    {
        VerifyNoIssues();

        testSubject.AddIssues([unresolvedIssue, unresolvedIssue2], clearAllExistingIssues);

        VerifyExpectedIssues(unresolvedIssue, unresolvedIssue2);
    }

    [TestMethod]
    public void AddIssues_NonEmptyList_ClearExisting_OldIssuesCleared()
    {
        VerifyNoIssues();

        testSubject.AddIssues([unresolvedIssue], false);
        VerifyExpectedIssues(unresolvedIssue);

        testSubject.AddIssues([unresolvedIssue2], true);
        VerifyExpectedIssues(unresolvedIssue2);
    }

    [TestMethod]
    public void AddIssues_ListOfAddIssues_NonEmptyList_DontClearExisting_OldIssuesAreRetained()
    {
        VerifyNoIssues();

        testSubject.AddIssues([unresolvedIssue], false);
        VerifyExpectedIssues(unresolvedIssue);

        testSubject.AddIssues([unresolvedIssue2], false);
        VerifyExpectedIssues(unresolvedIssue, unresolvedIssue2);
    }

    [TestMethod]
    public void AddIssues_ExistingIssues_NewIssuesUpdateExistingIssues()
    {
        var existing1 = CreateIssue("e1");
        var existing2 = CreateIssue("e2");
        // Set the initial list of items
        testSubject.AddIssues([existing1, existing2], true);
        VerifyExpectedIssues(existing1, existing2);

        // Modify the items in the store
        // e1 is unchanged
        // e2 is replaced
        // n1 is added
        // E1 is added - different case from "e1" so should be treated as different
        var mod1 = CreateIssue("e2");
        var mod2 = CreateIssue("e3");
        var new1 = CreateIssue("n1");
        var new2 = CreateIssue("E1");

        testSubject.AddIssues([mod1, mod2, new1, new2], false);

        VerifyExpectedIssues(existing1, mod1, mod2, new1, new2);
        testSubject.Get().Should().NotContain(existing2);
    }

    [TestMethod]
    public void AddIssues_AddNullIssues_EventIsNotInvoked()
    {
        var eventMock = Substitute.For<EventHandler>();
        testSubject.ServerIssuesChanged += eventMock;

        testSubject.AddIssues(null, false);

        VerifyNotRaisedServerIssueChanged(eventMock);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddIssues_EventIsInvoked(bool clearAllExistingIssues)
    {
        var eventMock = Substitute.For<EventHandler>();
        testSubject.ServerIssuesChanged += eventMock;

        testSubject.AddIssues([unresolvedIssue], clearAllExistingIssues);

        VerifyRaisedServerIssueChanged(eventMock);
    }

    [TestMethod]
    public void UpdateIssues_EmptyList_DoesNotInvokeEvent()
    {
        var eventMock = Substitute.For<EventHandler>();
        testSubject.ServerIssuesChanged += eventMock;
        VerifyNoIssues();

        testSubject.UpdateIssues(false, [resolvedIssue.IssueKey]);

        VerifyNotRaisedServerIssueChanged(eventMock);
    }

    [TestMethod]
    public void UpdateIssues_NonEmptyList_NoMatch_DoesNotInvokeEvent()
    {
        InitializeStoreWithIssue(unresolvedIssue);
        var eventMock = Substitute.For<EventHandler>();
        testSubject.ServerIssuesChanged += eventMock;

        testSubject.UpdateIssues(false, ["NOTMATCHINGKEY"]);

        VerifyNotRaisedServerIssueChanged(eventMock);
    }

    [TestMethod]
    public void UpdateIssues_NonEmptyList_Match_InvokesEventAndPropertyIsChanged()
    {
        InitializeStoreWithIssue(resolvedIssue, resolvedIssue2, unresolvedIssue);
        var eventMock = Substitute.For<EventHandler>();
        testSubject.ServerIssuesChanged += eventMock;

        testSubject.UpdateIssues(false, [resolvedIssue2.IssueKey, unresolvedIssue.IssueKey]);

        resolvedIssue.IsResolved.Should().BeTrue();
        resolvedIssue2.IsResolved.Should().BeFalse();
        unresolvedIssue.IsResolved.Should().BeFalse();

        // Changing a single property should be enough to trigger the event
        VerifyRaisedServerIssueChanged(eventMock);
    }

    [TestMethod]
    [DataRow(true, true)]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void UpdateIssues_IssueExists_InvokesEventIfPropertyDoesNotMatch(bool storeValue, bool newValue)
    {
        var issue1 = CreateIssue("issue1Key", storeValue);
        InitializeStoreWithIssue(issue1);
        var eventMock = Substitute.For<EventHandler>();
        testSubject.ServerIssuesChanged += eventMock;

        testSubject.UpdateIssues(newValue, [issue1.IssueKey]);

        issue1.IsResolved.Should().Be(newValue);
        var expectedEventCount = storeValue == newValue ? 0 : 1;
        eventMock.Received(expectedEventCount).Invoke(testSubject, Arg.Is<EventArgs>(x => x == EventArgs.Empty));
    }

    [TestMethod]
    public void Reset_HasIssue_AllIssuesRemoved()
    {
        InitializeStoreWithIssue(unresolvedIssue);

        testSubject.Reset();

        testSubject.Get().Should().BeEmpty();
    }

    [TestMethod]
    public void Reset_HasIssue_InvokesEvent()
    {
        InitializeStoreWithIssue(unresolvedIssue);
        var eventMock = Substitute.For<EventHandler>();
        testSubject.ServerIssuesChanged += eventMock;

        testSubject.Reset();

        VerifyRaisedServerIssueChanged(eventMock);
    }

    [TestMethod]
    public void Reset_NoIssue_DoesNotInvokeEvent()
    {
        var eventMock = Substitute.For<EventHandler>();
        testSubject.ServerIssuesChanged += eventMock;

        testSubject.Reset();

        VerifyNotRaisedServerIssueChanged(eventMock);
    }

    [TestMethod]
    public void Reset_CalledMultipleTimes_InvokesEventOnce()
    {
        InitializeStoreWithIssue(unresolvedIssue);
        var eventMock = Substitute.For<EventHandler>();
        testSubject.ServerIssuesChanged += eventMock;

        testSubject.Reset();
        testSubject.Reset();
        testSubject.Reset();

        VerifyRaisedServerIssueChanged(eventMock);
    }

    private static SonarQubeIssue CreateIssue(string key, bool isResolved = false)
    {
        var issue = new SonarQubeIssue(key, "", "", "", "", "", isResolved, SonarQubeIssueSeverity.Info, DateTimeOffset.MinValue, DateTimeOffset.MinValue, null, null);

        return issue;
    }

    private void InitializeStoreWithIssue(params SonarQubeIssue[] expectedIssues) => testSubject.AddIssues(expectedIssues, false);

    private void VerifyExpectedIssues(params SonarQubeIssue[] expectedIssues)
    {
        var result = testSubject.Get().ToList();
        result.Should().HaveCount(expectedIssues.Length);
        foreach (var expectedIssue in expectedIssues)
        {
            result.Should().Contain(expectedIssue);
        }
    }

    private void VerifyNoIssues()
    {
        var result = testSubject.Get();
        result.Should().BeEmpty();
    }

    private void VerifyNotRaisedServerIssueChanged(EventHandler eventMock) => eventMock.DidNotReceive().Invoke(testSubject, Arg.Any<EventArgs>());

    private void VerifyRaisedServerIssueChanged(EventHandler eventMock) => eventMock.Received(1).Invoke(testSubject, Arg.Is<EventArgs>(x => x == EventArgs.Empty));
}
