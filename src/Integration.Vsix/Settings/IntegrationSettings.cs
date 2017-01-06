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

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(IIntegrationSettings))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class IntegrationSettings : Component, IIntegrationSettings, IProfileManager
    {
        public const string SettingsRoot = "SonarLintForVisualStudio";
        public const string ShowServerNuGetTrustWarningKey = "ShowServerNuGetTrustWarning";

        private readonly WritableSettingsStore store;

        #region Constructors and initialization

        [ImportingConstructor]
        public IntegrationSettings([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : this(serviceProvider, new ShellSettingsManager(serviceProvider))
        {
        }

        internal /* for testing purposes */ IntegrationSettings(IServiceProvider serviceProvider, SettingsManager settingsManager)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (settingsManager == null)
            {
                throw new ArgumentNullException(nameof(settingsManager));
            }

            this.store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            Debug.Assert(this.store != null, "Could not get WritableSettingsStore, no settings will be persisted!");
            if (this.store != null)
            {
                this.Initialize();
            }
        }

        private void Initialize()
        {
            if (!this.store.CollectionExists(SettingsRoot))
            {
                this.store.CreateCollection(SettingsRoot);
            }
        }


        #endregion

        #region IProfileManager

        public void SaveSettingsToXml(IVsSettingsWriter writer)
        {
            // We do not support import/export
        }

        public void LoadSettingsFromXml(IVsSettingsReader reader)
        {
            // We do not support import/export
        }

        public void SaveSettingsToStorage()
        {
            // No-op; settings are live
        }

        public void LoadSettingsFromStorage()
        {
            // No-op; settings are live
        }

        public void ResetSettings()
        {
            // Not supported
        }

        #endregion

        #region Read/Write helpers

        internal /* testing purposes */ bool GetValueOrDefault(string key, bool defaultValue)
        {
            return this.store?.GetBoolean(SettingsRoot, key, defaultValue) ?? defaultValue;
        }

        internal /* testing purposes */ void SetValue(string key, bool value)
        {
            this.store?.SetBoolean(SettingsRoot, key, value);
        }

        #endregion

        #region Settings

        [LocalizedCategory(nameof(Strings.WarningsSettingsCategory))]
        [LocalizedDisplayName(nameof(Strings.ShowServerNuGetTrustWarningSettingName))]
        [LocalizedDescription(nameof(Strings.ShowServerNuGetTrustWarningSettingDescription))]
        public bool ShowServerNuGetTrustWarning
        {
            get { return this.GetValueOrDefault(ShowServerNuGetTrustWarningKey, true); }
            set { this.SetValue(ShowServerNuGetTrustWarningKey, value); }
        }

        #endregion
    }
}
