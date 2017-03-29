/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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
