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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.State;
using CleanCodeAttribute = SonarLint.VisualStudio.Core.Analysis.CleanCodeAttribute;
using IssueSeverity = SonarLint.VisualStudio.SLCore.Common.Models.IssueSeverity;
using SoftwareQuality = SonarLint.VisualStudio.Core.Analysis.SoftwareQuality;

namespace SonarLint.VisualStudio.Education.Rule;

[Export(typeof(IRuleMetaDataProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class SLCoreRuleMetaDataProvider : IRuleMetaDataProvider
{
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly ILogger logger;
    private readonly ISLCoreServiceProvider slCoreServiceProvider;

    [ImportingConstructor]
    public SLCoreRuleMetaDataProvider(ISLCoreServiceProvider slCoreServiceProvider,
        IActiveConfigScopeTracker activeConfigScopeTracker, ILogger logger)
    {
        this.slCoreServiceProvider = slCoreServiceProvider;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.logger = logger;
    }

    public async Task<IRuleInfo> GetRuleInfoAsync(SonarCompositeRuleId ruleId)
    {
        if (activeConfigScopeTracker.Current is { Id: var configurationScopeId }
            && slCoreServiceProvider.TryGetTransientService(out IRulesRpcService rulesRpcService))
        {
            try
            {
                var ruleDetailsResponse = await rulesRpcService.GetEffectiveRuleDetailsAsync(
                    new GetEffectiveRuleDetailsParams(configurationScopeId, ruleId.ToString()));
                return Convert(ruleDetailsResponse.details);
            }
            catch (Exception e)
            {
                logger.WriteLine(e.ToString());
            }
        }

        return null;
    }

    private static RuleInfo Convert(EffectiveRuleDetailsDto effectiveRuleDetailsAsync) =>
        new(effectiveRuleDetailsAsync.key,
            HtmlXmlCompatibilityHelper.EnsureHtmlIsXml(effectiveRuleDetailsAsync.description.Left?.htmlContent),
            effectiveRuleDetailsAsync.name,
            Convert(effectiveRuleDetailsAsync.severity),
            Convert(effectiveRuleDetailsAsync.type),
            effectiveRuleDetailsAsync.description.Right,
            Convert(effectiveRuleDetailsAsync.cleanCodeAttribute),
            Convert(effectiveRuleDetailsAsync.defaultImpacts));

    private static Dictionary<SoftwareQuality, SoftwareQualitySeverity> Convert(List<ImpactDto> cleanCodeAttribute) => 
        cleanCodeAttribute.ToDictionary(x => Convert(x.softwareQuality), x => Convert(x.impactSeverity));


    private static RuleIssueSeverity Convert(IssueSeverity issueSeverity) =>
        issueSeverity switch
        {
            IssueSeverity.BLOCKER => RuleIssueSeverity.Blocker,
            IssueSeverity.CRITICAL => RuleIssueSeverity.Critical,
            IssueSeverity.MAJOR => RuleIssueSeverity.Major,
            IssueSeverity.MINOR => RuleIssueSeverity.Minor,
            IssueSeverity.INFO => RuleIssueSeverity.Info,
            _ => throw new ArgumentOutOfRangeException(nameof(issueSeverity), issueSeverity, null)
        };

    private static RuleIssueType Convert(RuleType ruleType) =>
        ruleType switch
        {
            RuleType.CODE_SMELL => RuleIssueType.CodeSmell,
            RuleType.BUG => RuleIssueType.Bug,
            RuleType.VULNERABILITY => RuleIssueType.Vulnerability,
            RuleType.SECURITY_HOTSPOT => RuleIssueType.Hotspot,
            _ => throw new ArgumentOutOfRangeException(nameof(ruleType), ruleType, null)
        };

    private static SoftwareQuality Convert(SLCore.Common.Models.SoftwareQuality argSoftwareQuality) =>
        argSoftwareQuality switch
        {
            SLCore.Common.Models.SoftwareQuality.MAINTAINABILITY => SoftwareQuality.Maintainability,
            SLCore.Common.Models.SoftwareQuality.RELIABILITY => SoftwareQuality.Reliability,
            SLCore.Common.Models.SoftwareQuality.SECURITY => SoftwareQuality.Security,
            _ => throw new ArgumentOutOfRangeException(nameof(argSoftwareQuality), argSoftwareQuality, null)
        };

    private static SoftwareQualitySeverity Convert(ImpactSeverity cleanCodeAttribute) =>
        cleanCodeAttribute switch
        {
            ImpactSeverity.LOW => SoftwareQualitySeverity.Low,
            ImpactSeverity.MEDIUM => SoftwareQualitySeverity.Medium,
            ImpactSeverity.HIGH => SoftwareQualitySeverity.High,
            _ => throw new ArgumentOutOfRangeException(nameof(cleanCodeAttribute), cleanCodeAttribute, null)
        };

    private static CleanCodeAttribute? Convert(SLCore.Common.Models.CleanCodeAttribute? cleanCodeAttribute) =>
        cleanCodeAttribute switch
        {
            SLCore.Common.Models.CleanCodeAttribute.CONVENTIONAL => CleanCodeAttribute.Conventional,
            SLCore.Common.Models.CleanCodeAttribute.FORMATTED => CleanCodeAttribute.Formatted,
            SLCore.Common.Models.CleanCodeAttribute.IDENTIFIABLE => CleanCodeAttribute.Identifiable,
            SLCore.Common.Models.CleanCodeAttribute.CLEAR => CleanCodeAttribute.Clear,
            SLCore.Common.Models.CleanCodeAttribute.COMPLETE => CleanCodeAttribute.Complete,
            SLCore.Common.Models.CleanCodeAttribute.EFFICIENT => CleanCodeAttribute.Efficient,
            SLCore.Common.Models.CleanCodeAttribute.LOGICAL => CleanCodeAttribute.Logical,
            SLCore.Common.Models.CleanCodeAttribute.DISTINCT => CleanCodeAttribute.Distinct,
            SLCore.Common.Models.CleanCodeAttribute.FOCUSED => CleanCodeAttribute.Focused,
            SLCore.Common.Models.CleanCodeAttribute.MODULAR => CleanCodeAttribute.Modular,
            SLCore.Common.Models.CleanCodeAttribute.TESTED => CleanCodeAttribute.Tested,
            SLCore.Common.Models.CleanCodeAttribute.LAWFUL => CleanCodeAttribute.Lawful,
            SLCore.Common.Models.CleanCodeAttribute.RESPECTFUL => CleanCodeAttribute.Respectful,
            SLCore.Common.Models.CleanCodeAttribute.TRUSTWORTHY => CleanCodeAttribute.Trustworthy,
            null => null,
            _ => throw new ArgumentOutOfRangeException(nameof(cleanCodeAttribute), cleanCodeAttribute, null)
        };
}
