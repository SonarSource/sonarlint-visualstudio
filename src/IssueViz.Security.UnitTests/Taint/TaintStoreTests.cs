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

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint;

[TestClass]
public class TaintStoreTests
{
    private ITaintStore testSubject;

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        var batch = new CompositionBatch();

        var storeImport = new SingleObjectImporter<ITaintStore>();
        var issuesStoreImport = new SingleObjectImporter<IIssuesStore>();
        batch.AddPart(storeImport);
        batch.AddPart(issuesStoreImport);

        var catalog = new TypeCatalog(typeof(TaintStore));
        using var container = new CompositionContainer(catalog);
        container.Compose(batch);

        storeImport.Import.Should().NotBeNull();
        issuesStoreImport.Import.Should().NotBeNull();

        storeImport.Import.Should().BeSameAs(issuesStoreImport.Import);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new TaintStore();
    }

    [TestMethod]
    public void GetAll_ReturnsImmutableInstance()
    {
        var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
        testSubject.Set(oldItems, "config scope");

        var issuesList1 = testSubject.GetAll();
        testSubject.Update(new TaintVulnerabilityUpdate("config scope", [SetupIssueViz()], [], []));
        var issuesList2 = testSubject.GetAll();

        issuesList1.Count.Should().Be(2);
        issuesList2.Count.Should().Be(3);
    }

    [TestMethod]
    public void Set_NullCollection_ArgumentNullException()
    {
        Action act = () => testSubject.Set(null, "any");

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualizations");
    }

    [TestMethod]
    public void Set_NullConfigScope_ArgumentNullException()
    {
        Action act = () => testSubject.Set([], null);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("newConfigurationScope");
    }

    [TestMethod]
    public void Set_NoSubscribersToIssuesChangedEvent_NoException()
    {
        Action act = () => testSubject.Set(new[] { SetupIssueViz() }, "some config scope");

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Set_NoPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
    {
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        testSubject.Set([], "some config scope");

        testSubject.GetAll().Should().BeEmpty();
        eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
    }

    [TestMethod]
    public void Reset_NoPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
    {
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        testSubject.Reset();

        testSubject.GetAll().Should().BeEmpty();
        eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
    }

    [TestMethod]
    public void Set_NoPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
    {
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        var newItems = new[] { SetupIssueViz(), SetupIssueViz() };
        testSubject.Set(newItems, "some config scope");

        testSubject.GetAll().Should().BeEquivalentTo(newItems);
        eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
        var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
        eventArgs.RemovedIssues.Should().BeEmpty();
        eventArgs.AddedIssues.Should().BeEquivalentTo(newItems);
    }

    [TestMethod]
    public void Set_HasPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
    {
        var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
        testSubject.Set(oldItems, "some config scope");

        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        testSubject.Set([], "some config scope");

        testSubject.GetAll().Should().BeEmpty();
        eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
        var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
        eventArgs.RemovedIssues.Should().BeEquivalentTo(oldItems);
        eventArgs.AddedIssues.Should().BeEmpty();
    }

    [TestMethod]
    public void Set_HasPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
    {
        var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
        testSubject.Set(oldItems, "some config scope");

        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        var newItems = new[] { SetupIssueViz(), SetupIssueViz() };
        testSubject.Set(newItems, "some config scope");

        testSubject.GetAll().Should().BeEquivalentTo(newItems);
        eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
        var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
        eventArgs.RemovedIssues.Should().BeEquivalentTo(oldItems);
        eventArgs.AddedIssues.Should().BeEquivalentTo(newItems);
    }

    [TestMethod]
    public void Set_HasPreviousItems_HasSomeNewItems_CollectionChangedAndEventRaised()
    {
        var issueViz1 = SetupIssueViz();
        var issueViz2Id = Guid.NewGuid();
        var issueViz2 = SetupIssueViz(issueViz2Id);
        var issueViz2NewObject = SetupIssueViz(issueViz2Id);
        var issueViz3 = SetupIssueViz();

        var oldItems = new[] { issueViz1, issueViz2 };
        testSubject.Set(oldItems, "some config scope");

        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        var newItems = new[] { issueViz2NewObject, issueViz3};
        testSubject.Set(newItems, "some config scope");

        testSubject.GetAll().Should().BeEquivalentTo(newItems);
        eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
        var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
        eventArgs.RemovedIssues.Should().BeEquivalentTo([issueViz1, issueViz2]);
        eventArgs.AddedIssues.Should().BeEquivalentTo([issueViz2NewObject, issueViz3]);
    }

    [TestMethod]
    public void Set_HasItems_NoConfigScope_Throws()
    {
        var issueViz1 = SetupIssueViz();

        var act = () => testSubject.Set([issueViz1], null);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("newConfigurationScope");
    }

    [TestMethod]
    public void ConfigScope_NoInformation_ReturnsNull()
    {
        testSubject.Reset();

        var result = testSubject.ConfigurationScope;
        result.Should().BeNull();
    }

    [TestMethod]
    public void ConfigScope_HasInformation_ReturnsInformation()
    {
        const string newConfigurationScope = "some config scope";

        testSubject.Set([], newConfigurationScope);

        var result = testSubject.ConfigurationScope;
        result.Should().BeSameAs(newConfigurationScope);
    }

    private IAnalysisIssueVisualization SetupIssueViz(Guid? id = null)
    {
        id ??= Guid.NewGuid();

        var issueViz = Substitute.For<IAnalysisIssueVisualization>();
        issueViz.IssueId.Returns(id.Value);

        return issueViz;
    }
}
