//-----------------------------------------------------------------------
// <copyright file="BasicAuthenticationCredentialsValidator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Globalization;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace SonarLint.VisualStudio.Integration.Connection
{
    internal class BasicAuthenticationCredentialsValidator
    {
        private readonly static Regex InvalidCharacters = new Regex("[:]"); // colon

        private string username;
        private SecureString password;

        public bool IsUsernameValid
        {
            get { return this.InvalidUsernameErrorMessage == null; }
        }
        
        public bool IsPasswordValid
        {
            get { return this.InvalidPasswordErrorMessage == null; }
        }

        /// <summary>
        /// True iff both username (<see cref="IsUsernameValid"/>) and password (<see cref="IsPasswordValid"/>) valid.
        /// </summary>
        public bool IsValid
        {
            get { return this.IsUsernameValid && this.IsPasswordValid; }
        }

        /// <summary>
        /// Error message summary for the Username, or null if valid.
        /// </summary>
        public string InvalidUsernameErrorMessage
        {
            get
            {
                var errorMessage = new StringBuilder();

                // Required?
                bool usernameRequired = !this.password.IsNullOrEmpty() && string.IsNullOrEmpty(this.username);
                if (usernameRequired)
                {
                    AppendError(errorMessage, Resources.Strings.UsernameRequired);
                }

                // Invalid characters?
                if (this.username != null && InvalidCharacters.IsMatch(this.username))
                {
                    AppendError(errorMessage, Resources.Strings.InvalidCharacterColon);
                }

                return CreateMessage(errorMessage);
            }
        }

        /// <summary>
        /// Error message summary for the Password, or null if valid.
        /// </summary>
        public string InvalidPasswordErrorMessage
        {
            get
            {
                var errorMessage = new StringBuilder();

                // Required?
                bool passwordRequired = !string.IsNullOrEmpty(this.username) && this.password.IsNullOrEmpty();
                if (passwordRequired)
                {
                    AppendError(errorMessage, Resources.Strings.PasswordRequired);
                }

                return CreateMessage(errorMessage);
            }
        }

        public void Update(string username, SecureString password)
        {
            this.UpdateUsername(username);
            this.UpdatePassword(password);
        }

        internal /* testing purposes */ void UpdateUsername(string username)
        {
            this.username = username;
        }

        internal /* testing purposes */ void UpdatePassword(SecureString password)
        {
            this.password = password;
        }

        public void Reset()
        {
            this.username = null;
            this.password = null;
        }

        #region Error message helpers

        private static void AppendError(StringBuilder builder, string error, params object[] args)
        {
            IFormatProvider formatProvider = CultureInfo.CurrentCulture;

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }
            builder.AppendFormat(formatProvider, error, args);
        }

        private string CreateMessage(StringBuilder builder)
        {
            return builder.Length == 0 ? null : builder.ToString();
        }

        #endregion
    }
}
