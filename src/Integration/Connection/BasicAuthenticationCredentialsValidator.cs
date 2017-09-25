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
using System.Globalization;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.Connection
{
    internal class BasicAuthenticationCredentialsValidator
    {
        private readonly static Regex InvalidCharacters = new Regex("[:]"); // colon

        private string user;
        private SecureString pwd;

        public bool IsUsernameValid
        {
            get { return this.InvalidUsernameErrorMessage == null; }
        }

        /// <summary>
        /// True if username (<see cref="IsUsernameValid"/>) is valid.
        /// </summary>
        public bool IsValid
        {
            get { return this.IsUsernameValid; }
        }

        /// <summary>
        /// Error message summary for the Username, or null if valid.
        /// </summary>
        public string InvalidUsernameErrorMessage
        {
            get
            {
                var errorMessage = new StringBuilder();

                // Valid options:
                // 1. No user name or password -> anonymous access
                // 2. User name and password
                // 3. User name only, which is treated as a user token: http://docs.sonarqube.org/display/SONAR/User+Token.

                // Required?
                bool usernameRequired = !this.pwd.IsNullOrEmpty() && string.IsNullOrEmpty(this.user);
                if (usernameRequired)
                {
                    AppendError(errorMessage, Resources.Strings.UsernameRequired);
                }

                // Invalid characters?
                if (this.user != null && InvalidCharacters.IsMatch(this.user))
                {
                    AppendError(errorMessage, Resources.Strings.InvalidCharacterColon);
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
            this.user = username;
        }

        internal /* testing purposes */ void UpdatePassword(SecureString password)
        {
            this.pwd = password;
        }

        public void Reset()
        {
            this.user = null;
            this.pwd = null;
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
