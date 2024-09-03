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
using SonarQube.Client.Models;
using ICredentials = SonarLint.VisualStudio.Core.Binding.ICredentials;

namespace SonarLint.VisualStudio.Core.UnitTests.Binding;

[TestClass]
public class ServerConnectionTests
{
    private static readonly Uri Localhost = new("http://localhost:5000");
    private const string Org = "myOrg";
    
    [TestMethod]
    public void Ctor_SonarCloud_NullOrganization_Throws()
    {
        Action act = () => new ServerConnection.SonarCloud(null);

        act.Should().Throw<ArgumentNullException>();
    }
    
    [TestMethod]
    public void Ctor_SonarCloud_NullSettings_SetDefault()
    {
        var sonarCloud = new ServerConnection.SonarCloud(Org, null);

        sonarCloud.Settings.Should().BeSameAs(ServerConnection.DefaultSettings);
    }
    
    [TestMethod]
    public void Ctor_SonarCloud_NullCredentials_SetsNull()
    {
        var sonarCloud = new ServerConnection.SonarCloud(Org, credentials: null);

        sonarCloud.Credentials.Should().BeNull();
    }
    
    [TestMethod]
    public void Ctor_SonarCloud_SetsProperties()
    {
        var serverConnectionSettings = new ServerConnectionSettings(false);
        var credentials = Substitute.For<ICredentials>();
        var sonarCloud = new ServerConnection.SonarCloud(Org, serverConnectionSettings, credentials);

        sonarCloud.Id.Should().BeSameAs(Org);
        sonarCloud.OrganizationKey.Should().BeSameAs(Org);
        sonarCloud.ServerUri.Should().Be(new Uri("https://sonarcloud.io"));
        sonarCloud.Settings.Should().BeSameAs(serverConnectionSettings);
        sonarCloud.Credentials.Should().BeSameAs(credentials);
        sonarCloud.CredentialsUri.Should().Be(new Uri($"https://sonarcloud.io/organizations/{sonarCloud.OrganizationKey}"));
    }
    
    [TestMethod]
    public void Ctor_SonarQube_NullUri_Throws()
    {
        Action act = () => new ServerConnection.SonarQube(null);

        act.Should().Throw<ArgumentNullException>();
    }
    
    [TestMethod]
    public void Ctor_SonarQube_NullSettings_SetDefault()
    {
        var sonarQube = new ServerConnection.SonarQube(Localhost, null);

        sonarQube.Settings.Should().BeSameAs(ServerConnection.DefaultSettings);
    }
    
    [TestMethod]
    public void Ctor_SonarQube_NullCredentials_SetsNull()
    {
        var sonarQube = new ServerConnection.SonarQube(Localhost, credentials: null);

        sonarQube.Credentials.Should().BeNull();
    }
    
    [TestMethod]
    public void Ctor_SonarQube_SetsProperties()
    {
        var serverConnectionSettings = new ServerConnectionSettings(false);
        var credentials = Substitute.For<ICredentials>();
        var sonarQube = new ServerConnection.SonarQube(Localhost, serverConnectionSettings, credentials);

        sonarQube.Id.Should().Be(Localhost.ToString());
        sonarQube.ServerUri.Should().BeSameAs(Localhost);
        sonarQube.Settings.Should().BeSameAs(serverConnectionSettings);
        sonarQube.Credentials.Should().BeSameAs(credentials);
        sonarQube.CredentialsUri.Should().BeSameAs(sonarQube.ServerUri);
    }

    [TestMethod]
    public void FromBoundSonarQubeProject_SonarQubeConnection_ConvertedCorrectly()
    {
        var credentials = Substitute.For<ICredentials>();
        var expectedConnection = new ServerConnection.SonarQube(Localhost, credentials: credentials);

        var connection = ServerConnection.FromBoundSonarQubeProject(new BoundSonarQubeProject(Localhost, "any", "any", credentials));
        
        connection.Should().BeEquivalentTo(expectedConnection, options => options.ComparingByMembers<ServerConnection>());
    }
    
    [TestMethod]
    public void FromBoundSonarQubeProject_SonarCloudConnection_ConvertedCorrectly()
    {
        var uri = new Uri("https://sonarcloud.io");
        var organization = "org";
        var credentials = Substitute.For<ICredentials>();
        var expectedConnection = new ServerConnection.SonarCloud(organization, credentials: credentials);

        var connection = ServerConnection.FromBoundSonarQubeProject(new BoundSonarQubeProject(uri, "any", "any", credentials, new SonarQubeOrganization(organization, null)));
        
        connection.Should().BeEquivalentTo(expectedConnection, options => options.ComparingByMembers<ServerConnection>());
    }
    
    [TestMethod]
    public void FromBoundSonarQubeProject_InvalidConnection_ReturnsNull()
    {
        var connection = ServerConnection.FromBoundSonarQubeProject(new BoundSonarQubeProject(){ ProjectKey = "project"});

        connection.Should().BeNull();
    }
    
    [TestMethod]
    public void FromBoundSonarQubeProject_NullConnection_ReturnsNull()
    {
        var connection = ServerConnection.FromBoundSonarQubeProject(null);

        connection.Should().BeNull();
    }
}
