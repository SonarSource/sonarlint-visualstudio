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
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Hotspots;

[TestClass]
public class HotspotViewModelTests
{
    [TestMethod]
    public void Ctor_InitializesPropertiesAsExpected()
    {
        var hotspot = CreateMockedHotspot("csharp:101",
            Guid.NewGuid(),
            1,
            66,
            "remove todo comment",
            "myClass.cs");

        var testSubject = new HotspotViewModel(hotspot);

        testSubject.LocalHotspot.Should().Be(hotspot);
        testSubject.RuleInfo.RuleKey.Should().Be(hotspot.Visualization.RuleId);
        testSubject.RuleInfo.IssueId.Should().Be(hotspot.Visualization.IssueId);
        testSubject.Line.Should().Be(hotspot.Visualization.Issue.PrimaryLocation.TextRange.StartLine);
        testSubject.Column.Should().Be(hotspot.Visualization.Issue.PrimaryLocation.TextRange.StartLineOffset);
        testSubject.Title.Should().Be(hotspot.Visualization.Issue.PrimaryLocation.Message);
        testSubject.FilePath.Should().Be(hotspot.Visualization.Issue.PrimaryLocation.FilePath);
        testSubject.Issue.Should().Be(hotspot.Visualization);
    }

    private static LocalHotspot CreateMockedHotspot(
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

        return new LocalHotspot(analysisIssueVisualization, default, default);
    }
}
