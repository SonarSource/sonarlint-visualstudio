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
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.HotspotsList;

[TestClass]
public class HotspotStatusToLocalizedStatusConverterTest
{
    private HotspotStatusToLocalizedStatusConverter testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new HotspotStatusToLocalizedStatusConverter();

    [TestMethod]
    [DataRow(HotspotStatus.ToReview, "To Review")]
    [DataRow(HotspotStatus.Acknowledged, "Acknowledged")]
    [DataRow(HotspotStatus.Fixed, "Fixed")]
    [DataRow(HotspotStatus.Safe, "Safe")]
    public void Convert_StatusProvided_ConvertsAsExpected(HotspotStatus status, string expected)
    {
        var result = testSubject.Convert(status, null, null, null);

        result.Should().Be(expected);
    }

    [TestMethod]
    public void Convert_InvalidHotspotStatus_ReturnsEmptyString()
    {
        var result = testSubject.Convert((HotspotStatus)13, null, null, null);

        result.Should().Be(string.Empty);
    }

    [TestMethod]
    public void Convert_NoHotspotStatusProvided_ReturnsEmptyString()
    {
        var result = testSubject.Convert(null, null, null, null);

        result.Should().Be(string.Empty);
    }

    [TestMethod]
    public void ConvertBack_NotImplementedException()
    {
        Action act = () => testSubject.ConvertBack(null, null, null, null);

        act.Should().Throw<NotImplementedException>();
    }
}
