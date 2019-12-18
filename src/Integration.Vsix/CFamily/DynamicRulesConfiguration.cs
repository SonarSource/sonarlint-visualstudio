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
using System.Diagnostics;
using System.Linq;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    // Wrapper that handles applying user-level settings on top of the default config
    internal sealed class DynamicRulesConfiguration : IRulesConfiguration
    {
        private readonly IRulesConfiguration defaultRulesConfig;

        public DynamicRulesConfiguration(IRulesConfiguration defaultRulesConfig, UserSettings userSettings)
        {
            this.defaultRulesConfig = defaultRulesConfig ?? throw new ArgumentNullException(nameof(defaultRulesConfig));
            if (userSettings == null)
            {
                throw new ArgumentNullException(nameof(userSettings));
            }

            this.ActivePartialRuleKeys = CalculateActiveRules(defaultRulesConfig, userSettings);
        }

        #region IRulesConfiguration interface methods

        public string LanguageKey => defaultRulesConfig.LanguageKey;

        public IEnumerable<string> AllPartialRuleKeys => defaultRulesConfig.AllPartialRuleKeys;

        public IEnumerable<string> ActivePartialRuleKeys { get; }

        public IDictionary<string, IDictionary<string, string>> RulesParameters => defaultRulesConfig.RulesParameters;

        public IDictionary<string, RuleMetadata> RulesMetadata => defaultRulesConfig.RulesMetadata;

        #endregion IRulesConfiguration interface methods

        internal /* for testing */ static IEnumerable<string> CalculateActiveRules(IRulesConfiguration defaultRulesConfig, UserSettings userSettings)
        {
            if (userSettings?.Rules?.Count == 0)
            {
                return defaultRulesConfig.ActivePartialRuleKeys;
            }

            // We're only interested settings for rules that are for the same language as the supplied rules configuration.
            // The rule keys in the user settings include the repo prefix, but the rule keys in the rules config do not.
            var partialKeyToConfigMap = GetFilteredRulesKeyedByPartialKey(userSettings, defaultRulesConfig.LanguageKey);

            var deactivatedByUser = partialKeyToConfigMap.Where(kvp => kvp.Value.Level == RuleLevel.Off).Select(kvp => kvp.Key);
            var activatedByUser = partialKeyToConfigMap.Where(kvp => kvp.Value.Level == RuleLevel.On).Select(kvp => kvp.Key);

            var activeRules = defaultRulesConfig.ActivePartialRuleKeys
                .Concat(activatedByUser)
                .Except(deactivatedByUser, CFamilyHelper.RuleKeyComparer)
                .Distinct(CFamilyHelper.RuleKeyComparer).ToArray();

            return activeRules;
        }
        
        private static IDictionary<string, RuleConfig> GetFilteredRulesKeyedByPartialKey(UserSettings userSettings, string language)
        {
            Debug.Assert(!string.IsNullOrEmpty(language), "language should not be null/empty");
            var languagePrefix = language + ":";

            var partialKeyToConfigMap = userSettings.Rules
                .Where(kvp => kvp.Key.StartsWith(languagePrefix, CFamilyHelper.RuleKeyComparison))
                .ToDictionary(kvp => kvp.Key.Substring(languagePrefix.Length), kvp => kvp.Value);

            return partialKeyToConfigMap;
        }
    }
}
