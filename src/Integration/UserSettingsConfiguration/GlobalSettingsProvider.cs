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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Integration.UserSettingsConfiguration;

[Export(typeof(IGlobalUserSettingsUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class GlobalUserSettingsUpdater : IGlobalUserSettingsUpdater
{
    private readonly IGlobalSettingsStorage globalSettingsStorage;
    private readonly IUserSettingsProvider userSettingsProvider;
    private readonly Task initializationTask;

    [ImportingConstructor]
    public GlobalUserSettingsUpdater(
        IGlobalSettingsStorage globalSettingsStorage,
        IUserSettingsProvider userSettingsProvider,
        IInitializationProcessorFactory processorFactory,
        IThreadHandling threadHandling)
    {
        this.globalSettingsStorage = globalSettingsStorage;
        this.userSettingsProvider = userSettingsProvider;
        InitializationProcessor = processorFactory.Create<GlobalUserSettingsUpdater>([globalSettingsStorage, userSettingsProvider], _ => Task.CompletedTask);
        initializationTask = threadHandling.RunAsync(() => InitializationProcessor.InitializeAsync());
    }

    public IInitializationProcessor InitializationProcessor { get; }
    public ImmutableArray<string> FileExclusions => userSettingsProvider.UserSettings.AnalysisSettings.GlobalFileExclusions;

    public async Task DisableRule(string ruleId)
    {
        Debug.Assert(!string.IsNullOrEmpty(ruleId), "DisableRule: ruleId should not be null/empty");

        await initializationTask;
        var userSettings = userSettingsProvider.UserSettings;
        var newRules = userSettings.AnalysisSettings.Rules.SetItem(ruleId, new RuleConfig(RuleLevel.Off));
        var globalSettings = new GlobalAnalysisSettings(newRules, userSettings.AnalysisSettings.GlobalFileExclusions);
        globalSettingsStorage.SaveSettingsFile(globalSettings);
    }

    public async Task UpdateFileExclusions(IEnumerable<string> exclusions)
    {
        await initializationTask;
        var userSettings = userSettingsProvider.UserSettings;
        var globalSettings = new GlobalAnalysisSettings(userSettings.AnalysisSettings.Rules, exclusions.ToImmutableArray());
        globalSettingsStorage.SaveSettingsFile(globalSettings);
    }
}
