/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

namespace SonarLint.VisualStudio.TypeScript.Rules
{
    internal interface IRuleProvider
    {
        /// <summary>
        /// Returns the metadata descriptions for all rules for a single language repository
        /// </summary>
        IEnumerable<RuleDefinition> GetDefinitions();

        /// <summary>
        /// Returns the rule key to display in VS for the specified ESLint rule
        /// </summary>
        string GetSonarRuleKey(string eslintRuleKey);
    }

    internal class RuleDefinitionProvider : IRuleProvider
    {
        private IEnumerable<RuleDefinition> ruleDefinitions;

        public RuleDefinitionProvider(IEnumerable<RuleDefinition> ruleDefinitions)
        {
            this.ruleDefinitions = ruleDefinitions ?? throw new ArgumentNullException(nameof(ruleDefinitions));
        }

        public IEnumerable<RuleDefinition> GetDefinitions() => ruleDefinitions;

        public string GetSonarRuleKey(string eslintRuleKey) =>
            ruleDefinitions.FirstOrDefault(x => x.EslintKey.Equals(eslintRuleKey, StringComparison.OrdinalIgnoreCase))
                ?.RuleKey;
    }
}
