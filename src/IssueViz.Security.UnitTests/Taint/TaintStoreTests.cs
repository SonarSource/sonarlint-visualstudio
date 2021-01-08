/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
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

            Action act = () => testSubject.Set(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualizations");
        }

        [TestMethod]
        public void Set_NoSubscribersToIssuesChangedEvent_NoException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Set(new[] { Mock.Of<IAnalysisIssueVisualization>() });

            act.Should().NotThrow();
        }

        [TestMethod]
        public void Set_NoPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
        {
            var oldState = Enumerable.Empty<IAnalysisIssueVisualization>();
            var newState = Enumerable.Empty<IAnalysisIssueVisualization>();

            VerifyCollectionChanged(oldState, newState);
        }

        [TestMethod]
        public void Set_NoPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
        {
            var oldState = Enumerable.Empty<IAnalysisIssueVisualization>();
            var newState = new[] { Mock.Of<IAnalysisIssueVisualization>() };

            VerifyCollectionChanged(oldState, newState);
        }

        [TestMethod]
        public void Set_HasPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
        {
            var oldState = new[] { Mock.Of<IAnalysisIssueVisualization>() };
            var newState = Enumerable.Empty<IAnalysisIssueVisualization>();

            VerifyCollectionChanged(oldState, newState);
        }

        [TestMethod]
        public void Set_HasPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
        {
            var oldState = new[] { Mock.Of<IAnalysisIssueVisualization>() };
            var newState = new[] { Mock.Of<IAnalysisIssueVisualization>(), Mock.Of<IAnalysisIssueVisualization>() };

            VerifyCollectionChanged(oldState, newState);
        }

        [TestMethod]
        public void Set_HasPreviousItems_HasSomeNewItems_CollectionChangedAndEventRaised()
        {
            var mutualIssueViz = Mock.Of<IAnalysisIssueVisualization>();
            var oldState = new[] { mutualIssueViz, Mock.Of<IAnalysisIssueVisualization>() };
            var newState = new[] { Mock.Of<IAnalysisIssueVisualization>(), mutualIssueViz };

            VerifyCollectionChanged(oldState, newState);
        }


        private void VerifyCollectionChanged(IEnumerable<IAnalysisIssueVisualization> oldState, IEnumerable<IAnalysisIssueVisualization> newState)
        {
            var testSubject = CreateTestSubject();

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (sender, args) => { callCount++; suppliedArgs = args; };

            testSubject.Set(oldState);

            testSubject.GetAll().Should().BeEquivalentTo(oldState);

            callCount.Should().Be(1);
            suppliedArgs.OldIssues.Should().BeEmpty();
            suppliedArgs.NewIssues.Should().BeEquivalentTo(oldState);

            callCount = 0;

            testSubject.Set(newState);

            testSubject.GetAll().Should().BeEquivalentTo(newState);
            suppliedArgs.OldIssues.Should().BeEquivalentTo(oldState);
            suppliedArgs.NewIssues.Should().BeEquivalentTo(newState);

            callCount.Should().Be(1);
        }

        private ITaintStore CreateTestSubject()
        {
            return new TaintStore();
        }
    }
}
