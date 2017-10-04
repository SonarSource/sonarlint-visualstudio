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

using System.Linq;
using System.Security;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Connection;
using SonarQube.Client.Helpers;

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