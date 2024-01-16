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

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Education / help-related services
    /// </summary>
    public interface IEducation
    {
        /// <summary>
        /// Displays the help for the specific Sonar rule
        /// </summary>
        /// <remarks>If the metadata for the rule is available locally, the rule help will
        /// be displayed in the IDE. Otherwise, the rule help will be displayed in the
        /// browser i.e. at rules.sonarsource.com</remarks>
        /// <param name="issueContext">Key for the How to fix it Context acquired from a specific issue. Can be null.</param>
        void ShowRuleHelp(SonarCompositeRuleId ruleId, string issueContext);
    }
}
