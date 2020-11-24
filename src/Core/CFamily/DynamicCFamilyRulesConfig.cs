/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.Core.CFamily
{
    /// <summary>
    /// Wrapper that handles applying customised rules settings on top of the default config
    /// </summary>
    /// <remarks>The customised rules could be user-specified (if in standalone mode) or generated
    /// from a QP (if in connected mode)</remarks>
    internal sealed class DynamicCFamilyRulesConfig : ICFamilyRulesConfig
    {
        private readonly ICFamilyRulesConfig defaultRulesConfig;

        internal static readonly string[] ExcludedRulesKeys = new string[] {
            // Project-level:
            "cpp:S5536", "c:S5536",
            "cpp:S4830", "c:S4830",
            "cpp:S5527", "c:S5527",
            // Security hotspots:
            "cpp:S5801", "c:S5801",
            "cpp:S5814", "c:S5814",
            "cpp:S5815", "c:S5815",
            "cpp:S5816", "c:S5816",
            "cpp:S5824", "c:S5824",
            "cpp:S2612", "c:S2612",
            "cpp:S5802", "c:S5802",
            "cpp:S5849", "c:S5849",
            "cpp:S5982", "c:S5982",
            "cpp:S5813", "c:S5813",
            "cpp:S5332", "c:S5332",
        };

        public DynamicCFamilyRulesConfig(ICFamilyRulesConfig defaultRulesConfig, RulesSettings customRulesSettings, ILogger logger)
            :this(defaultRulesConfig, customRulesSettings, logger, ExcludedRulesKeys)
        {
        }

        internal /* for testing */ DynamicCFamilyRulesConfig(ICFamilyRulesConfig defaultRulesConfig, RulesSettings customRulesSettings, ILogger logger, IEnumerable<string> excludedRuleKeys)
        {
            this.defaultRulesConfig = defaultRulesConfig ?? throw new ArgumentNullException(nameof(defaultRulesConfig));
            if (customRulesSettings == null)
            {
                throw new ArgumentNullException(nameof(customRulesSettings));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (customRulesSettings.Rules.Count == 0)
            {
                logger.WriteLine(CoreStrings.CFamily_NoCustomRulesSettings);
            }

            var modifiedCustomRules = DisableExcludedRules(customRulesSettings, excludedRuleKeys, logger);

            ActivePartialRuleKeys = CalculateActiveRules(defaultRulesConfig, modifiedCustomRules);
            RulesMetadata = new Dictionary<string, RuleMetadata>();
            RulesParameters = new Dictionary<string, IDictionary<string, string>>();
            CalculateEffectiveSettings(defaultRulesConfig, modifiedCustomRules);
        }

        #region IRulesConfiguration interface methods

        public string LanguageKey => defaultRulesConfig.LanguageKey;

        public IEnumerable<string> AllPartialRuleKeys => defaultRulesConfig.AllPartialRuleKeys;

        public IEnumerable<string> ActivePartialRuleKeys { get; }

        public IDictionary<string, IDictionary<string, string>> RulesParameters { get; }

        public IDictionary<string, RuleMetadata> RulesMetadata { get; }

        #endregion IRulesConfiguration interface methods

        private static IEnumerable<string> CalculateActiveRules(ICFamilyRulesConfig defaultRulesConfig, RulesSettings customRulesSettings)
        {
            // We're only interested settings for rules that are for the same language as the supplied rules configuration.
            // The rule keys in the custom rules settings include the repo prefix, but the rule keys in the default rules config do not.
            var partialKeyToConfigMap = GetFilteredRulesKeyedByPartialKey(customRulesSettings, defaultRulesConfig.LanguageKey);

            var deactivatedByUser = partialKeyToConfigMap.Where(kvp => kvp.Value.Level == RuleLevel.Off).Select(kvp => kvp.Key);
            var activatedByUser = partialKeyToConfigMap.Where(kvp => kvp.Value.Level == RuleLevel.On).Select(kvp => kvp.Key);

            return defaultRulesConfig.ActivePartialRuleKeys
                .Concat(activatedByUser)
                .Except(deactivatedByUser, CFamilyShared.RuleKeyComparer)
                .Distinct(CFamilyShared.RuleKeyComparer).ToArray();
        }

        private static IDictionary<string, RuleConfig> GetFilteredRulesKeyedByPartialKey(RulesSettings rulesSettings, string language)
        {
            Debug.Assert(!string.IsNullOrEmpty(language), "language should not be null/empty");
            var languagePrefix = language + ":";

            return rulesSettings.Rules
                .Where(kvp => kvp.Key.StartsWith(languagePrefix, CFamilyShared.RuleKeyComparison))
                .ToDictionary(kvp => kvp.Key.Substring(languagePrefix.Length), kvp => kvp.Value);
        }

        private void CalculateEffectiveSettings(ICFamilyRulesConfig defaultRulesConfig, RulesSettings customRulesSettings)
        {
            Debug.Assert(customRulesSettings?.Rules != null && customRulesSettings.Rules.Count != 0);

            foreach (var partialRuleKey in defaultRulesConfig.AllPartialRuleKeys)
            {
                // Not all rules have params, but all should have metadata
                Debug.Assert(defaultRulesConfig.RulesMetadata[partialRuleKey] != null);

                var defaultMetadata = defaultRulesConfig.RulesMetadata[partialRuleKey];
                defaultRulesConfig.RulesParameters.TryGetValue(partialRuleKey, out var defaultParams);

                var fullRuleKey = GetFullRuleKey(defaultRulesConfig.LanguageKey, partialRuleKey);
                customRulesSettings.Rules.TryGetValue(fullRuleKey, out var userRuleConfig);

                RulesMetadata[partialRuleKey] = GetEffectiveMetadata(defaultMetadata, userRuleConfig);

                var effectiveParams = GetEffectiveParameters(defaultParams, userRuleConfig?.Parameters);
                if (effectiveParams != null)
                {
                    RulesParameters[partialRuleKey] = effectiveParams;
                }
            }
        }

        private static RuleMetadata GetEffectiveMetadata(RuleMetadata defaultMetadata, RuleConfig userConfig)
        {
            if (userConfig == null || !userConfig.Severity.HasValue)
            {
                return defaultMetadata;
            }

            return new RuleMetadata
            {
                DefaultSeverity = userConfig.Severity.Value,
                Title = defaultMetadata.Title,
                CompatibleLanguages = defaultMetadata.CompatibleLanguages,
                Type = defaultMetadata.Type
            };
        }

        internal /* for testing */ static IDictionary<string, string> GetEffectiveParameters(IDictionary<string, string> defaultParameters, IDictionary<string, string> userParameters)
        {
            if (defaultParameters == null)
            {
                return userParameters;
            }
            if (userParameters == null)
            {
                return defaultParameters;
            }

            var effectiveParams = new Dictionary<string, string>(defaultParameters, StringComparer.OrdinalIgnoreCase);
            foreach (var userParam in userParameters)
            {
                effectiveParams[userParam.Key] = userParam.Value;
            }
            return effectiveParams;
        }

        /// <summary>
        /// Returns a copy of the custom rules unioned with the config to disable the
        /// list of excluded rules
        /// </summary>
        internal /* for testing */ static RulesSettings DisableExcludedRules(RulesSettings customRules, IEnumerable<string> excludedRuleKeys, ILogger logger)
        {
            logger.WriteLine(CoreStrings.CFamily_RulesUnavailableInSonarLint, string.Join(", ", excludedRuleKeys));

            // We're making a shallow copy of the list of rules. If we modify the original list, any exclusions we 
            // add could end up be saved in the user settings.json file (if that is where the custom rules
            // came from). That doesn't cause a functional problem but it could be confusing.
            var modifiedSettings = new RulesSettings
            {
                Rules = new Dictionary<string, RuleConfig>(customRules.Rules, customRules.Rules.Comparer)
            };

            foreach (var key in excludedRuleKeys)
            {
                modifiedSettings.Rules[key] = new RuleConfig { Level = RuleLevel.Off };
            }

            return modifiedSettings;
        }

        private static string GetFullRuleKey(string language, string partialRuleKey)
            => $"{language}:{partialRuleKey}";
    }
}
