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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Configuration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Core.UnitTests.Configuration;

[TestClass]
public class ConnectedModeFeaturesConfigurationTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<ConnectedModeFeaturesConfiguration, IConnectedModeFeaturesConfiguration>(
            MefTestHelpers.CreateExport<ISonarQubeService>());
    }

    [TestMethod]
    public void IsAcceptTransitionAvailable_NoServerInfo_ReturnsFalse()
    {
        var testSubject = CreateTestSubject(null);

        testSubject.IsAcceptTransitionAvailable().Should().BeFalse();
    }

    [DataRow(1, 2, 3)]
    [DataRow(1231923123, 31312, 0)]
    [DataRow(9, 7, 3)]
    [DataRow(9, 6, 9)]
    [DataRow(0, 0, 0)]
    [DataTestMethod]
    public void IsAcceptTransitionAvailable_AnySonarCloudVersion_ReturnsTrue(int major, int minor, int build)
    {
        var testSubject = CreateTestSubject(new ServerInfo(new Version(major, minor, build), ServerType.SonarCloud));

        testSubject.IsAcceptTransitionAvailable().Should().BeTrue();
    }

    [DataRow(0, 0, 0, false)]
    [DataRow(10, 0, 0, false)]
    [DataRow(10, 5, 0, true)]
    [DataRow(10, 4, 0, true)]
    [DataRow(10, 3, 0, false)]
    [DataRow(12, 0, 0, true)]
    [DataRow(10, 0, 99, false)]
    [DataRow(9, 10, 0, false)]
    [DataTestMethod]
    public void IsAcceptTransitionAvailable_SonarQube_RespectsMinimumVersion(int major, int minor, int build, bool expectedResult)
    {
        var testSubject = CreateTestSubject(new ServerInfo(new Version(major, minor, build), ServerType.SonarQube));

        testSubject.IsAcceptTransitionAvailable().Should().Be(expectedResult);
    }

    [TestMethod]
    public void IsNewCctAvailable_NoServerInfo_ReturnsTrue()
    {
        var testSubject = CreateTestSubject(null);

        testSubject.IsNewCctAvailable().Should().BeTrue();
    }

    [DataRow(1, 2, 3)]
    [DataRow(1231923123, 31312, 0)]
    [DataRow(9, 7, 3)]
    [DataRow(9, 6, 9)]
    [DataRow(0, 0, 0)]
    [DataTestMethod]
    public void IsNewCctAvailable_AnySonarCloudVersion_ReturnsTrue(int major, int minor, int build)
    {
        var testSubject = CreateTestSubject(new ServerInfo(new Version(major, minor, build), ServerType.SonarCloud));

        testSubject.IsNewCctAvailable().Should().BeTrue();
    }

    [DataRow(1, 2, 3, false)]
    [DataRow(9, 7, 3, false)]
    [DataRow(9, 6, 9, false)]
    [DataRow(0, 0, 0, false)]
    [DataRow(10, 1, 0, false)]
    [DataRow(10, 2, 0, true)]
    [DataRow(10, 2, 1, true)]
    [DataRow(11111111, 2, 1, true)]
    [DataTestMethod]
    public void IsNewCctAvailable_SonarQube_RespectsMinimumVersion(int major, int minor, int build, bool expectedResult)
    {
        var testSubject = CreateTestSubject(new ServerInfo(new Version(major, minor, build), ServerType.SonarQube));

        testSubject.IsNewCctAvailable().Should().Be(expectedResult);
    }

    [TestMethod]
    public void IsHotspotsAnalysisEnabled_NoServerInfo_ReturnsFalse()
    {
        var testSubject = CreateTestSubject(null);

        testSubject.IsHotspotsAnalysisEnabled().Should().BeFalse();
    }

    [DataRow(1, 2, 3)]
    [DataRow(1231923123, 31312, 0)]
    [DataRow(9, 7, 3)]
    [DataRow(9, 6, 9)]
    [DataRow(0, 0, 0)]
    [DataTestMethod]
    public void IsHotspotsAnalysisEnabled_AnySonarCloudVersion_ReturnsTrue(int major, int minor, int build)
    {
        var testSubject = CreateTestSubject(new ServerInfo(new Version(major, minor, build), ServerType.SonarCloud));

        testSubject.IsHotspotsAnalysisEnabled().Should().BeTrue();
    }

    [DataRow(1, 2, 3, false)]
    [DataRow(1231923123, 31312, 0, true)]
    [DataRow(9, 7, 3, true)]
    [DataRow(9, 7, 0, true)]
    [DataRow(9, 6, 9, false)]
    [DataRow(0, 0, 0, false)]
    [DataTestMethod]
    public void IsHotspotsAnalysisEnabled_SonarQube_RespectsMinimumVersion(int major, int minor, int build, bool expectedResult)
    {
        var testSubject = CreateTestSubject(new ServerInfo(new Version(major, minor, build), ServerType.SonarQube));

        testSubject.IsHotspotsAnalysisEnabled().Should().Be(expectedResult);
    }

    private IConnectedModeFeaturesConfiguration CreateTestSubject(ServerInfo serverInfo)
    {
        var sonarQubeServiceMock = new Mock<ISonarQubeService>();
        sonarQubeServiceMock.Setup(x => x.GetServerInfo()).Returns(serverInfo);
        return new ConnectedModeFeaturesConfiguration(sonarQubeServiceMock.Object);
    }
}
