/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Settings;
using SonarLint.VisualStudio.Integration.Vsix.Settings;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(ISonarLintSettings))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SonarLintSettings : ISonarLintSettings
    {
        public const string SettingsRoot = "SonarLintForVisualStudio";

        // Lazily create the settings store on first use (we can't create it in the constructor)
        private readonly Lazy<WritableSettingsStore> writableSettingsStore;

        [ImportingConstructor]
        public SonarLintSettings(IWritableSettingsStoreFactory storeFactory)
        {
            // Called from MEF constructor -> must be free-threaded
            writableSettingsStore = new Lazy<WritableSettingsStore>(() => storeFactory.Create(SettingsRoot), LazyThreadSafetyMode.PublicationOnly);
        }

        internal /* testing purposes */ bool GetValueOrDefault(string key, bool defaultValue)
        {
            try
            {
                return writableSettingsStore.Value?.GetBoolean(SettingsRoot, key, defaultValue)
                ?? defaultValue;
            }
            catch (ArgumentException)
            {
                return defaultValue;
            }
        }

        internal /* testing purposes */ void SetValue(string key, bool value)
        {
            writableSettingsStore.Value?.SetBoolean(SettingsRoot, key, value);
        }

        internal /* testing purposes */ string GetValueOrDefault(string key, string defaultValue)
        {
            try
            {
                return writableSettingsStore.Value?.GetString(SettingsRoot, key, defaultValue)
                ?? defaultValue;
            }
            catch (ArgumentException)
            {
                return defaultValue;
            }
        }

        internal /* testing purposes */ void SetValue(string key, string value)
        {
            writableSettingsStore.Value?.SetString(SettingsRoot, key, value);
        }

        internal /* testing purposes */ int GetValueOrDefault(string key, int defaultValue)
        {
            try
            {
                return writableSettingsStore.Value?.GetInt32(SettingsRoot, key, defaultValue)
                    ?? defaultValue;
            }
            catch (ArgumentException)
            {
                return defaultValue;
            }
        }

        internal /* testing purposes */ void SetValue(string key, int value)
        {
            writableSettingsStore.Value?.SetInt32(SettingsRoot, key, value);
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

        public string JreLocation
        {
            get => this.GetValueOrDefault(nameof(JreLocation), null);
            set => this.SetValue(nameof(JreLocation), value);
        }
    }
}
