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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.UserSettingsConfiguration;

[Export(typeof(IUserSettingsProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class UserSettingsProvider : IUserSettingsProvider, IDisposable
{
    private static readonly object Lock = new();
    private readonly IActiveSolutionTracker activeSolutionTracker;
    private readonly ILogger logger;
    private readonly IGlobalSettingsStorage globalSettingsStorage;
    private readonly ISolutionSettingsStorage solutionSettingsStorage;
    private UserSettings userSettings;
    private bool disposed;

    [ImportingConstructor]
    public UserSettingsProvider(
        ILogger logger,
        IGlobalSettingsStorage globalSettingsStorage,
        ISolutionSettingsStorage solutionSettingsStorage,
        IActiveSolutionTracker activeSolutionTracker,
        IInitializationProcessorFactory processorFactory)
    {
        this.logger = logger;
        this.globalSettingsStorage = globalSettingsStorage;
        this.solutionSettingsStorage = solutionSettingsStorage;
        this.activeSolutionTracker = activeSolutionTracker;
        InitializationProcessor = processorFactory.CreateAndStart<UserSettingsProvider>(
            [globalSettingsStorage, solutionSettingsStorage, activeSolutionTracker],
            () =>
            {
                if (disposed)
                {
                    return;
                }

                globalSettingsStorage.SettingsFileChanged += OnSettingsFileFileChanged;
                solutionSettingsStorage.SettingsFileChanged += OnSettingsFileFileChanged;
                activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTrackerOnActiveSolutionChanged;
            });
    }

    public UserSettings UserSettings
    {
        get
        {
            lock (Lock)
            {
                userSettings ??= SafeLoadUserSettings();
                return userSettings;
            }
        }
    }

    public event EventHandler SettingsChanged;
    public IInitializationProcessor InitializationProcessor { get; }

    public void Dispose()
    {
        if (!disposed)
        {
            if (InitializationProcessor.IsFinalized)
            {
                solutionSettingsStorage.SettingsFileChanged -= OnSettingsFileFileChanged;
                solutionSettingsStorage.Dispose();
                globalSettingsStorage.SettingsFileChanged -= OnSettingsFileFileChanged;
                globalSettingsStorage.Dispose();
                activeSolutionTracker.ActiveSolutionChanged -= ActiveSolutionTrackerOnActiveSolutionChanged;
            }
            disposed = true;
        }
    }

    private void OnSettingsFileFileChanged(object sender, EventArgs e) => ResetConfiguration();

    private void ActiveSolutionTrackerOnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
    {
        if (!e.IsSolutionOpen || e.SolutionName == null)
        {
            SafeClearUserSettingsCache();
            return;
        }

        ResetConfiguration();
    }

    private void ResetConfiguration()
    {
        SafeClearUserSettingsCache();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SafeClearUserSettingsCache()
    {
        lock (Lock)
        {
            userSettings = null;
        }
    }

    private UserSettings SafeLoadUserSettings()
    {
        var globalSettings = globalSettingsStorage.LoadSettingsFile();

        SolutionAnalysisSettings solutionSettings = null;
        if (solutionSettingsStorage.SettingsFilePath != null)
        {
            solutionSettings = solutionSettingsStorage.LoadSettingsFile();
        }

        if (globalSettings == null && solutionSettings == null)
        {
            logger.WriteLine(Strings.Settings_UsingDefaultSettings);
            return new UserSettings(new AnalysisSettings(), globalSettingsStorage.ConfigurationBaseDirectory);
        }

        var rules = globalSettings?.Rules;
        var globalExclusions = globalSettings?.UserDefinedFileExclusions;
        var properties = solutionSettings?.AnalysisProperties;
        var solutionExclusions = solutionSettings?.UserDefinedFileExclusions;
        var generatedConfigsBase = solutionSettings != null ? solutionSettingsStorage.ConfigurationBaseDirectory : globalSettingsStorage.ConfigurationBaseDirectory;

        return new UserSettings(new AnalysisSettings(rules, globalExclusions, solutionExclusions, properties), generatedConfigsBase);
    }
}
