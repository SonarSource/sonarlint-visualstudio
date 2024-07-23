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
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Rules;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis;

[Export(typeof(IUserSettingsProvider))]
internal sealed class UserSettingsProvider : IUserSettingsProvider, IDisposable
{
    // Note: the data is stored in the roaming profile so it will be sync across machines
    // for domain-joined users.
    public static readonly string UserSettingsFilePath = Path.GetFullPath(
        Path.Combine(EnvironmentVariableProvider.Instance.GetSLVSAppDataRootPath(), "settings.json"));

    private readonly ISingleFileMonitor settingsFileMonitor;
    private readonly ISLCoreServiceProvider slCoreServiceProvider;
    private readonly IRuleSettingsMapper ruleSettingsMapper;
    private readonly IFileSystem fileSystem;
    private readonly ILogger logger;
    private readonly RulesSettingsSerializer serializer;
    private UserSettings userSettings;

    [ImportingConstructor]
    public UserSettingsProvider(ILogger logger, ISLCoreServiceProvider slCoreServiceProvider, IRuleSettingsMapper ruleSettingsMapper)
        : this(logger, new FileSystem(),
            new SingleFileMonitor(UserSettingsFilePath, logger), slCoreServiceProvider, ruleSettingsMapper)
    {
    }

    internal /* for testing */ UserSettingsProvider(ILogger logger,
        IFileSystem fileSystem, ISingleFileMonitor settingsFileMonitor, 
        ISLCoreServiceProvider slCoreServiceProvider, IRuleSettingsMapper ruleSettingsMapper)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.settingsFileMonitor = settingsFileMonitor ?? throw new ArgumentNullException(nameof(settingsFileMonitor));
        this.slCoreServiceProvider = slCoreServiceProvider;
        this.ruleSettingsMapper = ruleSettingsMapper;

        this.serializer = new RulesSettingsSerializer(fileSystem, logger);

        SettingsFilePath = settingsFileMonitor.MonitoredFilePath;
        settingsFileMonitor.FileChanged += OnFileChanged;
        this.ruleSettingsMapper = ruleSettingsMapper;
    }

    private void OnFileChanged(object sender, EventArgs e)
    {
        userSettings = SafeLoadUserSettings(SettingsFilePath, logger);
        UpdateStandaloneRulesConfiguration();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    #region IUserSettingsProvider implementation

    public UserSettings UserSettings
    {
        get
        {
            if (userSettings == null) { userSettings = SafeLoadUserSettings(SettingsFilePath, logger); }
            return userSettings;
        }
    }

    public event EventHandler SettingsChanged;

    public void DisableRule(string ruleId)
    {
        Debug.Assert(!string.IsNullOrEmpty(ruleId), "DisableRule: ruleId should not be null/empty");

        if (UserSettings.RulesSettings.Rules.TryGetValue(ruleId, out var ruleConfig))
        {
            ruleConfig.Level = RuleLevel.Off;
        }
        else
        {
            UserSettings.RulesSettings.Rules[ruleId] = new RuleConfig { Level = RuleLevel.Off };
        }

        serializer.SafeSave(SettingsFilePath, UserSettings.RulesSettings);
    }

    public string SettingsFilePath { get; }

    public void EnsureFileExists()
    {
        if (!fileSystem.File.Exists(SettingsFilePath))
        {
            serializer.SafeSave(SettingsFilePath, UserSettings.RulesSettings);
        }
    }

    #endregion

    private UserSettings SafeLoadUserSettings(string filePath, ILogger logger)
    {
        var settings = serializer.SafeLoad(filePath);
        if (settings == null)
        {
            logger.WriteLine(AnalysisStrings.Settings_UsingDefaultSettings);
            settings = new RulesSettings();
        }
        return new UserSettings(settings);
    }

    private void UpdateStandaloneRulesConfiguration()
    {
        if (!slCoreServiceProvider.TryGetTransientService(out IRulesSLCoreService rulesSlCoreService))
        {
            logger.WriteLine($"[{nameof(UserSettingsProvider)}] {SLCoreStrings.ServiceProviderNotInitialized}");
            return;
        }

        try
        {
            var slCoreSettings = ruleSettingsMapper.MapRuleSettingsToSlCoreSettings(UserSettings.RulesSettings);
            rulesSlCoreService.UpdateStandaloneRulesConfiguration(new UpdateStandaloneRulesConfigurationParams(slCoreSettings));
        }
        catch (Exception e)
        {
            logger.WriteLine(e.ToString());
        }
    }

    public void Dispose()
    {
        settingsFileMonitor.FileChanged -= OnFileChanged;
        settingsFileMonitor.Dispose();
    }
}
