using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Taints;

[TestClass]
public class TaintViewModelTest
{
    [TestMethod]
    public void Ctor_InitializesPropertiesAsExpected()
    {
        var analysisIssueVisualization = CreateMockedTaint("csharp:101",
            Guid.NewGuid(),
            1,
            66,
            "remove todo comment",
            "myClass.cs");

        var testSubject = new TaintViewModel(analysisIssueVisualization);

        testSubject.TaintIssue.Should().Be(analysisIssueVisualization.Issue);
        testSubject.RuleInfo.RuleKey.Should().Be(analysisIssueVisualization.RuleId);
        testSubject.RuleInfo.IssueId.Should().Be(analysisIssueVisualization.IssueId);
        testSubject.Line.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.TextRange.StartLine);
        testSubject.Column.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.TextRange.StartLineOffset);
        testSubject.Title.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.Message);
        testSubject.FilePath.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.FilePath);
        testSubject.Issue.Should().Be(analysisIssueVisualization);
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
        analysisIssueVisualization.RuleId.Returns(ruleId);
        analysisIssueVisualization.IssueId.Returns(issueId);

        var analysisIssueBase = Substitute.For<ITaintIssue>();
        analysisIssueBase.PrimaryLocation.TextRange.StartLine.Returns(startLine);
        analysisIssueBase.PrimaryLocation.TextRange.StartLineOffset.Returns(startLineOffset);
        analysisIssueBase.PrimaryLocation.Message.Returns(message);
        analysisIssueBase.PrimaryLocation.FilePath.Returns(filePath);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);

        return analysisIssueVisualization;
    }
}
