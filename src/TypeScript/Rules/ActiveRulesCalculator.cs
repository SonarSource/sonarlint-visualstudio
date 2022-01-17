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
        private readonly IUserSettingsProvider userSettingsProvider;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="rulesDefinitions">The set of applicable rule definitions</param>
        /// <param name="userSettingsProvider">Rule configurations specified by the user</param>
        public ActiveRulesCalculator(IEnumerable<RuleDefinition> rulesDefinitions,
            IUserSettingsProvider userSettingsProvider)
        {
            this.ruleDefinitions = rulesDefinitions?.ToArray() ?? Array.Empty<RuleDefinition>();
            this.userSettingsProvider = userSettingsProvider;
        }

        public IEnumerable<Rule> Calculate()
        {
            // TODO: handle QP configuration in connected mode #770
            return ruleDefinitions
                .Where(IncludeRule)
                .Select(Convert)
                .ToArray();
        }

        private bool IncludeRule(RuleDefinition ruleDefinition)
        {
            return ruleDefinition.Type != RuleType.SECURITY_HOTSPOT &&
                ruleDefinition.EslintKey != null && // should only apply to S2260
                IsRuleActive(ruleDefinition);
        }

        private bool IsRuleActive(RuleDefinition ruleDefinition)
        {
            // User settings override the default, if present
            if (userSettingsProvider.UserSettings.RulesSettings.Rules
                .TryGetValue(ruleDefinition.RuleKey, out var ruleConfig))
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
                Key = ruleDefinition.EslintKey,

                // TODO: handle parameterised rules #2284
                Configurations = ruleDefinition.DefaultParams
            };
        }
    }
}
