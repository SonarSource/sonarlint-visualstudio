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

using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Helpers;
using static SonarLint.VisualStudio.Core.Binding.ServerConnection;
using IConnectionCredentials = SonarLint.VisualStudio.Core.Binding.IConnectionCredentials;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class ServerConnectionsRepositoryAdapterTests
{
    private IServerConnectionsRepository serverConnectionsRepository;
    private ServerConnectionsRepositoryAdapter testSubject;
    private IServerConnectionWithInvalidTokenRepository serverConnectionWithInvalidTokenRepository;

    [TestInitialize]
    public void TestInitialize()
    {
        serverConnectionsRepository = Substitute.For<IServerConnectionsRepository>();
        serverConnectionWithInvalidTokenRepository = Substitute.For<IServerConnectionWithInvalidTokenRepository>();
        testSubject = new ServerConnectionsRepositoryAdapter(serverConnectionsRepository, serverConnectionWithInvalidTokenRepository);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ServerConnectionsRepositoryAdapter, IServerConnectionsRepositoryAdapter>(
            MefTestHelpers.CreateExport<IServerConnectionsRepository>(),
            MefTestHelpers.CreateExport<IServerConnectionWithInvalidTokenRepository>());

    [TestMethod]
    public void TryGet_CallServerConnectionsRepository()
    {
        var expectedServerConnection = new SonarCloud("myOrg");
        serverConnectionsRepository.TryGet("https://sonarcloud.io/organizations/myOrg", out _).Returns(callInfo =>
        {
            callInfo[1] = expectedServerConnection;
            return true;
        });

        testSubject.TryGet("https://sonarcloud.io/organizations/myOrg", out var serverConnection);

        serverConnection.Should().Be(expectedServerConnection);
    }

    [TestMethod]
    public void TryGet_ConnectionInfo_CallServerConnectionsRepository()
    {
        var expectedServerConnection = new SonarCloud("myOrg");
        serverConnectionsRepository.TryGet("https://sonarcloud.io/organizations/myOrg", out _).Returns(callInfo =>
        {
            callInfo[1] = expectedServerConnection;
            return true;
        });

        testSubject.TryGet(new ConnectionInfo("myOrg", ConnectionServerType.SonarCloud), out var serverConnection);

        serverConnection.Should().Be(expectedServerConnection);
    }

    [TestMethod]
    public void TryGetAllConnections_CallServerConnectionsRepository()
    {
        MockServerConnections([]);

        testSubject.TryGetAllConnections(out var connections);

        serverConnectionsRepository.Received(1).TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>());
        connections.Should().BeEmpty();
    }

    [TestMethod]
    [DynamicData(nameof(GetBoolWithCloudServerRegion), DynamicDataSourceType.Method)]
    public void TryGetAllConnections_HasOneSonarCloudConnection_ReturnsOneMappedConnection(bool isSmartNotificationsEnabled, CloudServerRegion cloudServerRegion)
    {
        var sonarCloud = CreateSonarCloudServerConnection(isSmartNotificationsEnabled, cloudServerRegion);
        MockServerConnections([sonarCloud]);

        testSubject.TryGetAllConnections(out var connections);

        connections.Should().BeEquivalentTo([new Connection(new ConnectionInfo(sonarCloud.OrganizationKey, ConnectionServerType.SonarCloud, cloudServerRegion), isSmartNotificationsEnabled)]);
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
    [DynamicData(nameof(GetBoolWithCloudServerRegion), DynamicDataSourceType.Method)]
    public void TryGetAllConnections_ReturnsStatusFromSlCore(bool expectedStatus, CloudServerRegion cloudServerRegion)
    {
        var sonarCloud = CreateSonarCloudServerConnection(region: cloudServerRegion);
        MockServerConnections([sonarCloud], succeeded: expectedStatus);

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
    [DynamicData(nameof(GetBoolWithCloudServerRegion), DynamicDataSourceType.Method)]
    public void TryGetAllConnectionsInfo_HasOneSonarCloudConnection_ReturnsOneMappedConnection(bool isSmartNotificationsEnabled, CloudServerRegion cloudServerRegion)
    {
        var sonarCloud = CreateSonarCloudServerConnection(isSmartNotificationsEnabled, cloudServerRegion);
        MockServerConnections([sonarCloud]);

        testSubject.TryGetAllConnectionsInfo(out var connections);

        connections.Should().BeEquivalentTo([new ConnectionInfo(sonarCloud.OrganizationKey, ConnectionServerType.SonarCloud, cloudServerRegion)]);
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
    [DynamicData(nameof(GetBoolWithCloudServerRegion), DynamicDataSourceType.Method)]
    public void TryAddConnection_AddsSonarCloudConnection_CallsSlCoreWithMappedConnection(bool enableSmartNotifications, CloudServerRegion cloudServerRegion)
    {
        var sonarCloud = CreateSonarCloudConnection(enableSmartNotifications, cloudServerRegion);

        testSubject.TryAddConnection(sonarCloud, Substitute.For<ICredentialsModel>());

        serverConnectionsRepository.Received(1)
            .TryAdd(Arg.Is<SonarCloud>(sc =>
                sc.Id == new Uri(cloudServerRegion.Url, $"organizations/{sonarCloud.Info.Id}").ToString() &&
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

        testSubject.TryAddConnection(sonarQube, new TokenCredentialsModel(token.CreateSecureString()));

        serverConnectionsRepository.Received(1)
            .TryAdd(Arg.Is<ServerConnection.SonarQube>(sq => IsExpectedTokenCredentials(sq.Credentials, token)));
    }

    [TestMethod]
    public void TryAddConnection_NullCredentials_TriesAddingAConnectionWithNoCredentials()
    {
        var sonarQube = CreateSonarQubeConnection();

        testSubject.TryAddConnection(sonarQube, null);

        serverConnectionsRepository.Received(1).TryAdd(Arg.Is<ServerConnection.SonarQube>(sq => sq.Credentials == null));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryUpdateCredentials_ReturnsStatusFromSlCore(bool slCoreResponse)
    {
        var sonarCloud = CreateSonarCloudConnection();
        serverConnectionsRepository.TryUpdateCredentialsById(Arg.Any<string>(), Arg.Any<IConnectionCredentials>()).Returns(slCoreResponse);

        var succeeded = testSubject.TryUpdateCredentials(sonarCloud, Substitute.For<ICredentialsModel>());

        succeeded.Should().Be(slCoreResponse);
    }

    [TestMethod]
    public void TryUpdateCredentials_TokenCredentialsModel_MapsCredentials()
    {
        var sonarQube = CreateSonarQubeConnection();
        const string token = "myToken";

        testSubject.TryUpdateCredentials(sonarQube, new TokenCredentialsModel(token.CreateSecureString()));

        serverConnectionsRepository.Received(1)
            .TryUpdateCredentialsById(Arg.Any<string>(), Arg.Is<IConnectionCredentials>(x => IsExpectedTokenCredentials(x, token)));
    }

    [TestMethod]
    public void TryUpdateCredentials_SonarQube_MapsConnection()
    {
        var sonarQube = CreateSonarQubeConnection();

        testSubject.TryUpdateCredentials(sonarQube, Substitute.For<ICredentialsModel>());

        serverConnectionsRepository.Received(1)
            .TryUpdateCredentialsById(Arg.Is<string>(x => x.Equals(sonarQube.Info.Id)), Arg.Any<IConnectionCredentials>());
    }

    [TestMethod]
    public void TryUpdateCredentials_SonarCloud_MapsConnection()
    {
        var sonarCloud = CreateSonarCloudConnection();

        testSubject.TryUpdateCredentials(sonarCloud, Substitute.For<ICredentialsModel>());

        serverConnectionsRepository.Received(1)
            .TryUpdateCredentialsById(Arg.Is<string>(x => x.EndsWith(sonarCloud.Info.Id)), Arg.Any<IConnectionCredentials>());
    }

    [TestMethod]
    public void TryUpdateCredentials_NullCredentials_TriesUpdatingConnectionWithNoCredentials()
    {
        var sonarQube = CreateSonarQubeConnection();

        testSubject.TryUpdateCredentials(sonarQube, null);

        serverConnectionsRepository.Received(1).TryUpdateCredentialsById(Arg.Any<string>(), Arg.Is<IConnectionCredentials>(x => x == null));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryDeleteConnection_ReturnsStatusFromSlCore(bool expectedStatus)
    {
        const string connectionInfoId = "http://localhost:9000/";
        var connectionInfo = new ConnectionInfo(connectionInfoId, ConnectionServerType.SonarQube);
        serverConnectionsRepository.TryDelete(connectionInfoId).Returns(expectedStatus);

        var succeeded = testSubject.TryRemoveConnection(connectionInfo);

        succeeded.Should().Be(expectedStatus);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryGet_ConnectionInfo_ReturnsStatusFromSlCore(bool expectedStatus)
    {
        const string connectionInfoId = "myOrg";
        var connectionInfo = new ConnectionInfo(connectionInfoId, ConnectionServerType.SonarCloud);
        var expectedServerConnection = new SonarCloud("myOrg");
        MockTryGet("https://sonarcloud.io/organizations/myOrg", expectedStatus, expectedServerConnection);

        var succeeded = testSubject.TryGet(connectionInfo, out var receivedServerConnection);

        succeeded.Should().Be(expectedStatus);
        receivedServerConnection.Should().Be(expectedServerConnection);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TryGet_ReturnsStatusFromSlCore(bool expectedStatus)
    {
        const string serverConnectionId = "https://sonarcloud.io/organizations/myOrg";
        var expectedServerConnection = new SonarCloud("myOrg");
        MockTryGet("https://sonarcloud.io/organizations/myOrg", expectedStatus, expectedServerConnection);

        var succeeded = testSubject.TryGet(serverConnectionId, out var receivedServerConnection);

        succeeded.Should().Be(expectedStatus);
        receivedServerConnection.Should().Be(expectedServerConnection);
    }

    [TestMethod]
    [DynamicData(nameof(GetBoolWithCloudServerRegion), DynamicDataSourceType.Method)]
    public void AddConnectionWithInvalidToken_SonarCloud_AddsConnectionIdToRepository(bool enableSmartNotifications, CloudServerRegion region)
    {
        var sonarCloud = CreateSonarCloudServerConnection(enableSmartNotifications, region);

        testSubject.AddConnectionWithInvalidToken(new Connection(new ConnectionInfo(sonarCloud.OrganizationKey, ConnectionServerType.SonarCloud, region)));

        serverConnectionWithInvalidTokenRepository.Received(1).AddConnectionIdWithInvalidToken(sonarCloud.Id);
    }

    [TestMethod]
    public void AddConnectionWithInvalidToken_SonarQube_AddsConnectionIdToRepository()
    {
        var sonarQube = CreateSonarQubeServerConnection();

        testSubject.AddConnectionWithInvalidToken(new Connection(new ConnectionInfo(sonarQube.Id, ConnectionServerType.SonarQube)));

        serverConnectionWithInvalidTokenRepository.Received(1).AddConnectionIdWithInvalidToken(sonarQube.Id);
    }

    [TestMethod]
    [DynamicData(nameof(GetBoolWithCloudServerRegion), DynamicDataSourceType.Method)]
    public void HasInvalidToken_SonarCloud_ReturnsResultFromRepository(bool expectedResult, CloudServerRegion region)
    {
        var sonarCloud = CreateSonarCloudServerConnection(region: region);
        serverConnectionWithInvalidTokenRepository.HasInvalidToken(sonarCloud.Id).Returns(expectedResult);

        var result = testSubject.HasInvalidToken(new Connection(new ConnectionInfo(sonarCloud.OrganizationKey, ConnectionServerType.SonarCloud, region)));

        result.Should().Be(expectedResult);
        serverConnectionWithInvalidTokenRepository.Received(1).HasInvalidToken(sonarCloud.Id);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void HasInvalidToken_SonarQube_ReturnsResultFromRepository(bool expectedResult)
    {
        var sonarQube = CreateSonarQubeServerConnection();
        serverConnectionWithInvalidTokenRepository.HasInvalidToken(sonarQube.Id).Returns(expectedResult);

        var result = testSubject.HasInvalidToken(new Connection(new ConnectionInfo(sonarQube.Id, ConnectionServerType.SonarQube)));

        result.Should().Be(expectedResult);
        serverConnectionWithInvalidTokenRepository.Received(1).HasInvalidToken(sonarQube.Id);
    }

    private static SonarCloud CreateSonarCloudServerConnection(bool isSmartNotificationsEnabled = true, CloudServerRegion region = null) =>
        new("myOrg", region, new ServerConnectionSettings(isSmartNotificationsEnabled), Substitute.For<IConnectionCredentials>());

    private static ServerConnection.SonarQube CreateSonarQubeServerConnection(bool isSmartNotificationsEnabled = true)
    {
        var sonarQube = new ServerConnection.SonarQube(new Uri("http://localhost"), new ServerConnectionSettings(isSmartNotificationsEnabled), Substitute.For<IConnectionCredentials>());
        return sonarQube;
    }

    private void MockServerConnections(List<ServerConnection> connections, bool succeeded = true) =>
        serverConnectionsRepository.TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>()).Returns(callInfo =>
        {
            callInfo[0] = connections;
            return succeeded;
        });

    private static Connection CreateSonarCloudConnection(bool enableSmartNotifications = true, CloudServerRegion region = null) =>
        new(new ConnectionInfo("mySecondOrg", ConnectionServerType.SonarCloud, region), enableSmartNotifications);

    private static Connection CreateSonarQubeConnection(bool enableSmartNotifications = true) =>
        new(new ConnectionInfo("http://localhost:9000/", ConnectionServerType.SonarQube), enableSmartNotifications);

    private static bool IsExpectedTokenCredentials(IConnectionCredentials credentials, string expectedToken) =>
        credentials is TokenAuthCredentials tokenAuthCredentials && tokenAuthCredentials.Token?.ToUnsecureString() == expectedToken;

    private void MockTryGet(string connectionId, bool expectedResponse, ServerConnection expectedServerConnection) =>
        serverConnectionsRepository.TryGet(connectionId, out _).Returns(callInfo =>
        {
            callInfo[1] = expectedServerConnection;
            return expectedResponse;
        });

    public static IEnumerable<object[]> GetBoolWithCloudServerRegion() =>
    [
        [true, CloudServerRegion.Eu],
        [false, CloudServerRegion.Eu],
        [true, CloudServerRegion.Us],
        [false, CloudServerRegion.Eu]
    ];
}
