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

            Action act = () => testSubject.Set(new[] { Mock.Of<IAnalysisIssueVisualization>() }, null);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void Set_NoPreviousItems_NoNewItems_CollectionChangedAndEventNotRaised()
        {
            var testSubject = CreateTestSubject();

            var callCount = 0;
            testSubject.IssuesChanged += (sender, args) => { callCount++; };

            testSubject.Set(Enumerable.Empty<IAnalysisIssueVisualization>(), null);

            testSubject.GetAll().Should().BeEmpty();
            callCount.Should().Be(0);
        }

        [TestMethod]
        public void Set_NoPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
        {
            var testSubject = CreateTestSubject();

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (sender, args) => { callCount++; suppliedArgs = args; };

            var newItems = new[] { Mock.Of<IAnalysisIssueVisualization>(), Mock.Of<IAnalysisIssueVisualization>() };
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

            var oldItems = new[] { Mock.Of<IAnalysisIssueVisualization>(), Mock.Of<IAnalysisIssueVisualization>() };
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

            var oldItems = new[] { Mock.Of<IAnalysisIssueVisualization>(), Mock.Of<IAnalysisIssueVisualization>() };
            testSubject.Set(oldItems, null);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (sender, args) => { callCount++; suppliedArgs = args; };

            var newItems = new[] { Mock.Of<IAnalysisIssueVisualization>(), Mock.Of<IAnalysisIssueVisualization>() };
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

            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz3 = Mock.Of<IAnalysisIssueVisualization>();

            var oldItems = new[] { issueViz1, issueViz2 };
            testSubject.Set(oldItems, null);

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (sender, args) => { callCount++; suppliedArgs = args; };

            var newItems = new[] { issueViz2, issueViz3 };
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

        private ITaintStore CreateTestSubject()
        {
            return new TaintStore();
        }
    }
}
