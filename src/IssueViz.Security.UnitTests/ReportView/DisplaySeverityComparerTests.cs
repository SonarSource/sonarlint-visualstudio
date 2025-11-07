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

using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class DisplaySeverityComparerTests
{
    private IComparer<DisplaySeverity> comparer;

    [TestInitialize]
    public void Initialize()
    {
        comparer = DisplaySeverityComparer.Instance;
    }

    [TestMethod]
    public void Compare_InfoVsBlocker_ReturnsLessThanZero()
    {
        var result = comparer.Compare(DisplaySeverity.Info, DisplaySeverity.Blocker);
        result.Should().BeLessThan(0);
    }

    [TestMethod]
    public void Compare_BlockerVsInfo_ReturnsGreaterThanZero()
    {
        var result = comparer.Compare(DisplaySeverity.Blocker, DisplaySeverity.Info);
        result.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void Compare_SameSeverity_ReturnsZero()
    {
        var result = comparer.Compare(DisplaySeverity.High, DisplaySeverity.High);
        result.Should().Be(0);
    }

    [TestMethod]
    public void Compare_OrderedSeverities_CorrectOrder()
    {
        var severities = new[]
        {
            DisplaySeverity.Blocker,
            DisplaySeverity.High,
            DisplaySeverity.Medium,
            DisplaySeverity.Low,
            DisplaySeverity.Info
        };
        var sorted = severities.OrderBy(s => s, comparer).ToArray();
        sorted.Should().ContainInOrder(
            DisplaySeverity.Info,
            DisplaySeverity.Low,
            DisplaySeverity.Medium,
            DisplaySeverity.High,
            DisplaySeverity.Blocker
        );
    }

    [TestMethod]
    public void Compare_InvalidSeverity_ThrowsKeyNotFoundException()
    {
        var invalidSeverity = (DisplaySeverity)999;
        var action = () => comparer.Compare(invalidSeverity, DisplaySeverity.Info);
        action.Should().Throw<KeyNotFoundException>();
    }
}
