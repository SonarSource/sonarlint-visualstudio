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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;

namespace SonarLint.VisualStudio.SLCore.Analysis;

public interface ISLCoreRuleSettingsUpdater
{
    void UpdateStandaloneRulesConfiguration();
}

public interface ISLCoreRuleSettingsProvider
{
    Dictionary<string, StandaloneRuleConfigDto> GetSLCoreRuleSettings();
}

[Export(typeof(ISLCoreRuleSettingsUpdater))]
[Export(typeof(ISLCoreRuleSettingsProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class SlCoreRuleSettings : ISLCoreRuleSettingsUpdater, ISLCoreRuleSettingsProvider
{
    private readonly ILogger logger;
    private readonly IUserSettingsProvider userSettingsProvider;
    private readonly ISLCoreServiceProvider slCoreServiceProvider;

    [ImportingConstructor]
    public SlCoreRuleSettings(ILogger logger, IUserSettingsProvider userSettingsProvider, ISLCoreServiceProvider slCoreServiceProvider)
    {
        this.logger = logger;
        this.userSettingsProvider = userSettingsProvider;
        this.slCoreServiceProvider = slCoreServiceProvider;
    }

    public Dictionary<string, StandaloneRuleConfigDto> GetSLCoreRuleSettings()
    {
        return userSettingsProvider.UserSettings.RulesSettings.Rules.ToDictionary(kvp => kvp.Key, kvp => MapStandaloneRuleConfigDto(kvp.Value));
    }

    public void UpdateStandaloneRulesConfiguration()
    {
        if (!slCoreServiceProvider.TryGetTransientService(out IRulesSLCoreService rulesSlCoreService))
        {
            logger.WriteLine($"[{nameof(SlCoreRuleSettings)}] {SLCoreStrings.ServiceProviderNotInitialized}");
            return;
        }

        try
        {
            rulesSlCoreService.UpdateStandaloneRulesConfiguration(new UpdateStandaloneRulesConfigurationParams(GetSLCoreRuleSettings()));
        }
        catch (Exception e)
        {
            logger.WriteLine(e.ToString());
        }
    }

    private static StandaloneRuleConfigDto MapStandaloneRuleConfigDto(RuleConfig ruleConfig) => new(MapIsActive(ruleConfig.Level), MapParameters(ruleConfig.Parameters));
    private static bool MapIsActive(RuleLevel ruleLevel) => ruleLevel == RuleLevel.On;
    private static Dictionary<string, string> MapParameters(Dictionary<string, string> ruleParameters) => ruleParameters ?? [];
}
