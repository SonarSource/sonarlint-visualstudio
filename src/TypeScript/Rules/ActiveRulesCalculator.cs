/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.Rules
{
    /// <summary>
    /// Calculates the set of active rules, taking into account user modifications
    /// to the default rule definitions
    /// </summary>
    internal interface IActiveRulesCalculator
    {
        IEnumerable<Rule> Calculate();
    }

    internal class ActiveRulesCalculator : IActiveRulesCalculator
    {
        private readonly IEnumerable<RuleDefinition> ruleDefinitions;
        private readonly IRuleSettingsProvider ruleSettingsProvider;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="rulesDefinitions">The set of applicable rule definitions</param>
        /// <param name="ruleSettingsProvider">Rule configurations specified in connected mode or by the user</param>
        public ActiveRulesCalculator(IEnumerable<RuleDefinition> rulesDefinitions,
            IRuleSettingsProvider ruleSettingsProvider)
        {
            this.ruleSettingsProvider = ruleSettingsProvider;
            this.ruleDefinitions = rulesDefinitions?.ToArray() ?? Array.Empty<RuleDefinition>();
        }

        public IEnumerable<Rule> Calculate()
        {
            var settings = ruleSettingsProvider.Get();

            return ruleDefinitions
                .Where(x => IncludeRule(settings, x))
                .Select(Convert)
                .ToArray();
        }

        private bool IncludeRule(RulesSettings settings, RuleDefinition ruleDefinition)
        {
            return ruleDefinition.Type != RuleType.SECURITY_HOTSPOT &&
                (ruleDefinition.EslintKey != null || ruleDefinition.StylelintKey != null) && // should only apply to S2260
                IsRuleActive(settings, ruleDefinition);
        }

        private bool IsRuleActive(RulesSettings settings, RuleDefinition ruleDefinition)
        {
            // Settings override the default, if present
            if (settings.Rules.TryGetValue(ruleDefinition.RuleKey, out var ruleConfig))
            {
                return ruleConfig.Level == RuleLevel.On;
            }

            return ruleDefinition.ActivatedByDefault;
        }

        private static Rule Convert(RuleDefinition ruleDefinition)
        {
            Debug.Assert(ruleDefinition.DefaultParams != null, $"JavaScript rule default params should not be null: {ruleDefinition.RuleKey}");
            return new Rule
            {
                Key = ruleDefinition.EslintKey ?? ruleDefinition.StylelintKey,

                // TODO: handle parameterised rules #2284
                Configurations = ruleDefinition.DefaultParams,

                FileTypeTarget = new[] { "MAIN" }
            };
        }
    }
}
