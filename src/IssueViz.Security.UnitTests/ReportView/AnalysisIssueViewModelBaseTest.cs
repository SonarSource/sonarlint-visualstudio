/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class AnalysisIssueViewModelBaseTest
{
    [TestMethod]
    public void Ctor_InitializesPropertiesAsExpected()
    {
        var analysisIssueVisualization = CreateMockedIssue("csharp:101",
            Guid.NewGuid(),
            1,
            66,
            "remove todo comment",
            "myClass.cs");

        var testSubject = CreateTestSubject(analysisIssueVisualization);

        testSubject.Issue.Should().Be(analysisIssueVisualization);
        testSubject.RuleInfo.RuleKey.Should().Be(analysisIssueVisualization.RuleId);
        testSubject.RuleInfo.IssueId.Should().Be(analysisIssueVisualization.IssueId);
        testSubject.Line.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.TextRange.StartLine);
        testSubject.Column.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.TextRange.StartLineOffset);
        testSubject.Title.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.Message);
        testSubject.FilePath.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.FilePath);
        testSubject.Issue.Should().Be(analysisIssueVisualization);
    }

    [TestMethod]
    public void HasSecondaryLocations_NoFlows_ReturnsFalse()
    {
        var issue = CreateMockedIssue(Guid.NewGuid(), "any");
        issue.Flows.Returns([]);

        CreateTestSubject(issue).HasSecondaryLocations.Should().BeFalse();
    }

    [TestMethod]
    public void HasSecondaryLocations_HasFlows_ReturnsTrue()
    {
        var issue = CreateMockedIssue(Guid.NewGuid(), "any");
        var flow = Substitute.For<IAnalysisIssueFlowVisualization>();
        issue.Flows.Returns([flow]);
        var location = Substitute.For<IAnalysisIssueLocationVisualization>();
        flow.Locations.Returns([location]);

        CreateTestSubject(issue).HasSecondaryLocations.Should().BeTrue();
    }

    [DataRow("key", true)]
    [DataRow(null, false)]
    [DataTestMethod]
    public void IsServerIssue_ReturnsBasedOnServerKey(string serverKey, bool isServerIssue)
    {
        var issue = CreateMockedIssue(Guid.NewGuid(), serverKey);

        CreateTestSubject(issue).IsServerIssue.Should().Be(isServerIssue);
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void IsOnNewCode_ReturnsBasedOnIssue(bool isOnNewCode)
    {
        var issue = CreateMockedIssue(Guid.NewGuid(), "any");
        issue.IsOnNewCode.Returns(isOnNewCode);

        CreateTestSubject(issue).IsOnNewCode.Should().Be(isOnNewCode);
    }

    /// <summary>
    /// To prevent adding the tests in all the subclasses we test the base class via any of its child classes.
    /// </summary>
    private static AnalysisIssueViewModelBase CreateTestSubject(IAnalysisIssueVisualization analysisIssueVisualization) =>
        new HotspotViewModel(new LocalHotspot(analysisIssueVisualization, default, default));

    private static IAnalysisIssueVisualization CreateMockedIssue(
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

        var analysisIssueBase = Substitute.For<IAnalysisIssueBase>();
        analysisIssueBase.PrimaryLocation.TextRange.StartLine.Returns(startLine);
        analysisIssueBase.PrimaryLocation.TextRange.StartLineOffset.Returns(startLineOffset);
        analysisIssueBase.PrimaryLocation.Message.Returns(message);
        analysisIssueBase.PrimaryLocation.FilePath.Returns(filePath);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);

        return analysisIssueVisualization;
    }

    private static IAnalysisIssueVisualization CreateMockedIssue(
        Guid issueId,
        string serverKey)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisIssueBase = Substitute.For<IAnalysisIssueBase>();
        analysisIssueBase.IssueServerKey.Returns(serverKey);
        analysisIssueBase.Id.Returns(issueId);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);

        return analysisIssueVisualization;
    }

    private static Guid GetGuid(string issueId) => issueId == null ? default : Guid.Parse(issueId);
}
