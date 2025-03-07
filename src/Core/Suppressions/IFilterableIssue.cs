﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.Suppressions
{
    /// <summary>
    /// Describes a single issue with the properties required for
    /// it to be compared against server-side issues by the issues filter
    /// </summary>
    public interface IFilterableIssue
    {
        /// <summary>
        /// The id of the issue that comes from SlCore
        /// Nullable due to the fact that some issues do not come from SlCore (e.g. Roslyn)
        /// </summary>
        Guid? IssueId { get; }
        string RuleId { get; }
        string FilePath { get; }
        string LineHash { get; }
        int? StartLine { get; }
    }
}
