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

using System.Security;

namespace SonarLint.VisualStudio.Integration.Service
{
    internal static class SecureStringExtensions
    {
        /// <summary>
        /// Create a read-only copy of a <see cref="SecureString"/>.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <see cref="SecureString.Copy"/> followed by <see cref="SecureString.MakeReadOnly"/>.
        /// </remarks>
        /// <returns>Read-only copy of <see cref="SecureString"/></returns>
        public static SecureString CopyAsReadOnly(this SecureString secureString)
        {
            SecureString copy = secureString.Copy();
            copy.MakeReadOnly();
            return copy;
        }

        public static bool IsEmpty(this SecureString secureString)
        {
            return secureString.Length == 0;
        }

        public static bool IsNullOrEmpty(this SecureString secureString)
        {
            return secureString == null || secureString.IsEmpty();
        }
    }
}
