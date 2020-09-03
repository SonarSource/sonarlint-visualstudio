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
using System.ComponentModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Models
{
    [TestClass]
    public class AnalysisIssueVisualizationTests
    {
        [TestMethod]
        public void Ctor_StepNumberIsZero()
        {
            var testSubject = CreateTestSubject();

            testSubject.StepNumber.Should().Be(0);
        }

        [TestMethod]
        public void Ctor_InitialFilePathIsTakenFromIssue()
        {
            var testSubject = CreateTestSubject(filePath: "test path");

            testSubject.CurrentFilePath.Should().Be("test path");
        }

        [TestMethod]
        public void Ctor_InitialSpanIsSetToGivenValue()
        {
            var span = CreateSpan();
            var testSubject = CreateTestSubject(span: span);

            testSubject.Span.Should().Be(span);
        }

        [TestMethod]
        public void SetCurrentFilePath_NoSubscribers_NoException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.CurrentFilePath = "new path";
            act.Should().NotThrow();

            testSubject.CurrentFilePath.Should().Be("new path");
        }

        [TestMethod]
        public void SetCurrentFilePath_HasSubscribers_NotifiesSubscribers()
        {
            var propertyChangedEventHandler = new Mock<PropertyChangedEventHandler>();

            var testSubject = CreateTestSubject();
            testSubject.PropertyChanged += propertyChangedEventHandler.Object;

            testSubject.CurrentFilePath = "new path";

            propertyChangedEventHandler.Verify(x =>
                    x(It.IsAny<object>(), It.Is((PropertyChangedEventArgs e) => e.PropertyName == nameof(IAnalysisIssueVisualization.CurrentFilePath))),
                Times.Once);

            propertyChangedEventHandler.VerifyNoOtherCalls();

            testSubject.CurrentFilePath.Should().Be("new path");
        }

        [TestMethod]
        public void SetSpan_NoSubscribers_NoException()
        {
            var newSpan = CreateSpan();
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Span = newSpan;
            act.Should().NotThrow();

            testSubject.Span.Should().Be(newSpan);
        }

        [TestMethod]
        public void SetSpan_HasSubscribers_NotifiesSubscribers()
        {
            var newSpan = CreateSpan();
            var propertyChangedEventHandler = new Mock<PropertyChangedEventHandler>();

            var testSubject = CreateTestSubject();
            testSubject.PropertyChanged += propertyChangedEventHandler.Object;

            testSubject.Span = newSpan;

            propertyChangedEventHandler.Verify(x =>
                    x(It.IsAny<object>(), It.Is((PropertyChangedEventArgs e) => e.PropertyName == nameof(IAnalysisIssueVisualization.Span))),
                Times.Once);

            propertyChangedEventHandler.VerifyNoOtherCalls();

            testSubject.Span.Should().Be(newSpan);
        }

        [TestMethod]
        public void IsFilterable()
        {
            var issueMock = new Mock<IAnalysisIssue>();
            issueMock.SetupGet(x => x.RuleKey).Returns("my key");
            issueMock.SetupGet(x => x.StartLine).Returns(999);
            issueMock.SetupGet(x => x.FilePath).Returns("x:\\aaa.foo");
            issueMock.SetupGet(x => x.LineHash).Returns("hash");

            var testSubject = new AnalysisIssueVisualization(null, issueMock.Object, new SnapshotSpan());

            testSubject.Should().BeAssignableTo<IFilterableIssue>();

            var filterable = (IFilterableIssue)testSubject;

            filterable.RuleId.Should().Be(issueMock.Object.RuleKey);
            filterable.StartLine.Should().Be(issueMock.Object.StartLine);
            filterable.FilePath.Should().Be(issueMock.Object.FilePath);
            filterable.LineHash.Should().Be(issueMock.Object.LineHash);
            filterable.ProjectGuid.Should().BeNull();
        }

        private SnapshotSpan CreateSpan()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(x => x.Length).Returns(20);

            return new SnapshotSpan(mockTextSnapshot.Object, new Span(0, 10));
        }

        private AnalysisIssueVisualization CreateTestSubject(string filePath = null, SnapshotSpan span = new SnapshotSpan())
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.FilePath).Returns(filePath);

            return new AnalysisIssueVisualization(null, issue.Object, span);
        }
    }
}
