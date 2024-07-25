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
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core.Resources;

namespace SonarLint.VisualStudio.Core.UserRuleSettings;

[Export(typeof(IUserSettingsProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class UserSettingsProvider : IUserSettingsProvider
{
    private readonly IFileSystem fileSystem;
    private readonly ILogger logger;
    private readonly RulesSettingsSerializer serializer;
    private UserSettings userSettings;

    [ImportingConstructor]
    public UserSettingsProvider(ILogger logger) : this(logger, new FileSystem(), UserSettingsConstants.UserSettingsFilePath) { }

    internal /* for testing */ UserSettingsProvider(ILogger logger, IFileSystem fileSystem, string settingsFilePath)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.serializer = new RulesSettingsSerializer(fileSystem, logger);

        SettingsFilePath = settingsFilePath;
    }

    #region IUserSettingsProvider implementation

    public UserSettings UserSettings => userSettings ??= SafeLoadUserSettings();

    public UserSettings SafeLoadUserSettings()
    {
        var settings = serializer.SafeLoad(SettingsFilePath);
        if (settings == null)
        {
            logger.WriteLine(Strings.Settings_UsingDefaultSettings);
            settings = new RulesSettings();
        }
        userSettings = new UserSettings(settings);
        return userSettings;
    }

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
}
