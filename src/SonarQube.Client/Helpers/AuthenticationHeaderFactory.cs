/*
 * SonarQube Client
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
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using SonarQube.Client.Models;

namespace SonarQube.Client.Helpers
{
    public static class AuthenticationHeaderFactory
    {
        internal const string BasicAuthUserNameAndPasswordSeparator = ":";

        /// <summary>
        /// Encoding used to create the basic authentication token
        /// </summary>
        internal static readonly Encoding BasicAuthEncoding = Encoding.UTF8;

        public static AuthenticationHeaderValue Create(ConnectionDTO connectionInfo)
        {
            if (connectionInfo.Authentication == AuthenticationType.Basic)
            {
                return string.IsNullOrWhiteSpace(connectionInfo.Login)
                    ? null
                    : new AuthenticationHeaderValue("Basic", GetBasicAuthToken(connectionInfo.Login,
                        connectionInfo.Password));
                // See more info: https://www.visualstudio.com/en-us/integrate/get-started/auth/overview
            }
            else
            {
                Debug.Fail("Unsupported Authentication: " + connectionInfo.Authentication);
                return null;
            }
        }

        internal static string GetBasicAuthToken(string user, SecureString password)
        {
            if (!string.IsNullOrEmpty(user) && user.Contains(BasicAuthUserNameAndPasswordSeparator))
            {
                // See also: http://tools.ietf.org/html/rfc2617#section-2
                Debug.Fail("Invalid user name: contains ':'");
                throw new ArgumentOutOfRangeException(nameof(user));
            }

            return Convert.ToBase64String(BasicAuthEncoding.GetBytes(string.Join(BasicAuthUserNameAndPasswordSeparator,
                user, password.ToUnsecureString())));
        }
    }
}
