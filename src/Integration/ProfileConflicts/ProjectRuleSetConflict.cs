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

using System;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Data-only class that a <see cref="RuleConflictInfo"/> and also captures the information used to find that conflict
    /// </summary>
    /// <seealso cref="IRuleSetInspector"/>
    internal class ProjectRuleSetConflict
    {
        public ProjectRuleSetConflict(RuleConflictInfo conflict, RuleSetInformation aggregate)
        {
            if (conflict == null)
            {
                throw new ArgumentNullException(nameof(conflict));
            }

            if (aggregate == null)
            {
                throw new ArgumentNullException(nameof(aggregate));
            }


            this.Conflict = conflict;
            this.RuleSetInfo = aggregate;
        }

        public RuleSetInformation RuleSetInfo { get; }

        public RuleConflictInfo Conflict { get; }
    }
}