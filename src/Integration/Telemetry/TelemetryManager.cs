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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.Integration;

[Export(typeof(ITelemetryManager))]
[Export(typeof(IQuickFixesTelemetryManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
public sealed class TelemetryManager : ITelemetryManager, 
    IQuickFixesTelemetryManager,
    IDisposable
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly ILogger logger;
    private readonly IKnownUIContexts knownUiContexts;
    private readonly IThreadHandling threadHandling;

    private static readonly object Lock = new object();

    [ImportingConstructor]
    public TelemetryManager(
        ISLCoreServiceProvider serviceProvider,
        IUserSettingsProvider userSettingsProvider,
        ILogger logger, IThreadHandling threadHandling)
        : this(serviceProvider, userSettingsProvider, logger,
            new KnownUIContextsWrapper(), threadHandling)
    {
    }

    public TelemetryManager(ISLCoreServiceProvider serviceProvider,
        IUserSettingsProvider userSettingsProvider,
        ILogger logger,
        IKnownUIContexts knownUIContexts,
        IThreadHandling threadHandling)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.knownUiContexts = knownUIContexts;
        this.threadHandling = threadHandling;
    }

    public void Dispose()
    {
        DisableAllEvents();
    }

    public void OptIn()
    {
        EnableAllEvents();
        SendTelemetry(telemetryService =>
        {
            telemetryService.EnableTelemetry();
        });
    }

    public bool? GetStatus()
    {
        return threadHandling.Run(async () =>
        {
            bool? result = null;
            await threadHandling.SwitchToBackgroundThread();
            if (serviceProvider.TryGetTransientService(out ITelemetrySLCoreService telemetryService))
            {
                result = (await telemetryService.GetStatusAsync()).enabled;
            }

            return result;
        });
    }

    public void OptOut()
    {
        DisableAllEvents();
        SendTelemetry(telemetryService =>
        {
            telemetryService.DisableTelemetry();
        });
    }

    private void DisableAllEvents()
    {
        knownUiContexts.CSharpProjectContextChanged -= OnCSharpProjectContextChanged;
        knownUiContexts.VBProjectContextChanged -= OnVBProjectContextChanged;
    }

    private void EnableAllEvents()
    {
        knownUiContexts.CSharpProjectContextChanged += OnCSharpProjectContextChanged;
        knownUiContexts.VBProjectContextChanged += OnVBProjectContextChanged;
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

    public void LanguageAnalyzed(string languageKey, int analysisTimeMs = 0)
    {
        var language = Convert(languageKey);
        SendTelemetry(telemetryService => telemetryService.AnalysisDoneOnSingleLanguage(new AnalysisDoneOnSingleLanguageParams(language, analysisTimeMs)));
    }
    
    private static Language Convert(string languageKey) =>
        languageKey switch
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
        
    public void TaintIssueInvestigatedLocally()
    {
        SendTelemetry(telemetryService => telemetryService.TaintVulnerabilitiesInvestigatedLocally());
    }

    public void TaintIssueInvestigatedRemotely()
    {
        SendTelemetry(telemetryService => telemetryService.TaintVulnerabilitiesInvestigatedRemotely());
    }

    public void QuickFixApplied(string ruleId)
    {
        SendTelemetry(telemetryService => telemetryService.AddQuickFixAppliedForRule(new AddQuickFixAppliedForRuleParams(ruleId)));
    }
    
    private void SendTelemetry(Action<ITelemetrySLCoreService> telemetryProducer)
    {
        threadHandling
            .RunOnBackgroundThread(() =>
            {
                if (!serviceProvider.TryGetTransientService(out ITelemetrySLCoreService telemetryService))
                {
                    // todo logger.
                    return;
                }

                telemetryProducer(telemetryService);
            })
            .Forget();
    }
}
