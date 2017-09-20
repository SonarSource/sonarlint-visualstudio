/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Client.Helpers.Tests
{
    [TestClass]
    public class AuthenticationHeaderHelperTests
    {
        [TestMethod]
        public void AuthenticationHeaderHelper_GetAuthToken()
        {
            // Invalid input
            string user = "hello:";
            string password = "world";
            Action action = () => AuthenticationHeaderHelper.GetBasicAuthToken(user, password.ToSecureString());
            action.ShouldThrow<ArgumentOutOfRangeException>();

            // ASCII
            user = "hello";
            password = "world";
            AssertAreEqualUserNameAndPassword(user, password, AuthenticationHeaderHelper.GetBasicAuthToken(user, password.ToSecureString()));

            // UTF-8
            user = "שלום"; // hello in Russian
            password = "你好"; // hello in Chinese
            AssertAreEqualUserNameAndPassword(user, password, AuthenticationHeaderHelper.GetBasicAuthToken(user, password.ToSecureString()));

            // Digits and signs (including ':' in the password)
            user = "1234567890";
            password = "+-/*!:%^&*(){}[];@~#<>,.?|`";
            AssertAreEqualUserNameAndPassword(user, password, AuthenticationHeaderHelper.GetBasicAuthToken(user, password.ToSecureString()));
        }

        private void AssertAreEqualUserNameAndPassword(string expectedUser, string expectedPassword, string userAndPasswordBase64String)
        {
            string userNameAndPassword = AuthenticationHeaderHelper.BasicAuthEncoding.GetString(Convert.FromBase64String(userAndPasswordBase64String));
            // Find first Colon (can't use Split since password may contain ':')
            int index = userNameAndPassword.IndexOf(AuthenticationHeaderHelper.BasicAuthUserNameAndPasswordSeparator, StringComparison.Ordinal);
            (index >= 0).Should().BeTrue("Expected a string in user:password format, got instead '{0}'", userNameAndPassword);

            string[] userNameAndPasswordTokens = new string[2];
            userNameAndPasswordTokens[0] = userNameAndPassword.Substring(0, index);
            userNameAndPasswordTokens[1] = userNameAndPassword.Substring(index + 1, userNameAndPassword.Length - index - 1);

            userNameAndPasswordTokens[0].Should().Be(expectedUser, "Unexpected user name");
            userNameAndPasswordTokens[1].Should().Be(expectedPassword, "Unexpected password");
        }
    }
}