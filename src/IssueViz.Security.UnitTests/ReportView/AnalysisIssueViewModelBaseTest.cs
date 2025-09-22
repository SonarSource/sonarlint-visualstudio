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
    [DataRow("931CF6A0-A479-4566-929B-FC6D3AB3D3EA", "serverKey")]
    [DataRow(null, "serverKey")]
    [DataRow(null, null)]
    public void IsSameAnalysisIssue_SameIdAndServerKey_ReturnsTrue(string issueId, string serverKey)
    {
        var analysisIssueVisualization1 = CreateMockedIssue(GetGuid(issueId), serverKey);
        var analysisIssueVisualization2 = CreateMockedIssue(analysisIssueVisualization1.Issue.Id, analysisIssueVisualization1.Issue.IssueServerKey);

        CreateTestSubject(analysisIssueVisualization1).IsSameAnalysisIssue(analysisIssueVisualization2).Should().BeTrue();
    }

    [TestMethod]
    public void IsSameAnalysisIssue_SameReference_ReturnsTrue()
    {
        var analysisIssueVisualization1 = CreateMockedIssue(Guid.NewGuid(), "E2670BAB-4B1E-49C2-8641-7B77CE2A6DBF");

        CreateTestSubject(analysisIssueVisualization1).IsSameAnalysisIssue(analysisIssueVisualization1).Should().BeTrue();
    }

    [TestMethod]
    [DataRow("931CF6A0-A479-4566-929B-FC6D3AB3D3EA", "E2670BAB-4B1E-49C2-8641-7B77CE2A6DBF")]
    [DataRow(null, "931CF6A0-A479-4566-929B-FC6D3AB3D3EA")]
    public void IsSameAnalysisIssue_DifferentId_ReturnsFalse(string issueId1, string issueId2)
    {
        var serverKey = Guid.NewGuid().ToString();
        var analysisIssueVisualization1 = CreateMockedIssue(GetGuid(issueId1), serverKey);
        var analysisIssueVisualization2 = CreateMockedIssue(GetGuid(issueId2), serverKey);

        CreateTestSubject(analysisIssueVisualization1).IsSameAnalysisIssue(analysisIssueVisualization2).Should().BeFalse();
    }

    [TestMethod]
    [DataRow("931CF6A0-A479-4566-929B-FC6D3AB3D3EA", "E2670BAB-4B1E-49C2-8641-7B77CE2A6DBF")]
    [DataRow(null, "931CF6A0-A479-4566-929B-FC6D3AB3D3EA")]
    public void IsSameAnalysisIssue_DifferentServerKey_ReturnsFalse(string serverKey1, string serverKey2)
    {
        var issueId = Guid.NewGuid();
        var analysisIssueVisualization1 = CreateMockedIssue(issueId, serverKey1);
        var analysisIssueVisualization2 = CreateMockedIssue(issueId, serverKey2);

        CreateTestSubject(analysisIssueVisualization1).IsSameAnalysisIssue(analysisIssueVisualization2).Should().BeFalse();
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
        Guid? issueId,
        string serverKey)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisIssueBase = Substitute.For<IAnalysisIssueBase>();
        analysisIssueBase.IssueServerKey.Returns(serverKey);
        analysisIssueBase.Id.Returns(issueId);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);

        return analysisIssueVisualization;
    }

    private static Guid? GetGuid(string issueId) => issueId == null ? null : Guid.Parse(issueId);
}
