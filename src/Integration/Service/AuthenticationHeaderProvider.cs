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

using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace SonarLint.VisualStudio.Integration.Service
{
    internal static class AuthenticationHeaderProvider
    {
        public const string BasicAuthUserNameAndPasswordSeparator = ":";

        /// <summary>
        /// Encoding used to create the basic authentication token
        /// </summary>
        public static readonly Encoding BasicAuthEncoding = Encoding.UTF8;

        public static AuthenticationHeaderValue GetAuthenticationHeader(ConnectionInformation connectionInfo)
        {
            if (connectionInfo.Authentication == AuthenticationType.Basic)
            {
                return string.IsNullOrWhiteSpace(connectionInfo.UserName)
                    ? null
                    : new AuthenticationHeaderValue("Basic", GetBasicAuthToken(connectionInfo.UserName, connectionInfo.Password));
                // See more info: https://www.visualstudio.com/en-us/integrate/get-started/auth/overview
            }
            else
            {
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
