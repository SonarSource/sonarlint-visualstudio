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

using System.ComponentModel;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Models;

[TestClass]
public class AnalysisIssueVisualizationTests
{
    private readonly SnapshotSpan emptySpan = new();
    private readonly string filePath = "filePath.txt";

    private IAnalysisIssue issue = Substitute.For<IAnalysisIssue>();
    private AnalysisIssueVisualization issueVisualizationWithEmptySpan;
    private AnalysisIssueVisualization issueVisualizationWithNoSpan;
    private AnalysisIssueVisualization issueVisualizationWithNotEmptySpan;
    private SnapshotSpan notEmptySpan;

    [TestInitialize]
    public void TestInitialize()
    {
        notEmptySpan = CreateSpan();
        issue = Substitute.For<IAnalysisIssue>();
        MockAnalysisIssue();

        issueVisualizationWithNoSpan = new AnalysisIssueVisualization(null, issue, null, null);
        issueVisualizationWithEmptySpan = new AnalysisIssueVisualization(null, issue, emptySpan, null);
        issueVisualizationWithNotEmptySpan = new AnalysisIssueVisualization(null, issue, notEmptySpan, null);
    }

    [TestMethod]
    public void Ctor_StepNumberIsZero() => issueVisualizationWithNoSpan.StepNumber.Should().Be(0);

    [TestMethod]
    public void Ctor_InitialFilePathIsTakenFromIssue() => issueVisualizationWithNoSpan.CurrentFilePath.Should().Be(filePath);

    [TestMethod]
    public void Ctor_NullSpan_InitialSpanIsSetToGivenValue() => issueVisualizationWithNoSpan.Span.Should().BeNull();

    [TestMethod]
    public void Ctor_EmptySpan_InitialSpanIsSetToGivenValue() => issueVisualizationWithEmptySpan.Span.Should().Be(emptySpan);

    [TestMethod]
    public void Ctor_NonEmptySpan_InitialSpanIsSetToGivenValue() => issueVisualizationWithNotEmptySpan.Span.Should().Be(notEmptySpan);

    [TestMethod]
    public void Location_ReturnsUnderlyingIssueLocation() => issueVisualizationWithEmptySpan.Location.Should().Be(issueVisualizationWithEmptySpan.Issue.PrimaryLocation);

    [TestMethod]
    public void SetCurrentFilePath_FilePathIsNull_SpanIsInvalidated()
    {
        VerifySpanAndLocationCorrect(filePath);
        var propertyChangedEventHandler = MockSubscriberToPropertyChanged();

        issueVisualizationWithNotEmptySpan.CurrentFilePath = null;

        issueVisualizationWithNotEmptySpan.Span.Value.IsEmpty.Should().BeTrue();
        issueVisualizationWithNotEmptySpan.CurrentFilePath.Should().BeNull();
        VerifyPropertyChangedRaised(propertyChangedEventHandler, issueVisualizationWithNotEmptySpan, nameof(issueVisualizationWithNotEmptySpan.CurrentFilePath));
        VerifyPropertyChangedRaised(propertyChangedEventHandler, issueVisualizationWithNotEmptySpan, nameof(issueVisualizationWithNotEmptySpan.Span));
        propertyChangedEventHandler.ReceivedCalls().Count().Should().Be(2);
    }

    [TestMethod]
    public void SetCurrentFilePath_FilePathIsNotNull_SpanNotChanged()
    {
        VerifySpanAndLocationCorrect(filePath);
        var propertyChangedEventHandler = MockSubscriberToPropertyChanged();

        issueVisualizationWithNotEmptySpan.CurrentFilePath = "newpath.txt";

        issueVisualizationWithNotEmptySpan.Span.Should().Be(notEmptySpan);
        issueVisualizationWithNotEmptySpan.CurrentFilePath.Should().Be("newpath.txt");
        VerifyPropertyChangedRaised(propertyChangedEventHandler, issueVisualizationWithNotEmptySpan, nameof(issueVisualizationWithNotEmptySpan.CurrentFilePath));
        VerifyPropertyChangedNotRaised(propertyChangedEventHandler, nameof(issueVisualizationWithNotEmptySpan.Span));
        propertyChangedEventHandler.ReceivedCalls().Count().Should().Be(1);
    }

    [TestMethod]
    public void SetCurrentFilePath_NoSubscribers_NoException()
    {
        Action act = () => issueVisualizationWithNotEmptySpan.CurrentFilePath = "new path";
        act.Should().NotThrow();

        issueVisualizationWithNotEmptySpan.CurrentFilePath.Should().Be("new path");
    }

    [TestMethod]
    public void SetCurrentFilePath_HasSubscribers_NotifiesSubscribers()
    {
        var propertyChangedEventHandler = MockSubscriberToPropertyChanged();

        issueVisualizationWithNotEmptySpan.CurrentFilePath = "new path";

        VerifyPropertyChangedRaised(propertyChangedEventHandler, issueVisualizationWithNotEmptySpan, nameof(issueVisualizationWithNotEmptySpan.CurrentFilePath));
        propertyChangedEventHandler.ReceivedCalls().Count().Should().Be(1);
        issueVisualizationWithNotEmptySpan.CurrentFilePath.Should().Be("new path");
    }

    [TestMethod]
    public void SetSpan_NoSubscribers_NoException()
    {
        var newSpan = CreateSpan();

        Action act = () => issueVisualizationWithNotEmptySpan.Span = newSpan;
        act.Should().NotThrow();

        issueVisualizationWithNotEmptySpan.Span.Should().Be(newSpan);
    }

    [TestMethod]
    public void SetSpan_HasSubscribers_NotifiesSubscribers()
    {
        var newSpan = CreateSpan();
        var propertyChangedEventHandler = MockSubscriberToPropertyChanged();

        issueVisualizationWithNotEmptySpan.Span = newSpan;

        VerifyPropertyChangedRaised(propertyChangedEventHandler, issueVisualizationWithNotEmptySpan, nameof(issueVisualizationWithNotEmptySpan.Span));
        propertyChangedEventHandler.ReceivedCalls().Count().Should().Be(1);
        issueVisualizationWithNotEmptySpan.Span.Should().Be(newSpan);
    }

    [TestMethod]
    public void IsFilterable()
    {
        issueVisualizationWithEmptySpan.Should().BeAssignableTo<IFilterableIssue>();

        var filterable = (IFilterableIssue)issueVisualizationWithEmptySpan;
        filterable.IssueId.Should().Be(issue.Id);
        filterable.RuleId.Should().Be(issue.RuleKey);
        filterable.FilePath.Should().Be(issue.PrimaryLocation.FilePath);
        filterable.StartLine.Should().Be(issue.PrimaryLocation.TextRange.StartLine);
        filterable.LineHash.Should().Be(issue.PrimaryLocation.TextRange.LineHash);
    }

    private void MockAnalysisIssue()
    {
        var id = Guid.NewGuid();
        issue.Id.Returns(id);
        issue.RuleKey.Returns("my key");
        issue.PrimaryLocation.FilePath.Returns("x:\\aaa.foo");
        issue.PrimaryLocation.TextRange.StartLine.Returns(999);
        issue.PrimaryLocation.TextRange.LineHash.Returns("hash");
        issue.PrimaryLocation.FilePath.Returns(filePath);
    }

    private SnapshotSpan CreateSpan()
    {
        var mockTextSnapshot = Substitute.For<ITextSnapshot>();
        mockTextSnapshot.Length.Returns(20);

        return new SnapshotSpan(mockTextSnapshot, new Span(0, 10));
    }

    private void VerifyPropertyChangedRaised(PropertyChangedEventHandler propertyChangedEventHandler, AnalysisIssueVisualization testSubject, string propertyName) =>
        propertyChangedEventHandler.Received().Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == propertyName));

    private void VerifyPropertyChangedNotRaised(PropertyChangedEventHandler propertyChangedEventHandler, string propertyName) =>
        propertyChangedEventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == propertyName));

    private PropertyChangedEventHandler MockSubscriberToPropertyChanged()
    {
        var propertyChangedEventHandler = Substitute.For<PropertyChangedEventHandler>();
        issueVisualizationWithNotEmptySpan.PropertyChanged += propertyChangedEventHandler;
        return propertyChangedEventHandler;
    }

    private void VerifySpanAndLocationCorrect(string oldFilePath)
    {
        issueVisualizationWithNotEmptySpan.Span.Should().Be(notEmptySpan);
        issueVisualizationWithNotEmptySpan.CurrentFilePath.Should().Be(oldFilePath);
    }
}
