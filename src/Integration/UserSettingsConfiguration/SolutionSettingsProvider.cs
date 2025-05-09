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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;

namespace SonarLint.VisualStudio.Integration.UserSettingsConfiguration;

[Export(typeof(ISolutionUserSettingsUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class SolutionUserSettingsUpdater : ISolutionUserSettingsUpdater
{
    private readonly ISolutionSettingsStorage solutionSettingsStorage1;
    private readonly IUserSettingsProvider userSettingsProvider1;

    [method: ImportingConstructor]
    public SolutionUserSettingsUpdater(
        ISolutionSettingsStorage solutionSettingsStorage,
        IUserSettingsProvider userSettingsProvider,
        IInitializationProcessorFactory processorFactory)
    {
        solutionSettingsStorage1 = solutionSettingsStorage;
        userSettingsProvider1 = userSettingsProvider;
        InitializationProcessor = processorFactory.CreateAndStart<SolutionUserSettingsUpdater>(
            [solutionSettingsStorage, userSettingsProvider],
            () =>
            {
            });
    }

    public IInitializationProcessor InitializationProcessor { get; }
    public ImmutableArray<string> FileExclusions => userSettingsProvider1.UserSettings.AnalysisSettings.SolutionFileExclusions;

    public void UpdateFileExclusions(IEnumerable<string> exclusions)
    {
        var userSettings = userSettingsProvider1.UserSettings;
        var solutionSettings = new SolutionAnalysisSettings(userSettings.AnalysisSettings.AnalysisProperties, exclusions.ToImmutableArray());
        solutionSettingsStorage1.SaveSettingsFile(solutionSettings);
    }

    public void UpdateAnalysisProperties(Dictionary<string, string> analysisProperties)
    {
        var userSettings = userSettingsProvider1.UserSettings;
        var solutionSettings = new SolutionAnalysisSettings(analysisProperties, userSettings.AnalysisSettings.SolutionFileExclusions);
        solutionSettingsStorage1.SaveSettingsFile(solutionSettings);
    }
}
