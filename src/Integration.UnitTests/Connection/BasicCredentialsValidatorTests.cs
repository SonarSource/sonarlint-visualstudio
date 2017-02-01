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

using System.Linq;
using System.Security;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Connection;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class BasicAuthenticationCredentialsValidatorTests
    {
        private static readonly char[] InvalidCharactersSubset = new[]
        {
            ':',      // colon
        };

        private static readonly char[] ValidCharacters = Enumerable.Range(1, 127)
                                                                 .Select(i => (char)i)
                                                                 .Except(InvalidCharactersSubset)
                                                                 .ToArray();

        [TestMethod]
        public void BasicCredentialsValidator_IsUsernameValid()
        {
            // Arrange
            var validator = new BasicAuthenticationCredentialsValidator();

            // Valid characters only
            VerifyUsername(validator, new string(ValidCharacters), expectedValid: true);

            // Invalid characters
            foreach (char c in InvalidCharactersSubset)
            {
                VerifyUsername(validator, $"admin{c}bad", expectedValid: false);
            }
        }

        [TestMethod]
        [Description("Verify that credentials are valid only if the username and password are both empty or both non-empty.")]
        public void BasicCredentialsValidator_IsValid_UsernameAndPasswordRequiredCombinations()
        {
            // Arrange
            var validator = new BasicAuthenticationCredentialsValidator();

            // Valid - user name and password
            VerifyUsernameAndPassword(validator, string.Empty, string.Empty, expectedValid: true);
            VerifyUsernameAndPassword(validator, "admin", "letmein", expectedValid: true);

            // Valid - just a token, no password
            VerifyUsernameAndPassword(validator, "eab759ea1ba04baa58dab0ac9e964f475a51ffe5", string.Empty, expectedValid: true);
            VerifyUsernameAndPassword(validator, "user name or token - can't tell which", string.Empty, expectedValid: true);

            // Not valid
            VerifyUsernameAndPassword(validator, string.Empty, "letmein", expectedValid: false);
        }

        #region Helpers

        private void VerifyUsernameAndPassword(BasicAuthenticationCredentialsValidator validator, string username, string password, bool expectedValid)
        {
            SecureString securePassword = password.ToSecureString();

            validator.Update(username, securePassword);

            bool result = validator.IsValid;

            if (expectedValid)
            {
                result.Should().BeTrue($"Username '{username}' and password '{password}' should be valid");
            }
            else
            {
                result.Should().BeFalse($"Username '{username}' and password '{password}' should be invalid");
            }
        }

        private void VerifyUsername(BasicAuthenticationCredentialsValidator validator, string username, bool expectedValid)
        {
            validator.UpdateUsername(username);

            bool result = validator.IsUsernameValid;

            if (expectedValid)
            {
                result.Should().BeTrue("Username '{0}' should be valid", username);
            }
            else
            {
                result.Should().BeFalse($"Username '{username}' should be invalid");
            }
        }

        #endregion Helpers
    }
}