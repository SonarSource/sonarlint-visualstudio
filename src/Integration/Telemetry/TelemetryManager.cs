/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.SLCore.Monitoring;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using SonarLint.VisualStudio.SLCore.Service.Telemetry.Models;

namespace SonarLint.VisualStudio.Integration.Telemetry;

[Export(typeof(ITelemetryManager))]
[Export(typeof(IQuickFixesTelemetryManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal sealed class TelemetryManager(ISlCoreTelemetryHelper telemetryHelper, IMonitoringService monitoringService) : ITelemetryManager,
    IQuickFixesTelemetryManager
{
    public void QuickFixApplied(string ruleId) =>
        telemetryHelper.Notify(telemetryService =>
            telemetryService.AddQuickFixAppliedForRule(new AddQuickFixAppliedForRuleParams(ruleId)));

    public void FixSuggestionResolved(string suggestionId, IEnumerable<bool> changeResolutionStatus) =>
        telemetryHelper.Notify(telemetryService =>
        {
            foreach (var resolvedParams in ConvertFixSuggestionChangeToResolvedParams(suggestionId, changeResolutionStatus))
            {
                telemetryService.FixSuggestionResolved(resolvedParams);
            }
        });

    public void AddedManualBindings() => telemetryHelper.Notify(telemetryService => telemetryService.AddedManualBindings());

    public void AddedFromSharedBindings() => telemetryHelper.Notify(telemetryService => telemetryService.AddedImportedBindings());

    public void AddedAutomaticBindings() => telemetryHelper.Notify(telemetryService => telemetryService.AddedAutomaticBindings());

    public void DependencyRiskInvestigatedLocally() => telemetryHelper.Notify(telemetryService => telemetryService.DependencyRiskInvestigatedLocally());

    public void HotspotInvestigatedLocally() => telemetryHelper.Notify(telemetryService => telemetryService.HotspotInvestigatedLocally());

    public void HotspotInvestigatedRemotely() => telemetryHelper.Notify(telemetryService => telemetryService.HotspotInvestigatedRemotely());

    private static IEnumerable<FixSuggestionResolvedParams> ConvertFixSuggestionChangeToResolvedParams(string suggestionId, IEnumerable<bool> changeApplicationStatus) =>
        changeApplicationStatus
            .Select((status, index) =>
                new FixSuggestionResolvedParams(
                    suggestionId,
                    status ? FixSuggestionStatus.ACCEPTED : FixSuggestionStatus.DECLINED,
                    index));

    public SlCoreTelemetryStatus GetStatus() => telemetryHelper.GetStatus();

    public void OptIn()
    {
        telemetryHelper.Notify(telemetryService => telemetryService.EnableTelemetry());
        try
        {
            monitoringService.Reinit();
        }
        catch (Exception e)
        {
            //Swallow errors for not supported VS versions
        }
    }

    public void OptOut()
    {
        telemetryHelper.Notify(telemetryService => telemetryService.DisableTelemetry());
        try
        {
            monitoringService.Close();
        }
        catch (Exception e)
        {
            //Swallow errors for not supported VS versions
        }
    }

    public void TaintIssueInvestigatedLocally() => telemetryHelper.Notify(telemetryService => telemetryService.TaintVulnerabilitiesInvestigatedLocally());

    public void TaintIssueInvestigatedRemotely() => telemetryHelper.Notify(telemetryService => telemetryService.TaintVulnerabilitiesInvestigatedRemotely());

    public void LinkClicked(string linkId) => telemetryHelper.Notify(telemetryService => telemetryService.HelpAndFeedbackLinkClicked(new HelpAndFeedbackClickedParams(linkId)));
}
