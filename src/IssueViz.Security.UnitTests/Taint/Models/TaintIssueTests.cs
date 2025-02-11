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
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint.Models;

[TestClass]
public class TaintIssueTests
{
    [TestMethod]
    public void Ctor_NullLocation_ArgumentNullException()
    {
        Action act = () => new TaintIssue(Guid.Empty, "issue key", "rule key",
            null,
            AnalysisIssueSeverity.Major, SoftwareQualitySeverity.High, DateTimeOffset.MinValue, null, null);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("primaryLocation");
    }

    [TestMethod]
    public void Ctor_PropertiesSet()
    {
        var created = DateTimeOffset.Parse("2001-01-31T01:02:03+0200");
        var id = Guid.NewGuid();
        var issue = new TaintIssue(id, "issue key", "rule key",
            new AnalysisIssueLocation("message", "local-path.cpp", new TextRange(1, 2, 3, 4, "hash")),
            AnalysisIssueSeverity.Major, SoftwareQualitySeverity.High, created, null, "contextKey");

        issue.Id.Should().Be(id);
        issue.IssueServerKey.Should().Be("issue key");
        issue.RuleKey.Should().Be("rule key");
        issue.Severity.Should().Be(AnalysisIssueSeverity.Major);
        issue.CreationTimestamp.Should().Be(created);
        issue.RuleDescriptionContextKey.Should().Be("contextKey");
        issue.IsResolved.Should().BeFalse();

        issue.PrimaryLocation.FilePath.Should().Be("local-path.cpp");
        issue.PrimaryLocation.Message.Should().Be("message");
        issue.PrimaryLocation.TextRange.StartLine.Should().Be(1);
        issue.PrimaryLocation.TextRange.EndLine.Should().Be(2);
        issue.PrimaryLocation.TextRange.StartLineOffset.Should().Be(3);
        issue.PrimaryLocation.TextRange.EndLineOffset.Should().Be(4);
        issue.PrimaryLocation.TextRange.LineHash.Should().Be("hash");
    }

    [TestMethod]
    public void Ctor_NoFlows_EmptyFlows()
    {
        IReadOnlyList<IAnalysisIssueFlow> flows = null;
        var issue = new TaintIssue(Guid.NewGuid(), "issue key", "rule key",
            new AnalysisIssueLocation("message", "local-path.cpp", new TextRange(1, 2, 3, 4, "hash")),
            AnalysisIssueSeverity.Major, SoftwareQualitySeverity.High, DateTimeOffset.MinValue, flows, null);

        issue.Flows.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_HasFlows_CorrectFlows()
    {
        var flows = new[] { Substitute.For<IAnalysisIssueFlow>(), Substitute.For<IAnalysisIssueFlow>() };
        var issue = new TaintIssue(Guid.NewGuid(), "issue key", "rule key",
            new AnalysisIssueLocation("message", "local-path.cpp", new TextRange(1, 2, 3, 4, "hash")),
            AnalysisIssueSeverity.Major, SoftwareQualitySeverity.High, DateTimeOffset.MinValue, flows, null);

        issue.Flows.Should().BeEquivalentTo(flows);
    }

    [TestMethod]
    public void Ctor_HasNoStandardAndNoCCTSeverity_Throws()
    {
        AnalysisIssueSeverity? analysisIssueSeverity = null;
        SoftwareQualitySeverity? highestSoftwareQualitySeverity = null;
        var act = () => new TaintIssue(Guid.NewGuid(),
            "issue key",
            "rule key",
            new AnalysisIssueLocation("msg", "local-path.cpp", new TextRange(1, 2, 3, 4, "hash")),
            analysisIssueSeverity,
            highestSoftwareQualitySeverity,
            DateTimeOffset.Now,
            [],
            null);

        act.Should().Throw<ArgumentException>().WithMessage(string.Format(TaintResources.TaintIssue_SeverityUndefined, "issue key"));
    }
}
