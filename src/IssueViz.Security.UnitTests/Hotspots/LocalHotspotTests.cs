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

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots;

[TestClass]
public class LocalHotspotTests
{
    [TestMethod]
    public void Ctor_VisualizationIsNull_Throws()
    {
        Action test = () => new LocalHotspot(null, default, default);

        test.Should().Throw<ArgumentNullException>().WithMessage("*visualization*");
    }

    [TestMethod]
    [DataRow(HotspotStatus.ToReview)]
    [DataRow(HotspotStatus.Acknowledged)]
    [DataRow(HotspotStatus.Fixed)]
    [DataRow(HotspotStatus.Safe)]
    public void ToLocalHotspot_WithHotspotStatus_CreatesLocalHotspot(HotspotStatus hotspotStatus)
    {
        var analysisIssueVisualization = CreateMockedHotspot(hotspotStatus);

        var result = LocalHotspot.ToLocalHotspot(analysisIssueVisualization);

        result.Visualization.Should().Be(analysisIssueVisualization);
        result.HotspotStatus.Should().Be(hotspotStatus);
    }

    [TestMethod]
    [DataRow(HotspotPriority.High)]
    [DataRow(HotspotPriority.Low)]
    [DataRow(HotspotPriority.Medium)]
    public void ToLocalHotspot_WithHotspotPriority_CreatesLocalHotspot(HotspotPriority hotspotPriority)
    {
        var analysisIssueVisualization = CreateMockedHotspot(hotspotStatus: default, hotspotPriority);

        var result = LocalHotspot.ToLocalHotspot(analysisIssueVisualization);

        result.Visualization.Should().Be(analysisIssueVisualization);
        result.Priority.Should().Be(hotspotPriority);
    }

    [TestMethod]
    public void ToLocalHotspot_NoHotspotPriority_ReturnsHighPriority()
    {
        var analysisIssueVisualization = CreateMockedHotspot(hotspotStatus: default, hotspotPriority: null);

        var result = LocalHotspot.ToLocalHotspot(analysisIssueVisualization);

        result.Visualization.Should().Be(analysisIssueVisualization);
        result.Priority.Should().Be(HotspotPriority.High);
    }

    private IAnalysisIssueVisualization CreateMockedHotspot(HotspotStatus hotspotStatus, HotspotPriority? hotspotPriority = null)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisHotspotIssue = Substitute.For<IAnalysisHotspotIssue>();
        analysisHotspotIssue.HotspotStatus.Returns(hotspotStatus);
        analysisHotspotIssue.HotspotPriority.Returns(hotspotPriority);
        analysisIssueVisualization.Issue.Returns(analysisHotspotIssue);

        return analysisIssueVisualization;
    }
}
