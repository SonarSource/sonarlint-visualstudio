//-----------------------------------------------------------------------
// <copyright file="BasicCredentialsValidatorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Security;

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
            // Setup
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
            // Setup
            var validator = new BasicAuthenticationCredentialsValidator();

            // Valid
            VerifyUsernameAndPassword(validator, string.Empty, string.Empty, expectedValid: true);
            VerifyUsernameAndPassword(validator, "admin", "letmein", expectedValid: true);

            // Not valid
            VerifyUsernameAndPassword(validator, "admin", string.Empty, expectedValid: false);
            VerifyUsernameAndPassword(validator, string.Empty, "letmein", expectedValid: false);
        }

        #region Helpers

        private void VerifyUsernameAndPassword(BasicAuthenticationCredentialsValidator validator, string username, string password, bool expectedValid)
        {
            SecureString securePassword = password.ConvertToSecureString();

            validator.Update(username, securePassword);

            bool result = validator.IsValid;

            if (expectedValid)
            {
                Assert.IsTrue(result, $"Username '{username}' and password '{password}' should be valid");
            }
            else
            {
                Assert.IsFalse(result, $"Username '{username}' and password '{password}' should be invalid");
            }
        }

        private void VerifyUsername(BasicAuthenticationCredentialsValidator validator, string username, bool expectedValid)
        {
            validator.UpdateUsername(username);

            bool result = validator.IsUsernameValid;

            if (expectedValid)
            {
                Assert.IsTrue(result, $"Username '{username}' should be valid");
            }
            else
            {
                Assert.IsFalse(result, $"Username '{username}' should be invalid");
            }
        }

        private void VerifyPassword(BasicAuthenticationCredentialsValidator validator, string password, bool expectedValid)
        {
            SecureString securePassword = password.ConvertToSecureString();

            validator.UpdatePassword(securePassword);

            bool result = validator.IsUsernameValid;

            if (expectedValid)
            {
                Assert.IsTrue(result, $"Password '{password}' should be valid");
            }
            else
            {
                Assert.IsFalse(result, $"Password '{password}' should be invalid");
            }
        }

        #endregion

    }
}
