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

using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class ServerConnectionModelMapperTest
{
    private ServerConnectionModelMapper testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new ServerConnectionModelMapper();
    }

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<ServerConnectionModelMapper, IServerConnectionModelMapper>();
    }

    [TestMethod]
    public void Mef_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ServerConnectionModelMapper>();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetServerConnection_SonarCloud_ReturnsSonarCloudConnection(bool isSmartNotificationsEnabled)
    {
        var connectionsModel = GetSonarCloudJsonModel("myOrg", isSmartNotificationsEnabled);

        var serverConnection = testSubject.GetServerConnection(connectionsModel);

        IsExpectedSonarCloudConnection(serverConnection, connectionsModel);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetServerConnection_SonarQube_ReturnsSonarQubeConnection(bool isSmartNotificationsEnabled)
    {
        var connectionsModel = GetSonarQubeJsonModel("http://localhost:9000", isSmartNotificationsEnabled);

        var serverConnection = testSubject.GetServerConnection(connectionsModel);

        IsExpectedSonarQubeConnection(serverConnection, connectionsModel);
    }

    [TestMethod]
    public void GetServerConnection_InvalidServerType_ThrowsException()
    {
        var connectionsModel = new ServerConnectionJsonModel { ServerType = (ConnectionServerType)66 };

        Action act = () => testSubject.GetServerConnection(connectionsModel);
        
        act.Should().Throw<InvalidOperationException>($"Invalid {nameof(ConnectionServerType)} {connectionsModel.ServerType}");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetServerConnectionsListJsonModel_OneSonarCloudConnection_ReturnsServerConnectionModelForSonarCloud(bool isSmartNotifications)
    {
        var sonarCloud = new ServerConnection.SonarCloud("myOrg", new ServerConnectionSettings(isSmartNotifications));

        var model = testSubject.GetServerConnectionsListJsonModel([sonarCloud]);

        model.Should().NotBeNull();
        model.ServerConnections.Count.Should().Be(1);
        model.ServerConnections[0].Should().BeEquivalentTo(new ServerConnectionJsonModel
        {
            Id = sonarCloud.Id,
            OrganizationKey = sonarCloud.OrganizationKey,
            ServerType = ConnectionServerType.SonarCloud,
            Settings = sonarCloud.Settings,
            ServerUri = null
        });
    }

    [TestMethod]
    public void GetServerConnectionsListJsonModel_OneSonarCloudConnectionWithNullSettings_ThrowsExceptions()
    {
        var sonarCloud = new ServerConnection.SonarCloud("myOrg")
        {
            Settings = null
        };

        Action act = () => testSubject.GetServerConnectionsListJsonModel([sonarCloud]);

        act.Should().Throw<InvalidOperationException>($"{nameof(ServerConnection.Settings)} can not be null");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetServerConnectionsListJsonModel_OneSonarQubeConnection_ReturnsServerConnectionModelForSonarQube(bool isSmartNotifications)
    {
        var sonarQube = new ServerConnection.SonarQube(new Uri("http://localhost"), new ServerConnectionSettings(isSmartNotifications));

        var model = testSubject.GetServerConnectionsListJsonModel([sonarQube]);

        model.Should().NotBeNull();
        model.ServerConnections.Count.Should().Be(1);
        model.ServerConnections[0].Should().BeEquivalentTo(new ServerConnectionJsonModel
        {
            Id = sonarQube.Id,
            OrganizationKey = null,
            ServerUri = sonarQube.ServerUri.ToString(),
            ServerType = ConnectionServerType.SonarQube,
            Settings = sonarQube.Settings
        });
    }

    [TestMethod]
    public void GetServerConnectionsListJsonModel_OneSonarQubeConnectionWithNullSettings_ThrowsExceptions()
    {
        var sonarQube = new ServerConnection.SonarQube(new Uri("http://localhost"))
        {
            Settings = null
        };

        Action act = () => testSubject.GetServerConnectionsListJsonModel([sonarQube]);

        act.Should().Throw<InvalidOperationException>($"{nameof(ServerConnection.Settings)} can not be null");
    }

    [TestMethod]
    public void GetServerConnectionsListJsonModel_NoConnection_ReturnsServerConnectionModelWithNoConnection()
    {
        var model = testSubject.GetServerConnectionsListJsonModel([]);

        model.Should().NotBeNull();
        model.ServerConnections.Should().BeEmpty();
    }

    private static ServerConnectionJsonModel GetSonarCloudJsonModel(string id, bool isSmartNotificationsEnabled = false)
    {
        return new ServerConnectionJsonModel
        {
            Id = id,
            OrganizationKey = id,
            ServerType = ConnectionServerType.SonarCloud,
            Settings = new ServerConnectionSettings(isSmartNotificationsEnabled)
        };
    }

    private static ServerConnectionJsonModel GetSonarQubeJsonModel(string id, bool isSmartNotificationsEnabled = false)
    {
        return new ServerConnectionJsonModel
        {
            Id = id,
            ServerUri = id,
            ServerType = ConnectionServerType.SonarQube,
            Settings = new ServerConnectionSettings(isSmartNotificationsEnabled)
        };
    }

    private static void IsExpectedSonarCloudConnection(ServerConnection serverConnection, ServerConnectionJsonModel connectionsModel)
    {
        serverConnection.Should().BeOfType<ServerConnection.SonarCloud>();
        serverConnection.Id.Should().Be(serverConnection.Id);
        serverConnection.Settings.Should().NotBeNull();
        serverConnection.Settings.IsSmartNotificationsEnabled.Should().Be(connectionsModel.Settings.IsSmartNotificationsEnabled);
        ((ServerConnection.SonarCloud)serverConnection).OrganizationKey.Should().Be(connectionsModel.OrganizationKey);
    }

    private static void IsExpectedSonarQubeConnection(ServerConnection serverConnection, ServerConnectionJsonModel connectionsModel)
    {
        serverConnection.Should().BeOfType<ServerConnection.SonarQube>();
        serverConnection.Id.Should().Be(serverConnection.Id);
        serverConnection.Settings.Should().NotBeNull();
        serverConnection.Settings.IsSmartNotificationsEnabled.Should().Be(connectionsModel.Settings.IsSmartNotificationsEnabled);
        ((ServerConnection.SonarQube)serverConnection).ServerUri.Should().Be(connectionsModel.ServerUri);
    }
}
