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
using NSubstitute;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

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
        var uri = new TargetUri("http://localhost");
        const string username = "username123";
        const string password = "password123";
        credentialStoreService.ReadCredentials(uri).Returns(new Credential(username, password));

        var connectionCredentials = testSubject.GetCredentials(uri);

        connectionCredentials.Username.Should().BeSameAs(username);
        connectionCredentials.Password.Should().BeSameAs(password);
        credentialStoreService.Received(1).ReadCredentials(uri);
    }
    
    [TestMethod]
    public void GetCredentials_NoCredentials_ReturnsNull()
    {
        var testSubject = CreateTestSubject(out var credentialStoreService);
        var uri = new TargetUri("http://localhost");
        credentialStoreService.ReadCredentials(uri).Returns((Credential)null);

        var connectionCredentials = testSubject.GetCredentials(uri);

        connectionCredentials.Should().BeNull();
        credentialStoreService.Received(1).ReadCredentials(uri);
    }

    private CredentialProvider CreateTestSubject(out ICredentialStoreService credentialStoreService)
    {
        credentialStoreService = Substitute.For<ICredentialStoreService>();
        return new CredentialProvider(credentialStoreService);
    }
}
