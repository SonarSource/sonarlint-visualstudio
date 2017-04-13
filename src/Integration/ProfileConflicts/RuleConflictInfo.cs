/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
