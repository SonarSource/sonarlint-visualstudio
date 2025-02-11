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

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class ConnectionInfoTests
{
    [TestMethod]
    public void Ctor_SonarCloudWithNoRegion_DefaultsToEu()
    {
        var connectionInfo = new ConnectionInfo("myOrg", ConnectionServerType.SonarCloud);

        connectionInfo.CloudServerRegion.Should().Be(CloudServerRegion.Eu);
    }

    [DataTestMethod]
    [DynamicData(nameof(GetCloudServerRegions), DynamicDataSourceType.Method)]
    public void Ctor_SonarCloudWithRegion_SetsRegion(CloudServerRegion region)
    {
        var connectionInfo = new ConnectionInfo("myOrg", ConnectionServerType.SonarCloud, region);

        connectionInfo.CloudServerRegion.Should().Be(region);
    }

    [DataTestMethod]
    [DynamicData(nameof(GetCloudServerRegions), DynamicDataSourceType.Method)]
    public void Ctor_SonarQubeWithRegion_IgnoresRegion(CloudServerRegion region)
    {
        var connectionInfo = new ConnectionInfo("myOrg", ConnectionServerType.SonarQube, region);

        connectionInfo.CloudServerRegion.Should().BeNull();
    }

    [TestMethod]
    public void FromServerConnection_ShouldReturnConnectionInfoWithSameId()
    {
        var sonarCloudServerConnection = new ServerConnection.SonarCloud("organization");

        var connectionInfo = ConnectionInfo.From(sonarCloudServerConnection);

        connectionInfo.Id.Should().Be("organization");
    }

    [TestMethod]
    public void FromServerConnection_WithSonarCloudForEuRegion_ShouldReturnConnectionInfo()
    {
        var sonarCloudServerConnection = new ServerConnection.SonarCloud("organization");

        var connectionInfo = ConnectionInfo.From(sonarCloudServerConnection);

        connectionInfo.ServerType.Should().Be(ConnectionServerType.SonarCloud);
        connectionInfo.CloudServerRegion.Should().Be(CloudServerRegion.Eu);
    }

    [TestMethod]
    public void FromServerConnection_WithSonarCloudForUsRegion_ShouldReturnConnectionInfo()
    {
        var sonarCloudServerConnection = new ServerConnection.SonarCloud("organization", CloudServerRegion.Us);

        var connectionInfo = ConnectionInfo.From(sonarCloudServerConnection);

        connectionInfo.ServerType.Should().Be(ConnectionServerType.SonarCloud);
        connectionInfo.CloudServerRegion.Should().Be(CloudServerRegion.Us);
    }

    [TestMethod]
    public void FromServerConnection_WithSonarQube_ShouldReturnConnectionInfo()
    {
        var sonarQubeServerConnection = new ServerConnection.SonarQube(new Uri("http://localhost:9000"));

        var connectionInfo = ConnectionInfo.From(sonarQubeServerConnection);

        connectionInfo.ServerType.Should().Be(ConnectionServerType.SonarQube);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ToServerConnection_SonarQube_ReturnsAsExpected(bool enableSmartNotifications)
    {
        var connectionInfo = new ConnectionInfo("http://localhost:9000/", ConnectionServerType.SonarQube);
        var connection = new Connection(connectionInfo, enableSmartNotifications: enableSmartNotifications);

        var serverConnection = connection.ToServerConnection();

        serverConnection.Should().BeOfType<ServerConnection.SonarQube>();
        serverConnection.Id.Should().Be(connectionInfo.Id);
        serverConnection.Settings.IsSmartNotificationsEnabled.Should().Be(enableSmartNotifications);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ToServerConnection_SonarCloud_ReturnsAsExpected(bool enableSmartNotifications)
    {
        var connectionInfo = new ConnectionInfo("myOrg", ConnectionServerType.SonarCloud);
        var connection = new Connection(connectionInfo, enableSmartNotifications: enableSmartNotifications);

        var serverConnection = connection.ToServerConnection();

        serverConnection.Should().BeOfType<ServerConnection.SonarCloud>();
        ((ServerConnection.SonarCloud)serverConnection).OrganizationKey.Should().Be("myOrg");
        serverConnection.Settings.IsSmartNotificationsEnabled.Should().Be(enableSmartNotifications);
    }

    [TestMethod]
    [DynamicData(nameof(GetCloudServerRegions), DynamicDataSourceType.Method)]
    public void ToServerConnection_SonarCloud_MapsRegionCorrectly(CloudServerRegion region)
    {
        var connectionInfo = new ConnectionInfo("myOrg", ConnectionServerType.SonarCloud, region);
        var connection = new Connection(connectionInfo);

        var serverConnection = connection.ToServerConnection();

        var sonarCloud = serverConnection as ServerConnection.SonarCloud;
        sonarCloud.Should().NotBeNull();
        sonarCloud.Region.Should().Be(region);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ToConnection_SonarQube_ReturnsAsExpected(bool isSmartNotificationsEnabled)
    {
        var serverConnection = new ServerConnection.SonarQube(new Uri("http://localhost:9000"), new ServerConnectionSettings(isSmartNotificationsEnabled));

        var connection = serverConnection.ToConnection();

        connection.Info.Id.Should().Be(serverConnection.Id);
        connection.Info.ServerType.Should().Be(ConnectionServerType.SonarQube);
        connection.EnableSmartNotifications.Should().Be(isSmartNotificationsEnabled);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ToConnection_SonarCloud_ReturnsAsExpected(bool isSmartNotificationsEnabled)
    {
        var serverConnection = new ServerConnection.SonarCloud("myOrg", new ServerConnectionSettings(isSmartNotificationsEnabled));

        var connection = serverConnection.ToConnection();

        connection.Info.Id.Should().Be("myOrg");
        connection.Info.ServerType.Should().Be(ConnectionServerType.SonarCloud);
        connection.EnableSmartNotifications.Should().Be(isSmartNotificationsEnabled);
    }

    [TestMethod]
    [DynamicData(nameof(GetCloudServerRegions), DynamicDataSourceType.Method)]
    public void ToConnection_SonarCloud_MapsRegionCorrectly(CloudServerRegion region)
    {
        var serverConnection = new ServerConnection.SonarCloud("myOrg", region: region);

        var connection = serverConnection.ToConnection();

        connection.Info.ServerType.Should().Be(ConnectionServerType.SonarCloud);
        connection.Info.CloudServerRegion.Should().Be(region);
    }

    [TestMethod]
    public void GetServerIdFromConnectionInfo_SonarQube_ReturnsAsExpected()
    {
        var connectionInfo = new ConnectionInfo("http://localhost:9000/", ConnectionServerType.SonarQube);

        var connectionId = connectionInfo.GetServerIdFromConnectionInfo();

        connectionId.Should().Be("http://localhost:9000/");
    }

    [TestMethod]
    [DynamicData(nameof(GetCloudServerRegions), DynamicDataSourceType.Method)]
    public void GetServerIdFromConnectionInfo_SonarCloud_ReturnsAsExpected(CloudServerRegion region)
    {
        var connectionInfo = new ConnectionInfo("myOrg", ConnectionServerType.SonarCloud, region);

        var connectionId = connectionInfo.GetServerIdFromConnectionInfo();

        connectionId.Should().Be(new Uri(region.Url, "organizations/myOrg").ToString());
    }

    public static IEnumerable<object[]> GetCloudServerRegions() => [[CloudServerRegion.Eu], [CloudServerRegion.Us],];
}
