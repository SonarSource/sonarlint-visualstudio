/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Net.Http.Headers;
using System.Security;
using System.Text;
using SonarQube.Client.Models;

namespace SonarQube.Client.Helpers
{
    public static class AuthenticationHeaderFactory
    {
        internal const string BasicAuthCredentialSeparator = ":";
        private const string BearerScheme = "Bearer";
        private const string BasicScheme = "Basic";

        /// <summary>
        /// Encoding used to create the basic authentication token
        /// </summary>
        internal static readonly Encoding BasicAuthEncoding = Encoding.UTF8;

        public static AuthenticationHeaderValue Create(IConnectionCredentials credentials, bool shouldUseBearer = true)
        {
            switch (credentials)
            {
                case ITokenCredentials tokenCredentials:
                {
                    ValidateSecureString(tokenCredentials.Token, nameof(tokenCredentials.Token));
                    return CreateAuthenticationHeaderValueForTokenAuth(shouldUseBearer, tokenCredentials);
                }
                case IBasicAuthCredentials basicAuthCredentials:
                {
                    ValidateCredentials(basicAuthCredentials);
                    return CreateAuthenticationHeaderValueForBasicAuth(basicAuthCredentials.UserName, basicAuthCredentials.Password);
                }
                case INoCredentials:
                    return null;
                default:
                    Debug.Fail("Unsupported Authentication: " + credentials?.GetType());
                    return null;
            }
        }

        private static AuthenticationHeaderValue CreateAuthenticationHeaderValueForTokenAuth(bool shouldUseBearer, ITokenCredentials tokenCredentials)
        {
            if (shouldUseBearer)
            {
                return new AuthenticationHeaderValue(BearerScheme, tokenCredentials.Token.ToUnsecureString());
            }
            return CreateAuthenticationHeaderValueForBasicAuth(tokenCredentials.Token.ToUnsecureString(), new SecureString());
        }

        private static AuthenticationHeaderValue CreateAuthenticationHeaderValueForBasicAuth(string username, SecureString password) => new(BasicScheme, GetBasicAuthToken(username, password));

        // See more info: https://www.visualstudio.com/en-us/integrate/get-started/auth/overview
        internal static string GetBasicAuthToken(string user, SecureString password)
        {
            if (!string.IsNullOrEmpty(user) && user.Contains(BasicAuthCredentialSeparator))
            {
                // See also: http://tools.ietf.org/html/rfc2617#section-2
                Debug.Fail("Invalid user name: contains ':'");
                throw new ArgumentOutOfRangeException(nameof(user));
            }

            return Convert.ToBase64String(BasicAuthEncoding.GetBytes(string.Join(BasicAuthCredentialSeparator,
                user, password.ToUnsecureString())));
        }

        private static void ValidateCredentials(IBasicAuthCredentials credentials)
        {
            if (string.IsNullOrEmpty(credentials.UserName))
            {
                throw new ArgumentException(nameof(IBasicAuthCredentials.UserName));
            }
            ValidateSecureString(credentials.Password, nameof(IBasicAuthCredentials.Password));
        }

        private static void ValidateSecureString(SecureString secureString, string parameterName)
        {
            if (secureString.IsNullOrEmpty())
            {
                throw new ArgumentException(parameterName);
            }
        }
    }
}
