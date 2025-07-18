/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
        testSubject.Set([], "test-scope2");
        testSubject.Set([], "test-scope3");

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
    public void Remove_ItemExists_RemovesItemAndRaisesEvent()
    {
        var risk1 = CreateDependencyRisk();
        var risk2 = CreateDependencyRisk();
        testSubject.Set([risk1, risk2], "test-scope");

        eventHandler.ClearReceivedCalls();
        testSubject.Remove(risk1);

        var storedRisks = testSubject.GetAll();
        storedRisks.Should().BeEquivalentTo(risk2);
        eventHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
    }

    [TestMethod]
    public void Remove_ItemDoesNotExist_DoesNothing()
    {
        var risk1 = CreateDependencyRisk();
        var risk2 = CreateDependencyRisk();
        testSubject.Set([risk1, risk2], "test-scope");
        var nonExistentRisk = CreateDependencyRisk();

        eventHandler.ClearReceivedCalls();
        testSubject.Remove(nonExistentRisk);

        testSubject.GetAll().Should().BeEquivalentTo(risk1, risk2);
        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
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
        var result2= testSubject.GetAll();
        result1.Should().NotBeSameAs(result2);

        var risk3 = CreateDependencyRisk();
        testSubject.Set([risk3], "updated-scope");

        result1.Should().BeEquivalentTo(originalRisks);
        testSubject.GetAll().Should().BeEquivalentTo(risk3);
    }

    private static IDependencyRisk CreateDependencyRisk()
    {
        var risk = Substitute.For<IDependencyRisk>();
        return risk;
    }
}
