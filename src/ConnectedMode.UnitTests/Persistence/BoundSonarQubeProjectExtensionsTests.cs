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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests;

[TestClass]
public class BoundSonarQubeProjectExtensionsTests
{
    [TestMethod]
    public void FromBoundSonarQubeProject_SonarCloud_ReturnsSonarCloudConnection()
    {
        var org = new SonarQubeOrganization("org-key", "org-name");
        var boundProject = new BoundSonarQubeProject
        {
            Organization = org,
            ProjectKey = "project-key",
            ProjectName = "project-name"
        };

        var result = boundProject.FromBoundSonarQubeProject();

        result.Should().BeOfType<ServerConnection.SonarCloud>();
        var cloud = (ServerConnection.SonarCloud)result;
        cloud.OrganizationKey.Should().Be(org.Key);
    }

    [TestMethod]
    public void FromBoundSonarQubeProject_SonarQube_ReturnsSonarQubeConnection()
    {
        var uri = new Uri("https://sonarqube.local");
        var boundProject = new BoundSonarQubeProject
        {
            ServerUri = uri,
            ProjectKey = "project-key",
            ProjectName = "project-name"
        };

        var result = boundProject.FromBoundSonarQubeProject();

        result.Should().BeOfType<ServerConnection.SonarQube>();
        var sq = (ServerConnection.SonarQube)result;
        sq.ServerUri.Should().Be(uri);
    }

    [TestMethod]
    public void FromBoundSonarQubeProject_NullProperties_ReturnsNull()
    {
        var boundProject = new BoundSonarQubeProject();
        var result = boundProject.FromBoundSonarQubeProject();
        result.Should().BeNull();
    }

    [TestMethod]
    public void FromBoundSonarQubeProject_ToBoundServerProject_Valid()
    {
        var uri = new Uri("https://sonarqube.local");
        var boundProject = new BoundSonarQubeProject
        {
            ServerUri = uri,
            ProjectKey = "project-key",
            ProjectName = "project-name"
        };
        var connection = new ServerConnection.SonarQube(uri);
        var localBindingKey = "local-key";

        var result = boundProject.FromBoundSonarQubeProject(localBindingKey, connection);

        result.LocalBindingKey.Should().Be(localBindingKey);
        result.ServerProjectKey.Should().Be(boundProject.ProjectKey);
        result.ServerConnection.Should().Be(connection);
    }

    [TestMethod]
    public void FromBoundSonarQubeProject_ToBoundServerProject_NullArgs_Throws()
    {
        var boundProject = new BoundSonarQubeProject { ProjectKey = "project-key" };
        var connection = new ServerConnection.SonarCloud("org-key");

        Assert.ThrowsException<ArgumentNullException>(() => boundProject.FromBoundSonarQubeProject(null, connection));
        Assert.ThrowsException<ArgumentNullException>(() => ((BoundSonarQubeProject)null).FromBoundSonarQubeProject("local", connection));
        Assert.ThrowsException<ArgumentNullException>(() => boundProject.FromBoundSonarQubeProject("local", null));
    }
}
