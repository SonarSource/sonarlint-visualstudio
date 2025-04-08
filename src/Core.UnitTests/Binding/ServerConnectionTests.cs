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
using SonarQube.Client.Models;
using static SonarLint.VisualStudio.ConnectedMode.Binding.BoundSonarQubeProjectExtensions;
using IConnectionCredentials = SonarLint.VisualStudio.Core.Binding.IConnectionCredentials;

namespace SonarLint.VisualStudio.Core.UnitTests.Binding;

[TestClass]
public class ServerConnectionTests
{
    private static readonly Uri Localhost = new("http://localhost:5000");
    private const string Org = "myOrg";

    [TestMethod]
    public void Ctor_SonarCloud_OrganizationKey_RemovesWhitespaces()
    {
        var connection = new ServerConnection.SonarCloud(" k e y ");

        connection.OrganizationKey.Should().Be("key");
    }

    [TestMethod]
    public void Ctor_SonarQube_ServerUri_RemovesWhitespaces()
    {
        var connection = new ServerConnection.SonarQube(new Uri(" http://localhost:9000 "));

        connection.ServerUri.Should().Be("http://localhost:9000/");
    }

    [TestMethod]
    public void Ctor_SonarCloud_NullOrganization_Throws()
    {
        Action act = () => _ = new ServerConnection.SonarCloud(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Ctor_SonarCloud_NullSettings_SetDefault()
    {
        var sonarCloud = new ServerConnection.SonarCloud(Org, settings: null);

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
        var credentials = Substitute.For<IConnectionCredentials>();
        var region = CloudServerRegion.Us;
        var sonarCloud = new ServerConnection.SonarCloud(Org, region, serverConnectionSettings, credentials);

        var expectedId = "https://sonarqube.us/organizations/myOrg";
        sonarCloud.Id.Should().Be(expectedId);
        sonarCloud.OrganizationKey.Should().BeSameAs(Org);
        sonarCloud.Region.Should().BeSameAs(region);
        sonarCloud.ServerUri.Should().Be(region.Url);
        sonarCloud.Settings.Should().BeSameAs(serverConnectionSettings);
        sonarCloud.Credentials.Should().BeSameAs(credentials);
        sonarCloud.CredentialsUri.Should().Be(new Uri(expectedId));
    }

    [TestMethod]
    public void Ctor_SonarCloud_NoRegion_SetsEu()
    {
        var sonarCloud = new ServerConnection.SonarCloud(Org);

        sonarCloud.Region.Should().Be(CloudServerRegion.Eu);
        sonarCloud.ServerUri.Should().Be(CloudServerRegion.Eu.Url);
    }

    [TestMethod]
    public void Ctor_SonarQube_NullUri_Throws()
    {
        Action act = () => _ = new ServerConnection.SonarQube(null);

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
        var credentials = Substitute.For<IConnectionCredentials>();
        var sonarQube = new ServerConnection.SonarQube(Localhost, serverConnectionSettings, credentials);

        sonarQube.Id.Should().Be(Localhost.ToString());
        sonarQube.ServerUri.Should().Be(Localhost);
        sonarQube.Settings.Should().BeSameAs(serverConnectionSettings);
        sonarQube.Credentials.Should().BeSameAs(credentials);
        sonarQube.CredentialsUri.ToString().Should().BeSameAs(sonarQube.Id);
    }

    [TestMethod]
    public void FromBoundSonarQubeProject_SonarQubeConnection_ConvertedCorrectly()
    {
        var credentials = Substitute.For<IConnectionCredentials>();
        var expectedConnection = new ServerConnection.SonarQube(Localhost, credentials: credentials);

        var connection = new BoundSonarQubeProject(Localhost, "any", "any", credentials).FromBoundSonarQubeProject();

        connection.Should().BeEquivalentTo(expectedConnection, options => options.ComparingByMembers<ServerConnection>());
    }

    [TestMethod]
    public void FromBoundSonarQubeProject_SonarCloudConnection_ConvertedCorrectly()
    {
        var uri = new Uri("https://sonarcloud.io");
        var organization = "org";
        var credentials = Substitute.For<IConnectionCredentials>();
        var expectedConnection = new ServerConnection.SonarCloud(organization, credentials: credentials);

        var connection = new BoundSonarQubeProject(uri, "any", "any", credentials, new SonarQubeOrganization(organization, null)).FromBoundSonarQubeProject();

        connection.Should().BeEquivalentTo(expectedConnection, options => options.ComparingByMembers<ServerConnection>());
    }

    [TestMethod]
    public void FromBoundSonarQubeProject_InvalidConnection_ReturnsNull()
    {
        var connection = new BoundSonarQubeProject { ProjectKey = "project" }.FromBoundSonarQubeProject();

        connection.Should().BeNull();
    }

    [TestMethod]
    public void FromBoundSonarQubeProject_NullConnection_ReturnsNull()
    {
        var connection = ((BoundSonarQubeProject)null).FromBoundSonarQubeProject();

        connection.Should().BeNull();
    }
}
