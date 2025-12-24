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

using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Credentials;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests;

[TestClass]
public class CredentialsListenerTests
{
    private const string ConnectionId = "http://myfavouriteuri.nonexistingdomain";
    private static readonly Uri Uri = new(ConnectionId);

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<CredentialsListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<ISolutionBindingCredentialsLoader>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<CredentialsListener>();

    [TestMethod]
    public void GetCredentialsAsync_CredentialsNotFound_ReturnsNoCredentials()
    {
        var testSubject = CreateTestSubject(out var credentialStoreMock);
        credentialStoreMock.Load(Arg.Is<Uri>(x => UriEquals(x, Uri))).ReturnsNull();

        var response = testSubject.GetCredentials(new GetCredentialsParams(ConnectionId));

        response.Should().BeSameAs(GetCredentialsResponse.NoCredentials);
    }

    [TestMethod]
    public void GetCredentialsAsync_SonarQubeUsernameAndPasswordFound_ReturnsUsernameAndPassword()
    {
        const string username = "user1";
        const string password = "password123";

        var testSubject = CreateTestSubject(out var credentialStoreMock);
        var credentials = Substitute.For<IUsernameAndPasswordCredentials>();
        credentials.UserName.Returns(username);
        credentials.Password.Returns(password.ToSecureString());
        credentialStoreMock.Load(Arg.Is<Uri>(x => UriEquals(x, Uri))).Returns(credentials);

        var response = testSubject.GetCredentials(new GetCredentialsParams(ConnectionId));

        response.Should().BeEquivalentTo(new GetCredentialsResponse(new UsernamePasswordDto(username, password)));
    }

    [TestMethod]
    public void GetCredentialsAsync_SonarQubeTokenFound_ReturnsToken()
    {
        const string token = "token123";

        var testSubject = CreateTestSubject(out var credentialStoreMock);
        var credentials = Substitute.For<ITokenCredentials>();
        credentials.Token.Returns(token.ToSecureString());
        credentialStoreMock.Load(Arg.Is<Uri>(x => UriEquals(x, Uri))).Returns(credentials);

        var response = testSubject.GetCredentials(new GetCredentialsParams(ConnectionId));

        response.Should().BeEquivalentTo(new GetCredentialsResponse(new TokenDto(token)));
    }

    private CredentialsListener CreateTestSubject(out ISolutionBindingCredentialsLoader credentialStoreMock)
    {
        credentialStoreMock = Substitute.For<ISolutionBindingCredentialsLoader>();

        return new CredentialsListener(credentialStoreMock);
    }

    private static bool UriEquals(Uri uri, Uri serverUri) => serverUri.ToString() == uri.ToString();
}
