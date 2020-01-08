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
using SonarLint.VisualStudio.Integration.ProfileConflicts;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableRuleSetInspector : IRuleSetInspector
    {
        #region IRuleSetInspector

        RuleConflictInfo IRuleSetInspector.FindConflictingRules(string baselineRuleSet, string targetRuleSet, params string[] ruleSetDirectories)
        {
            return this.FindConflictingRulesAction?.Invoke(baselineRuleSet, targetRuleSet, ruleSetDirectories);
        }

        FixedRuleSetInfo IRuleSetInspector.FixConflictingRules(string baselineRuleSetPath, string targetRuleSetPath, params string[] ruleSetDirectories)
        {
            return this.FixConflictingRulesAction?.Invoke(baselineRuleSetPath, targetRuleSetPath, ruleSetDirectories);
        }

        #endregion IRuleSetInspector

        #region Test helpers

        public Func<string, string, string[], RuleConflictInfo> FindConflictingRulesAction
        {
            get; set;
        }

        public Func<string, string, string[], FixedRuleSetInfo> FixConflictingRulesAction
        {
            get; set;
        }

        #endregion Test helpers
    }
}