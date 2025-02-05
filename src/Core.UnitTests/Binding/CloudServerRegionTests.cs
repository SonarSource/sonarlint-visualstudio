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

using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Core.UnitTests.Binding;

[TestClass]
public class CloudServerRegionTests
{
    [TestMethod]
    public void EuRegion_ExpectedProperties()
    {
        CloudServerRegion.Eu.Name.Should().Be("EU");
        CloudServerRegion.Eu.Url.Should().Be(new Uri("https://sonarcloud.io"));
    }

    [TestMethod]
    public void UsRegion_ExpectedProperties()
    {
        CloudServerRegion.Us.Name.Should().Be("US");
        CloudServerRegion.Us.Url.Should().Be(new Uri("https://us.sonarcloud.io"));
    }

    [TestMethod]
    [DataRow("Eu")]
    [DataRow("EU")]
    [DataRow("eU")]
    [DataRow("eu")]
    [DataRow("eu  ")]
    [DataRow("  eu  ")]
    public void GetRegionByName_EuName_ReturnsEuRegion(string name)
    {
        var result = CloudServerRegion.GetRegionByName(name);

        result.Should().Be(CloudServerRegion.Eu);
    }

    [TestMethod]
    [DataRow("Us")]
    [DataRow("US")]
    [DataRow("uS")]
    [DataRow("us")]
    [DataRow("us   ")]
    [DataRow("    us   ")]
    public void GetRegionByName_UsName_ReturnsEuRegion(string name)
    {
        var result = CloudServerRegion.GetRegionByName(name);

        result.Should().Be(CloudServerRegion.Us);
    }

    [TestMethod]
    [DataRow("    ")]
    [DataRow("")]
    [DataRow(null)]
    public void GetRegionByName_EmptyName_DefaultsToEu(string invalidName)
    {
        var result = CloudServerRegion.GetRegionByName(invalidName);

        result.Should().Be(CloudServerRegion.Eu);
    }

    [TestMethod]
    [DataRow("invalid")]
    [DataRow("ro")]
    public void GetRegionByName_InvalidName_Throws(string invalidName)
    {
        var act = () => CloudServerRegion.GetRegionByName(invalidName);

        act.Should().Throw<ArgumentException>().And.Message.Should().Contain($"Invalid region name: {invalidName}");
    }
}
