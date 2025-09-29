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
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Hotspots;

[TestClass]
public class HotspotViewModelTests
{
    private readonly LocalHotspot hotspot = CreateMockedHotspot("csharp:101",
        Guid.NewGuid(),
        1,
        66,
        "remove todo comment",
        "myClass.cs");
    private HotspotViewModel testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new HotspotViewModel(hotspot);

    [TestMethod]
    public void Ctor_InitializesPropertiesAsExpected()
    {
        testSubject.LocalHotspot.Should().Be(hotspot);
        testSubject.RuleInfo.RuleKey.Should().Be(hotspot.Visualization.RuleId);
        testSubject.RuleInfo.IssueId.Should().Be(hotspot.Visualization.IssueId);
        testSubject.Line.Should().Be(hotspot.Visualization.Issue.PrimaryLocation.TextRange.StartLine);
        testSubject.Column.Should().Be(hotspot.Visualization.Issue.PrimaryLocation.TextRange.StartLineOffset);
        testSubject.Title.Should().Be(hotspot.Visualization.Issue.PrimaryLocation.Message);
        testSubject.FilePath.Should().Be(hotspot.Visualization.Issue.PrimaryLocation.FilePath);
        testSubject.Issue.Should().Be(hotspot.Visualization);
        testSubject.IssueType.Should().Be(IssueType.SecurityHotspot);
    }

    [DataTestMethod]
    [DataRow(HotspotPriority.Low, DisplaySeverity.Low)]
    [DataRow(HotspotPriority.Medium, DisplaySeverity.Medium)]
    [DataRow(HotspotPriority.High, DisplaySeverity.High)]
    public void Ctor_ReturnsCorrectDisplaySeverity(HotspotPriority hotspotPriority, DisplaySeverity expectedSeverity)
    {
        var mockedHotspot = CreateMockedHotspot(hotspotPriority);

        var hotspotViewModel = new HotspotViewModel(mockedHotspot);

        hotspotViewModel.DisplaySeverity.Should().Be(expectedSeverity);
    }

    [DataTestMethod]
    public void Ctor_UnknownSeverity_ReturnsInfo()
    {
        var mockedHotspot = CreateMockedHotspot((HotspotPriority)666);

        var hotspotViewModel = new HotspotViewModel(mockedHotspot);

        hotspotViewModel.DisplaySeverity.Should().Be(DisplaySeverity.Info);
    }

    [DataTestMethod]
    [DataRow(HotspotStatus.ToReview, DisplayStatus.Open)]
    [DataRow(HotspotStatus.Acknowledged, DisplayStatus.Open)]
    [DataRow(HotspotStatus.Fixed, DisplayStatus.Resolved)]
    [DataRow(HotspotStatus.Safe, DisplayStatus.Resolved)]
    public void Ctor_ReturnsCorrectStatus(HotspotStatus hotspotStatus, DisplayStatus expectedStatus)
    {
        var mockedHotspot = CreateMockedHotspot(default, hotspotStatus);

        var hotspotViewModel = new HotspotViewModel(mockedHotspot);

        hotspotViewModel.Status.Should().Be(expectedStatus);
    }

    [DataTestMethod]
    public void Ctor_UnknownStatus_DefaultsToOpen()
    {
        var mockedHotspot = CreateMockedHotspot(default, (HotspotStatus)666);

        var hotspotViewModel = new HotspotViewModel(mockedHotspot);

        hotspotViewModel.Status.Should().Be(DisplayStatus.Open);
    }

    [TestMethod]
    public void ExistsOnServer_LocalHotspot_ReturnsFalse()
    {
        hotspot.Visualization.Issue.IssueServerKey.Returns((string)null);

        testSubject.ExistsOnServer.Should().BeFalse();
    }

    [TestMethod]
    public void ExistsOnServer_ServerHotspot_ReturnsTrue()
    {
        hotspot.Visualization.Issue.IssueServerKey.Returns(Guid.NewGuid().ToString());

        testSubject.ExistsOnServer.Should().BeTrue();
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

    private static LocalHotspot CreateMockedHotspot(HotspotPriority priority, HotspotStatus status = default)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisIssueBase = Substitute.For<IAnalysisIssueBase>();
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);

        return new LocalHotspot(analysisIssueVisualization, priority, status);
    }
}
