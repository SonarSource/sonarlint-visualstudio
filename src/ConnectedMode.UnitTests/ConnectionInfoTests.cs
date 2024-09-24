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

using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class ConnectionInfoTests
{
    [TestMethod]
    public void FromServerConnection_ShouldReturnConnectionInfoWithSameId()
    {
        var sonarCloudServerConnection = new ServerConnection.SonarCloud("organization");

        var connectionInfo = ConnectionInfo.From(sonarCloudServerConnection);
        
        connectionInfo.Id.Should().Be("organization");
    }
    
    [TestMethod]
    public void FromServerConnection_WithSonarCloud_ShouldReturnConnectionInfo()
    {
        var sonarCloudServerConnection = new ServerConnection.SonarCloud("organization");

        var connectionInfo = ConnectionInfo.From(sonarCloudServerConnection);
        
        connectionInfo.ServerType.Should().Be(ConnectionServerType.SonarCloud);
    }
    
    [TestMethod]
    public void FromServerConnection_WithSonarQube_ShouldReturnConnectionInfo()
    {
        var sonarQubeServerConnection = new ServerConnection.SonarQube(new Uri("http://localhost:9000"));

        var connectionInfo = ConnectionInfo.From(sonarQubeServerConnection);
        
        connectionInfo.ServerType.Should().Be(ConnectionServerType.SonarQube);
    }
    
    [TestMethod]
    public void GetIdForTransientConnection_SonarCloudWithNullId_ReturnsSonarCloudUrl()
    {
        var connectionInfo = new ConnectionInfo(null, ConnectionServerType.SonarCloud);

        connectionInfo.GetIdForTransientConnection().Should().Be(UiResources.SonarCloudUrl);
    }

    [TestMethod]
    public void GetIdForTransientConnection_SonarCloudWithNotNullId_ReturnsId()
    {
        var id = "my org";
        var connectionInfo = new ConnectionInfo(id, ConnectionServerType.SonarCloud);

        connectionInfo.GetIdForTransientConnection().Should().Be(id);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("http://localhost:9000")]
    public void GetIdForTransientConnection_SonarQube_ReturnsId(string id)
    {
        var connectionInfo = new ConnectionInfo(id, ConnectionServerType.SonarQube);

        connectionInfo.GetIdForTransientConnection().Should().Be(id);
    }
}
