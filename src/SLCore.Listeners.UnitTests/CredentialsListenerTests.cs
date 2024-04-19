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
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Credentials;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests;

[TestClass]
public class CredentialsListenerTests
{
    private const string ConnectionId = "connectionId123";
    private static readonly Uri Uri = new("http://myfavouriteuri.nonexistingdomain");
    
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<CredentialsListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<ICredentialProvider>(),
            MefTestHelpers.CreateExport<IConnectionIdHelper>());
    }
    
    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<CredentialsListener>();
    }
    
    [TestMethod]
    public async Task GetCredentialsAsync_NullConnectionId_ReturnsNoCredentials()
    {
        var testSubject = CreateTestSubject(out _, out var connectionIdHelperMock);
        connectionIdHelperMock.Setup(x => x.GetUriFromConnectionId(null)).Returns((Uri)null);
        
        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(null));

        response.Should().BeSameAs(GetCredentialsResponse.NoCredentials);
    }
    
    [TestMethod]
    public async Task GetCredentialsAsync_NullParams_ReturnsNoCredentials()
    {
        var testSubject = CreateTestSubject(out _, out var connectionIdHelperMock);
        connectionIdHelperMock.Setup(x => x.GetUriFromConnectionId(null)).Returns((Uri)null);

        var response = await testSubject.GetCredentialsAsync(null);

        response.Should().BeSameAs(GetCredentialsResponse.NoCredentials);
    }

    [TestMethod]
    public async Task GetCredentialsAsync_CredentialsNotFound_ReturnsNoCredentials()
    {
        var testSubject = CreateTestSubject(out var credentialStoreMock, out var connectionIdHelperMock);
        SetUpConnectionIdHelper(connectionIdHelperMock);
        credentialStoreMock.Setup(x => x.GetCredentials(It.Is((TargetUri targetUri) => UriEquals(targetUri, Uri)))).Returns((ConnectionCredentials)null);

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(ConnectionId));

        response.Should().BeSameAs(GetCredentialsResponse.NoCredentials);
    }


    [TestMethod]
    public async Task GetCredentialsAsync_SonarQubeUsernameAndPasswordFound_ReturnsUsernameAndPassword()
    {
        const string username = "user1";
        const string password = "password123";
        
        var testSubject = CreateTestSubject(out var credentialStoreMock, out var connectionIdHelperMock);
        SetUpConnectionIdHelper(connectionIdHelperMock);
        credentialStoreMock.Setup(x => x.GetCredentials(It.Is((TargetUri targetUri) => UriEquals(targetUri, Uri)))).Returns(new ConnectionCredentials(username, password));

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(ConnectionId));

        response.Should().BeEquivalentTo(new GetCredentialsResponse(new UsernamePasswordDto(username, password)));
    }

    [TestMethod]
    public async Task GetCredentialsAsync_SonarQubeTokenFound_ReturnsToken()
    {
        const string token = "token123";
        
        var testSubject = CreateTestSubject(out var credentialStoreMock, out var connectionIdHelperMock);
        SetUpConnectionIdHelper(connectionIdHelperMock);
        credentialStoreMock.Setup(x => x.GetCredentials(It.Is((TargetUri targetUri) => UriEquals(targetUri, Uri)))).Returns(new ConnectionCredentials(token));

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams(ConnectionId));

        response.Should().BeEquivalentTo(new GetCredentialsResponse(new TokenDto(token)));
    }

    private CredentialsListener CreateTestSubject(out Mock<ICredentialProvider> credentialStoreMock, out Mock<IConnectionIdHelper> connectionIdHelperMock)
    {
        credentialStoreMock = new Mock<ICredentialProvider>();
        connectionIdHelperMock = new Mock<IConnectionIdHelper>();
        
        return new CredentialsListener(credentialStoreMock.Object, connectionIdHelperMock.Object);
    }
    
    private static void SetUpConnectionIdHelper(Mock<IConnectionIdHelper> connectionIdHelperMock)
    {
        connectionIdHelperMock.Setup(x => x.GetUriFromConnectionId(ConnectionId)).Returns(Uri);
    }
    
    private static bool UriEquals(TargetUri uri, Uri serverUri)
    {
        return serverUri.Equals((Uri)uri);
    }
}
