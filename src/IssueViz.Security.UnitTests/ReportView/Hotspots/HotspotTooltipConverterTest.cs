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

using System.Globalization;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Hotspots;

[TestClass]
public class HotspotTooltipConverterTest
{
    private HotspotTooltipConverter testSubject;

    [TestInitialize]
    public void Initialize() => testSubject = new HotspotTooltipConverter();

    [TestMethod]
    public void Convert_ReturnsNull_WhenInvalidNumberOfParametersUsed()
    {
        var result = testSubject.Convert([default, default, default, default], null, null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_ReturnsNull_WhenLocalHotspotNotProvided()
    {
        var result = testSubject.Convert([null, true], null, null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_ReturnsNull_WhenIsCloudNotProvided()
    {
        var result = testSubject.Convert([CreateMockedHotspot(serverKey: null, default), null], null, null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    [DynamicData(nameof(GetAllHotspotPriorities))]
    public void Convert_ServerHotspotForCloud_ReturnsExpectedResource(HotspotPriority priority)
    {
        var serverHotspot = CreateMockedHotspot(serverKey: Guid.NewGuid().ToString(), priority);
        const bool isCloud = true;
        var expectedResource = string.Format(Resources.ServerHotspotTooltip, serverHotspot.Priority, CoreStrings.SonarQubeCloudProductName);

        var result = testSubject.Convert([serverHotspot, isCloud], null, null, CultureInfo.InvariantCulture);

        result.Should().Be(expectedResource);
    }

    [TestMethod]
    [DynamicData(nameof(GetAllHotspotPriorities))]
    public void Convert_ServerHotspotForServer_ReturnsExpectedResource(HotspotPriority priority)
    {
        var serverHotspot = CreateMockedHotspot(serverKey: Guid.NewGuid().ToString(), priority);
        const bool isCloud = false;
        var expectedResource = string.Format(Resources.ServerHotspotTooltip, serverHotspot.Priority, CoreStrings.SonarQubeServerProductName);

        var result = testSubject.Convert([serverHotspot, isCloud], null, null, CultureInfo.InvariantCulture);

        result.Should().Be(expectedResource);
    }

    [TestMethod]
    [DynamicData(nameof(GetAllHotspotPriorities))]
    public void Convert_LocalHotspot_ReturnsExpectedResource(HotspotPriority priority)
    {
        var serverHotspot = CreateMockedHotspot(serverKey: null, priority);
        const bool isCloud = true;
        var expectedResource = string.Format(Resources.LocalHotspotTooltip, serverHotspot.Priority);

        var result = testSubject.Convert([serverHotspot, isCloud], null, null, CultureInfo.InvariantCulture);

        result.Should().Be(expectedResource);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Convert_LocalHotspot_IgnoresServerValue(bool isCloud)
    {
        var serverHotspot = CreateMockedHotspot(serverKey: null, HotspotPriority.High);
        var expectedResource = string.Format(Resources.LocalHotspotTooltip, serverHotspot.Priority);

        var result = testSubject.Convert([serverHotspot, isCloud], null, null, CultureInfo.InvariantCulture);

        result.Should().Be(expectedResource);
    }

    [TestMethod]
    public void ConvertBack_ThrowsException()
    {
        var act = () => testSubject.ConvertBack(null, null, null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotImplementedException>();
    }

    private static LocalHotspot CreateMockedHotspot(string serverKey, HotspotPriority priority)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        analysisIssueVisualization.Issue.IssueServerKey.Returns(serverKey);
        return new LocalHotspot(analysisIssueVisualization, priority, default);
    }

    public static object[][] GetAllHotspotPriorities =>
        Enum.GetValues(typeof(HotspotPriority)).Cast<object>()
            .Select(priority => new[] { priority })
            .ToArray();
}
