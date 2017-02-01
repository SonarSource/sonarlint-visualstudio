/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using SonarLint.VisualStudio.Integration.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;
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
                Exceptions.Expect<ArgumentOutOfRangeException>(() => AuthenticationHeaderProvider.GetBasicAuthToken(user, password.ToSecureString()));
            }

            // ASCII
            user = "hello";
            password = "world";
            AssertAreEqualUserNameAndPassword(user, password, AuthenticationHeaderProvider.GetBasicAuthToken(user, password.ToSecureString()));

            // UTF-8
            user = "שלום"; // hello in Russian
            password = "你好"; // hello in Chinese
            AssertAreEqualUserNameAndPassword(user, password, AuthenticationHeaderProvider.GetBasicAuthToken(user, password.ToSecureString()));

            // Digits and signs (including ':' in the password)
            user = "1234567890";
            password = "+-/*!:%^&*(){}[];@~#<>,.?|`";
            AssertAreEqualUserNameAndPassword(user, password, AuthenticationHeaderProvider.GetBasicAuthToken(user, password.ToSecureString()));
        }

        private void AssertAreEqualUserNameAndPassword(string expectedUser, string expectedPassword, string userAndPasswordBase64String)
        {
            string userNameAndPassword = AuthenticationHeaderProvider.BasicAuthEncoding.GetString(Convert.FromBase64String(userAndPasswordBase64String));
            // Find first Colon (can't use Split since password may contain ':')
            int index = userNameAndPassword.IndexOf(AuthenticationHeaderProvider.BasicAuthUserNameAndPasswordSeparator, StringComparison.Ordinal);
            (index >= 0).Should().BeTrue("Expected a string in user:password format, got instead '{0}'", userNameAndPassword);

            string[] userNameAndPasswordTokens = new string[2];
            userNameAndPasswordTokens[0] = userNameAndPassword.Substring(0, index);
            userNameAndPasswordTokens[1] = userNameAndPassword.Substring(index + 1, userNameAndPassword.Length - index - 1);

            userNameAndPasswordTokens[0].Should().Be(expectedUser, "Unexpected user name");
            userNameAndPasswordTokens[1].Should().Be(expectedPassword, "Unexpected password");
        }
    }
}