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

using System;
using System.Collections.Generic;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.Rules
{
    internal interface IRulesProvider
    {
        /// <summary>
        /// Returns the metadata descriptions for all rules for a single language repository
        /// </summary>
        IEnumerable<RuleDefinition> GetDefinitions();

        /// <summary>
        /// Returns the eslint configuration for the currently active rules
        /// </summary>
        IEnumerable<Rule> GetActiveRulesConfiguration();
    }

    internal class RulesProvider : IRulesProvider
    {
        private readonly IEnumerable<RuleDefinition> ruleDefinitions;
        private readonly IActiveRulesCalculator activeRulesCalculator;

        public RulesProvider(IEnumerable<RuleDefinition> ruleDefinitions, IActiveRulesCalculator activeRulesCalculator)
        {
            this.ruleDefinitions = ruleDefinitions ?? throw new ArgumentNullException(nameof(ruleDefinitions));
            this.activeRulesCalculator = activeRulesCalculator ?? throw new ArgumentNullException(nameof(activeRulesCalculator));
        }

        // The set of definitions is static...
        public IEnumerable<RuleDefinition> GetDefinitions() => ruleDefinitions;

        // ... but the set of active rules is calculated dynamically
        public IEnumerable<Rule> GetActiveRulesConfiguration() => activeRulesCalculator.Calculate();
    }
}
