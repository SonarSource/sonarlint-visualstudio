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
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.UserSettingsConfiguration;

[Export(typeof(IUserSettingsProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class UserSettingsProvider : IUserSettingsProvider, IDisposable
{
    private const string GeneratedGlobalSettingsFolderName = ".global";
    private const string SolutionSettingsFolderName = "SolutionSettings";
    private const string SettingsFileName = "settings.json";
    private static readonly object Lock = new();
    private readonly IFileSystem fileSystem;
    private readonly IActiveSolutionTracker activeSolutionTracker;
    private readonly ILogger logger;
    private readonly IAnalysisSettingsSerializer serializer;
    private readonly ISingleFileMonitorFactory fileMonitorFactory;
    private ISingleFileMonitor globalSettingsFileMonitor;
    private ISingleFileMonitor solutionSettingsFileMonitor;
    private UserSettings userSettings;
    private string appDataRoot;
    private bool disposed;
    private (string settingsFile, string generatedConfigsBaseDirectory) globalFilePaths;
    private (string settingsFile, string generatedConfigsBaseDirectory)? solutionFilePaths;

    [ImportingConstructor]
    public UserSettingsProvider(
        ILogger logger,
        ISingleFileMonitorFactory singleFileMonitorFactory,
        IFileSystemService fileSystem,
        IAnalysisSettingsSerializer serializer,
        IEnvironmentVariableProvider environmentVariableProvider,
        IActiveSolutionTracker activeSolutionTracker,
        IInitializationProcessorFactory processorFactory)
    {
        this.logger = logger;
        this.fileSystem = fileSystem;
        this.activeSolutionTracker = activeSolutionTracker;
        fileMonitorFactory = singleFileMonitorFactory;
        this.serializer = serializer;
        InitializationProcessor = processorFactory.CreateAndStart<UserSettingsProvider>(
            [activeSolutionTracker],
            () =>
            {
                if (disposed)
                {
                    return;
                }

                // Note: the data is stored in the roaming profile so it will be sync across machines
                // for domain-joined users.
                appDataRoot = environmentVariableProvider.GetSLVSAppDataRootPath();
                CreateGlobalSettingsMonitorAndSubscribe();
                if (activeSolutionTracker.CurrentSolutionName is { } solutionName)
                {
                    CreateSolutionSettingsMonitorAndSubscribe(solutionName);
                }
                activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTrackerOnActiveSolutionChanged;
            });
    }

    private void CreateGlobalSettingsMonitorAndSubscribe()
    {
        var globalAnalysisSettingsFilePath = Path.GetFullPath(Path.Combine(appDataRoot, SettingsFileName));
        var generatedGlobalSettingsFolder = Path.Combine(appDataRoot, GeneratedGlobalSettingsFolderName);
        globalFilePaths = (globalAnalysisSettingsFilePath, generatedGlobalSettingsFolder);
        globalSettingsFileMonitor = fileMonitorFactory.Create(GlobalAnalysisSettingsFilePath);
        globalSettingsFileMonitor.FileChanged += OnFileChanged;
    }

    private void CreateSolutionSettingsMonitorAndSubscribe(string solutionName)
    {
        var solutionSettingsParentFolder = Path.Combine(appDataRoot, SolutionSettingsFolderName, solutionName);
        var solutionSettingsFilePath = Path.GetFullPath(Path.Combine(solutionSettingsParentFolder, SettingsFileName));
        solutionFilePaths = (solutionSettingsFilePath, solutionSettingsParentFolder);
        solutionSettingsFileMonitor = fileMonitorFactory.Create(SolutionAnalysisSettingsFilePath);
        solutionSettingsFileMonitor.FileChanged += OnFileChanged;
    }

    private void ActiveSolutionTrackerOnActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
    {
        DisposeSolutionSettingsMonitor();

        if (!e.IsSolutionOpen || e.SolutionName == null)
        {
            SafeClearUserSettingsCache();
            return;
        }

        CreateSolutionSettingsMonitorAndSubscribe(e.SolutionName);
        ResetConfiguration();
    }

    private void DisposeSolutionSettingsMonitor()
    {
        if (solutionSettingsFileMonitor == null)
        {
            return;
        }

        solutionFilePaths = null;
        solutionSettingsFileMonitor.FileChanged -= OnFileChanged;
        solutionSettingsFileMonitor.Dispose();
    }

    private void OnFileChanged(object sender, EventArgs e) => ResetConfiguration();

    private void ResetConfiguration()
    {
        SafeClearUserSettingsCache();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
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
    public string GlobalAnalysisSettingsFilePath => globalFilePaths.settingsFile;
    public string SolutionAnalysisSettingsFilePath => solutionFilePaths?.settingsFile;

    public void DisableRule(string ruleId)
    {
        Debug.Assert(!string.IsNullOrEmpty(ruleId), "DisableRule: ruleId should not be null/empty");

        var newRules = UserSettings.AnalysisSettings.Rules.SetItem(ruleId, new RuleConfig(RuleLevel.Off));
        var globalSettings = new GlobalAnalysisSettings(newRules, UserSettings.AnalysisSettings.GlobalFileExclusions);
        serializer.SafeSave(GlobalAnalysisSettingsFilePath, globalSettings);
        SafeClearUserSettingsCache();
    }

    public void UpdateGlobalFileExclusions(IEnumerable<string> exclusions)
    {
        var globalSettings = new GlobalAnalysisSettings(UserSettings.AnalysisSettings.Rules, exclusions.ToImmutableArray());
        serializer.SafeSave(GlobalAnalysisSettingsFilePath, globalSettings);
        SafeClearUserSettingsCache();
    }

    public void UpdateSolutionFileExclusions(IEnumerable<string> exclusions)
    {
        var solutionSettings = new SolutionAnalysisSettings(UserSettings.AnalysisSettings.AnalysisProperties, exclusions.ToImmutableArray());
        serializer.SafeSave(SolutionAnalysisSettingsFilePath, solutionSettings);
        SafeClearUserSettingsCache();
    }

    public void UpdateAnalysisProperties(Dictionary<string, string> analysisProperties)
    {
        var solutionSettings = new SolutionAnalysisSettings(analysisProperties, UserSettings.AnalysisSettings.SolutionFileExclusions);
        serializer.SafeSave(SolutionAnalysisSettingsFilePath, solutionSettings);
        SafeClearUserSettingsCache();
    }

    public void EnsureGlobalAnalysisSettingsFileExists()
    {
        if (!fileSystem.File.Exists(GlobalAnalysisSettingsFilePath))
        {
            serializer.SafeSave(GlobalAnalysisSettingsFilePath, new GlobalAnalysisSettings());
        }
    }

    public void EnsureSolutionAnalysisSettingsFileExists()
    {
        if (SolutionAnalysisSettingsFilePath == null)
        {
            return;
        }

        if (!fileSystem.File.Exists(SolutionAnalysisSettingsFilePath))
        {
            serializer.SafeSave(SolutionAnalysisSettingsFilePath, new SolutionAnalysisSettings());
        }
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
        var globalSettings = serializer.SafeLoad<GlobalAnalysisSettings>(GlobalAnalysisSettingsFilePath);

        SolutionAnalysisSettings solutionSettings = null;
        if (solutionFilePaths != null)
        {
            solutionSettings = serializer.SafeLoad<SolutionAnalysisSettings>(SolutionAnalysisSettingsFilePath);
        }

        if (globalSettings == null && solutionSettings == null)
        {
            logger.WriteLine(Strings.Settings_UsingDefaultSettings);
            return new UserSettings(new AnalysisSettings(), globalFilePaths.generatedConfigsBaseDirectory);
        }

        var rules = globalSettings?.Rules;
        var globalExclusions = globalSettings?.UserDefinedFileExclusions;
        var properties = solutionSettings?.AnalysisProperties;
        var solutionExclusions = solutionSettings?.UserDefinedFileExclusions;
        var generatedConfigsBase = solutionSettings != null
            ? solutionFilePaths!.Value.generatedConfigsBaseDirectory
            : globalFilePaths.generatedConfigsBaseDirectory;

        return new UserSettings(new AnalysisSettings(rules, globalExclusions, solutionExclusions, properties), generatedConfigsBase);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            if (InitializationProcessor.IsFinalized)
            {
                DisposeSolutionSettingsMonitor();
                if (globalSettingsFileMonitor != null)
                {
                    globalSettingsFileMonitor.FileChanged -= OnFileChanged;
                    globalSettingsFileMonitor.Dispose();
                }
                activeSolutionTracker.ActiveSolutionChanged -= ActiveSolutionTrackerOnActiveSolutionChanged;
            }
            disposed = true;
        }
    }
}
