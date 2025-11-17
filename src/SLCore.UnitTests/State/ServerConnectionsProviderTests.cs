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
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.UnitTests.State;

[TestClass]
public class ServerConnectionsProviderTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ServerConnectionsProvider, IServerConnectionsProvider>(
            MefTestHelpers.CreateExport<IServerConnectionsRepository>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ServerConnectionsProvider>();
    }

    [TestMethod]
    public void GetServerConnections_CorrectlyReturnsSonarQubeConnection()
    {
        const string servierUriString = "http://localhost/";
        var serverUri = new Uri(servierUriString);
        var connection = new ServerConnection.SonarQube(serverUri);
        var serverConnectionsRepository = SetServerConnectionsRepository(succeeded: true, connection);
        var testSubject = CreateTestSubject(serverConnectionsRepository);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(1);
        serverConnections[connection.Id].Should().BeOfType<SonarQubeConnectionConfigurationDto>().Which.serverUrl.Should().Be(servierUriString);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetServerConnections_CorrectlyReturnsSonarQubeNotifications(bool isSmartNotificationsEnabled)
    {
        const string servierUriString = "http://localhost/";
        var serverUri = new Uri(servierUriString);
        var connection = new ServerConnection.SonarQube(serverUri, settings: new ServerConnectionSettings(isSmartNotificationsEnabled));
        var serverConnectionsRepository = SetServerConnectionsRepository(succeeded: true, connection);
        var testSubject = CreateTestSubject(serverConnectionsRepository);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(1);
        serverConnections[connection.Id].Should().BeOfType<SonarQubeConnectionConfigurationDto>().Which.disableNotification.Should().Be(!isSmartNotificationsEnabled);
    }

    [TestMethod]
    [DataRow("eu", SonarCloudRegion.EU, "https://sonarcloud.io/organizations/org")]
    [DataRow("us", SonarCloudRegion.US, "https://sonarqube.us/organizations/org")]
    public void GetServerConnections_CorrectlyReturnsSonarCloudConnection(string regionName, SonarCloudRegion expectedRegion, string expectedId)
    {
        const string organizationKey = "org";
        var region = CloudServerRegion.GetRegionByName(regionName);
        var connection = new ServerConnection.SonarCloud(organizationKey, region);
        var serverConnectionsRepository = SetServerConnectionsRepository(succeeded: true, connection);
        var testSubject = CreateTestSubject(serverConnectionsRepository);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(1);
        serverConnections[connection.Id].Should().BeEquivalentTo(new SonarCloudConnectionConfigurationDto(expectedId, false, "org", expectedRegion));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetServerConnections_CorrectlyReturnsSonarCloudNotifications(bool isSmartNotificationsEnabled)
    {
        const string organizationKey = "org";
        var connection = new ServerConnection.SonarCloud(organizationKey, settings: new ServerConnectionSettings(isSmartNotificationsEnabled));
        var serverConnectionsRepository = SetServerConnectionsRepository(succeeded: true, connection);
        var testSubject = CreateTestSubject(serverConnectionsRepository);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(1);
        serverConnections[connection.Id].Should()
            .BeEquivalentTo(new SonarCloudConnectionConfigurationDto("https://sonarcloud.io/organizations/org", !isSmartNotificationsEnabled, "org", SonarCloudRegion.EU));
    }

    [TestMethod]
    public void GetServerConnections_CorrectlyHandlesMultipleConnections()
    {
        var connectionSQ1 = new ServerConnection.SonarQube(new Uri("http://localhost/"));
        var connectionSQ2 = new ServerConnection.SonarQube(new Uri("https://next.sonarqube.org/sonarqube/"));
        var connectionSC1 = new ServerConnection.SonarCloud("myorg");
        var connectionSC2 = new ServerConnection.SonarCloud("myorg", CloudServerRegion.Us);
        var serverConnectionsRepository = SetServerConnectionsRepository(succeeded: true, connectionSQ1, connectionSQ2, connectionSC1, connectionSC2);
        var testSubject = CreateTestSubject(serverConnectionsRepository);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(4);
        serverConnections["https://sonarcloud.io/organizations/myorg"].Should().BeOfType<SonarCloudConnectionConfigurationDto>();
        serverConnections["https://sonarqube.us/organizations/myorg"].Should().BeOfType<SonarCloudConnectionConfigurationDto>();
        serverConnections["http://localhost/"].Should().BeOfType<SonarQubeConnectionConfigurationDto>();
        serverConnections["https://next.sonarqube.org/sonarqube/"].Should().BeOfType<SonarQubeConnectionConfigurationDto>();
    }

    [TestMethod]
    public void GetServerConnections_CorrectlyHandlesNoConnections()
    {
        var serverConnectionsRepository = SetServerConnectionsRepository(succeeded: true);
        var testSubject = CreateTestSubject(serverConnectionsRepository);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(0);
    }

    [TestMethod]
    public void GetServerConnections_ConnectionsCouldNotBeRead_ReturnsNoConnection()
    {
        var serverConnectionsRepository = SetServerConnectionsRepository(succeeded: false);
        var testSubject = CreateTestSubject(serverConnectionsRepository);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(0);
    }

    private static IServerConnectionsRepository SetServerConnectionsRepository(
        bool succeeded,
        params ServerConnection[] serverConnections)
    {
        var bindingRepository = Substitute.For<IServerConnectionsRepository>();
        bindingRepository.TryGetAll(out var _).Returns(callInfo =>
        {
            callInfo[0] = serverConnections;
            return succeeded;
        });

        return bindingRepository;
    }

    private static IServerConnectionsProvider CreateTestSubject(IServerConnectionsRepository serverConnectionsRepository) => new ServerConnectionsProvider(serverConnectionsRepository);
}
