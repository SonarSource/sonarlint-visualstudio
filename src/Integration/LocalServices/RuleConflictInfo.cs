//-----------------------------------------------------------------------
// <copyright file="RuleConflictInfo.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Data-only class that returns conflict information between the SonarQube RuleSet and the configured RuleSet
    /// </summary>
    public class RuleConflictInfo
    {
        public RuleConflictInfo(IEnumerable<RuleReference> missing, IEnumerable<RuleReference> weak)
        {
            this.MissingRules = (missing ?? Enumerable.Empty<RuleReference>()).ToArray();
            this.WeakerActionRules = (weak ?? Enumerable.Empty<RuleReference>()).ToArray();
            this.HasConflicts = this.MissingRules.Count > 0 || this.WeakerActionRules.Count > 0;
        }

        public IReadOnlyList<RuleReference> MissingRules { get; }

        public IReadOnlyList<RuleReference> WeakerActionRules { get; }

        public bool HasConflicts { get; }
    }
}
