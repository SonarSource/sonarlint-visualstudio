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

using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Filters;

[TestClass]
public class IssueCountHelperTests
{
    [DataRow(0, 0, "No b")]
    [DataRow(0, 1, "0 of 1 a")]
    [DataRow(1, 1, "1 a")]
    [DataRow(0, 2, "0 of 2 b")]
    [DataRow(1, 2, "1 of 2 b")]
    [DataRow(2, 2, "2 b")]
    [DataTestMethod]
    public void FormatString_ReturnsExpectedValue(int filtered, int total, string expectedValue)
    {
        var singular = "a";
        var plural = "b";

        IssueCountHelper.FormatString(filtered, total, singular, plural).Should().BeEquivalentTo(expectedValue);
    }
}
