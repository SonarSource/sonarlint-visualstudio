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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;

namespace SonarLint.VisualStudio.Integration.Vsix
{

    public enum DaemonLogLevel { Verbose, Info, Minimal };

    public interface ISonarLintSettings
    {
        bool ShowServerNuGetTrustWarning { get; set; }
        bool IsActivateMoreEnabled { get; set; }
        bool SkipActivateMoreDialog { get; set; }
        DaemonLogLevel DaemonLogLevel { get; set; }
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
            if (this.writableSettingsStore != null &&
                !this.writableSettingsStore.CollectionExists(SettingsRoot))
            {
                this.writableSettingsStore.CreateCollection(SettingsRoot);
            }
        }

        internal /* testing purposes */ bool GetValueOrDefault(string key, bool defaultValue)
        {
            try
            {
                return this.writableSettingsStore?.GetBoolean(SettingsRoot, key, defaultValue)
                ?? defaultValue;
            }
            catch (System.ArgumentException e)
            {
                return defaultValue;
            }
        }

        internal /* testing purposes */ void SetValue(string key, bool value)
        {
            this.writableSettingsStore?.SetBoolean(SettingsRoot, key, value);
        }

        internal /* testing purposes */ string GetValueOrDefault(string key, string defaultValue)
        {
            try
            {
                return this.writableSettingsStore?.GetString(SettingsRoot, key, defaultValue)
                ?? defaultValue;
            }
            catch (System.ArgumentException e)
            {
                return defaultValue;
            }
        }

        internal /* testing purposes */ void SetValue(string key, string value)
        {
            this.writableSettingsStore?.SetString(SettingsRoot, key, value);
        }

        internal /* testing purposes */ int GetValueOrDefault(string key, int defaultValue)
        {
            try
            {
                return this.writableSettingsStore?.GetInt32(SettingsRoot, key, defaultValue)
                    ?? defaultValue;
            }
            catch (System.ArgumentException e)
            {
                return defaultValue;
            }
        }

        internal /* testing purposes */ void SetValue(string key, int value)
        {
            this.writableSettingsStore?.SetInt32(SettingsRoot, key, value);
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

        public bool SkipActivateMoreDialog
        {
            get { return this.GetValueOrDefault(nameof(SkipActivateMoreDialog), false); }
            set { this.SetValue(nameof(SkipActivateMoreDialog), value); }
        }

        public bool IsActivateMoreEnabled
        {
            get { return this.GetValueOrDefault(nameof(IsActivateMoreEnabled), false); }
            set { this.SetValue(nameof(IsActivateMoreEnabled), value); }
        }

        public DaemonLogLevel DaemonLogLevel
        {
            get { return (DaemonLogLevel)this.GetValueOrDefault(nameof(DaemonLogLevel), (int)DaemonLogLevel.Minimal); }
            set { this.SetValue(nameof(DaemonLogLevel), (int)value); }
        }
    }
}
