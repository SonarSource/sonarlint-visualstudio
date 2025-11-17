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

using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Connection.Models;

[TestClass]
public class SonarCloudRegionExtensionsTests
{
    [TestMethod]
    [DynamicData(nameof(GetCoreToExpectedSlCoreRegion))]
    public void ToSlCoreRegion_MapsAsExpects(CloudServerRegion coreRegion, SonarCloudRegion expectedSlCoreRegion)
    {
        var result = coreRegion.ToSlCoreRegion();

        result.Should().Be(expectedSlCoreRegion);
    }

    [TestMethod]
    public void ToSlCoreRegion_UnknownRegion_Throws()
    {
        var act = () => (null as CloudServerRegion).ToSlCoreRegion();

        act.Should().Throw<Exception>();
    }

    [TestMethod]
    [DynamicData(nameof(GetSlCoreToExpectedCoreRegion))]
    public void ToCloudServerRegion_MapsAsExpects(SonarCloudRegion slCoreRegion, CloudServerRegion expectedCoreRegion)
    {
        var result = slCoreRegion.ToCloudServerRegion();

        result.Should().Be(expectedCoreRegion);
    }

    [TestMethod]
    public void ToCloudServerRegion_UnknownRegion_Throws()
    {
        var act = () => ((SonarCloudRegion)666).ToCloudServerRegion();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    public static IEnumerable<object[]> GetCoreToExpectedSlCoreRegion =>
    [
        [CloudServerRegion.Eu, SonarCloudRegion.EU],
        [CloudServerRegion.Us, SonarCloudRegion.US]
    ];

    public static IEnumerable<object[]> GetSlCoreToExpectedCoreRegion =>
    [
        [SonarCloudRegion.EU, CloudServerRegion.Eu],
        [SonarCloudRegion.US, CloudServerRegion.Us]
    ];
}
