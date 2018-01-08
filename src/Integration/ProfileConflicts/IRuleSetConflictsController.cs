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

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Provides the UX for RuleSet conflict - detection and auto-fix
    /// </summary>
    /// <seealso cref="IConflictsManager"/>
    /// <seealso cref="IRuleSetInspector"/>
    internal interface IRuleSetConflictsController : ILocalService
    {
        /// <summary>
        /// Checks whether the current solution has projects with conflicts RuleSets.
        /// The check is against the solution level RuleSet (if solution is bound).
        /// </summary>
        /// <returns>Whether has conflicts (in which case there will be a UX to auto-fix them as well)</returns>
        bool CheckForConflicts();

        /// <summary>
        /// Clears any UX that was activated part of <see cref="CheckForConflicts"/>
        /// </summary>
        void Clear();
    }
}
