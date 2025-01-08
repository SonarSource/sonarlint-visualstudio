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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Issue;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.Core.ConfigurationScope;

namespace SonarLint.VisualStudio.Education.Rule;

[Export(typeof(IRuleMetaDataProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SLCoreRuleMetaDataProvider(
    ISLCoreServiceProvider slCoreServiceProvider,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    IRuleInfoConverter ruleInfoConverter,
    ILogger logger)
    : IRuleMetaDataProvider
{
    /// <inheritdoc />
    public async Task<IRuleInfo> GetRuleInfoAsync(SonarCompositeRuleId ruleId, Guid? issueId = null)
    {
        if (activeConfigScopeTracker.Current is not { Id: var configurationScopeId })
        {
            return null;
        }

        var ruleInfoFromIssue = issueId != null ? await GetEffectiveIssueDetailsAsync(configurationScopeId, issueId.Value) : null;

        return ruleInfoFromIssue ?? await GetEffectiveRuleDetailsAsync(configurationScopeId, ruleId);
    }

    private async Task<IRuleInfo> GetEffectiveIssueDetailsAsync(string configurationScopeId, Guid issueId)
    {
        if (slCoreServiceProvider.TryGetTransientService(out IIssueSLCoreService rulesRpcService))
        {
            try
            {
                var issueDetailsResponse = await rulesRpcService.GetEffectiveIssueDetailsAsync(
                    new GetEffectiveIssueDetailsParams(configurationScopeId, issueId));
                return ruleInfoConverter.Convert(issueDetailsResponse.details);
            }
            catch (Exception e)
            {
                logger.WriteLine(e.ToString());
            }
        }

        return null;
    }

    private async Task<IRuleInfo> GetEffectiveRuleDetailsAsync(string configurationScopeId, SonarCompositeRuleId ruleId)
    {
        if (slCoreServiceProvider.TryGetTransientService(out IRulesSLCoreService rulesRpcService))
        {
            try
            {
                var ruleDetailsResponse = await rulesRpcService.GetEffectiveRuleDetailsAsync(
                    new GetEffectiveRuleDetailsParams(configurationScopeId, ruleId.ToString()));
                return ruleInfoConverter.Convert(ruleDetailsResponse.details);
            }
            catch (Exception e)
            {
                logger.WriteLine(e.ToString());
            }
        }

        return null;
    }
}
