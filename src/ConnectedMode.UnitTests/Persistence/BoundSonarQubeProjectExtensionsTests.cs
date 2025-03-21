﻿/*
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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests;

[TestClass]
public class BoundSonarQubeProjectExtensionsTests
{
    [TestMethod]
    public void BoundSonarQubeProject_CreateConnectionInformation_ArgCheck()
    {
        Exceptions.Expect<ArgumentNullException>(() => BoundSonarQubeProjectExtensions.CreateConnectionInformation((BoundSonarQubeProject)null));
    }

    [TestMethod]
    public void BoundSonarQubeProject_CreateConnectionInformation_NoCredentials()
    {
        // Arrange
        var input = new BoundSonarQubeProject(new Uri("http://server"), "ProjectKey", "projectName",
            organization: new SonarQubeOrganization("org_key", "org_name"));

        // Act
        ConnectionInformation conn = input.CreateConnectionInformation();

        // Assert
        conn.ServerUri.Should().Be(input.ServerUri);
        conn.Credentials.Should().BeAssignableTo<INoCredentials>();
        conn.Organization.Key.Should().Be("org_key");
        conn.Organization.Name.Should().Be("org_name");
    }

    [TestMethod]
    public void BoundSonarQubeProject_CreateConnectionInformation_BasicAuthCredentials()
    {
        // Arrange
        var creds = new UsernameAndPasswordCredentials("UserName", "password".ToSecureString());
        var input = new BoundSonarQubeProject(new Uri("http://server"), "ProjectKey", "projectName", creds,
            new SonarQubeOrganization("org_key", "org_name"));

        // Act
        ConnectionInformation conn = input.CreateConnectionInformation();

        // Assert
        conn.ServerUri.Should().Be(input.ServerUri);
        var basicAuth = conn.Credentials as IUsernameAndPasswordCredentials;
        basicAuth.Should().NotBeNull();
        basicAuth.UserName.Should().Be(creds.UserName);
        basicAuth.Password.ToUnsecureString().Should().Be(creds.Password.ToUnsecureString());
        conn.Organization.Key.Should().Be("org_key");
        conn.Organization.Name.Should().Be("org_name");
    }

    [TestMethod]
    public void BoundSonarQubeProject_CreateConnectionInformation_NoOrganizationNoAuth()
    {
        // Arrange
        var input = new BoundSonarQubeProject(new Uri("http://server"), "ProjectKey", "projectName");

        // Act
        ConnectionInformation conn = input.CreateConnectionInformation();

        // Assert
        conn.ServerUri.Should().Be(input.ServerUri);
        conn.Credentials.Should().BeAssignableTo<INoCredentials>();
        conn.Organization.Should().BeNull();
    }

    [TestMethod]
    public void BoundServerProject_CreateConnectionInformation_ArgCheck()
    {
        Exceptions.Expect<ArgumentNullException>(() => BoundSonarQubeProjectExtensions.CreateConnectionInformation((BoundServerProject)null));
    }

    [TestMethod]
    public void BoundServerProject_CreateConnectionInformation_NoCredentials()
    {
        // Arrange
        var input = new BoundServerProject("solution", "ProjectKey", new ServerConnection.SonarCloud("org_key"));

        // Act
        ConnectionInformation conn = input.CreateConnectionInformation();

        // Assert
        conn.ServerUri.Should().Be(input.ServerConnection.ServerUri);
        conn.Credentials.Should().BeAssignableTo<INoCredentials>();
        conn.Organization.Key.Should().Be("org_key");
    }

    [TestMethod]
    public void BoundServerProject_CreateConnectionInformation_BasicAuthCredentials()
    {
        // Arrange
        var creds = new UsernameAndPasswordCredentials("UserName", "password".ToSecureString());
        var input = new BoundServerProject("solution", "ProjectKey", new ServerConnection.SonarCloud("org_key", credentials: creds));

        // Act
        ConnectionInformation conn = input.CreateConnectionInformation();

        // Assert
        conn.ServerUri.Should().Be(input.ServerConnection.ServerUri);
        var basicAuth = conn.Credentials as IUsernameAndPasswordCredentials;
        basicAuth.Should().NotBeNull();
        basicAuth.UserName.Should().Be(creds.UserName);
        basicAuth.Password.ToUnsecureString().Should().Be(creds.Password.ToUnsecureString());
        conn.Organization.Key.Should().Be("org_key");
    }

    [TestMethod]
    public void BoundServerProject_CreateConnectionInformation_NoOrganizationNoAuth()
    {
        // Arrange
        var input = new BoundServerProject("solution", "ProjectKey", new ServerConnection.SonarQube(new Uri("http://localhost")));

        // Act
        ConnectionInformation conn = input.CreateConnectionInformation();

        // Assert
        conn.ServerUri.Should().Be(input.ServerConnection.ServerUri);
        conn.Credentials.Should().BeAssignableTo<INoCredentials>();
        conn.Organization.Should().BeNull();
    }
}
