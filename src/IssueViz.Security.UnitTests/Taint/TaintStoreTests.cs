// /*
//  * SonarLint for Visual Studio
//  * Copyright (C) 2016-2024 SonarSource SA
//  * mailto:info AT sonarsource DOT com
//  *
//  * This program is free software; you can redistribute it and/or
//  * modify it under the terms of the GNU Lesser General Public
//  * License as published by the Free Software Foundation; either
//  * version 3 of the License, or (at your option) any later version.
//  *
//  * This program is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  * Lesser General Public License for more details.
//  *
//  * You should have received a copy of the GNU Lesser General Public License
//  * along with this program; if not, write to the Free Software Foundation,
//  * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//  */
//
// using System.ComponentModel.Composition;
// using System.ComponentModel.Composition.Hosting;
// using SonarLint.VisualStudio.TestInfrastructure;
// using SonarLint.VisualStudio.IssueVisualization.Models;
// using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
// using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
// using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
//
// namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint;
//
// [TestClass]
// public class TaintStoreTests
// {
//     private ITaintStore testSubject;
//
//     [TestMethod]
//     public void MefCtor_CheckExports()
//     {
//         var batch = new CompositionBatch();
//
//         var storeImport = new SingleObjectImporter<ITaintStore>();
//         var issuesStoreImport = new SingleObjectImporter<IIssuesStore>();
//         batch.AddPart(storeImport);
//         batch.AddPart(issuesStoreImport);
//
//         var catalog = new TypeCatalog(typeof(TaintStore));
//         using var container = new CompositionContainer(catalog);
//         container.Compose(batch);
//
//         storeImport.Import.Should().NotBeNull();
//         issuesStoreImport.Import.Should().NotBeNull();
//
//         storeImport.Import.Should().BeSameAs(issuesStoreImport.Import);
//     }
//
//     [TestInitialize]
//     public void TestInitialize()
//     {
//         testSubject = new TaintStore();
//     }
//
//     [TestMethod]
//     public void GetAll_ReturnsImmutableInstance()
//     {
//         var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
//         testSubject.Set(oldItems, "config scope");
//
//         var issuesList1 = testSubject.GetAll();
//         testSubject.Add(SetupIssueViz());
//         var issuesList2 = testSubject.GetAll();
//
//         issuesList1.Count.Should().Be(2);
//         issuesList2.Count.Should().Be(3);
//     }
//
//     [TestMethod]
//     public void Set_NullCollection_ArgumentNullException()
//     {
//         Action act = () => testSubject.Set(null, null);
//
//         act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualizations");
//     }
//
//     [TestMethod]
//     public void Set_NoSubscribersToIssuesChangedEvent_NoException()
//     {
//         Action act = () => testSubject.Set(new[] { SetupIssueViz() }, "some config scope");
//
//         act.Should().NotThrow();
//     }
//
//     [TestMethod]
//     public void Set_NoPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
//     {
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         testSubject.Set([], null);
//
//         testSubject.GetAll().Should().BeEmpty();
//         eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
//     }
//
//     [TestMethod]
//     public void Set_NoPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
//     {
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         var newItems = new[] { SetupIssueViz(), SetupIssueViz() };
//         testSubject.Set(newItems, "some config scope");
//
//         testSubject.GetAll().Should().BeEquivalentTo(newItems);
//         eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
//         var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
//         eventArgs.RemovedIssues.Should().BeEmpty();
//         eventArgs.AddedIssues.Should().BeEquivalentTo(newItems);
//     }
//
//     [TestMethod]
//     public void Set_HasPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
//     {
//         var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
//         testSubject.Set(oldItems, "some config scope");
//
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         testSubject.Set([], "some config scope");
//
//         testSubject.GetAll().Should().BeEmpty();
//         eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
//         var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
//         eventArgs.RemovedIssues.Should().BeEquivalentTo(oldItems);
//         eventArgs.AddedIssues.Should().BeEmpty();
//     }
//
//     [TestMethod]
//     public void Set_HasPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
//     {
//         var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
//         testSubject.Set(oldItems, "some config scope");
//
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         var newItems = new[] { SetupIssueViz(), SetupIssueViz() };
//         testSubject.Set(newItems, "some config scope");
//
//         testSubject.GetAll().Should().BeEquivalentTo(newItems);
//         eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
//         var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
//         eventArgs.RemovedIssues.Should().BeEquivalentTo(oldItems);
//         eventArgs.AddedIssues.Should().BeEquivalentTo(newItems);
//     }
//
//     [TestMethod]
//     public void Set_HasPreviousItems_HasSomeNewItems_CollectionChangedAndEventRaised()
//     {
//         var issueViz1 = SetupIssueViz("key1");
//         var issueViz2 = SetupIssueViz("key2");
//         var issueViz2NewObject = SetupIssueViz("key2");
//         var issueViz3 = SetupIssueViz("key3");
//
//         var oldItems = new[] { issueViz1, issueViz2 };
//         testSubject.Set(oldItems, "some config scope");
//
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         var newItems = new[] { issueViz2NewObject, issueViz3};
//         testSubject.Set(newItems, "some config scope");
//
//         testSubject.GetAll().Should().BeEquivalentTo(newItems);
//         eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
//         var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
//         eventArgs.RemovedIssues.Should().BeEquivalentTo(issueViz1);
//         eventArgs.AddedIssues.Should().BeEquivalentTo(issueViz3);
//     }
//
//     [TestMethod]
//     public void Set_HasItems_NoConfigScope_Throws()
//     {
//         var issueViz1 = SetupIssueViz("key1");
//
//         var act = () => testSubject.Set([issueViz1], null);
//
//         act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("newConfigurationScope");
//     }
//
//     [TestMethod]
//     public void ConfigScope_NoInformation_ReturnsNull()
//     {
//         testSubject.Set([], null);
//
//         var result = testSubject.ConfigurationScope;
//         result.Should().BeNull();
//     }
//
//     [TestMethod]
//     public void ConfigScope_HasInformation_ReturnsInformation()
//     {
//         const string newConfigurationScope = "some config scope";
//
//         testSubject.Set([], newConfigurationScope);
//
//         var result = testSubject.ConfigurationScope;
//         result.Should().BeSameAs(newConfigurationScope);
//     }
//
//     [TestMethod]
//     public void Remove_IssueKeyIsNull_ArgumentNullException()
//     {
//         Action act = () => testSubject.Remove(null);
//
//         act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueKey");
//     }
//
//     [TestMethod]
//     public void Remove_IssueNotFound_NoIssuesInList_NoEventIsRaised()
//     {
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         testSubject.Remove("some unknown key");
//
//         eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
//         testSubject.GetAll().Should().BeEmpty();
//     }
//
//     [TestMethod]
//     public void Remove_IssueNotFound_NoIssueWithThisId_NoEventIsRaised()
//     {
//         var existingIssue = SetupIssueViz("key1");
//
//         testSubject.Set(new[] { existingIssue }, "some config scope");
//
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         testSubject.Remove("some unknown key");
//
//         eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
//         testSubject.GetAll().Should().BeEquivalentTo(existingIssue);
//     }
//
//     [TestMethod]
//     public void Remove_IssueFound_IssueIsRemovedAndEventIsRaised()
//     {
//         var existingIssue1 = SetupIssueViz("key1");
//         var existingIssue2 = SetupIssueViz("key2");
//         var existingIssue3 = SetupIssueViz("key3");
//
//         testSubject.Set([existingIssue1, existingIssue2, existingIssue3], "some config scope");
//
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         testSubject.Remove("key2");
//
//         eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
//         var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
//         eventArgs.RemovedIssues.Should().BeEquivalentTo(existingIssue2);
//         eventArgs.AddedIssues.Should().BeEmpty();
//         testSubject.GetAll().Should().BeEquivalentTo(existingIssue1, existingIssue3);
//     }
//
//     [TestMethod]
//     public void Remove_MultipleIssuesFoundWithSameId_FirstIssueIsRemovedAndEventIsRaised()
//     {
//         var existingIssue1 = SetupIssueViz("key1");
//         var existingIssue2 = SetupIssueViz("key1");
//         var existingIssue3 = SetupIssueViz("key1");
//
//         testSubject.Set([existingIssue1, existingIssue2, existingIssue3], "some config scope");
//
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         testSubject.Remove("key1");
//
//         eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
//         var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
//         eventArgs.RemovedIssues.Should().BeEquivalentTo(existingIssue1);
//         eventArgs.AddedIssues.Should().BeEmpty();
//         testSubject.GetAll().Should().BeEquivalentTo(existingIssue2, existingIssue3);
//     }
//
//     [TestMethod]
//     public void Add_IssueIsNull_ArgumentNullException()
//     {
//         Action act = () => testSubject.Add(null);
//
//         act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualization");
//     }
//
//     [TestMethod]
//     public void Add_NoConfigScope_IssueIgnoredAndNoEventIsRaised()
//     {
//         testSubject.Set([], null);
//
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         testSubject.Add(SetupIssueViz());
//
//         eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
//         testSubject.GetAll().Should().BeEmpty();
//     }
//
//     [TestMethod]
//     public void Add_HasConfigScope_IssueAddedAndEventIsRaised()
//     {
//         var existingIssue = SetupIssueViz("key1");
//
//         testSubject.Set([existingIssue], "some config scope");
//
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         var newIssue = SetupIssueViz();
//         testSubject.Add(newIssue);
//
//         eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
//         var eventArgs = (IssuesChangedEventArgs)eventHandlerMock.ReceivedCalls().Single().GetArguments()[1]!;
//         eventArgs.RemovedIssues.Should().BeEmpty();
//         eventArgs.AddedIssues.Should().BeEquivalentTo(newIssue);
//         testSubject.GetAll().Should().BeEquivalentTo(existingIssue, newIssue);
//     }
//
//     [TestMethod]
//     public void Add_DuplicateIssue_IssueIgnoredAndNoEventIsRaised()
//     {
//         var issueKey = "key1";
//         var existingIssue = SetupIssueViz(issueKey);
//
//         testSubject.Set([existingIssue], "some config scope");
//
//         var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
//         testSubject.IssuesChanged += eventHandlerMock;
//
//         var newIssue = SetupIssueViz(issueKey);
//         testSubject.Add(newIssue);
//
//         eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
//         testSubject.GetAll().Should().BeEquivalentTo(existingIssue);
//     }
//
//     private IAnalysisIssueVisualization SetupIssueViz(string issueKey = null)
//     {
//         issueKey ??= Guid.NewGuid().ToString();
//
//         var taintIssue = Substitute.For<ITaintIssue>();
//         taintIssue.IssueKey.Returns(issueKey);
//
//         var issueViz = Substitute.For<IAnalysisIssueVisualization>();
//         issueViz.Issue.Returns(taintIssue);
//
//         return issueViz;
//     }
// }
