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
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using SonarLint.VisualStudio.SLCore.Service.Telemetry.Models;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.Integration.Telemetry;

[Export(typeof(ITelemetryManager))]
[Export(typeof(IQuickFixesTelemetryManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class TelemetryManager : ITelemetryManager,
    IQuickFixesTelemetryManager,
    IDisposable
{
    private readonly IKnownUIContexts knownUiContexts;
    private readonly ISlCoreTelemetryHelper telemetryHelper;

    [ImportingConstructor]
    public TelemetryManager(ISlCoreTelemetryHelper telemetryHelper, IKnownUIContexts knownUIContexts)
    {
        this.telemetryHelper = telemetryHelper;
        knownUiContexts = knownUIContexts;
        knownUiContexts.CSharpProjectContextChanged += OnCSharpProjectContextChanged;
        knownUiContexts.VBProjectContextChanged += OnVBProjectContextChanged;
    }

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

    public void OptIn() => telemetryHelper.Notify(telemetryService => telemetryService.EnableTelemetry());

    public void OptOut() => telemetryHelper.Notify(telemetryService => telemetryService.DisableTelemetry());

    public void TaintIssueInvestigatedLocally() => telemetryHelper.Notify(telemetryService => telemetryService.TaintVulnerabilitiesInvestigatedLocally());

    public void TaintIssueInvestigatedRemotely() => telemetryHelper.Notify(telemetryService => telemetryService.TaintVulnerabilitiesInvestigatedRemotely());

    public void LinkClicked(string linkId) => telemetryHelper.Notify(telemetryService => telemetryService.HelpAndFeedbackLinkClicked(new HelpAndFeedbackClickedParams(linkId)));

    public void Dispose()
    {
        knownUiContexts.CSharpProjectContextChanged -= OnCSharpProjectContextChanged;
        knownUiContexts.VBProjectContextChanged -= OnVBProjectContextChanged;
    }

    private void OnCSharpProjectContextChanged(object sender, UIContextChangedEventArgs e)
    {
        if (e.Activated)
        {
            LanguageAnalyzed(TimeSpan.Zero, Language.CS);
        }
    }

    private void OnVBProjectContextChanged(object sender, UIContextChangedEventArgs e)
    {
        if (e.Activated)
        {
            LanguageAnalyzed(TimeSpan.Zero, Language.VBNET);
        }
    }

    private void LanguageAnalyzed(TimeSpan analysisTime, Language language) =>
        telemetryHelper.Notify(telemetryService =>
            telemetryService.AnalysisDoneOnSingleLanguage(new AnalysisDoneOnSingleLanguageParams(language, (int)Math.Round(analysisTime.TotalMilliseconds))));
}
