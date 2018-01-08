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


namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// RuleSet inspection service
    /// </summary>
    public interface IRuleSetInspector : ILocalService
    {
        /// <summary>
        /// Inspects whether the <paramref name="baselineRuleSet"/> rules are missing or less strict than in the <paramref name="targetRuleSet"/>
        /// </summary>
        /// <param name="baselineRuleSet">Required full path to baseline RuleSet</param>
        /// <param name="targetRuleSet">Required full path to target RuleSet</param>
        /// <param name="ruleSetDirectories">Optional rule set directories i.e. when the <paramref name="targetRuleSet"/> is not absolute</param>
        /// <returns><see cref="RuleConflictInfo"/></returns>
        RuleConflictInfo FindConflictingRules(string baselineRuleSet, string targetRuleSet, params string[] ruleSetDirectories);

        /// <summary>
        /// Will analyze the RuleSet in <paramref name="targetRuleSetPath"/> for conflicts with RuleSet in <paramref name="baselineRuleSetPath"/>.
        /// Will fix those conflicts in-memory and will either way return the target RuleSet (i.e. even if there were no conflicts to begin with).
        /// </summary>
        /// <param name="baselineRuleSet">Required full path to baseline RuleSet</param>
        /// <param name="targetRuleSet">Required full path to target RuleSet</param>
        /// <param name="ruleSetDirectories">Optional rule set directories i.e. when the <paramref name="targetRuleSet"/> is not absolute</param>
        FixedRuleSetInfo FixConflictingRules(string baselineRuleSetPath, string targetRuleSetPath, params string[] ruleSetDirectories);
    }
}
