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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Alm.Authentication;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableCredentialStore : ICredentialStore
    {
        internal readonly Dictionary<Uri, Credential> data =
            new Dictionary<Uri, Credential>();

        #region ICredentialStore

        void ICredentialStore.DeleteCredentials(Uri targetUri)
        {
            this.data.Remove(targetUri);
        }

        bool ICredentialStore.ReadCredentials(Uri targetUri, out Credential credentials)
        {
            return this.data.TryGetValue(targetUri, out credentials);
        }

        void ICredentialStore.WriteCredentials(Uri targetUri, Credential credentials)
        {
            this.data[targetUri] = credentials;
        }

        #endregion ICredentialStore
    }
}