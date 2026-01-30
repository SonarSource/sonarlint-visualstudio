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

using Microsoft.Alm.Authentication;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class CredentialsExtensionMethodsTests
{
    [TestMethod]
    public void ToCredential_NullCredentials_Throws()
    {
        Action act = () => ((IConnectionCredentials)null).ToCredential();

        act.Should().Throw<NotSupportedException>();
    }

    [TestMethod]
    public void ToCredential_BasicAuthCredentials_ReturnsExpected()
    {
        var basicAuthCredentials = new UsernameAndPasswordCredentials("user", "pwd".ToSecureString());

        var result = basicAuthCredentials.ToCredential();

        result.Username.Should().Be(basicAuthCredentials.UserName);
        result.Password.Should().Be(basicAuthCredentials.Password.ToUnsecureString());
    }

    [TestMethod]
    public void ToCredential_TokenAuthCredentials_ReturnsExpected()
    {
        var tokenAuthCredentials = new TokenAuthCredentials("token".ToSecureString());

        var result = tokenAuthCredentials.ToCredential();

        result.Username.Should().Be(tokenAuthCredentials.Token.ToUnsecureString());
        result.Password.Should().Be(string.Empty);
    }

    [TestMethod]
    public void ToConnectionCredentials_UsernameIsEmpty_ReturnsBasicAuthCredentialsWithPasswordAsToken()
    {
        var credential = new Credential(string.Empty, "token");

        var result = credential.ToConnectionCredentials();

        var basicAuth = result as UsernameAndPasswordCredentials;
        basicAuth.Should().NotBeNull();
        basicAuth.UserName.Should().Be(credential.Username);
        basicAuth.Password.ToUnsecureString().Should().Be(credential.Password);
    }

    /// <summary>
    /// For backward compatibility
    /// </summary>
    [TestMethod]
    public void ToConnectionCredentials_PasswordIsEmpty_ReturnsTokenAuthCredentialsWithUsernameAsToken()
    {
        var credential = new Credential("token", string.Empty);

        var result = credential.ToConnectionCredentials();

        var tokenAuth = result as TokenAuthCredentials;
        tokenAuth.Should().NotBeNull();
        tokenAuth.Token.ToUnsecureString().Should().Be(credential.Username);
    }

    [TestMethod]
    public void ToConnectionCredentials_PasswordAndUsernameFilled_ReturnsBasicAuthCredentials()
    {
        var credential = new Credential("username", "pwd");

        var result = credential.ToConnectionCredentials();

        var basicAuth = result as UsernameAndPasswordCredentials;
        basicAuth.Should().NotBeNull();
        basicAuth.UserName.Should().Be(credential.Username);
        basicAuth.Password.ToUnsecureString().Should().Be(credential.Password);
    }

    [TestMethod]
    public void ToConnectionCredentials_Null_ReturnsNull()
    {
        var result = ((Credential)null).ToConnectionCredentials();

        result.Should().BeNull();
    }
}
