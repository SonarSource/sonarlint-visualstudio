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
using System.Security;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Credentials for user authentication.
    /// </summary>
    public sealed class Credential : Secret
    {
        /// <summary>
        /// Creates a credential object with a username and password pair.
        /// </summary>
        /// <param name="username">The username value of the <see cref="Credential"/>.</param>
        /// <param name="password">The password value of the <see cref="Credential"/>.</param>
        public Credential(string username, SecureString password)
        {
            this.Username = username;
            this.Password = password;
        }

        /// <summary>
        /// Secret related to the username.
        /// </summary>
        public readonly SecureString Password;
        /// <summary>
        /// Unique identifier of the user.
        /// </summary>
        public readonly string Username;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static void Validate(Credential credentials)
        {
            if (credentials == null)
            {
                throw new ArgumentNullException(nameof(credentials));
            }

            if (credentials.Password.Length > NativeMethods.Credential.PasswordMaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(credentials.Password));
            }

            if (credentials.Username.Length > NativeMethods.Credential.UsernameMaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(credentials.Username));
            }
        }
    }
}

