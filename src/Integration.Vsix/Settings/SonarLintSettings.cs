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

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public interface ISonarLintSettings
    {
        bool ShowServerNuGetTrustWarning { get; set; }
        bool IsAnonymousDataShared { get; set; }
    }

    [Export(typeof(ISonarLintSettings))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SonarLintSettings : ISonarLintSettings, IProfileManager
    {
        public const string SettingsRoot = "SonarLintForVisualStudio";

        private readonly WritableSettingsStore writableSettingsStore;

        [ImportingConstructor]
        public SonarLintSettings(SVsServiceProvider serviceProvider)
            : this(new ShellSettingsManager(serviceProvider))
        {
        }

        internal SonarLintSettings(SettingsManager settingsManager)
        {
            if (settingsManager == null)
            {
                throw new ArgumentNullException(nameof(settingsManager));
            }

            this.writableSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!this.writableSettingsStore.CollectionExists(SettingsRoot))
            {
                this.writableSettingsStore.CreateCollection(SettingsRoot);
            }
        }

        internal /* testing purposes */ bool GetValueOrDefault(string key, bool defaultValue)
        {
            return this.writableSettingsStore?.GetBoolean(SettingsRoot, key, defaultValue)
                ?? defaultValue;
        }

        internal /* testing purposes */ void SetValue(string key, bool value)
        {
            this.writableSettingsStore?.SetBoolean(SettingsRoot, key, value);
        }

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
        #endregion // IProfileManager

        public bool ShowServerNuGetTrustWarning
        {
            get { return this.GetValueOrDefault(nameof(ShowServerNuGetTrustWarning), true); }
            set { this.SetValue(nameof(ShowServerNuGetTrustWarning), value); }
        }

        public bool IsAnonymousDataShared
        {
            get { return this.GetValueOrDefault(nameof(IsAnonymousDataShared), true); }
            set { this.SetValue(nameof(IsAnonymousDataShared), value); }
        }
    }
}
