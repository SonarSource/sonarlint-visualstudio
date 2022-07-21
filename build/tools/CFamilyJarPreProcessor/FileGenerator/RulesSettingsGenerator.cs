/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System.Linq;
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.Core;

namespace CFamilyJarPreProcessor.FileGenerator
{
    internal class RulesSettingsGenerator
    {
        private readonly ILogger logger;
        private readonly RulesSettings settings;
        private readonly RuleConfigGenerator converter;

        public static RulesSettings Create(string language, string ruleDirectoryPath, ILogger logger)
        {
            return new RulesSettingsGenerator(logger).Create(language, ruleDirectoryPath);
        }

        private RulesSettingsGenerator(ILogger logger)
        {
            this.logger = logger;
            settings = new RulesSettings();
            converter = new RuleConfigGenerator(logger);
        }

        private RulesSettings Create(string language, string ruleDirectoryPath)
        {
            var inputConfig = GetInputRulesConfiguration(ruleDirectoryPath);
            ProcessConfigForLanguage(language, inputConfig);
            return settings;
        }

        private ICFamilyRulesConfigProvider GetInputRulesConfiguration(string ruleDirectoryPath)
        {
            logger.LogMessage("Fetching input rules configuration...");
            var configProvider = new CFamilySonarWayRulesConfigProvider(ruleDirectoryPath);
            return configProvider;
        }

        private void ProcessConfigForLanguage(string languageKey, ICFamilyRulesConfigProvider configProvider)
        {
            logger.LogMessage($"Processing language: {languageKey}:");
            var config = configProvider.GetRulesConfiguration(languageKey);

            logger.LogMessage($"  Total rules: {config.AllPartialRuleKeys.Count()}, Active rules: {config.AllPartialRuleKeys}");

            foreach (var partialRuleKey in config.AllPartialRuleKeys)
            {
                var fullRuleKey = GetFullRuleKey(languageKey, partialRuleKey);
                settings.Rules[fullRuleKey] = converter.CreateRuleConfig(partialRuleKey, config);
            }
        }

        private static string GetFullRuleKey(string language, string partialRuleKey)
            => $"{language}:{partialRuleKey}";
    }
}
