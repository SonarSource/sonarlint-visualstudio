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

using System;
using System.ComponentModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppressions;
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
        public void Ctor_NullSpan_InitialSpanIsSetToGivenValue()
        {
            var testSubject = CreateTestSubject(span: null);

            testSubject.Span.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_EmptySpan_InitialSpanIsSetToGivenValue()
        {
            var span = new SnapshotSpan();
            var testSubject = CreateTestSubject(span: span);

            testSubject.Span.Should().Be(span);
        }

        [TestMethod]
        public void Ctor_NonEmptySpan_InitialSpanIsSetToGivenValue()
        {
            var span = CreateSpan();
            var testSubject = CreateTestSubject(span: span);

            testSubject.Span.Should().Be(span);
        }

        [TestMethod]
        public void Location_ReturnsUnderlyingIssueLocation()
        {
            var testSubject = CreateTestSubject();
            testSubject.Location.Should().Be(testSubject.Issue.PrimaryLocation);
        }

        [TestMethod]
        public void SetCurrentFilePath_FilePathIsNull_SpanIsInvalidated()
        {
            // Arrange
            var oldFilePath = "oldpath.txt";
            var oldSpan = CreateSpan();

            var testSubject = CreateTestSubject(oldFilePath, oldSpan);
            testSubject.Span.Should().Be(oldSpan);
            testSubject.CurrentFilePath.Should().Be(oldFilePath);

            var propertyChangedEventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += propertyChangedEventHandler.Object;

            // Act
            testSubject.CurrentFilePath = null;

            // Assert
            testSubject.Span.Value.IsEmpty.Should().BeTrue();
            testSubject.CurrentFilePath.Should().BeNull();

            VerifyPropertyChangedRaised(propertyChangedEventHandler, nameof(testSubject.CurrentFilePath));
            VerifyPropertyChangedRaised(propertyChangedEventHandler, nameof(testSubject.Span));
            propertyChangedEventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SetCurrentFilePath_FilePathIsNotNull_SpanNotChanged()
        {
            // Arrange
            var oldFilePath = "oldpath.txt";
            var oldSpan = CreateSpan();

            var testSubject = CreateTestSubject(oldFilePath, oldSpan);
            testSubject.Span.Should().Be(oldSpan);
            testSubject.CurrentFilePath.Should().Be(oldFilePath);

            var propertyChangedEventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += propertyChangedEventHandler.Object;

            // Act
            testSubject.CurrentFilePath = "newpath.txt";

            // Assert
            testSubject.Span.Should().Be(oldSpan);
            testSubject.CurrentFilePath.Should().Be("newpath.txt");

            VerifyPropertyChangedRaised(propertyChangedEventHandler, nameof(testSubject.CurrentFilePath));
            VerifyPropertyChangedNotRaised(propertyChangedEventHandler, nameof(testSubject.Span));
            propertyChangedEventHandler.VerifyNoOtherCalls();
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

            VerifyPropertyChangedRaised(propertyChangedEventHandler, nameof(testSubject.CurrentFilePath));
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

            VerifyPropertyChangedRaised(propertyChangedEventHandler, nameof(testSubject.Span));
            propertyChangedEventHandler.VerifyNoOtherCalls();

            testSubject.Span.Should().Be(newSpan);
        }

        [TestMethod]
        public void SetIsSuppressed_HasSubscribers_VerifyRaised()
        {
            var testSubject = CreateTestSubject();

            testSubject.IsSuppressed.Should().BeFalse();

            var propertyChangedEventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += propertyChangedEventHandler.Object;

            testSubject.IsSuppressed = true;

            VerifyPropertyChangedRaised(propertyChangedEventHandler, nameof(testSubject.IsSuppressed));
            propertyChangedEventHandler.VerifyNoOtherCalls();

            testSubject.IsSuppressed.Should().BeTrue();
        }

        [TestMethod]
        public void IsFilterable()
        {
            var issueMock = new Mock<IAnalysisIssue>();
            issueMock.SetupGet(x => x.RuleKey).Returns("my key");
            issueMock.SetupGet(x => x.PrimaryLocation.FilePath).Returns("x:\\aaa.foo");
            issueMock.SetupGet(x => x.PrimaryLocation.TextRange.StartLine).Returns(999);
            issueMock.SetupGet(x => x.PrimaryLocation.TextRange.LineHash).Returns("hash");

            var testSubject = new AnalysisIssueVisualization(null, issueMock.Object, new SnapshotSpan(), null);

            testSubject.Should().BeAssignableTo<IFilterableIssue>();

            var filterable = (IFilterableIssue)testSubject;

            filterable.RuleId.Should().Be(issueMock.Object.RuleKey);
            filterable.FilePath.Should().Be(issueMock.Object.PrimaryLocation.FilePath);
            filterable.StartLine.Should().Be(issueMock.Object.PrimaryLocation.TextRange.StartLine);
            filterable.LineHash.Should().Be(issueMock.Object.PrimaryLocation.TextRange.LineHash);
        }

        private SnapshotSpan CreateSpan()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(x => x.Length).Returns(20);

            return new SnapshotSpan(mockTextSnapshot.Object, new Span(0, 10));
        }

        private AnalysisIssueVisualization CreateTestSubject(string filePath = null, SnapshotSpan? span = null)
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.PrimaryLocation.FilePath).Returns(filePath);

            return new AnalysisIssueVisualization(null, issue.Object, span, null);
        }

        private void VerifyPropertyChangedRaised(Mock<PropertyChangedEventHandler> propertyChangedEventHandler, string propertyName)
        {
            propertyChangedEventHandler.Verify(x =>
                    x(It.IsAny<object>(), It.Is((PropertyChangedEventArgs e) => e.PropertyName == propertyName)),
                Times.Once);
        }

        private void VerifyPropertyChangedNotRaised(Mock<PropertyChangedEventHandler> propertyChangedEventHandler, string propertyName)
        {
            propertyChangedEventHandler.Verify(x =>
                    x(It.IsAny<object>(), It.Is((PropertyChangedEventArgs e) => e.PropertyName == propertyName)),
                Times.Never);
        }
    }
}
