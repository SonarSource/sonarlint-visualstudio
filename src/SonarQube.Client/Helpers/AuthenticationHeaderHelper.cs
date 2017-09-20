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
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using SonarQube.Client.Models;

namespace SonarQube.Client.Helpers
{
    public static class AuthenticationHeaderHelper
    {
        public const string BasicAuthUserNameAndPasswordSeparator = ":";

        /// <summary>
        /// Encoding used to create the basic authentication token
        /// </summary>
        public static readonly Encoding BasicAuthEncoding = Encoding.UTF8;

        public static AuthenticationHeaderValue GetAuthenticationHeader(ConnectionDTO connectionInfo)
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

        public static string GetBasicAuthToken(string user, SecureString password)
        {
            if (!string.IsNullOrEmpty(user) && user.Contains(BasicAuthUserNameAndPasswordSeparator))
            {
                // See also: http://tools.ietf.org/html/rfc2617#section-2
                Debug.Fail("Invalid user name: contains ':'");
                throw new ArgumentOutOfRangeException(nameof(user));
            }

            return Convert.ToBase64String(BasicAuthEncoding.GetBytes(string.Join(BasicAuthUserNameAndPasswordSeparator,
                user, ConvertToUnsecureString(password))));
        }

        // Copied from http://blogs.msdn.com/b/fpintos/archive/2009/06/12/how-to-properly-convert-securestring-to-string.aspx
        /// <summary>
        /// WARNING: This will create plain-text <see cref="string"/> version of the <see cref="SecureString"/> in
        /// memory which is not encrypted. This could lead to leaking of sensitive information and other security
        /// vulnerabilities - heavy caution is advised.
        /// </summary>
        [SecurityCritical]
        private static string ConvertToUnsecureString(SecureString secureString)
        {
            if (secureString == null)
            {
                throw new ArgumentNullException(nameof(secureString));
            }

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
