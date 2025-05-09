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

[Export(typeof(IGlobalSettingsProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class GlobalSettingsProvider(
    IGlobalSettingsStorage globalSettingsStorage,
    IUserSettingsProvider userSettingsProvider,
    IInitializationProcessorFactory processorFactory)
    : IGlobalSettingsProvider
{
    public void DisableRule(string ruleId)
    {
        Debug.Assert(!string.IsNullOrEmpty(ruleId), "DisableRule: ruleId should not be null/empty");

        var userSettings = userSettingsProvider.UserSettings;
        var newRules = userSettings.AnalysisSettings.Rules.SetItem(ruleId, new RuleConfig(RuleLevel.Off));
        var globalSettings = new GlobalAnalysisSettings(newRules, userSettings.AnalysisSettings.GlobalFileExclusions);
        globalSettingsStorage.SaveSettingsFile(globalSettings);
    }

    public void UpdateFileExclusions(IEnumerable<string> exclusions)
    {
        var userSettings = userSettingsProvider.UserSettings;
        var globalSettings = new GlobalAnalysisSettings(userSettings.AnalysisSettings.Rules, exclusions.ToImmutableArray());
        globalSettingsStorage.SaveSettingsFile(globalSettings);
    }

    public IInitializationProcessor InitializationProcessor { get; } = processorFactory.CreateAndStart<GlobalSettingsProvider>(
        [globalSettingsStorage, userSettingsProvider], () => { });

    public ImmutableArray<string> FileExclusions => userSettingsProvider.UserSettings.AnalysisSettings.GlobalFileExclusions;
}
