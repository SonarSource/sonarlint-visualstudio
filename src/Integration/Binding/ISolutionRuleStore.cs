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

using System.Collections.Generic;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Provides access to solution level rules
    /// </summary>
    internal interface ISolutionRuleStore
    {
        /// <summary>
        /// Registers a mapping of <see cref="Language"/> to <see cref="RuleSet"/>.
        /// </summary>
        /// <param name="ruleSets">Required</param>
        void RegisterKnownRuleSets(IDictionary<Language, RuleSet> ruleSets);

        /// <summary>
        /// Retrieves the solution-level <see cref="RuleSet"/> mapped to the <see cref="Language"/>.
        /// </summary>
        RuleSetInformation GetRuleSetInformation(Language language);
    }
}
