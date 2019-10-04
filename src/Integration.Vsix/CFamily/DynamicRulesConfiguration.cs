/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    // Wrapper that handles applying user-level settings on top of the default config
    internal sealed class DynamicRulesConfiguration : IRulesConfiguration
    {
        private static StringComparer RuleKeyComparer = StringComparer.Ordinal;

        private readonly IRulesConfiguration defaultRulesConfig;
        private readonly string userSettingsFilePath;
        private readonly ILogger logger;
        private readonly IFile fileSystem;

        public DynamicRulesConfiguration(IRulesConfiguration defaultRulesConfig, string userSettingsFilePath, ILogger logger, IFile fileWrapper)
        {
            this.defaultRulesConfig = defaultRulesConfig;
            this.userSettingsFilePath = userSettingsFilePath;
            this.logger = logger;
            this.fileSystem = fileWrapper;

            var userSettings = SafeLoadUserSettings();
            this.ActiveRuleKeys = CalculateActiveRules(defaultRulesConfig, userSettings);
        }

        #region IRulesConfiguration interface methods

        public IEnumerable<string> AllRuleKeys => defaultRulesConfig.AllRuleKeys;

        public IEnumerable<string> ActiveRuleKeys { get; }

        public IDictionary<string, IDictionary<string, string>> RulesParameters => defaultRulesConfig.RulesParameters;

        public IDictionary<string, RulesLoader.RuleMetadata> RulesMetadata => defaultRulesConfig.RulesMetadata;

        #endregion IRulesConfiguration interface methods

        private UserSettings SafeLoadUserSettings()
        {
            if (!fileSystem.Exists(userSettingsFilePath))
            {
                logger.WriteLine($"User settings file does not exist: {userSettingsFilePath}. Using default rules configuration.");
                return null;
            }

            UserSettings userSettings = null;
            try
            {
                logger.WriteLine($"Loading user settings from {userSettingsFilePath} ...");
                var data = fileSystem.ReadAllText(userSettingsFilePath);
                userSettings = JsonConvert.DeserializeObject<UserSettings>(data);
            }
            catch(Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine($"Error loading user settings from {userSettingsFilePath}. Error: {ex.Message}");
                logger.WriteLine($"Default rule settings will be used.");
            }
            return userSettings;
        }

        internal /* for testing */ static IEnumerable<string> CalculateActiveRules(IRulesConfiguration defaultRulesConfig, UserSettings userSettings)
        {
            if (userSettings == null || userSettings.Rules == null)
            {
                return defaultRulesConfig.ActiveRuleKeys;
            }

            // We're only interested in C Family rules
            var cppRulesInUserConfig = userSettings?.Rules
                .Where(kvp => defaultRulesConfig.AllRuleKeys.Contains(kvp.Key, RuleKeyComparer))
                .ToArray();

            var deactivatedByUser = cppRulesInUserConfig.Where(kvp => kvp.Value.Level == RuleLevel.Off).Select(kvp => kvp.Key);
            var activatedByUser = cppRulesInUserConfig.Where(kvp => kvp.Value.Level == RuleLevel.On).Select(kvp => kvp.Key);

            var activeRules = defaultRulesConfig.ActiveRuleKeys
                .Concat(activatedByUser)
                .Except(deactivatedByUser, RuleKeyComparer)
                .Distinct(RuleKeyComparer).ToArray();

            return activeRules;
        }
    }
}
