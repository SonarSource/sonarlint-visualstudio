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
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Helpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static SonarLint.VisualStudio.Core.Binding.ServerConnection;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class ServerConnectionsRepositoryAdapterTests
{
    private IServerConnectionsRepository serverConnectionsRepository;
    private ServerConnectionsRepositoryAdapter testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        serverConnectionsRepository = Substitute.For<IServerConnectionsRepository>();
        testSubject = new ServerConnectionsRepositoryAdapter(serverConnectionsRepository);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
        => MefTestHelpers.CheckTypeCanBeImported<ServerConnectionsRepositoryAdapter, IServerConnectionsRepositoryAdapter>(MefTestHelpers.CreateExport<IServerConnectionsRepository>());

    [TestMethod]
    public void TryGetAllConnections_CallServerConnectionsRepository()
    {
        MockServerConnections([]);

        testSubject.TryGetAllConnections(out var connections);

        serverConnectionsRepository.Received(1).TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>());
        connections.Should().BeEmpty();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryGetAllConnections_HasOneSonarCloudConnection_ReturnsOneMappedConnection(bool isSmartNotificationsEnabled)
    {
        var sonarCloud = CreateSonarCloudServerConnection(isSmartNotificationsEnabled);
        MockServerConnections([sonarCloud]);

        testSubject.TryGetAllConnections(out var connections);

        connections.Should().BeEquivalentTo([new Connection(new ConnectionInfo(sonarCloud.Id, ConnectionServerType.SonarCloud), isSmartNotificationsEnabled)]);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryGetAllConnections_HasOneSonarQubeConnection_ReturnsOneMappedConnection(bool isSmartNotificationsEnabled)
    {
        var sonarQube = CreateSonarQubeServerConnection(isSmartNotificationsEnabled);
        MockServerConnections([sonarQube]);

        testSubject.TryGetAllConnections(out var connections);

        connections.Should().BeEquivalentTo([new Connection(new ConnectionInfo(sonarQube.Id, ConnectionServerType.SonarQube), isSmartNotificationsEnabled)]);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryGetAllConnections_ReturnsStatusFromSlCore(bool expectedStatus)
    {
        var sonarCloud = CreateSonarCloudServerConnection();
        MockServerConnections([sonarCloud], succeeded:expectedStatus);

        var succeeded = testSubject.TryGetAllConnections(out _);

        succeeded.Should().Be(expectedStatus);
    }

    [TestMethod]
    public void TryGetAllConnectionsInfo_CallServerConnectionsRepository()
    {
        MockServerConnections([]);

        testSubject.TryGetAllConnectionsInfo(out var connections);

        serverConnectionsRepository.Received(1).TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>());
        connections.Should().BeEmpty();
    }

    [TestMethod]
    public void TryGetAllConnectionsInfo_HasOneSonarCloudConnection_ReturnsOneMappedConnection()
    {
        var sonarCloud = CreateSonarCloudServerConnection();
        MockServerConnections([sonarCloud]);

        testSubject.TryGetAllConnectionsInfo(out var connections);

        connections.Should().BeEquivalentTo([new ConnectionInfo(sonarCloud.Id, ConnectionServerType.SonarCloud)]);
    }

    [TestMethod]
    public void TryGetAllConnectionsInfo_HasOneSonarQubeConnection_ReturnsOneMappedConnection()
    {
        var sonarQube = CreateSonarQubeServerConnection();
        MockServerConnections([sonarQube]);

        testSubject.TryGetAllConnectionsInfo(out var connections);

        connections.Should().BeEquivalentTo([new ConnectionInfo(sonarQube.Id, ConnectionServerType.SonarQube)]);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryGetAllConnectionsInfo_ReturnsStatusFromSlCore(bool expectedStatus)
    {
        var sonarQube = CreateSonarQubeServerConnection();
        MockServerConnections([sonarQube], succeeded: expectedStatus);

        var succeeded = testSubject.TryGetAllConnectionsInfo(out _);

        succeeded.Should().Be(expectedStatus);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryAddConnection_ReturnsStatusFromSlCore(bool expectedStatus)
    {
        var sonarCloud = CreateSonarCloudConnection();
        serverConnectionsRepository.TryAdd(Arg.Any<ServerConnection>()).Returns(expectedStatus);

        var succeeded = testSubject.TryAddConnection(sonarCloud, Substitute.For<ICredentialsModel>());

        succeeded.Should().Be(expectedStatus);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]    
    public void TryAddConnection_AddsSonarCloudConnection_CallsSlCoreWithMappedConnection(bool enableSmartNotifications)
    {
        var sonarCloud = CreateSonarCloudConnection(enableSmartNotifications);

        testSubject.TryAddConnection(sonarCloud, Substitute.For<ICredentialsModel>());

        serverConnectionsRepository.Received(1)
            .TryAdd(Arg.Is<SonarCloud>(sc =>
                sc.Id == sonarCloud.Info.Id &&
                sc.OrganizationKey == sonarCloud.Info.Id &&
                sc.Settings.IsSmartNotificationsEnabled == sonarCloud.EnableSmartNotifications));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryAddConnection_AddsSonarQubeConnection_CallsSlCoreWithMappedConnection(bool enableSmartNotifications)
    {
        var sonarQube = CreateSonarQubeConnection(enableSmartNotifications);

        testSubject.TryAddConnection(sonarQube, Substitute.For<ICredentialsModel>());

        serverConnectionsRepository.Received(1)
            .TryAdd(Arg.Is<ServerConnection.SonarQube>(sc =>
                sc.Id == new Uri(sonarQube.Info.Id).ToString() &&
                sc.ServerUri == new Uri(sonarQube.Info.Id) &&
                sc.Settings.IsSmartNotificationsEnabled == sonarQube.EnableSmartNotifications));
    }

    [TestMethod]
    public void TryAddConnection_TokenCredentialsModel_MapsCredentials()
    {
        var sonarQube = CreateSonarQubeConnection();
        var token = "myToken";

        testSubject.TryAddConnection(sonarQube, new TokenCredentialsModel(token));

        serverConnectionsRepository.Received(1)
            .TryAdd(Arg.Is<ServerConnection.SonarQube>(sc => IsExpectedCredentials(sc, token, string.Empty)));
    }

    [TestMethod]
    public void TryAddConnection_UsernamePasswordModel_MapsCredentials()
    {
        var sonarQube = CreateSonarQubeConnection();
        var username = "username";
        var password = "password";

        testSubject.TryAddConnection(sonarQube, new UsernamePasswordModel(username, password));

        serverConnectionsRepository.Received(1)
            .TryAdd(Arg.Is<ServerConnection.SonarQube>(sc => IsExpectedCredentials(sc, username, password)));
    }

    [TestMethod]
    public void TryAddConnection_NullCredentials_TriesAddingAConnectionWithNoCredentials()
    {
        var sonarQube = CreateSonarQubeConnection();

        testSubject.TryAddConnection(sonarQube, null);

        serverConnectionsRepository.Received(1).TryAdd(Arg.Is<ServerConnection.SonarQube>(sc => sc.Credentials == null));
    }

    private static SonarCloud CreateSonarCloudServerConnection(bool isSmartNotificationsEnabled = true)
    {
        return new SonarCloud("myOrg", new ServerConnectionSettings(isSmartNotificationsEnabled), Substitute.For<ICredentials>());
    }

    private static ServerConnection.SonarQube CreateSonarQubeServerConnection(bool isSmartNotificationsEnabled = true)
    {
        var sonarQube = new ServerConnection.SonarQube(new Uri("http://localhost"), new ServerConnectionSettings(isSmartNotificationsEnabled), Substitute.For<ICredentials>());
        return sonarQube;
    }

    private void MockServerConnections(List<ServerConnection> connections, bool succeeded = true)
    {
        serverConnectionsRepository.TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>()).Returns(callInfo =>
        {
            callInfo[0] = connections;
            return succeeded;
        });
    }

    private static Connection CreateSonarCloudConnection(bool enableSmartNotifications = true)
    {
        return new Connection(new ConnectionInfo("mySecondOrg", ConnectionServerType.SonarCloud), enableSmartNotifications);
    }

    private static Connection CreateSonarQubeConnection(bool enableSmartNotifications = true)
    {
        return new Connection(new ConnectionInfo("http://localhost:9000", ConnectionServerType.SonarQube), enableSmartNotifications);
    }

    private static bool IsExpectedCredentials(ServerConnection.SonarQube sc, string expectedUsername, string expectedPassword)
    {
        return sc.Credentials is BasicAuthCredentials basicAuthCredentials && basicAuthCredentials.UserName == expectedUsername && basicAuthCredentials.Password?.ToUnsecureString() == expectedPassword;
    }
}
