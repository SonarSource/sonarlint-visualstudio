﻿/*
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
using SonarLint.VisualStudio.SLCore.Common.Helpers;
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
            MefTestHelpers.CreateExport<ISolutionBindingRepository>(),
            MefTestHelpers.CreateExport<IConnectionIdHelper>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ServerConnectionsProvider>();
    }

    [TestMethod]
    public void GetServerConnections_CorrectlyReturnsSonarQubeConnection()
    {
        const string connectionId = "connectionId";
        const string servierUriString = "http://localhost/";
        var serverUri = new Uri(servierUriString);
        var binding = new BoundServerProject("solution", "project", new ServerConnection.SonarQube(serverUri));
        var solutionBindingRepository = SetUpBindingRepository(binding);
        var connectionIdHelper = Substitute.For<IConnectionIdHelper>();
        connectionIdHelper.GetConnectionIdFromServerConnection(Arg.Is<ServerConnection>(s => s.ServerUri == serverUri)).Returns(connectionId);
        var testSubject = CreateTestSubject(solutionBindingRepository, connectionIdHelper);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(1);
        serverConnections[connectionId].Should().BeOfType<SonarQubeConnectionConfigurationDto>().Which.serverUrl.Should().Be(servierUriString);
    }
    
    [TestMethod]
    public void GetServerConnections_CorrectlyReturnsSonarCloudConnection()
    {
        const string connectionId = "https://sonarcloud.io/organizations/org";
        var serverUri = new Uri("https://sonarcloud.io/");
        const string organizationKey = "org";
        var binding = new BoundServerProject("solution", "project", new ServerConnection.SonarCloud(organizationKey));
        var solutionBindingRepository = SetUpBindingRepository(binding);
        var connectionIdHelper = Substitute.For<IConnectionIdHelper>();
        connectionIdHelper.GetConnectionIdFromServerConnection(Arg.Is<ServerConnection>(s => s.ServerUri.Equals(serverUri) && s.Id == connectionId)).Returns(connectionId);
        var testSubject = CreateTestSubject(solutionBindingRepository, connectionIdHelper);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(1);
        serverConnections[connectionId].Should().BeOfType<SonarCloudConnectionConfigurationDto>().Which.organization.Should().Be(organizationKey);
    }
    
    [TestMethod]
    public void GetServerConnections_CorrectlyHandlesMultipleConnections()
    {
        var bindingSQ1 = new BoundServerProject("solution1", "project1", new ServerConnection.SonarQube(new Uri("http://localhost/")));
        var bindingSQ2 = new BoundServerProject("solution2", "project2", new ServerConnection.SonarQube(new Uri("https://next.sonarqube.org/sonarqube/")));
        var bindingSC = new BoundServerProject("solution3", "project3", new ServerConnection.SonarCloud("myorg"));
        var solutionBindingRepository = SetUpBindingRepository(bindingSQ1, bindingSQ2, bindingSC);
        var connectionIdHelper = Substitute.ForPartsOf<ConnectionIdHelper>();
        var testSubject = CreateTestSubject(solutionBindingRepository, connectionIdHelper);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(3);
        serverConnections["sc|https://sonarcloud.io/organizations/myorg"].Should().BeOfType<SonarCloudConnectionConfigurationDto>();
        serverConnections["sq|http://localhost/"].Should().BeOfType<SonarQubeConnectionConfigurationDto>();
        serverConnections["sq|https://next.sonarqube.org/sonarqube/"].Should().BeOfType<SonarQubeConnectionConfigurationDto>();
    }
    
    [TestMethod]
    public void GetServerConnections_CorrectlyHandlesNoConnections()
    {
        var solutionBindingRepository = SetUpBindingRepository();
        var connectionIdHelper = Substitute.ForPartsOf<ConnectionIdHelper>();
        var testSubject = CreateTestSubject(solutionBindingRepository, connectionIdHelper);

        var serverConnections = testSubject.GetServerConnections();

        serverConnections.Should().HaveCount(0);
    }

    private static ISolutionBindingRepository SetUpBindingRepository(
        params BoundServerProject[] bindings)
    {
        var bindingRepository = Substitute.For<ISolutionBindingRepository>();
        bindingRepository.List().Returns(bindings);
        return bindingRepository;
    }

    private static IServerConnectionsProvider CreateTestSubject(ISolutionBindingRepository bindingRepository, IConnectionIdHelper connectionIdHelper) => 
        new ServerConnectionsProvider(bindingRepository, connectionIdHelper);
}
