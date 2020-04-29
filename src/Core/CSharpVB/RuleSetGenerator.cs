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
using System.Linq;
using SonarQube.Client.Models;

// Copied from the SonarScanner for MSBuild with a few tweaks.
// S4MSB version: https://github.com/SonarSource/sonar-scanner-msbuild/blob/9ccfdb648a0411014b29c7aee8e347aeab87ea71/src/SonarScanner.MSBuild.PreProcessor/Roslyn/RoslynRuleSetGenerator.cs#L28
//
// Modifications:
// * the SLVS version groups and sort rules alphabetically.
//      Rationale: S4MSB generates a new file for each run so the order doesn't really matter. However,
//      the SLVS version is checked in, and will be updated as the QP changes. Ordering the ruleset
//      reduces the churn when comparing old/new files.
// * the SLVS version doesn't include taint-analysis rules

namespace SonarLint.VisualStudio.Core.CSharpVB
{
    public class RuleSetGenerator
    {
        private const string SONARANALYZER_PARTIAL_REPO_KEY = "sonaranalyzer-{0}";
        private const string ROSLYN_REPOSITORY_PREFIX = "roslyn.";

        private readonly IDictionary<string, string> sonarProperties;
        private readonly string inactiveRuleActionText = GetActionText(RuleAction.None);

        private readonly string activeRuleActionText = GetActionText(RuleAction.Warning);

        public RuleSetGenerator(IDictionary<string, string> sonarProperties)
        {
            this.sonarProperties = sonarProperties ?? throw new ArgumentNullException(nameof(sonarProperties));
        }

        /// <summary>
        /// Generates a RuleSet that is serializable (XML).
        /// The ruleset can be empty if there are no active rules belonging to the repo keys "vbnet", "csharpsquid" or "roslyn.*".
        /// </summary>
        /// <exception cref="InvalidOperationException">if required properties that should be associated with the repo key are missing.</exception>
        public RuleSet Generate(string language, IEnumerable<SonarQubeRule> activeRules, IEnumerable<SonarQubeRule> inactiveRules)
        {
            if (activeRules == null)
            {
                throw new ArgumentNullException(nameof(activeRules));
            }
            if (inactiveRules == null)
            {
                throw new ArgumentNullException(nameof(inactiveRules));
            }
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            var rulesElements = activeRules.Concat(inactiveRules)
                .GroupBy(
                    rule => GetPartialRepoKey(rule, language),
                    rule => rule)
                .Where(IsSupportedRuleRepo)
                .Select(CreateRulesElement)
                .OrderBy(group => group.AnalyzerId)
                .ToList();

            var ruleSet = new RuleSet
            {
                Name = "Rules for SonarQube",
                Description = "This rule set was automatically generated from SonarQube",
                ToolsVersion = "14.0"
            };

            ruleSet.Rules.AddRange(rulesElements);

            return ruleSet;
        }

        private static bool IsSupportedRuleRepo(IGrouping<string, SonarQubeRule> analyzerRules)
        {
            var partialRepoKey = analyzerRules.Key;
            return !string.IsNullOrEmpty(partialRepoKey) && !RoslynPluginRuleKeyExtensions.IsExcludedRuleRepository(partialRepoKey);
        }

        private Rules CreateRulesElement(IGrouping<string, SonarQubeRule> analyzerRules)
        {
            var partialRepoKey = analyzerRules.Key;
            return new Rules
            {
                AnalyzerId = GetRequiredPropertyValue($"{partialRepoKey}.analyzerId"),
                RuleNamespace = GetRequiredPropertyValue($"{partialRepoKey}.ruleNamespace"),
                RuleList = analyzerRules
                    .Select(CreateRuleElement)
                    .OrderBy(r => r.Id)
                    .ToList()
            };
        }

        private Rule CreateRuleElement(SonarQubeRule sonarRule) =>
            new Rule(sonarRule.Key, sonarRule.IsActive ? activeRuleActionText : inactiveRuleActionText);

        internal /* for testing */ static string GetActionText(RuleAction ruleAction)
        {
            switch (ruleAction)
            {
                case RuleAction.None:
                    return "None";
                case RuleAction.Info:
                    return "Info";
                case RuleAction.Warning:
                    return "Warning";
                case RuleAction.Error:
                    return "Error";
                case RuleAction.Hidden:
                    return "Hidden";
                default:
                    throw new NotSupportedException($"{ruleAction} is not a supported RuleAction.");
            }
        }

        private static string GetPartialRepoKey(SonarQubeRule rule, string language)
        {
            if (rule.RepositoryKey.StartsWith(ROSLYN_REPOSITORY_PREFIX))
            {
                return rule.RepositoryKey.Substring(ROSLYN_REPOSITORY_PREFIX.Length);
            }
            else if ("csharpsquid".Equals(rule.RepositoryKey) || "vbnet".Equals(rule.RepositoryKey))
            {
                return string.Format(SONARANALYZER_PARTIAL_REPO_KEY, language);
            }
            else
            {
                return null;
            }
        }

        private string GetRequiredPropertyValue(string propertyKey)
        {
            if (!this.sonarProperties.TryGetValue(propertyKey, out var propertyValue))
            {
                var message = $"Property does not exist: {propertyKey}. This property should be set by the plugin in SonarQube.";

                if (propertyKey.StartsWith(string.Format(SONARANALYZER_PARTIAL_REPO_KEY, "vbnet")))
                {
                    message += " Possible cause: this Scanner is not compatible with SonarVB 2.X. If necessary, upgrade SonarVB latest in SonarQube.";
                }

                throw new InvalidOperationException(message);
            }

            return propertyValue;
        }
    }
}
