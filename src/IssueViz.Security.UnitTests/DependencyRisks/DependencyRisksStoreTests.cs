/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class DependencyRisksStoreTests
{
    private DependencyRisksStore testSubject;
    private EventHandler eventHandler;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new DependencyRisksStore();
        eventHandler = Substitute.For<EventHandler>();
        testSubject.DependencyRisksChanged += eventHandler;
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<DependencyRisksStore, IDependencyRisksStore>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<DependencyRisksStore>();

    [TestMethod]
    public void GetAll_NoItems_ReturnsEmptyCollection()
    {
        var result = testSubject.GetAll();

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Set_SetsItems()
    {
        var risk1 = CreateDependencyRisk();
        var risk2 = CreateDependencyRisk();
        IDependencyRisk[] risks = [risk1, risk2];

        testSubject.Set(risks, "test-scope");

        var storedRisks = testSubject.GetAll();
        storedRisks.Should().BeEquivalentTo(risks);
        testSubject.CurrentConfigurationScope.Should().Be("test-scope");
        eventHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void Set_RaisesEvent()
    {
        testSubject.Set([], "test-scope1");
        testSubject.Set([], "test-scope1");
        testSubject.Set([], "test-scope2");

        eventHandler.Received(3).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void Set_OverwritesExistingItems()
    {
        var initialRisks = new[] { CreateDependencyRisk(), CreateDependencyRisk() };
        testSubject.Set(initialRisks, "initial-scope");
        testSubject.CurrentConfigurationScope.Should().Be("initial-scope");

        var newRisk = CreateDependencyRisk();

        testSubject.Set([newRisk], "new-scope");

        var storedRisks = testSubject.GetAll();
        storedRisks.Should().BeEquivalentTo(newRisk);
        testSubject.CurrentConfigurationScope.Should().Be("new-scope");
    }

    [TestMethod]
    public void Reset_HasItems_ClearsItemsAndRaisesEvent()
    {
        testSubject.Set([CreateDependencyRisk(), CreateDependencyRisk()], "test-scope");
        testSubject.CurrentConfigurationScope.Should().Be("test-scope");

        eventHandler.ClearReceivedCalls();
        testSubject.Reset();

        testSubject.GetAll().Should().BeEmpty();
        testSubject.CurrentConfigurationScope.Should().BeNull();
        eventHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void Reset_NoItems_DoesNotRaiseEvent()
    {
        testSubject.Reset();

        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
    }

    [TestMethod]
    public void GetAll_ReturnsCopyOfCollection()
    {
        var risk1 = CreateDependencyRisk();
        var risk2 = CreateDependencyRisk();
        IDependencyRisk[] originalRisks = [risk1, risk2];
        testSubject.Set(originalRisks, "original-scope");

        var result1 = testSubject.GetAll();
        var result2 = testSubject.GetAll();
        result1.Should().NotBeSameAs(result2);

        var risk3 = CreateDependencyRisk();
        testSubject.Set([risk3], "updated-scope");

        result1.Should().BeEquivalentTo(originalRisks);
        testSubject.GetAll().Should().BeEquivalentTo(risk3);
    }

    [TestMethod]
    public void Update_DifferentConfigurationScope_DoesNothing()
    {
        var risk1 = CreateDependencyRisk();
        var risk2 = CreateDependencyRisk();
        SetInitialState([risk1, risk2], "scope1");
        var update = new DependencyRisksUpdate("scope2", [], [], []);

        testSubject.Update(update);

        testSubject.GetAll().Should().BeEquivalentTo(risk1, risk2);
        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
    }

    [TestMethod]
    public void Update_AddRisks_AddsRisksAndRaisesEvent()
    {
        var risk1 = CreateDependencyRisk();
        SetInitialState([risk1], "scope1");
        var newRisk1 = CreateDependencyRisk();
        var newRisk2 = CreateDependencyRisk();
        var update = new DependencyRisksUpdate("scope1", [newRisk1, newRisk2], [], []);

        testSubject.Update(update);

        testSubject.GetAll().Should().BeEquivalentTo(risk1, newRisk1, newRisk2);
        eventHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void Update_UpdateRisks_UpdatesRisksAndRaisesEvent()
    {
        var matchingId = Guid.NewGuid();
        var risk1 = CreateDependencyRisk(matchingId);
        var risk2 = CreateDependencyRisk();
        SetInitialState([risk1, risk2], "scope1");
        var updatedRisk = CreateDependencyRisk(matchingId);
        var update = new DependencyRisksUpdate("scope1", [], [updatedRisk], []);

        testSubject.Update(update);

        testSubject.GetAll().Should().BeEquivalentTo(updatedRisk, risk2);
        testSubject.GetAll().Should().NotContain(risk1);
        eventHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void Update_CloseRisks_RemovesRisksAndRaisesEvent()
    {
        var risk1 = CreateDependencyRisk();
        var risk2 = CreateDependencyRisk();
        SetInitialState([risk1, risk2], "scope1");
        var update = new DependencyRisksUpdate("scope1", [], [], [risk1.Id]);

        testSubject.Update(update);

        testSubject.GetAll().Should().BeEquivalentTo(risk2);
        testSubject.GetAll().Should().NotContain(risk1);
        eventHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void Update_MultipleDifferentOperations_ProcessesAllAndRaisesEventOnce()
    {
        var updatedId =  Guid.NewGuid();
        var risk1 = CreateDependencyRisk();
        var risk2 = CreateDependencyRisk(updatedId);
        var risk3 = CreateDependencyRisk();
        SetInitialState([risk1, risk2, risk3], "scope1");
        var newRisk = CreateDependencyRisk();
        var updatedRisk = CreateDependencyRisk(updatedId);

        var update = new DependencyRisksUpdate("scope1", [newRisk], [updatedRisk], [risk3.Id]);

        testSubject.Update(update);

        testSubject.GetAll().Should().BeEquivalentTo(risk1, updatedRisk, newRisk);
        testSubject.GetAll().Should().NotContain(risk2);
        testSubject.GetAll().Should().NotContain(risk3);
        eventHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void Update_NoChanges_DoesNotRaiseEvent()
    {
        var risk1 = CreateDependencyRisk();
        SetInitialState([risk1], "scope1");

        var update = new DependencyRisksUpdate("scope1", [], [], []);

        testSubject.Update(update);

        testSubject.GetAll().Should().BeEquivalentTo(risk1);
        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
    }

    [TestMethod]
    public void Update_TryAddExistingRisk_IgnoresAndDoesNotRaiseEvent()
    {
        var risk1 = CreateDependencyRisk();
        SetInitialState([risk1], "scope1");
        var update = new DependencyRisksUpdate("scope1", [risk1], [], []);

        testSubject.Update(update);

        testSubject.GetAll().Should().BeEquivalentTo(risk1);
        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
    }

    [TestMethod]
    public void Update_TryUpdateNonExistentRisk_IgnoresAndDoesNotRaiseEvent()
    {
        var risk1 = CreateDependencyRisk();
        SetInitialState([risk1], "scope1");
        var nonExistentRisk = CreateDependencyRisk();
        var update = new DependencyRisksUpdate("scope1", [], [nonExistentRisk], []);

        testSubject.Update(update);

        testSubject.GetAll().Should().BeEquivalentTo(risk1);
        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
    }

    [TestMethod]
    public void Update_TryCloseNonExistentRisk_IgnoresAndDoesNotRaiseEvent()
    {
        var risk1 = CreateDependencyRisk();
        SetInitialState([risk1], "scope1");
        var nonExistentRiskId = Guid.NewGuid();
        var update = new DependencyRisksUpdate("scope1", [], [], [nonExistentRiskId]);

        testSubject.Update(update);

        testSubject.GetAll().Should().BeEquivalentTo(risk1);
        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
    }

    private static IDependencyRisk CreateDependencyRisk(Guid? matchingId = null)
    {
        var risk = Substitute.For<IDependencyRisk>();
        risk.Id.Returns(matchingId ?? Guid.NewGuid());
        return risk;
    }

    private void SetInitialState(IEnumerable<IDependencyRisk> risks, string configurationScope)
    {
        testSubject.Set(risks, configurationScope);
        eventHandler.ClearReceivedCalls();
    }
}
