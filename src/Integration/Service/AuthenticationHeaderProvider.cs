//-----------------------------------------------------------------------
// <copyright file="AuthenticationHeaderProvider.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace SonarLint.VisualStudio.Integration.Service
{
    internal class AuthenticationHeaderProvider
    {
        public const string BasicAuthUserNameAndPasswordSeparator = ":";

        /// <summary>
        /// Encoding used to create the basic authentication token
        /// </summary>
        public static readonly Encoding BasicAuthEncoding = UTF8Encoding.UTF8;

        public static AuthenticationHeaderValue GetAuthenticationHeader(ConnectionInformation connectionInfo)
        {
            switch (connectionInfo.Authentication)
            {
                case AuthenticationType.Basic:
                    if (string.IsNullOrWhiteSpace(connectionInfo.UserName))
                    {
                        return null;
                    }
                    else
                    {
                        // See more info: https://www.visualstudio.com/en-us/integrate/get-started/auth/overview
                        return new AuthenticationHeaderValue("Basic", GetBasicAuthToken(connectionInfo.UserName, connectionInfo.Password));
                    }
                default:
                    Debug.Fail("Unsupported Authentication: " + connectionInfo.Authentication);
                    return null;
            }
        }

        internal /*for testing purposes*/ static string GetBasicAuthToken(string user, SecureString password)
        {
            if (!string.IsNullOrEmpty(user) && user.Contains(BasicAuthUserNameAndPasswordSeparator))
            {
                // See also: http://tools.ietf.org/html/rfc2617#section-2
                Debug.Fail("Invalid user name: contains ':'");
                throw new ArgumentOutOfRangeException(nameof(user));
            }

            return Convert.ToBase64String(BasicAuthEncoding.GetBytes(string.Join(BasicAuthUserNameAndPasswordSeparator, user, ConvertToUnsecureString(password))));
        }

        // Copied from http://blogs.msdn.com/b/fpintos/archive/2009/06/12/how-to-properly-convert-securestring-to-string.aspx
        /// <summary>
        /// WARNING: This will create plaintext <see cref="string"/> version of the <see cref="SecureString"/> in
        /// memory which is not encrypted. This could lead to leaking of sensitive information and other security
        /// vulnerabilities – heavy caution is advised.
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
