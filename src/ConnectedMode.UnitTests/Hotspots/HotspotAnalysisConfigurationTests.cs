/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Hotspots;

[TestClass]
public class HotspotAnalysisConfigurationTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<HotspotAnalysisConfiguration, IHotspotAnalysisConfiguration>(
            MefTestHelpers.CreateExport<ISonarQubeService>());
    }

    [TestMethod]
    public void IsEnabled_NoServerInfo_ReturnsFalse()
    {
        var testSubject = CreateTestSubject(null);

        testSubject.IsEnabled().Should().BeFalse();
    }
    
    [DataRow(1, 2, 3)]
    [DataRow(1231923123, 31312, 0)]
    [DataRow(9, 7, 3)]
    [DataRow(9, 6, 9)]
    [DataRow(0, 0, 0)]
    [DataTestMethod]
    public void IsEnabled_AnySonarCloudVersion_ReturnsTrue(int major, int minor, int build)
    {
        var testSubject = CreateTestSubject(new ServerInfo(new Version(major, minor, build), ServerType.SonarCloud));

        testSubject.IsEnabled().Should().BeTrue();
    }

    [DataRow(1, 2, 3, false)]
    [DataRow(1231923123, 31312, 0, true)]
    [DataRow(9, 7, 3, true)]
    [DataRow(9, 7, 0, true)]
    [DataRow(9, 6, 9, false)]
    [DataRow(0, 0, 0, false)]
    [DataTestMethod]
    public void IsEnabled_SonarQube_RespectsMinimumVersion(int major, int minor, int build, bool expectedResult)
    {
        var testSubject = CreateTestSubject(new ServerInfo(new Version(major, minor, build), ServerType.SonarQube));

        testSubject.IsEnabled().Should().Be(expectedResult);
    }

    private IHotspotAnalysisConfiguration CreateTestSubject(ServerInfo serverInfo)
    {
        var sonarQubeServiceMock = new Mock<ISonarQubeService>();
        sonarQubeServiceMock.Setup(x => x.GetServerInfo()).Returns(serverInfo);
        return new HotspotAnalysisConfiguration(sonarQubeServiceMock.Object);
    }
}
