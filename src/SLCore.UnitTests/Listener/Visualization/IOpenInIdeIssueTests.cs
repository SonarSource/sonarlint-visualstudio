/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Visualization;

[TestClass]
public class IOpenInIdeIssueTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Issue_ReturnsType_DependingOnIsTaintProperty(bool isTaint)
    {
        const string issueKey = "key123";
        var testSubject = new IssueDetailDto(issueKey, default, default, default, default,
            default, default, default, isTaint, default, default);

        testSubject.Key.Should().BeSameAs(issueKey);
        testSubject.Type.Should().Be(isTaint ? "Taint" : "Issue");
    }

    [TestMethod]
    public void Hotspot_ReturnsHotspotType()
    {
        const string hotspotKey = "key123";
        var testSubject = new HotspotDetailsDto(hotspotKey, default, default, default,
            default, default, default, default, default);

        testSubject.Key.Should().BeSameAs(hotspotKey);
        testSubject.Type.Should().Be("Hotspot");
    }
}
