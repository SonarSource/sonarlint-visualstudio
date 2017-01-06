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
