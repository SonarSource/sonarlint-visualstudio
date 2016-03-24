//-----------------------------------------------------------------------
// <copyright file="RuleConflictInfo.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Data-only class that returns conflict information between the SonarQube RuleSet and the configured RuleSet
    /// </summary>
    public class RuleConflictInfo
    {
        public RuleConflictInfo()
            : this(new RuleReference[0])
        {
            Debug.Assert(!this.HasConflicts);
        }

        public RuleConflictInfo(IEnumerable<RuleReference> missing)
            : this(missing, new Dictionary<RuleReference, RuleAction>())
        {
        }

        public RuleConflictInfo(IDictionary<RuleReference, RuleAction> weakenedRulesMap)
            : this(new RuleReference[0], weakenedRulesMap)
        {
        }

        public RuleConflictInfo(IEnumerable<RuleReference> missing, IDictionary<RuleReference, RuleAction> weakenedRulesMap)
        {
            if (missing == null)
            {
                throw new ArgumentNullException(nameof(missing));
            }

            if (weakenedRulesMap == null)
            {
                throw new ArgumentNullException(nameof(weakenedRulesMap));
            }

            this.MissingRules = missing.ToArray();
            this.WeakerActionRules = new Dictionary<RuleReference, RuleAction>(weakenedRulesMap);
            this.HasConflicts = this.MissingRules.Count > 0 || this.WeakerActionRules.Count > 0;
        }

        /// <summary>
        /// All the baseline rules that are missing (removed or set to None)
        /// </summary>
        public IReadOnlyList<RuleReference> MissingRules { get; }

        /// <summary>
        /// Map of conflicts. The key is the conflicting rule and the value is the expected RuleAction
        /// </summary>
        public IReadOnlyDictionary<RuleReference, RuleAction> WeakerActionRules { get; }

        public bool HasConflicts { get; }
    }
}
