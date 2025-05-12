/*
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

using System.Collections.Immutable;
using SonarLint.VisualStudio.Core.Initialization;

namespace SonarLint.VisualStudio.Core.UserRuleSettings;

public interface IFileExclusionsProvider
{
    /// <summary>
    /// Updates the user settings to include the provided global/solution file exclusions. The value will override existing exclusions.
    /// </summary>
    Task UpdateFileExclusions(IEnumerable<string> exclusions);

    ImmutableArray<string> FileExclusions { get; }
}

public interface IGlobalUserSettingsUpdater : IRequireInitialization, IFileExclusionsProvider
{
    /// <summary>
    /// Updates the user settings to disable the specified rule
    /// </summary>
    Task DisableRule(string ruleId);
}

public interface ISolutionUserSettingsUpdater : IRequireInitialization, IFileExclusionsProvider
{
    /// <summary>
    /// Updates the solution level analysis settings to include the provided analysis properties. The value will override existing analysis settings.
    /// </summary>
    Task UpdateAnalysisProperties(Dictionary<string, string> analysisProperties);
}
