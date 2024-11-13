/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Suppressions;

namespace SonarLint.VisualStudio.Education.Rule
{
    public interface IRuleMetaDataProvider
    {
        /// <summary>
        /// Returns rule information for the specified rule ID, or null if a rule description
        /// could not be found.
        /// </summary>
        Task<IRuleInfo> GetRuleInfoAsync(SonarCompositeRuleId ruleId);

        /// <summary>
        /// Returns rule information for the specified issue ID.
        /// If <paramref name="issueId"/> is null, returns the rule information for the specified rule ID
        /// If no rule information can be found, null is returned.
        /// </summary>
        Task<IRuleInfo> GetRuleInfoAsync(SonarCompositeRuleId ruleId, Guid? issueId);
    }
}
