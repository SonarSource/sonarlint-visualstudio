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

using SonarLint.VisualStudio.Core.Initialization;

namespace SonarLint.VisualStudio.Core.UserRuleSettings;

public interface IUserSettingsProvider : IRequireInitialization, IGlobalUserSettingsUpdater, ISolutionUserSettingsUpdater
{
    /// <summary>
    /// The settings for the current user
    /// </summary>
    UserSettings UserSettings { get; }

    event EventHandler SettingsChanged;
}

public interface IGlobalUserSettingsUpdater
{
    /// <summary>
    /// Updates the user settings to disable the specified rule
    /// </summary>
    void DisableRule(string ruleId);

    /// <summary>
    /// Updates the user settings to include the provided global/solution file exclusions. The value will override existing exclusions.
    /// </summary>
    void UpdateGlobalFileExclusions(IEnumerable<string> exclusions);
}

public interface ISolutionUserSettingsUpdater
{
    /// <summary>
    /// Updates the solution level analysis settings to include the provided analysis properties. The value will override existing analysis settings.
    /// </summary>
    void UpdateAnalysisProperties(Dictionary<string, string> analysisProperties);

    /// <summary>
    /// Updates the user settings to include the provided global/solution file exclusions. The value will override existing exclusions.
    /// </summary>
    void UpdateSolutionFileExclusions(IEnumerable<string> exclusions);
}
