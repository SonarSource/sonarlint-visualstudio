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

using Microsoft.Alm.Authentication;
using SonarLint.VisualStudio.ConnectedMode.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding;

[TestClass]
public class CredentialProviderTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<CredentialProvider, ICredentialProvider>(
            MefTestHelpers.CreateExport<ICredentialStoreService>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<CredentialProvider>();
    }

    [TestMethod]
    public void GetCredentials_ProxiesToICredentialStoreService()
    {
        var testSubject = CreateTestSubject(out var credentialStoreService);
        var uri = new Uri("http://localhost/");
        const string username = "username123";
        const string password = "password123";
        credentialStoreService.ReadCredentials(Arg.Is<TargetUri>(targetUri => targetUri.ToString() == uri.ToString())).Returns(new Credential(username, password));

        var connectionCredentials = testSubject.GetCredentials(uri);

        connectionCredentials.Username.Should().BeSameAs(username);
        connectionCredentials.Password.Should().BeSameAs(password);
        credentialStoreService.ReceivedWithAnyArgs(1).ReadCredentials(default);
    }
    
    [TestMethod]
    public void GetCredentials_NoCredentials_ReturnsNull()
    {
        var testSubject = CreateTestSubject(out var credentialStoreService);
        var uri = new Uri("http://localhost");
        credentialStoreService.ReadCredentials(Arg.Is<TargetUri>(targetUri => targetUri.ToString() == uri.ToString())).Returns((Credential)null);

        var connectionCredentials = testSubject.GetCredentials(uri);

        connectionCredentials.Should().BeNull();
        credentialStoreService.ReceivedWithAnyArgs(1).ReadCredentials(default);
    }

    private CredentialProvider CreateTestSubject(out ICredentialStoreService credentialStoreService)
    {
        credentialStoreService = Substitute.For<ICredentialStoreService>();
        return new CredentialProvider(credentialStoreService);
    }
}
