﻿/*
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
[Export(typeof(IGlobalRawSettingsService))]
[Export(typeof(ISolutionRawSettingsService))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class UserSettingsProvider : IUserSettingsProvider, IGlobalRawSettingsService, ISolutionRawSettingsService, IDisposable
{
    private static readonly object Lock = new();
    private readonly IActiveSolutionTracker activeSolutionTracker;
    private readonly ILogger logger;
    private readonly IGlobalSettingsStorage globalSettingsStorage;
    private readonly ISolutionSettingsStorage solutionSettingsStorage;
    private (UserSettings userSettings, GlobalRawAnalysisSettings globalSettings, SolutionRawAnalysisSettings solutionSettings)? cache;
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

                globalSettingsStorage.SettingsFileChanged += OnSettingsFileChanged;
                solutionSettingsStorage.SettingsFileChanged += OnSettingsFileChanged;
                // The subscription to the ActiveSolutionTracker events should happen after the ISolutionSettingsStorage is initialized,
                // as the storage depends on ActiveSolutionChanged to calculate the path to the settings file
                // This prevents a situation in which we might try to load the settings file on solution changes before the storage is actually initialized
                activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTrackerOnActiveSolutionChanged;
            });
    }

    public UserSettings UserSettings
    {
        get
        {
            lock (Lock)
            {
                cache ??= SafeLoadUserSettings();
                return cache.Value.userSettings;
            }
        }
    }

    public event EventHandler SettingsChanged;
    public IInitializationProcessor InitializationProcessor { get; }

    public GlobalRawAnalysisSettings GlobalRawAnalysisSettings
    {
        get
        {
            lock (Lock)
            {
                cache ??= SafeLoadUserSettings();
                return cache.Value.globalSettings;
            }
        }
    }

    void IGlobalRawSettingsService.DisableRule(string ruleId)
    {
        Debug.Assert(!string.IsNullOrEmpty(ruleId), "DisableRule: ruleId should not be null/empty");

        var newRules = GlobalRawAnalysisSettings.Rules.SetItem(ruleId, new RuleConfig(RuleLevel.Off));
        var globalSettings = new GlobalRawAnalysisSettings(newRules, GlobalRawAnalysisSettings.UserDefinedFileExclusions);
        globalSettingsStorage.SaveSettingsFile(globalSettings);
        SafeClearCache();
    }

    void IGlobalRawSettingsService.UpdateFileExclusions(IEnumerable<string> exclusions)
    {
        var globalSettings = new GlobalRawAnalysisSettings(GlobalRawAnalysisSettings.Rules, exclusions.ToImmutableArray());
        globalSettingsStorage.SaveSettingsFile(globalSettings);
        SafeClearCache();
    }

    public SolutionRawAnalysisSettings SolutionRawAnalysisSettings
    {
        get
        {
            lock (Lock)
            {
                cache ??= SafeLoadUserSettings();
                return cache.Value.solutionSettings;
            }
        }
    }

    void ISolutionRawSettingsService.UpdateAnalysisProperties(Dictionary<string, string> analysisProperties)
    {
        var solutionSettings = new SolutionRawAnalysisSettings(analysisProperties, SolutionRawAnalysisSettings.UserDefinedFileExclusions);
        solutionSettingsStorage.SaveSettingsFile(solutionSettings);
        SafeClearCache();
    }

    void ISolutionRawSettingsService.UpdateFileExclusions(IEnumerable<string> exclusions)
    {
        var solutionSettings = new SolutionRawAnalysisSettings(SolutionRawAnalysisSettings.AnalysisProperties, exclusions.ToImmutableArray());
        solutionSettingsStorage.SaveSettingsFile(solutionSettings);
        SafeClearCache();
    }

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
            }
            disposed = true;
        }
    }

    private void OnSettingsFileChanged(object sender, EventArgs e) => ResetConfiguration();

    private void ActiveSolutionTrackerOnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
    {
        if (!e.IsSolutionOpen || e.SolutionName == null)
        {
            SafeClearCache();
            return;
        }

        ResetConfiguration();
    }

    private void ResetConfiguration()
    {
        SafeClearCache();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SafeClearCache()
    {
        lock (Lock)
        {
            cache = null;
        }
    }

    private (UserSettings userSettings, GlobalRawAnalysisSettings globalAnalysisSettings, SolutionRawAnalysisSettings solutionAnalysisSettings) SafeLoadUserSettings()
    {
        var globalAnalysisSettings = globalSettingsStorage.LoadSettingsFile();
        SolutionRawAnalysisSettings solutionSettingsFromStorage = null;

        SolutionRawAnalysisSettings solutionRawAnalysisSettings = null;

        if (solutionSettingsStorage.SettingsFilePath != null)
        {
            solutionSettingsFromStorage = solutionSettingsStorage.LoadSettingsFile();
            solutionRawAnalysisSettings = solutionSettingsFromStorage ?? new SolutionRawAnalysisSettings();
        }

        if (globalAnalysisSettings == null && solutionRawAnalysisSettings == null)
        {
            logger.WriteLine(Strings.Settings_UsingDefaultSettings);
            return (new UserSettings(new AnalysisSettings(), globalSettingsStorage.ConfigurationBaseDirectory), globalAnalysisSettings, solutionRawAnalysisSettings);
        }

        var rules = globalAnalysisSettings?.Rules;
        var globalExclusions = globalAnalysisSettings?.UserDefinedFileExclusions;
        var properties = solutionRawAnalysisSettings?.AnalysisProperties;
        var solutionExclusions = solutionRawAnalysisSettings?.UserDefinedFileExclusions;
        var generatedConfigsBase = solutionSettingsFromStorage != null ? solutionSettingsStorage.ConfigurationBaseDirectory : globalSettingsStorage.ConfigurationBaseDirectory;

        return (new UserSettings(new AnalysisSettings(rules, globalExclusions, solutionExclusions, properties), generatedConfigsBase), globalAnalysisSettings, solutionRawAnalysisSettings);
    }
}
