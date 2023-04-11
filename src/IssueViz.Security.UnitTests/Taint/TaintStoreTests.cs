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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint
{
    [TestClass]
    public class TaintStoreTests
    {
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
        
        [TestMethod]
        public void GetAll_ReturnsImmutableInstance()
        {
            var testSubject = CreateTestSubject();
            var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
            testSubject.Set(oldItems, new AnalysisInformation("some branch", DateTimeOffset.Now));

            var issuesList1 = testSubject.GetAll();
            testSubject.Add(SetupIssueViz());
            var issuesList2 = testSubject.GetAll();

            issuesList1.Count.Should().Be(2);
            issuesList2.Count.Should().Be(3);
        }

        [TestMethod]
        public void Set_NullCollection_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Set(null, null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualizations");
        }

        [TestMethod]
        public void Set_NoSubscribersToIssuesChangedEvent_NoException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Set(new[] { SetupIssueViz() }, null);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void Set_NoPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
        {
            var testSubject = CreateTestSubject();

            var callCount = 0;
            testSubject.IssuesChanged += (sender, args) => { callCount++; };

            testSubject.Set(Enumerable.Empty<IAnalysisIssueVisualization>(), null);

            testSubject.GetAll().Should().BeEmpty();
            callCount.Should().Be(1);
        }

        [TestMethod]
        public void Set_NoPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
        {
            var testSubject = CreateTestSubject();

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (sender, args) => { callCount++; suppliedArgs = args; };

            var newItems = new[] { SetupIssueViz(), SetupIssueViz() };
            testSubject.Set(newItems, null);

            testSubject.GetAll().Should().BeEquivalentTo(newItems);
            callCount.Should().Be(1);
            suppliedArgs.RemovedIssues.Should().BeEmpty();
            suppliedArgs.AddedIssues.Should().BeEquivalentTo(newItems);
        }

        [TestMethod]
        public void Set_HasPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
        {
            var testSubject = CreateTestSubject();

            var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
            testSubject.Set(oldItems, null);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (sender, args) => { callCount++; suppliedArgs = args; };

            testSubject.Set(Enumerable.Empty<IAnalysisIssueVisualization>(), null);

            testSubject.GetAll().Should().BeEmpty();
            callCount.Should().Be(1);
            suppliedArgs.RemovedIssues.Should().BeEquivalentTo(oldItems);
            suppliedArgs.AddedIssues.Should().BeEmpty();
        }

        [TestMethod]
        public void Set_HasPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
        {
            var testSubject = CreateTestSubject();

            var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
            testSubject.Set(oldItems, null);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (sender, args) => { callCount++; suppliedArgs = args; };

            var newItems = new[] { SetupIssueViz(), SetupIssueViz() };
            testSubject.Set(newItems, null);

            testSubject.GetAll().Should().BeEquivalentTo(newItems);
            callCount.Should().Be(1);
            suppliedArgs.RemovedIssues.Should().BeEquivalentTo(oldItems);
            suppliedArgs.AddedIssues.Should().BeEquivalentTo(newItems);
        }

        [TestMethod]
        public void Set_HasPreviousItems_HasSomeNewItems_CollectionChangedAndEventRaised()
        {
            var testSubject = CreateTestSubject();

            var issueViz1 = SetupIssueViz("key1");
            var issueViz2 = SetupIssueViz("key2");
            var issueViz2NewObject = SetupIssueViz("key2");
            var issueViz3 = SetupIssueViz("key3");

            var oldItems = new[] { issueViz1, issueViz2 };
            testSubject.Set(oldItems, null);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (sender, args) => { callCount++; suppliedArgs = args; };

            var newItems = new[] { issueViz2NewObject, issueViz3};
            testSubject.Set(newItems, null);

            testSubject.GetAll().Should().BeEquivalentTo(newItems);
            callCount.Should().Be(1);
            suppliedArgs.RemovedIssues.Should().BeEquivalentTo(issueViz1);
            suppliedArgs.AddedIssues.Should().BeEquivalentTo(issueViz3);
        }

        [TestMethod]
        public void GetAnalysisInformation_NoInformation_ReturnsNull()
        {
            var testSubject = CreateTestSubject();
            testSubject.Set(Enumerable.Empty<IAnalysisIssueVisualization>(), null);

            var result = testSubject.GetAnalysisInformation();
            result.Should().BeNull();
        }

        [TestMethod]
        public void GetAnalysisInformation_HasInformation_ReturnsInformation()
        {
            var analysisInformation = new AnalysisInformation("some branch", DateTimeOffset.Now);

            var testSubject = CreateTestSubject();
            testSubject.Set(Enumerable.Empty<IAnalysisIssueVisualization>(), analysisInformation);

            var result = testSubject.GetAnalysisInformation();
            result.Should().BeSameAs(analysisInformation);
        }

        [TestMethod]
        public void Remove_IssueKeyIsNull_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Remove(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueKey");
        }

        [TestMethod]
        public void Remove_IssueNotFound_NoIssuesInList_NoEventIsRaised()
        {
            var testSubject = CreateTestSubject();

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (_, args) => { callCount++; suppliedArgs = args; };

            testSubject.Remove("some unknown key");

            callCount.Should().Be(0);
            testSubject.GetAll().Should().BeEmpty();
        }

        [TestMethod]
        public void Remove_IssueNotFound_NoIssueWithThisId_NoEventIsRaised()
        {
            var existingIssue = SetupIssueViz("key1");

            var testSubject = CreateTestSubject();
            testSubject.Set(new[] { existingIssue }, null);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (_, args) => { callCount++; suppliedArgs = args; };

            testSubject.Remove("some unknown key");

            callCount.Should().Be(0);
            testSubject.GetAll().Should().BeEquivalentTo(existingIssue);
        }

        [TestMethod]
        public void Remove_IssueFound_IssueIsRemovedAndEventIsRaised()
        {
            var existingIssue1 = SetupIssueViz("key1");
            var existingIssue2 = SetupIssueViz("key2");
            var existingIssue3 = SetupIssueViz("key3");

            var testSubject = CreateTestSubject();
            testSubject.Set(new[] {existingIssue1, existingIssue2, existingIssue3}, null);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (_, args) => { callCount++; suppliedArgs = args; };

            testSubject.Remove("key2");

            callCount.Should().Be(1);
            suppliedArgs.RemovedIssues.Should().BeEquivalentTo(existingIssue2);
            suppliedArgs.AddedIssues.Should().BeEmpty();
            testSubject.GetAll().Should().BeEquivalentTo(existingIssue1, existingIssue3);
        }

        [TestMethod]
        public void Remove_MultipleIssuesFoundWithSameId_FirstIssueIsRemovedAndEventIsRaised()
        {
            var existingIssue1 = SetupIssueViz("key1");
            var existingIssue2 = SetupIssueViz("key1");
            var existingIssue3 = SetupIssueViz("key1");

            var testSubject = CreateTestSubject();
            testSubject.Set(new[] { existingIssue1, existingIssue2, existingIssue3 }, null);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (_, args) => { callCount++; suppliedArgs = args; };

            testSubject.Remove("key1");

            callCount.Should().Be(1);
            suppliedArgs.RemovedIssues.Should().BeEquivalentTo(existingIssue1);
            suppliedArgs.AddedIssues.Should().BeEmpty();
            testSubject.GetAll().Should().BeEquivalentTo(existingIssue2, existingIssue3);
        }

        [TestMethod]
        public void Add_IssueIsNull_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Add(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualization");
        }

        [TestMethod]
        public void Add_NoAnalysisInformation_IssueIgnoredAndNoEventIsRaised()
        {
            var existingIssue = SetupIssueViz("key1");

            var testSubject = CreateTestSubject();
            testSubject.Set(new[] { existingIssue}, null);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (_, args) => { callCount++; suppliedArgs = args; };

            testSubject.Add(SetupIssueViz());

            callCount.Should().Be(0);
            testSubject.GetAll().Should().BeEquivalentTo(existingIssue);
        }

        [TestMethod]
        public void Add_HasAnalysisInformation_IssueAddedAndEventIsRaised()
        {
            var analysisInformation = new AnalysisInformation("some branch", DateTimeOffset.Now);
            var existingIssue = SetupIssueViz("key1");

            var testSubject = CreateTestSubject();
            testSubject.Set(new[] { existingIssue }, analysisInformation);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (_, args) => { callCount++; suppliedArgs = args; };

            var newIssue = SetupIssueViz();
            testSubject.Add(newIssue);

            callCount.Should().Be(1);
            suppliedArgs.RemovedIssues.Should().BeEmpty();
            suppliedArgs.AddedIssues.Should().BeEquivalentTo(newIssue);
            testSubject.GetAll().Should().BeEquivalentTo(existingIssue, newIssue);
        }

        [TestMethod]
        public void Add_DuplicateIssue_IssueIgnoredAndNoEventIsRaised()
        {
            var analysisInformation = new AnalysisInformation("some branch", DateTimeOffset.Now);
            var issueKey = "key1";
            var existingIssue = SetupIssueViz(issueKey);

            var testSubject = CreateTestSubject();
            testSubject.Set(new[] { existingIssue }, analysisInformation);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (_, args) => { callCount++; suppliedArgs = args; };

            var newIssue = SetupIssueViz(issueKey);
            testSubject.Add(newIssue);

            callCount.Should().Be(0);
            testSubject.GetAll().Should().BeEquivalentTo(existingIssue);
        }

        private IAnalysisIssueVisualization SetupIssueViz(string issueKey = null)
        {
            issueKey ??= Guid.NewGuid().ToString();

            var taintIssue = new Mock<ITaintIssue>();
            taintIssue.Setup(x => x.IssueKey).Returns(issueKey);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(taintIssue.Object);

            return issueViz.Object;
        }

        private ITaintStore CreateTestSubject()
        {
            return new TaintStore();
        }
    }
}
