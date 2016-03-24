//-----------------------------------------------------------------------
// <copyright file="ConfigurableRuleSetInspector.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;

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
        #endregion

        #region Test helpers
        public Func<string, string, string[], RuleConflictInfo> FindConflictingRulesAction
        {
            get; set;
        }

        public Func<string, string, string[], FixedRuleSetInfo> FixConflictingRulesAction
        {
            get; set;
        }
        #endregion
    }
}
