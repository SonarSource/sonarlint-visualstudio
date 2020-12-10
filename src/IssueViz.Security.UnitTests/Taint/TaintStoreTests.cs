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
using System.Collections.ObjectModel;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint
{
    [TestClass]
    public class TaintStoreTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var issueStoreObserver = new Mock<IIssueStoreObserver>();
            issueStoreObserver
                .Setup(x => x.Register(It.IsAny<ReadOnlyObservableCollection<IAnalysisIssueVisualization>>()))
                .Returns(Mock.Of<IDisposable>());

            MefTestHelpers.CheckTypeCanBeImported<TaintStore, ITaintStore>(null, new[]
            {
                MefTestHelpers.CreateExport<IIssueStoreObserver>(issueStoreObserver.Object)
            });
        }

        [TestMethod]
        public void Ctor_RegisterToIssueStoreObserver()
        {
            var issueStoreObserver = new Mock<IIssueStoreObserver>();
            CreateTestSubject(issueStoreObserver.Object);

            issueStoreObserver.Verify(x => x.Register(It.IsAny<ReadOnlyObservableCollection<IAnalysisIssueVisualization>>()), Times.Once);
            issueStoreObserver.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_InnerIssueVizStoreIsDisposed()
        {
            var unregisterMock = new Mock<IDisposable>();
            var issueStoreObserver = new Mock<IIssueStoreObserver>();
            issueStoreObserver
                .Setup(x => x.Register(It.IsAny<ReadOnlyObservableCollection<IAnalysisIssueVisualization>>()))
                .Returns(unregisterMock.Object);

            var testSubject = CreateTestSubject(issueStoreObserver.Object);

            unregisterMock.VerifyNoOtherCalls();

            testSubject.Dispose();

            unregisterMock.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void GetAll_ReturnsReadOnlyObservableWrapper()
        {
            var testSubject = CreateTestSubject();
            var readOnlyWrapper = testSubject.GetAll();

            readOnlyWrapper.Should().BeAssignableTo<IReadOnlyCollection<IAnalysisIssueVisualization>>();

            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();

            testSubject.Initialize(new List<IAnalysisIssueVisualization> {issueViz1, issueViz2});

            readOnlyWrapper.Count.Should().Be(2);
            readOnlyWrapper.First().Should().Be(issueViz1);
            readOnlyWrapper.Last().Should().Be(issueViz2);

            testSubject.Initialize(new List<IAnalysisIssueVisualization> { issueViz2 });

            readOnlyWrapper.Count.Should().Be(1);
            readOnlyWrapper.First().Should().Be(issueViz2);
        }

        [TestMethod]
        public void Initialize_NullCollection_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Initialize(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualizations");
        }

        private ITaintStore CreateTestSubject(IIssueStoreObserver issueStoreObserver = null)
        {
            issueStoreObserver ??= Mock.Of<IIssueStoreObserver>();
            return new TaintStore(issueStoreObserver);
        }
    }
}
