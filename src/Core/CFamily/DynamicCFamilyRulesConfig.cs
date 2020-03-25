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

        internal static readonly string[] ExcludedRulesKeys = new string[] { "cpp:S5536", "c:S5536" };

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

            DisableExcludedRules(customRulesSettings, excludedRuleKeys, logger);

            if ((customRulesSettings?.Rules?.Count ?? 0) == 0)
            {
                ActivePartialRuleKeys = defaultRulesConfig.ActivePartialRuleKeys;
                RulesMetadata = defaultRulesConfig.RulesMetadata;
                RulesParameters = defaultRulesConfig.RulesParameters;
            }
            else
            {
                ActivePartialRuleKeys = CalculateActiveRules(defaultRulesConfig, customRulesSettings);

                RulesMetadata = new Dictionary<string, RuleMetadata>();
                RulesParameters = new Dictionary<string, IDictionary<string, string>>();
                CalculateEffectiveSettings(defaultRulesConfig, customRulesSettings);
            }
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
            Debug.Assert(customRulesSettings?.Rules != null && customRulesSettings.Rules.Count != 0);

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

        internal /* for testing */ static void DisableExcludedRules(RulesSettings customRules, IEnumerable<string> excludedRuleKeys, ILogger logger)
        {
            logger.WriteLine(CoreStrings.CFamily_RulesUnavailableInSonarLint, string.Join(", ", excludedRuleKeys));

            foreach (var key in excludedRuleKeys)
            {
                customRules.Rules[key] = new RuleConfig { Level = RuleLevel.Off };
            }
        }

        private static string GetFullRuleKey(string language, string partialRuleKey)
            => $"{language}:{partialRuleKey}";
    }
}
