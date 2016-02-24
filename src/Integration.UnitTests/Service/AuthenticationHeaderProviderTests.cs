//-----------------------------------------------------------------------
// <copyright file="AuthenticationHeaderProviderTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class AuthenticationHeaderProviderTests
    {
        [TestMethod]
        public void AuthenticationHeaderProvider_GetAuthToken()
        {
            // Invalid input
            string user = "hello:";
            string password = "world";
            using (new AssertIgnoreScope())
            {
                Exceptions.Expect<ArgumentOutOfRangeException>(() => AuthenticationHeaderProvider.GetBasicAuthToken(user, password.ConvertToSecureString()));
            }

            // Ascii
            user = "hello";
            password = "world";
            AssertAreEqualUserNameAndPassword(user, password, AuthenticationHeaderProvider.GetBasicAuthToken(user, password.ConvertToSecureString()));

            // UTF-8
            user = "שלום"; // hello in Russian
            password = "你好"; // hello in Chinese 
            AssertAreEqualUserNameAndPassword(user, password, AuthenticationHeaderProvider.GetBasicAuthToken(user, password.ConvertToSecureString()));

            // Digits and signs (including ':' in the password)
            user = "1234567890";
            password = "+-/*!:%^&*(){}[];@~#<>,.?|`";
            AssertAreEqualUserNameAndPassword(user, password, AuthenticationHeaderProvider.GetBasicAuthToken(user, password.ConvertToSecureString()));
        }

        private void AssertAreEqualUserNameAndPassword(string expectedUser, string expectedPassword, string userAndPasswordBase64String)
        {
            string userNameAndPassword = AuthenticationHeaderProvider.BasicAuthEncoding.GetString(Convert.FromBase64String(userAndPasswordBase64String));
            // Find first Colon (can't use Split since password may contain ':')
            int index = userNameAndPassword.IndexOf(AuthenticationHeaderProvider.BasicAuthUserNameAndPasswordSeparator);
            Assert.IsTrue(index >= 0, "Expected a string in user:password format, got instead '{0}'", userNameAndPassword);

            string[] userNameAndPasswordTokens = new string[2];
            userNameAndPasswordTokens[0] = userNameAndPassword.Substring(0, index);
            userNameAndPasswordTokens[1] = userNameAndPassword.Substring(index + 1, userNameAndPassword.Length - index - 1);

            Assert.AreEqual(expectedUser, userNameAndPasswordTokens[0], "Unexpected user name");
            Assert.AreEqual(expectedPassword, userNameAndPasswordTokens[1], "Unexpected password");
        }
    }
}