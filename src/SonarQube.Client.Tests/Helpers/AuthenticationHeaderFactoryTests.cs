/*
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

using System.Security;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Helpers
{
    [TestClass]
    public class AuthenticationHeaderFactoryTests
    {
        private const string Password = "password";
        private const string Token = "token";
        private const string Username = "username";

        [TestMethod]
        public void GetAuthToken_ReturnsExpectedString()
        {
            // Invalid input
            string user = "hello:";
            string password = "world";
            using (new AssertIgnoreScope())
            {
                Action action = () => AuthenticationHeaderFactory.GetBasicAuthToken(user, password.ToSecureString());
                action.Should().ThrowExactly<ArgumentOutOfRangeException>();
            }

            // ASCII
            user = "hello";
            password = "world";
            AssertAreEqualUserNameAndPassword(user, password,
                AuthenticationHeaderFactory.GetBasicAuthToken(user, password.ToSecureString()));

            // UTF-8
            user = "שלום"; // hello in Russian
            password = "你好"; // hello in Chinese
            AssertAreEqualUserNameAndPassword(user, password,
                AuthenticationHeaderFactory.GetBasicAuthToken(user, password.ToSecureString()));

            // Digits and signs (including ':' in the password)
            user = "1234567890";
            password = "+-/*!:%^&*(){}[];@~#<>,.?|`";
            AssertAreEqualUserNameAndPassword(user, password,
                AuthenticationHeaderFactory.GetBasicAuthToken(user, password.ToSecureString()));
        }

        [TestMethod]
        public void Create_NoCredentials_ReturnsNull()
        {
            var authenticationHeaderValue = AuthenticationHeaderFactory.Create(new SonarQube.Client.Models.NoCredentials());

            authenticationHeaderValue.Should().BeNull();
        }

        [TestMethod]
        public void Create_UnsupportedAuthentication_ReturnsNull()
        {
            using var scope = new AssertIgnoreScope();

            var authenticationHeaderValue = AuthenticationHeaderFactory.Create(null);

            authenticationHeaderValue.Should().BeNull();
        }

        [TestMethod]
        public void Create_BasicAuth_UsernameIsNull_Throws()
        {
            var credentials = MockBasicAuthCredentials(null, Password.ToSecureString());

            var act = () => AuthenticationHeaderFactory.Create(credentials);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Create_BasicAuth_PasswordIsNull_Throws()
        {
            var credentials = MockBasicAuthCredentials(Username, null);

            var act = () => AuthenticationHeaderFactory.Create(credentials);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Create_BasicAuth_PasswordIsEmpty_Throws()
        {
            var credentials = MockBasicAuthCredentials(Username, "".ToSecureString());

            var act = () => AuthenticationHeaderFactory.Create(credentials);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Create_BasicAuth_CredentialsProvided_ReturnsBasicScheme()
        {
            var credentials = MockBasicAuthCredentials(Username, Password.ToSecureString());

            var authenticationHeaderValue = AuthenticationHeaderFactory.Create(credentials);

            authenticationHeaderValue.Scheme.Should().Be("Basic");
            AssertAreEqualUserNameAndPassword(Username, Password, authenticationHeaderValue.Parameter);
        }

        [TestMethod]
        public void Create_BearerSupported_TokenIsNull_Throws()
        {
            var credentials = MockTokenCredentials(null);

            var act = () => AuthenticationHeaderFactory.Create(credentials);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Create_BearerSupported_TokenIsEmpty_Throws()
        {
            var credentials = MockTokenCredentials("".ToSecureString());

            var act = () => AuthenticationHeaderFactory.Create(credentials);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Create_BearerSupported_TokenIsFilled_ReturnsBearerScheme()
        {
            var credentials = MockTokenCredentials(Token.ToSecureString());

            var authenticationHeaderValue = AuthenticationHeaderFactory.Create(credentials);

            authenticationHeaderValue.Scheme.Should().Be("Bearer");
            authenticationHeaderValue.Parameter.Should().Be(Token);
        }

        [TestMethod]
        public void Create_BearerNotSupported_TokenIsFilled_ReturnsBasicScheme()
        {
            var credentials = MockTokenCredentials(Token.ToSecureString());

            var authenticationHeaderValue = AuthenticationHeaderFactory.Create(credentials, shouldUseBearer: false);

            authenticationHeaderValue.Scheme.Should().Be("Basic");
            AssertAreEqualUserNameAndPassword(Token, string.Empty, authenticationHeaderValue.Parameter);
        }

        [TestMethod]
        public void Create_BearerNotSupported_TokenIsEmpty_Throws()
        {
            var credentials = MockTokenCredentials("".ToSecureString());

            var act = () => AuthenticationHeaderFactory.Create(credentials, shouldUseBearer: false);

            act.Should().Throw<ArgumentException>();
        }

        private static void AssertAreEqualUserNameAndPassword(
            string expectedUser,
            string expectedPassword,
            string userAndPasswordBase64String)

        {
            string userNameAndPassword =
                AuthenticationHeaderFactory.BasicAuthEncoding.GetString(Convert.FromBase64String(userAndPasswordBase64String));
            // Find first Colon (can't use Split since password may contain ':')
            int index = userNameAndPassword.IndexOf(AuthenticationHeaderFactory.BasicAuthCredentialSeparator,
                StringComparison.Ordinal);
            (index >= 0).Should().BeTrue("Expected a string in user:password format, got instead '{0}'", userNameAndPassword);

            string[] userNameAndPasswordTokens = new string[2];
            userNameAndPasswordTokens[0] = userNameAndPassword.Substring(0, index);
            userNameAndPasswordTokens[1] = userNameAndPassword.Substring(index + 1, userNameAndPassword.Length - index - 1);

            userNameAndPasswordTokens.Should().HaveElementAt(0, expectedUser);
            userNameAndPasswordTokens.Should().HaveElementAt(1, expectedPassword);
        }

        private static IUsernameAndPasswordCredentials MockBasicAuthCredentials(string userName, SecureString password)
        {
            var mock = Substitute.For<IUsernameAndPasswordCredentials>();
            mock.UserName.Returns(userName);
            mock.Password.Returns(password);
            return mock;
        }

        private static ITokenCredentials MockTokenCredentials(SecureString token)
        {
            var mock = Substitute.For<ITokenCredentials>();
            mock.Token.Returns(token);
            return mock;
        }
    }
}
