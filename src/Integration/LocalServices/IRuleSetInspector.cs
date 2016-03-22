//-----------------------------------------------------------------------
// <copyright file="IRuleSetInspector.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// RuleSet inspection service
    /// </summary>
    public interface IRuleSetInspector
    {
        /// <summary>
        /// Inspects whether the <paramref name="baseLineRuleSet"/> rules are missing or less strict in the <paramref name="targetRuleSet"/>
        /// </summary>
        /// <param name="baseLineRuleSet">Required full path to baseLine</param>
        /// <param name="targetRuleSet">Required full path to target</param>
        /// <returns><see cref="RuleConflictInfo"/></returns>
        RuleConflictInfo FindConflictingRules(string baseLineRuleSet, string targetRuleSet);
    }
}
