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
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.Core.Resources;

namespace SonarLint.VisualStudio.Core.UserRuleSettings;

[Export(typeof(IUserSettingsProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class UserSettingsProvider : IUserSettingsProvider, IDisposable
{
    private static readonly object Lock = new();
    // Note: the data is stored in the roaming profile so it will be sync across machines
    // for domain-joined users.
    private static readonly string UserSettingsFilePath = Path.GetFullPath(
        Path.Combine(EnvironmentVariableProvider.Instance.GetSLVSAppDataRootPath(), "settings.json"));
    private readonly IFileSystem fileSystem;
    private readonly ILogger logger;
    private readonly AnalysisSettingsSerializer serializer;
    private readonly ISingleFileMonitor settingsFileMonitor;
    private UserSettings userSettings;

    [ImportingConstructor]
    public UserSettingsProvider(ILogger logger, ISingleFileMonitorFactory singleFileMonitorFactory) : this(logger, singleFileMonitorFactory, new FileSystem(), UserSettingsFilePath) { }

    internal /* for testing */ UserSettingsProvider(
        ILogger logger,
        ISingleFileMonitorFactory singleFileMonitorFactory,
        IFileSystem fileSystem,
        string globalAnalysisSettingsFilePath)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        var fileMonitorFactory = singleFileMonitorFactory ?? throw new ArgumentNullException(nameof(singleFileMonitorFactory));
        serializer = new AnalysisSettingsSerializer(fileSystem, logger);

        GlobalAnalysisSettingsFilePath = globalAnalysisSettingsFilePath;
        settingsFileMonitor = fileMonitorFactory.Create(GlobalAnalysisSettingsFilePath);
        settingsFileMonitor.FileChanged += OnFileChanged;
    }

    public void Dispose()
    {
        settingsFileMonitor.FileChanged -= OnFileChanged;
        settingsFileMonitor.Dispose();
    }

    private void OnFileChanged(object sender, EventArgs e)
    {
        SafeClearUserSettingsCache();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    #region IUserSettingsProvider implementation

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

    public void DisableRule(string ruleId)
    {
        Debug.Assert(!string.IsNullOrEmpty(ruleId), "DisableRule: ruleId should not be null/empty");

        var newRules = UserSettings.AnalysisSettings.Rules.SetItem(ruleId, new RuleConfig(RuleLevel.Off));
        var globalSettings = new GlobalAnalysisSettings(newRules, UserSettings.AnalysisSettings.UserDefinedFileExclusions);
        serializer.SafeSave(GlobalAnalysisSettingsFilePath, globalSettings);
        SafeClearUserSettingsCache();
    }

    public void UpdateFileExclusions(IEnumerable<string> exclusions)
    {
        var globalSettings = new GlobalAnalysisSettings(UserSettings.AnalysisSettings.Rules, exclusions);
        serializer.SafeSave(GlobalAnalysisSettingsFilePath,globalSettings);
        SafeClearUserSettingsCache();
    }

    public string GlobalAnalysisSettingsFilePath { get; }

    public void EnsureFileExists()
    {
        if (!fileSystem.File.Exists(GlobalAnalysisSettingsFilePath))
        {
            serializer.SafeSave(GlobalAnalysisSettingsFilePath, UserSettings.AnalysisSettings);
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

        if (globalSettings != null)
        {
            return new UserSettings(new AnalysisSettings(rules: globalSettings.Rules, fileExclusions: globalSettings.UserDefinedFileExclusions));
        }

        logger.WriteLine(Strings.Settings_UsingDefaultSettings);
        return new UserSettings(new AnalysisSettings());
    }

    #endregion
}
