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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.UserSettingsConfiguration;

[Export(typeof(IUserSettingsProvider))]
[Export(typeof(IGlobalUserSettingsUpdater))]
[Export(typeof(ISolutionUserSettingsUpdater))]
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
    private SolutionUserSettingsUpdater solutionSettingsUpdater;
    private GlobalUserSettingsUpdater globalSettingsUpdater;

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

                globalSettingsStorage.SettingsFileChanged += OnSettingsFileChanged;
                solutionSettingsStorage.SettingsFileChanged += OnSettingsFileChanged;
                // The subscription to the ActiveSolutionTracker events should happen after the ISolutionSettingsStorage is initialized,
                // as the storage depends on ActiveSolutionChanged to calculate the path to the settings file
                // This prevents a situation in which we might try to load the settings file on solution changes before the storage is actually initialized
                activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTrackerOnActiveSolutionChanged;

                solutionSettingsUpdater = new SolutionUserSettingsUpdater(solutionSettingsStorage);
                globalSettingsUpdater = new GlobalUserSettingsUpdater(globalSettingsStorage);
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

    #region IGlobalUserSettingsUpdater

    public ImmutableDictionary<string, RuleConfig> Rules => UserSettings.AnalysisSettings.Rules;
    public ImmutableArray<string> GlobalFileExclusions => UserSettings.AnalysisSettings.GlobalFileExclusions;

    public void DisableRule(string ruleId)
    {
        globalSettingsUpdater.DisableRule(UserSettings, ruleId);
        SafeClearUserSettingsCache();
    }

    public void UpdateGlobalFileExclusions(IEnumerable<string> exclusions)
    {
        globalSettingsUpdater.UpdateFileExclusions(UserSettings, exclusions);
        SafeClearUserSettingsCache();
    }

    private sealed class GlobalUserSettingsUpdater(IGlobalSettingsStorage globalSettingsStorage)
    {
        internal void DisableRule(UserSettings userSettings, string ruleId)
        {
            Debug.Assert(!string.IsNullOrEmpty(ruleId), "DisableRule: ruleId should not be null/empty");

            var newRules = userSettings.AnalysisSettings.Rules.SetItem(ruleId, new RuleConfig(RuleLevel.Off));
            var globalSettings = new GlobalAnalysisSettings(newRules, userSettings.AnalysisSettings.GlobalFileExclusions);
            globalSettingsStorage.SaveSettingsFile(globalSettings);
        }

        internal void UpdateFileExclusions(UserSettings userSettings, IEnumerable<string> exclusions)
        {
            var globalSettings = new GlobalAnalysisSettings(userSettings.AnalysisSettings.Rules, exclusions.ToImmutableArray());
            globalSettingsStorage.SaveSettingsFile(globalSettings);
        }
    }

    #endregion IGlobalUserSettingsUpdater

    #region ISolutionUserSettingsUpdater

    public ImmutableDictionary<string, string> AnalysisProperties => UserSettings.AnalysisSettings.AnalysisProperties;
    public ImmutableArray<string> SolutionFileExclusions => UserSettings.AnalysisSettings.SolutionFileExclusions;

    public void UpdateAnalysisProperties(Dictionary<string, string> analysisProperties)
    {
        solutionSettingsUpdater.UpdateAnalysisProperties(UserSettings, analysisProperties);
        SafeClearUserSettingsCache();
    }

    public void UpdateSolutionFileExclusions(IEnumerable<string> exclusions)
    {
        solutionSettingsUpdater.UpdateFileExclusions(UserSettings, exclusions);
        SafeClearUserSettingsCache();
    }

    private sealed class SolutionUserSettingsUpdater(ISolutionSettingsStorage solutionSettingsStorage)
    {
        internal void UpdateFileExclusions(UserSettings userSettings, IEnumerable<string> exclusions)
        {
            var solutionSettings = new SolutionAnalysisSettings(userSettings.AnalysisSettings.AnalysisProperties, exclusions.ToImmutableArray());
            solutionSettingsStorage.SaveSettingsFile(solutionSettings);
        }

        internal void UpdateAnalysisProperties(UserSettings userSettings, Dictionary<string, string> analysisProperties)
        {
            var solutionSettings = new SolutionAnalysisSettings(analysisProperties, userSettings.AnalysisSettings.SolutionFileExclusions);
            solutionSettingsStorage.SaveSettingsFile(solutionSettings);
        }
    }

    #endregion ISolutionUserSettingsUpdater

    public void Dispose()
    {
        if (!disposed)
        {
            if (InitializationProcessor.IsFinalized)
            {
                solutionSettingsStorage.SettingsFileChanged -= OnSettingsFileChanged;
                solutionSettingsStorage.Dispose();
                globalSettingsStorage.SettingsFileChanged -= OnSettingsFileChanged;
                globalSettingsStorage.Dispose();
                activeSolutionTracker.ActiveSolutionChanged -= ActiveSolutionTrackerOnActiveSolutionChanged;
                solutionSettingsUpdater = null;
                globalSettingsUpdater = null;
            }
            disposed = true;
        }
    }

    private void OnSettingsFileChanged(object sender, EventArgs e) => ResetConfiguration();

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
