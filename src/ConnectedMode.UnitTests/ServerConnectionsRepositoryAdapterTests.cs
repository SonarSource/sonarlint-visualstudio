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

using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
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
    public void GetAllConnections_CallServerConnectionsRepository()
    {
        MockServerConnections([]);

        var connections = testSubject.GetAllConnections();

        serverConnectionsRepository.Received(1).TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>());
        connections.Should().BeEmpty();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetAllConnections_HasOneSonarCloudConnection_ReturnsOneMappedConnection(bool isSmartNotificationsEnabled)
    {
        var sonarCloud = CreateSonarCloudServerConnection(isSmartNotificationsEnabled);
        MockServerConnections([sonarCloud]);

        var connections = testSubject.GetAllConnections();

        connections.Should().BeEquivalentTo([new Connection(new ConnectionInfo(sonarCloud.Id, ConnectionServerType.SonarCloud), isSmartNotificationsEnabled)]);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetAllConnections_HasOneSonarQubeConnection_ReturnsOneMappedConnection(bool isSmartNotificationsEnabled)
    {
        var sonarQube = CreateSonarQubeServerConnection(isSmartNotificationsEnabled);
        MockServerConnections([sonarQube]);

        var connections = testSubject.GetAllConnections();

        connections.Should().BeEquivalentTo([new Connection(new ConnectionInfo(sonarQube.Id, ConnectionServerType.SonarQube), isSmartNotificationsEnabled)]);
    }

    [TestMethod]
    public void GetAllConnectionsInfo_CallServerConnectionsRepository()
    {
        MockServerConnections([]);

        var connections = testSubject.GetAllConnectionsInfo();

        serverConnectionsRepository.Received(1).TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>());
        connections.Should().BeEmpty();
    }

    [TestMethod]
    public void GetAllConnectionsInfo_HasOneSonarCloudConnection_ReturnsOneMappedConnection()
    {
        var sonarCloud = CreateSonarCloudServerConnection();
        MockServerConnections([sonarCloud]);

        var connections = testSubject.GetAllConnectionsInfo();

        connections.Should().BeEquivalentTo([new ConnectionInfo(sonarCloud.Id, ConnectionServerType.SonarCloud)]);
    }

    [TestMethod]
    public void GetAllConnectionsInfo_HasOneSonarQubeConnection_ReturnsOneMappedConnection()
    {
        var sonarQube = CreateSonarQubeServerConnection();
        MockServerConnections([sonarQube]);

        var connections = testSubject.GetAllConnectionsInfo();

        connections.Should().BeEquivalentTo([new ConnectionInfo(sonarQube.Id, ConnectionServerType.SonarQube)]);
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

    private void MockServerConnections(List<ServerConnection> connections)
    {
        serverConnectionsRepository.TryGetAll(out Arg.Any<IReadOnlyList<ServerConnection>>()).Returns(callInfo =>
        {
            callInfo[0] = connections;
            return true;
        });
    }
}
