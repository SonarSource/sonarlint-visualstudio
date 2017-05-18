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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

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
