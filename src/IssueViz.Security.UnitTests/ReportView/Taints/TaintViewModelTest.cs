using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Taints;

[TestClass]
public class TaintViewModelTest
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Ctor_InitializesPropertiesAsExpected(bool isSolutionLevelTaintDisplay)
    {
        var analysisIssueVisualization = CreateMockedTaint("csharp:101",
            Guid.NewGuid(),
            1,
            66,
            "remove todo comment",
            @"C:\a\myClass.cs");

        var testSubject = new TaintViewModel(analysisIssueVisualization, isSolutionLevelTaintDisplay);

        testSubject.TaintIssue.Should().Be(analysisIssueVisualization.Issue);
        testSubject.RuleId.Should().Be(analysisIssueVisualization.SonarRuleId.Id);
        testSubject.Id.Should().Be(analysisIssueVisualization.IssueId);
        testSubject.Line.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.TextRange.StartLine);
        testSubject.Column.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.TextRange.StartLineOffset);
        testSubject.Title.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.Message);
        testSubject.FilePath.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.FilePath);
        testSubject.FileDisplayName.Should().Be("myClass.cs");
        testSubject.IsSolutionLevelTaintDisplay.Should().Be(isSolutionLevelTaintDisplay);
        testSubject.Issue.Should().Be(analysisIssueVisualization);
        testSubject.IssueType.Should().Be(IssueType.TaintVulnerability);
        testSubject.Status.Should().Be(DisplayStatus.Open);
        testSubject.IsResolved.Should().BeFalse();
    }

    [TestMethod]
    public void IsSolutionLevelTaintDisplay_DefaultValue_False()
    {
        var analysisIssueVisualization = CreateMockedTaint("csharp:101",
            Guid.NewGuid(),
            1,
            66,
            "remove todo comment",
            @"C:\a\myClass.cs");

        var testSubject = new TaintViewModel(analysisIssueVisualization);

        testSubject.IsSolutionLevelTaintDisplay.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow(AnalysisIssueSeverity.Info, DisplaySeverity.Info)]
    [DataRow(AnalysisIssueSeverity.Minor, DisplaySeverity.Low)]
    [DataRow(AnalysisIssueSeverity.Major, DisplaySeverity.Medium)]
    [DataRow(AnalysisIssueSeverity.Critical, DisplaySeverity.High)]
    [DataRow(AnalysisIssueSeverity.Blocker, DisplaySeverity.Blocker)]
    public void Ctor_TaintHasSeverity_ReturnsCorrectDisplaySeverity(AnalysisIssueSeverity dependencyRiskSeverity, DisplaySeverity expectedSeverity)
    {
        var taintIssue = CreateMockedTaint(dependencyRiskSeverity);

        var testSubject = new TaintViewModel(taintIssue);

        testSubject.DisplaySeverity.Should().Be(expectedSeverity);
    }

    [DataTestMethod]
    [DataRow(true, DisplayStatus.Resolved)]
    [DataRow(false, DisplayStatus.Open)]
    public void Ctor_Status_ReturnsCorrectValueBasedOnIsResloved(bool isResolved, DisplayStatus expectedStatus)
    {
        var taintIssue = CreateMockedTaint(isResolved: isResolved);

        var testSubject = new TaintViewModel(taintIssue);

        testSubject.Status.Should().Be(expectedStatus);
        testSubject.IsResolved.Should().Be(isResolved);
    }

    [DataTestMethod]
    public void Ctor_TaintHasSeverity_UnknownSeverity_ReturnsInfo()
    {
        var taintIssue = CreateMockedTaint((AnalysisIssueSeverity)666);

        var testSubject = new TaintViewModel(taintIssue);

        testSubject.DisplaySeverity.Should().Be(DisplaySeverity.Info);
    }

    [DataTestMethod]
    [DataRow(SoftwareQualitySeverity.Info, DisplaySeverity.Info)]
    [DataRow(SoftwareQualitySeverity.Low, DisplaySeverity.Low)]
    [DataRow(SoftwareQualitySeverity.Medium, DisplaySeverity.Medium)]
    [DataRow(SoftwareQualitySeverity.High, DisplaySeverity.High)]
    [DataRow(SoftwareQualitySeverity.Blocker, DisplaySeverity.Blocker)]
    public void Ctor_TaintHasSoftwareQualitySeverity_ReturnsCorrectDisplaySeverity(SoftwareQualitySeverity softwareQualitySeverity, DisplaySeverity expectedSeverity)
    {
        var taintIssue = CreateMockedTaint(severity: AnalysisIssueSeverity.Info, softwareQualitySeverity: softwareQualitySeverity);

        var testSubject = new TaintViewModel(taintIssue);

        testSubject.DisplaySeverity.Should().Be(expectedSeverity);
    }

    [DataTestMethod]
    public void Ctor_TaintHasSoftwareQualitySeverity_UnknownSeverity_ReturnsInfo()
    {
        var taintIssue = CreateMockedTaint(softwareQualitySeverity: (SoftwareQualitySeverity)666);

        var testSubject = new TaintViewModel(taintIssue);

        testSubject.DisplaySeverity.Should().Be(DisplaySeverity.Info);
    }

    [DataTestMethod]
    public void Ctor_TaintHasNoSoftwareQualitySeverityAndNoSeverity_ReturnsInfo()
    {
        var taintIssue = CreateMockedTaint(severity: null, softwareQualitySeverity: null);

        var testSubject = new TaintViewModel(taintIssue);

        testSubject.DisplaySeverity.Should().Be(DisplaySeverity.Info);
    }

    private static IAnalysisIssueVisualization CreateMockedTaint(
        string ruleId,
        Guid issueId,
        int startLine,
        int startLineOffset,
        string message,
        string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        SonarCompositeRuleId.TryParse(ruleId, out var sonarRuleId).Should().BeTrue();
        analysisIssueVisualization.SonarRuleId.Returns(sonarRuleId);
        analysisIssueVisualization.IssueId.Returns(issueId);

        var analysisIssueBase = Substitute.For<ITaintIssue>();
        analysisIssueBase.PrimaryLocation.TextRange.StartLine.Returns(startLine);
        analysisIssueBase.PrimaryLocation.TextRange.StartLineOffset.Returns(startLineOffset);
        analysisIssueBase.PrimaryLocation.Message.Returns(message);
        analysisIssueBase.PrimaryLocation.FilePath.Returns(filePath);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);

        return analysisIssueVisualization;
    }

    private static IAnalysisIssueVisualization CreateMockedTaint(AnalysisIssueSeverity? severity = null, SoftwareQualitySeverity? softwareQualitySeverity = null, bool isResolved = false)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var taintIssue = Substitute.For<ITaintIssue>();
        taintIssue.Severity.Returns(severity);
        taintIssue.HighestSoftwareQualitySeverity.Returns(softwareQualitySeverity);
        taintIssue.IsResolved.Returns(isResolved);
        analysisIssueVisualization.IsResolved.Returns(isResolved);
        analysisIssueVisualization.Issue.Returns(taintIssue);

        return analysisIssueVisualization;
    }
}
