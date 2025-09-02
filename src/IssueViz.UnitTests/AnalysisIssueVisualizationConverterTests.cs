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

using System.IO;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests;

[TestClass]
public class AnalysisIssueVisualizationConverterTests
{
    private Mock<IIssueSpanCalculator> issueSpanCalculatorMock;
    private ITextSnapshot textSnapshotMock;

    private AnalysisIssueVisualizationConverter testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        issueSpanCalculatorMock = new Mock<IIssueSpanCalculator>();
        textSnapshotMock = Mock.Of<ITextSnapshot>();

        testSubject = new AnalysisIssueVisualizationConverter(issueSpanCalculatorMock.Object, Substitute.For<ISpanTranslator>(), Substitute.For<IRoslynQuickFixProvider>());
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<AnalysisIssueVisualizationConverter, IAnalysisIssueVisualizationConverter>(
            MefTestHelpers.CreateExport<IIssueSpanCalculator>(),
            MefTestHelpers.CreateExport<ISpanTranslator>(),
            MefTestHelpers.CreateExport<IRoslynQuickFixProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<AnalysisIssueVisualizationConverter>();

    [TestMethod]
    public void Convert_NoTextBuffer_IssueWithNullSpan()
    {
        var issue = CreateIssue(Path.GetRandomFileName());

        var result = testSubject.Convert(issue, textSnapshot: null);

        result.Should().NotBeNull();
        result.Span.HasValue.Should().BeFalse();
        issueSpanCalculatorMock.Invocations.Count.Should().Be(0);
    }

    [TestMethod]
    public void Convert_NoTextBuffer_SecondaryLocationSpansAreNotCalculated()
    {
        var issueFilePath = Path.GetRandomFileName();
        var locationInSameFile = CreateLocation(issueFilePath);
        var flow = CreateFlow(locationInSameFile);
        var issue = CreateIssue(issueFilePath, flows: flow);

        var result = testSubject.Convert(issue, textSnapshot: null);

        result.Flows[0].Locations[0].Span.HasValue.Should().BeFalse();
        issueSpanCalculatorMock.Invocations.Count.Should().Be(0);
    }

    [TestMethod]
    public void Convert_NoTextBuffer_QuickFixSpansAreNotCalculated()
    {
        var issue = CreateIssue(Mock.Of<IQuickFixBase>());

        var result = testSubject.Convert(issue, textSnapshot: null);

        result.QuickFixes.Should().BeEmpty();
        issueSpanCalculatorMock.Invocations.Count.Should().Be(0);
    }

    [TestMethod]
    public void Convert_IssueHasNoQuickFixes_IssueVisualizationWithoutQuickFixes()
    {
        var issue = CreateIssue();
        SetupSpanCalculator(issue.PrimaryLocation.TextRange, new SnapshotSpan());

        var result = testSubject.Convert(issue, textSnapshotMock);

        result.QuickFixes.Should().BeEmpty();
    }

    [TestMethod]
    public void Convert_IssueHasQuickFixes_QuickFixesSpansAreCalculated()
    {
        var textRange1 = Mock.Of<ITextRange>();
        var span1 = CreateNonEmptySpan();
        SetupSpanCalculator(textRange1, span1);
        var edit1 = CreateEdit(textRange1);

        var textRange2 = Mock.Of<ITextRange>();
        var span2 = CreateNonEmptySpan();
        SetupSpanCalculator(textRange2, span2);
        var edit2 = CreateEdit(textRange2);

        var textRange3 = Mock.Of<ITextRange>();
        var span3 = CreateNonEmptySpan();
        SetupSpanCalculator(textRange3, span3);
        var edit3 = CreateEdit(textRange3);

        var fix1 = new TextBasedQuickFix("fix1", [edit1, edit2]);
        var fix2 = new TextBasedQuickFix("fix2", [edit3]);
        var issue = CreateIssue(fix1, fix2);

        var result = testSubject.Convert(issue, textSnapshotMock);

        result.QuickFixes.Should().NotBeEmpty();
        result.QuickFixes.Count.Should().Be(2);

        var quickFix1 = result.QuickFixes[0].Should().BeOfType<TextBasedQuickFixApplication>().Subject.QuickFixVisualization;
        quickFix1.Fix.Should().Be(fix1);
        quickFix1.EditVisualizations.Should().NotBeEmpty();
        quickFix1.EditVisualizations.Count.Should().Be(2);
        quickFix1.EditVisualizations[0].Edit.Should().Be(edit1);
        quickFix1.EditVisualizations[0].Span.Should().Be(span1);
        quickFix1.EditVisualizations[1].Edit.Should().Be(edit2);
        quickFix1.EditVisualizations[1].Span.Should().Be(span2);

        var quickFix2 = result.QuickFixes[1].Should().BeOfType<TextBasedQuickFixApplication>().Subject.QuickFixVisualization;
        quickFix2.Fix.Should().Be(fix2);
        quickFix2.EditVisualizations.Should().NotBeEmpty();
        quickFix2.EditVisualizations.Count.Should().Be(1);
        quickFix2.EditVisualizations[0].Edit.Should().Be(edit3);
        quickFix2.EditVisualizations[0].Span.Should().Be(span3);
    }

    [TestMethod]
    public void Convert_EmptySpan_IssueWithEmptySpan()
    {
        var issue = CreateIssue(Path.GetRandomFileName());
        SetupSpanCalculator(issue.PrimaryLocation.TextRange, new SnapshotSpan());

        var result = testSubject.Convert(issue, textSnapshotMock);

        result.Should().NotBeNull();
        result.Span.HasValue.Should().BeTrue();
        result.Span.Value.IsEmpty.Should().BeTrue();
    }

    [TestMethod]
    public void Convert_IssueHasNoFlows_IssueVisualizationWithoutFlows()
    {
        var flows = Array.Empty<IAnalysisIssueFlow>();
        var issueSpan = CreateNonEmptySpan();
        var issue = CreateIssue(Path.GetRandomFileName(), flows: flows);
        SetupSpanCalculator(issue.PrimaryLocation.TextRange, issueSpan);

        var expectedIssueVisualization = new AnalysisIssueVisualization(
            [],
            issue,
            issueSpan,
            []);

        var actualIssueVisualization = testSubject.Convert(issue, textSnapshotMock);

        AssertConversion(expectedIssueVisualization, actualIssueVisualization);
    }

    [TestMethod]
    public void Convert_IssueHasOneFlow_IssueVisualizationWithFlow()
    {
        var location = CreateLocation(Path.GetRandomFileName());
        var flow = CreateFlow(location);

        var issueSpan = CreateNonEmptySpan();
        var issue = CreateIssue(Path.GetRandomFileName(), flows: flow);
        SetupSpanCalculator(issue.PrimaryLocation.TextRange, issueSpan);

        var expectedLocationVisualization = new AnalysisIssueLocationVisualization(1, location);
        var expectedFlowVisualization = new AnalysisIssueFlowVisualization(1, [expectedLocationVisualization], flow);

        var expectedIssueVisualization = new AnalysisIssueVisualization(
            [expectedFlowVisualization],
            issue,
            issueSpan,
            []);

        var actualIssueVisualization = testSubject.Convert(issue, textSnapshotMock);

        AssertConversion(expectedIssueVisualization, actualIssueVisualization);
    }

    [TestMethod]
    public void Convert_IssueHasTwoFlows_IssueVisualizationWithTwoFlows()
    {
        var firstFlowFirstLocation = CreateLocation(Path.GetRandomFileName());
        var firstFlowSecondLocation = CreateLocation(Path.GetRandomFileName());
        var firstFlow = CreateFlow(firstFlowFirstLocation, firstFlowSecondLocation);

        var secondFlowFirstLocation = CreateLocation(Path.GetRandomFileName());
        var secondFlowSecondLocation = CreateLocation(Path.GetRandomFileName());
        var secondFlow = CreateFlow(secondFlowFirstLocation, secondFlowSecondLocation);

        var issueSpan = CreateNonEmptySpan();
        var issue = CreateIssue(Path.GetRandomFileName(), isResolved: false, firstFlow, secondFlow);
        SetupSpanCalculator(issue.PrimaryLocation.TextRange, issueSpan);

        var expectedFirstFlowFirstLocationVisualization = new AnalysisIssueLocationVisualization(1, firstFlowFirstLocation);
        var expectedFirstFlowSecondLocationVisualization = new AnalysisIssueLocationVisualization(2, firstFlowSecondLocation);
        var expectedFirstFlowVisualization = new AnalysisIssueFlowVisualization(1, [expectedFirstFlowFirstLocationVisualization, expectedFirstFlowSecondLocationVisualization], firstFlow);

        var expectedSecondFlowFirstLocationVisualization = new AnalysisIssueLocationVisualization(1, secondFlowFirstLocation);
        var expectedSecondFlowSecondLocationVisualization = new AnalysisIssueLocationVisualization(2, secondFlowSecondLocation);
        var expectedSecondFlowVisualization
            = new AnalysisIssueFlowVisualization(2, [expectedSecondFlowFirstLocationVisualization, expectedSecondFlowSecondLocationVisualization], secondFlow);

        var expectedIssueVisualization = new AnalysisIssueVisualization(
            [expectedFirstFlowVisualization, expectedSecondFlowVisualization],
            issue,
            issueSpan,
            []);

        var actualIssueVisualization = testSubject.Convert(issue, textSnapshotMock);

        AssertConversion(expectedIssueVisualization, actualIssueVisualization);
    }

    [TestMethod]
    public void Convert_IssueHasLocationsInDifferentFiles_CalculatesSpanForLocationsInSameFile()
    {
        var issueFilePath = Path.GetRandomFileName();

        var locationInSameFile = CreateLocation(issueFilePath);
        var locationInAnotherFile = CreateLocation(Path.GetRandomFileName());
        var flow = CreateFlow(locationInSameFile, locationInAnotherFile);

        var issueSpan = CreateNonEmptySpan();
        var issue = CreateIssue(issueFilePath, flows: flow);
        SetupSpanCalculator(issue.PrimaryLocation.TextRange, issueSpan);

        var locationSpan = CreateNonEmptySpan();
        SetupSpanCalculator(locationInSameFile.TextRange, locationSpan);

        var expectedLocation1 = new AnalysisIssueLocationVisualization(1, locationInSameFile) { Span = locationSpan };
        var expectedLocation2 = new AnalysisIssueLocationVisualization(2, locationInAnotherFile) { Span = null };
        var expectedFlow = new AnalysisIssueFlowVisualization(1, [expectedLocation1, expectedLocation2], flow);

        var expectedIssueVisualization = new AnalysisIssueVisualization(
            [expectedFlow],
            issue,
            issueSpan,
            []);

        var actualIssueVisualization = testSubject.Convert(issue, textSnapshotMock);

        AssertConversion(expectedIssueVisualization, actualIssueVisualization);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Convert_IssueIsResolved_SetsIsSuppressed(bool isResolved)
    {
        var issue = CreateIssue(Path.GetRandomFileName(), isResolved);

        var result = testSubject.Convert(issue, textSnapshotMock);

        result.Should().NotBeNull();
        result.IsResolved.Should().Be(isResolved);
    }

    private void AssertConversion(IAnalysisIssueVisualization expectedIssueVisualization, IAnalysisIssueVisualization actualIssueVisualization)
    {
        actualIssueVisualization.Issue.Should().Be(expectedIssueVisualization.Issue);

        actualIssueVisualization.Flows.Should().BeEquivalentTo(expectedIssueVisualization.Flows, c => c.WithStrictOrdering());
    }

    private IAnalysisIssue CreateIssue(params IQuickFixBase[] quickFixes)
    {
        var issue = new AnalysisIssue(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            false,
            AnalysisIssueSeverity.Blocker,
            AnalysisIssueType.Bug,
            new Impact(SoftwareQuality.Maintainability, SoftwareQualitySeverity.High),
            CreateLocation(Guid.NewGuid().ToString()),
            null,
            quickFixes
        );

        return issue;
    }

    private IAnalysisIssue CreateIssue(string filePath, bool isResolved = false, params IAnalysisIssueFlow[] flows)
    {
        var issue = new AnalysisIssue(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            isResolved,
            AnalysisIssueSeverity.Blocker,
            AnalysisIssueType.Bug,
            new Impact(SoftwareQuality.Maintainability, SoftwareQualitySeverity.High),
            CreateLocation(filePath),
            flows
        );

        return issue;
    }

    private static IAnalysisIssueFlow CreateFlow(params IAnalysisIssueLocation[] locations)
    {
        return new AnalysisIssueFlow(locations);
    }

    private static IAnalysisIssueLocation CreateLocation(string filePath)
    {
        return new AnalysisIssueLocation(
            Guid.NewGuid().ToString(),
            filePath,
            new TextRange(
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().ToString())
        );
    }

    private SnapshotSpan CreateNonEmptySpan()
    {
        var mockTextSnapshot = new Mock<ITextSnapshot>();
        mockTextSnapshot.SetupGet(x => x.Length).Returns(20);

        return new SnapshotSpan(mockTextSnapshot.Object, new Span(0, 10));
    }

    private void SetupSpanCalculator(ITextRange textRange, SnapshotSpan nonEmptySpan)
    {
        issueSpanCalculatorMock.Setup(x => x.CalculateSpan(textRange, textSnapshotMock)).Returns(nonEmptySpan);
    }

    private IEdit CreateEdit(ITextRange textRange)
    {
        var edit = new Mock<IEdit>();
        edit.Setup(x => x.RangeToReplace).Returns(textRange);

        return edit.Object;
    }
}
