//-----------------------------------------------------------------------
// <copyright file="FixedRuleSetInfo.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Data-only class that references the fixed <see cref="RuleSet"/> and has the list of fixes applied to get it into that state
    /// </summary>
    public class FixedRuleSetInfo
    {
        public FixedRuleSetInfo(RuleSet ruleSet, IEnumerable<string> includesReset, IEnumerable<string> rulesDeleted)
        {
            if (ruleSet == null)
            {
                throw new ArgumentNullException(nameof(ruleSet));
            }

            this.FixedRuleSet = ruleSet;
            this.IncludesReset = includesReset ?? Enumerable.Empty<string>();
            this.RulesDeleted = rulesDeleted ?? Enumerable.Empty<string>();
        }

        public RuleSet FixedRuleSet { get; }

        public IEnumerable<string> IncludesReset { get; }

        public IEnumerable<string> RulesDeleted { get; }
    }
}
