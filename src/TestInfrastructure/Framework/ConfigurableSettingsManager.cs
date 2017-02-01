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
using Microsoft.VisualStudio.Settings;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableSettingsManager : SettingsManager
    {
        public WritableSettingsStore WritableSettingsStore { get; private set; }

        public bool StoreFailsToLoad { get; set; }

        public ConfigurableSettingsManager(WritableSettingsStore store)
        {
            this.WritableSettingsStore = store;
        }

        #region SettingsManager

        public override string GetApplicationDataFolder(ApplicationDataFolder folder)
        {
            throw new NotImplementedException();
        }

        public override EnclosingScopes GetCollectionScopes(string collectionPath)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetCommonExtensionsSearchPaths()
        {
            throw new NotImplementedException();
        }

        public override EnclosingScopes GetPropertyScopes(string collectionPath, string propertyName)
        {
            throw new NotImplementedException();
        }

        public override SettingsStore GetReadOnlySettingsStore(SettingsScope scope)
        {
            throw new NotImplementedException();
        }

        public override WritableSettingsStore GetWritableSettingsStore(SettingsScope scope)
        {
            return this.StoreFailsToLoad ? null : this.WritableSettingsStore;
        }

        #endregion SettingsManager
    }
}