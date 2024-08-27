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
using ICredentials = SonarLint.VisualStudio.Core.Binding.ICredentials;

namespace SonarLint.VisualStudio.Core.UnitTests.Binding;

[TestClass]
public class ServerConnectionTests
{
    private static readonly Uri localhost = new Uri("http://localhost:5000");
    private static readonly string org = "myOrg";
    
    [TestMethod]
    public void Ctor_SonarCloud_NullOrganization_Throws()
    {
        Action act = () => new ServerConnection.SonarCloud(null);

        act.Should().Throw<ArgumentNullException>();
    }
    
    [TestMethod]
    public void Ctor_SonarCloud_NullSettings_SetDefault()
    {
        var sonarCloud = new ServerConnection.SonarCloud(org, null);

        sonarCloud.Settings.Should().BeSameAs(ServerConnection.DefaultSettings);
    }
    
    [TestMethod]
    public void Ctor_SonarCloud_NullCredentials_SetsNull()
    {
        var sonarCloud = new ServerConnection.SonarCloud(org, credentials: null);

        sonarCloud.Credentials.Should().BeNull();
    }
    
    [TestMethod]
    public void Ctor_SonarCloud_SetsProperties()
    {
        var serverConnectionSettings = new ServerConnectionSettings(false);
        var credentials = Substitute.For<ICredentials>();
        var sonarCloud = new ServerConnection.SonarCloud(org, serverConnectionSettings, credentials);

        sonarCloud.Id.Should().BeSameAs(org);
        sonarCloud.OrganizationKey.Should().BeSameAs(org);
        sonarCloud.ServerUri.Should().Be(new Uri("https://sonarcloud.io"));
        sonarCloud.Settings.Should().BeSameAs(serverConnectionSettings);
        sonarCloud.Credentials.Should().BeSameAs(credentials);
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
        var sonarQube = new ServerConnection.SonarQube(localhost, null);

        sonarQube.Settings.Should().BeSameAs(ServerConnection.DefaultSettings);
    }
    
    [TestMethod]
    public void Ctor_SonarQube_NullCredentials_SetsNull()
    {
        var sonarQube = new ServerConnection.SonarQube(localhost, credentials: null);

        sonarQube.Credentials.Should().BeNull();
    }
    
    [TestMethod]
    public void Ctor_SonarQube_SetsProperties()
    {
        var serverConnectionSettings = new ServerConnectionSettings(false);
        var credentials = Substitute.For<ICredentials>();
        var sonarQube = new ServerConnection.SonarQube(localhost, serverConnectionSettings, credentials);

        sonarQube.Id.Should().Be(localhost.ToString());
        sonarQube.ServerUri.Should().BeSameAs(localhost);
        sonarQube.Settings.Should().BeSameAs(serverConnectionSettings);
        sonarQube.Credentials.Should().BeSameAs(credentials);
    }
}
