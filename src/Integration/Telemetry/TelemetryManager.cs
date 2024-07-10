﻿/*
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
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.Integration.Telemetry;

[Export(typeof(ITelemetryManager))]
[Export(typeof(IQuickFixesTelemetryManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class TelemetryManager : ITelemetryManager,
    IQuickFixesTelemetryManager,
    IDisposable
{
    private readonly ITelemetryChangeHandler telemetryChangeHandler;
    private readonly IKnownUIContexts knownUiContexts;

    [ImportingConstructor]
    public TelemetryManager(ITelemetryChangeHandler telemetryChangeHandler)
        : this(telemetryChangeHandler, new KnownUIContextsWrapper())
    {
    }

    internal /* for testing */ TelemetryManager(ITelemetryChangeHandler telemetryChangeHandler,
        IKnownUIContexts knownUIContexts)
    {
        this.telemetryChangeHandler = telemetryChangeHandler;
        knownUiContexts = knownUIContexts;
        knownUiContexts.CSharpProjectContextChanged += OnCSharpProjectContextChanged;
        knownUiContexts.VBProjectContextChanged += OnVBProjectContextChanged;
    }

    public void QuickFixApplied(string ruleId)
    {
        telemetryChangeHandler.SendTelemetry(telemetryService => telemetryService.AddQuickFixAppliedForRule(new AddQuickFixAppliedForRuleParams(ruleId)));
    }

    public bool? GetStatus()
    {
        return telemetryChangeHandler.GetStatus();
    }

    public void OptIn()
    {
        telemetryChangeHandler.SendTelemetry(telemetryService => telemetryService.EnableTelemetry());
    }
    
    public void OptOut()
    {
        telemetryChangeHandler.SendTelemetry(telemetryService => telemetryService.DisableTelemetry());
    }

    public void LanguageAnalyzed(string languageKey, int analysisTimeMs = 0)
    {
        var language = Convert(languageKey);
        telemetryChangeHandler.SendTelemetry(telemetryService =>
            telemetryService.AnalysisDoneOnSingleLanguage(new AnalysisDoneOnSingleLanguageParams(language, analysisTimeMs)));
    }

    public void TaintIssueInvestigatedLocally()
    {
        telemetryChangeHandler.SendTelemetry(telemetryService => telemetryService.TaintVulnerabilitiesInvestigatedLocally());
    }

    public void TaintIssueInvestigatedRemotely()
    {
        telemetryChangeHandler.SendTelemetry(telemetryService => telemetryService.TaintVulnerabilitiesInvestigatedRemotely());
    }
    
    public void Dispose()
    {
        knownUiContexts.CSharpProjectContextChanged -= OnCSharpProjectContextChanged;
        knownUiContexts.VBProjectContextChanged -= OnVBProjectContextChanged;
    }

    private void OnCSharpProjectContextChanged(object sender, UIContextChangedEventArgs e)
    {
        if (e.Activated)
        {
            LanguageAnalyzed(SonarLanguageKeys.CSharp);
        }
    }

    private void OnVBProjectContextChanged(object sender, UIContextChangedEventArgs e)
    {
        if (e.Activated)
        {
            LanguageAnalyzed(SonarLanguageKeys.VBNet);
        }
    }

    private static Language Convert(string languageKey)
    {
        return languageKey switch
        {
            SonarLanguageKeys.CPlusPlus => Language.CPP,
            SonarLanguageKeys.C => Language.C,
            SonarLanguageKeys.Css => Language.CSS,
            SonarLanguageKeys.JavaScript => Language.JS,
            SonarLanguageKeys.TypeScript => Language.TS,
            SonarLanguageKeys.VBNet => Language.VBNET,
            SonarLanguageKeys.CSharp => Language.CS,
            SonarLanguageKeys.Secrets => Language.SECRETS,
            _ => throw new ArgumentOutOfRangeException(nameof(languageKey), languageKey, null)
        };
    }
}
