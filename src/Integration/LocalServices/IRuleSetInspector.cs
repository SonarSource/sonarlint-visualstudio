//-----------------------------------------------------------------------
// <copyright file="IRuleSetInspector.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// RuleSet inspection service
    /// </summary>
    public interface IRuleSetInspector
    {
        /// <summary>
        /// Inspects whether the <paramref name="baselineRuleSet"/> rules are missing or less strict than in the <paramref name="targetRuleSet"/>
        /// </summary>
        /// <param name="baselineRuleSet">Required full path to baseline RuleSet</param>
        /// <param name="targetRuleSet">Required full path to target RuleSet</param>
        /// <returns><see cref="RuleConflictInfo"/></returns>
        RuleConflictInfo FindConflictingRules(string baselineRuleSet, string targetRuleSet);

        /// <summary>
        /// Will analyse the RuleSet in <paramref name="targetRuleSetPath"/> for conflicts with RuleSet in <paramref name="baselineRuleSetPath"/>.
        /// Will fix those conflicts in-memory and will either way return the target RuleSet (i.e. even if there were no conflicts to begin with).
        /// </summary>
        RuleSet FixConflictingRules(string baselineRuleSetPath, string targetRuleSetPath);
    }
}
