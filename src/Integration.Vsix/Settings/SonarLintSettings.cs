/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.Vsix.Settings;

[Export(typeof(ISonarLintSettings))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SonarLintSettings : ISonarLintSettings, IDisposable
{
    public const string SettingsRoot = "SonarLintForVisualStudio";

    // Lazily create the settings store on first use (we can't create it in the constructor)
    private readonly Lazy<WritableSettingsStore> writableSettingsStore;

    private bool disposed;

    [ImportingConstructor]
    public SonarLintSettings(IWritableSettingsStoreFactory storeFactory)
    {
        // Called from MEF constructor -> must be free-threaded
        writableSettingsStore = new Lazy<WritableSettingsStore>(() => storeFactory.Create(SettingsRoot), LazyThreadSafetyMode.PublicationOnly);
    }

    public bool IsActivateMoreEnabled
    {
        get => GetValueOrDefault(nameof(IsActivateMoreEnabled), false);
        set => SetValue(nameof(IsActivateMoreEnabled), value);
    }

    public DaemonLogLevel DaemonLogLevel
    {
        get => (DaemonLogLevel)GetValueOrDefault(nameof(DaemonLogLevel), (int)DaemonLogLevel.Minimal);
        set => SetValue(nameof(DaemonLogLevel), (int)value);
    }

    public string JreLocation
    {
        get => GetValueOrDefault(nameof(JreLocation), string.Empty);
        set => SetValue(nameof(JreLocation), value);
    }

    public bool ShowCloudRegion
    {
        get => GetValueOrDefault(nameof(ShowCloudRegion), false);
        set => SetValue(nameof(ShowCloudRegion), value);
    }

    public bool IsFocusOnNewCodeEnabled
    {
        get => GetValueOrDefault(nameof(IsFocusOnNewCodeEnabled), false);
        set => SetValue(nameof(IsFocusOnNewCodeEnabled), value);
    }

    public void Dispose() => disposed = true;

    internal bool GetValueOrDefault(string key, bool defaultValue)
    {
        try
        {
            return GetWritableSettingsStore()?.GetBoolean(SettingsRoot, key, defaultValue)
                   ?? defaultValue;
        }
        catch (ArgumentException)
        {
            return defaultValue;
        }
    }

    internal string GetValueOrDefault(string key, string defaultValue)
    {
        try
        {
            return GetWritableSettingsStore()?.GetString(SettingsRoot, key, defaultValue)
                   ?? defaultValue;
        }
        catch (ArgumentException)
        {
            return defaultValue;
        }
    }

    internal int GetValueOrDefault(string key, int defaultValue)
    {
        try
        {
            return GetWritableSettingsStore()?.GetInt32(SettingsRoot, key, defaultValue)
                   ?? defaultValue;
        }
        catch (ArgumentException)
        {
            return defaultValue;
        }
    }

    internal void SetValue(string key, bool value) => GetWritableSettingsStore()?.SetBoolean(SettingsRoot, key, value);

    internal void SetValue(string key, string value) => GetWritableSettingsStore()?.SetString(SettingsRoot, key, value ?? string.Empty);

    internal void SetValue(string key, int value) => GetWritableSettingsStore()?.SetInt32(SettingsRoot, key, value);

    private WritableSettingsStore GetWritableSettingsStore() => disposed ? null : writableSettingsStore.Value;
}
