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

using System.Net;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Binding;
using ICredentials = SonarLint.VisualStudio.Core.Binding.ICredentials;

namespace SonarLint.VisualStudio.Core.UnitTests.Binding;

[TestClass]
public class ServerConnectionTests
{
    [TestMethod]
    public void Ctor_SonarCloudOrg_HasCorrectType()
    {
        new ServerConnection("Sonar Cloud Org").Type.Should().Be(ServerConnectionType.SonarCloud);
    }
    
    [TestMethod]
    public void Ctor_SonarQubeUri_HasCorrectType()
    {
        new ServerConnection(new Uri("http://localhost")).Type.Should().Be(ServerConnectionType.SonarQube);
    }
    
    [DataTestMethod]
    [DataRow("http://localhost:8080/")]
    [DataRow("https://next.sonarqube.com/next")]
    [DataRow("http://sonarqube")]
    public void SonarQubeConnection_SerializeDeserialize_AsExpected(string sonarQubeUri)
    {
        var serverConnection = new ServerConnection(new Uri(sonarQubeUri));
        
        var deserializedServerConnection = JsonConvert.DeserializeObject<ServerConnection>(JsonConvert.SerializeObject(serverConnection));
        
        deserializedServerConnection.Should().BeEquivalentTo(serverConnection);
    }
    
    [TestMethod]
    public void SonarQubeConnection_CredentialsNotSerialized()
    {
        var serverConnection = new ServerConnection(new Uri("http://localhost")) { Credentials = Substitute.For<ICredentials>()};
        
        var deserializedServerConnection = JsonConvert.DeserializeObject<ServerConnection>(JsonConvert.SerializeObject(serverConnection));
        
        deserializedServerConnection.Credentials.Should().BeNull();
    }
    
    [DataTestMethod]
    [DataRow("ORGANIZATION")]
    [DataRow("MY ORGANIZATION")]
    [DataRow("MY_favourite_ORGANIZATION")]
    public void SonarCloudConnection_SerializeDeserialize_AsExpected(string sonarCloudOrganization)
    {
        var serverConnection = new ServerConnection(sonarCloudOrganization);
        
        var deserializedServerConnection = JsonConvert.DeserializeObject<ServerConnection>(JsonConvert.SerializeObject(serverConnection));
        
        deserializedServerConnection.Should().BeEquivalentTo(serverConnection);
    }
    
    [TestMethod]
    public void SonarCloudConnection_CredentialsNotSerialized()
    {
        var serverConnection = new ServerConnection("my sonar cloud org") { Credentials = Substitute.For<ICredentials>()};
        
        var deserializedServerConnection = JsonConvert.DeserializeObject<ServerConnection>(JsonConvert.SerializeObject(serverConnection));
        
        deserializedServerConnection.Credentials.Should().BeNull();
    }

    [TestMethod]
    public void GetSonarCloudOrganization_SonarQubeConnection_ReturnsNull()
    {
        new ServerConnection(new Uri("http://localhost")).GetSonarCloudOrganization().Should().BeNull();
    }
    
    [TestMethod]
    public void GetSonarCloudOrganization_SonarCloudConnection_ReturnsId()
    {
        var organization = "organization is id";
        new ServerConnection(organization).GetSonarCloudOrganization().Should().BeSameAs(organization);
    }
    
    [TestMethod]
    public void GetSonarQubeUri_SonarCloudConnection_ReturnsNull()
    {
        new ServerConnection("id").GetSonarQubeUri().Should().BeNull();
    }
    
    [TestMethod]
    public void GetSonarQubeUri_SonarQubeConnection_ReturnsId()
    {
        var sonarQubeUri = new Uri("http://uri_is_id");
        new ServerConnection(sonarQubeUri).GetSonarQubeUri().Should().Be(sonarQubeUri);
    }
}
