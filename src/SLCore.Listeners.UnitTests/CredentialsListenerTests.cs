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

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.SLCore.Common;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Credentials;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests;

[TestClass]
public class CredentialsListenerTests
{
    private static readonly Uri SonarQubeUri = new("https://next.sonarqube.com");
    private static readonly Uri SonarCloudUri = new("https://sonarcloud.io");
    private static readonly string SonarQubeConnectionId = ConnectionIdPrefix.SonarQubePrefix + SonarQubeUri;
    private const string SonarCloudConnectionId = ConnectionIdPrefix.SonarCloudPrefix + "myorganization";
    
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<CredentialsListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<ICredentialStoreService>());
    }
    
    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<CredentialsListener>();
    }
    
    [TestMethod]
    public async Task GetCredentialsAsync_NullConnectionId_ReturnsNoCredentials()
    {
        var testSubject = CreateTestSubject(out _);

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(null));

        response.Should().BeSameAs(GetCredentialsResponse.NoCredentials);
    }
    
    [TestMethod]
    public async Task GetCredentialsAsync_NullParams_ReturnsNoCredentials()
    {
        var testSubject = CreateTestSubject(out _);

        var response = await testSubject.GetCredentialsAsync(null);

        response.Should().BeSameAs(GetCredentialsResponse.NoCredentials);
    }

    [TestMethod]
    public async Task GetCredentialsAsync_SonarQubeCredentialsNotFound_ReturnsNoCredentials()
    {
        var testSubject = CreateTestSubject(out var credentialStoreMock);
        credentialStoreMock.Setup(x => x.ReadCredentials(It.Is((TargetUri uri) => UriEquals(uri, SonarQubeUri)))).Returns((Credential)null);

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(SonarQubeConnectionId));

        response.Should().BeSameAs(GetCredentialsResponse.NoCredentials);
    }
    
    [TestMethod]
    public async Task GetCredentialsAsync_SonarCloudCredentialsNotFound_ReturnsNoCredentials()
    {
        var testSubject = CreateTestSubject(out var credentialStoreMock);
        credentialStoreMock.Setup(x => x.ReadCredentials(It.Is((TargetUri uri) => UriEquals(uri, SonarCloudUri)))).Returns((Credential)null);

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(SonarQubeConnectionId));

        response.Should().BeSameAs(GetCredentialsResponse.NoCredentials);
    }
    
    [TestMethod]
    public async Task GetCredentialsAsync_SonarQubeUsernameAndPasswordFound_ReturnsUsernameAndPassword()
    {
        const string username = "user1";
        const string password = "password123";
        
        var testSubject = CreateTestSubject(out var credentialStoreMock);
        credentialStoreMock.Setup(x => x.ReadCredentials(It.Is((TargetUri uri) => UriEquals(uri, SonarQubeUri)))).Returns(new Credential(username, password));

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(SonarQubeConnectionId));

        response.Should().BeEquivalentTo(new GetCredentialsResponse(new UsernamePasswordDto(username, password)));
    }

    [TestMethod]
    public async Task GetCredentialsAsync_SonarQubeTokenFound_ReturnsToken()
    {
        const string token = "token123";
        
        var testSubject = CreateTestSubject(out var credentialStoreMock);
        credentialStoreMock.Setup(x => x.ReadCredentials(It.Is((TargetUri uri) => UriEquals(uri, SonarQubeUri)))).Returns(new Credential(token));

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(SonarQubeConnectionId));

        response.Should().BeEquivalentTo(new GetCredentialsResponse(new TokenDto(token)));
    }
    
    [TestMethod]
    public async Task GetCredentialsAsync_SonarCloudUsernameAndPasswordFound_ReturnsUsernameAndPassword()
    {
        const string username = "user1";
        const string password = "password123";
        
        var testSubject = CreateTestSubject(out var credentialStoreMock);
        credentialStoreMock.Setup(x => x.ReadCredentials(It.Is((TargetUri uri) => UriEquals(uri, SonarCloudUri)))).Returns(new Credential(username, password));

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(SonarCloudConnectionId));

        response.Should().BeEquivalentTo(new GetCredentialsResponse(new UsernamePasswordDto(username, password)));
    }
    
    [TestMethod]
    public async Task GetCredentialsAsync_SonarCloudTokenFound_ReturnsToken()
    {
        const string token = "token123";
        
        var testSubject = CreateTestSubject(out var credentialStoreMock);
        credentialStoreMock.Setup(x => x.ReadCredentials(It.Is((TargetUri uri) => UriEquals(uri, SonarCloudUri)))).Returns(new Credential(token));

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(SonarCloudConnectionId));

        response.Should().BeEquivalentTo(new GetCredentialsResponse(new TokenDto(token)));
    }

    private CredentialsListener CreateTestSubject(out Mock<ICredentialStoreService> credentialStoreMock)
    {
        credentialStoreMock = new Mock<ICredentialStoreService>();
        
        return new CredentialsListener(credentialStoreMock.Object);
    }

    private bool UriEquals(TargetUri uri, Uri serverUri) => serverUri.Equals((Uri)uri);
}
