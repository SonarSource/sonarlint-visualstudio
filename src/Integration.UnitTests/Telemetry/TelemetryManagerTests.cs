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

using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using SonarLint.VisualStudio.TestInfrastructure;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.Integration.Tests;

[TestClass]
public class TelemetryManagerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<TelemetryManager, ITelemetryManager>(
            MefTestHelpers.CreateExport<ITelemetryChangeHandler>(),
            MefTestHelpers.CreateExport<IKnownUIContexts>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<TelemetryManager>();
    }
    
    [DataTestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(false)]
    public void GetStatus_CallsRpcService(bool? status)
    {
        CreteTelemetryService(out var telemetryHandler, out _);
        telemetryHandler.GetStatus().Returns(status);
        var telemetryManager = CreateTestSubject(telemetryHandler);
        
        telemetryManager.GetStatus().Should().Be(status);
    }
    
    [TestMethod]
    public void OptOut_CallsRpcService()
    {
        CreteTelemetryService(out var telemetryHandler, out var telemetryService);
        var telemetryManager = CreateTestSubject(telemetryHandler);
        
        telemetryManager.OptOut();
        
        Received.InOrder(() =>
        {
            telemetryHandler.SendTelemetry(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.DisableTelemetry();
        });
    }
    
    [TestMethod]
    public void OptIn_CallsRpcService()
    {
        CreteTelemetryService(out var telemetryHandler, out var telemetryService);
        var telemetryManager = CreateTestSubject(telemetryHandler);
        
        telemetryManager.OptIn();
        
        Received.InOrder(() =>
        {
            telemetryHandler.SendTelemetry(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.EnableTelemetry();
        });
    }
    
    [TestMethod]
    public void TaintIssueInvestigatedLocally_CallsRpcService()
    {
        CreteTelemetryService(out var telemetryHandler, out var telemetryService);
        var telemetryManager = CreateTestSubject(telemetryHandler);
        
        telemetryManager.TaintIssueInvestigatedLocally();
        
        Received.InOrder(() =>
        {
            telemetryHandler.SendTelemetry(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.TaintVulnerabilitiesInvestigatedLocally();
        });
    }
    
    [TestMethod]
    public void TaintVulnerabilitiesInvestigatedRemotely_CallsRpcService()
    {
        CreteTelemetryService(out var telemetryHandler, out var telemetryService);
        var telemetryManager = CreateTestSubject(telemetryHandler);
        
        telemetryManager.TaintIssueInvestigatedRemotely();
        
        Received.InOrder(() =>
        {
            telemetryHandler.SendTelemetry(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.TaintVulnerabilitiesInvestigatedRemotely();
        });
    }
    
    [TestMethod]
    public void QuickFixApplied_CallsRpcService()
    {
        CreteTelemetryService(out var telemetryHandler, out var telemetryService);
        var telemetryManager = CreateTestSubject(telemetryHandler);
        
        telemetryManager.QuickFixApplied("myrule");
        
        Received.InOrder(() =>
        {
            telemetryHandler.SendTelemetry(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.AddQuickFixAppliedForRule(Arg.Is<AddQuickFixAppliedForRuleParams>(a => a.ruleKey == "myrule"));
        });
    }
    
    [DataTestMethod]
    [DataRow(SonarLanguageKeys.C, Language.C, 1)]
    [DataRow(SonarLanguageKeys.CPlusPlus, Language.CPP, 2)]
    [DataRow(SonarLanguageKeys.CSharp, Language.CS, 3)]
    [DataRow(SonarLanguageKeys.VBNet, Language.VBNET, 4)]
    [DataRow(SonarLanguageKeys.JavaScript, Language.JS, 5)]
    [DataRow(SonarLanguageKeys.TypeScript, Language.TS, 6)]
    [DataRow(SonarLanguageKeys.Css, Language.CSS, 7)]
    [DataRow(SonarLanguageKeys.Secrets, Language.SECRETS, 8)]
    public void LanguageAnalyzed_CallsRpcService(string languageKey, Language language, int analysisTimeMs)
    {
        CreteTelemetryService(out var telemetryHandler, out var telemetryService);
        var telemetryManager = CreateTestSubject(telemetryHandler);
        
        telemetryManager.LanguageAnalyzed(languageKey, analysisTimeMs);
        
        Received.InOrder(() =>
        {
            telemetryHandler.SendTelemetry(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.AnalysisDoneOnSingleLanguage(Arg.Is<AnalysisDoneOnSingleLanguageParams>(a => a.language == language && a.analysisTimeMs == analysisTimeMs));
        });
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CSharpUIContext_SendsCSharpAnalysisUpdate(bool activated)
    {
        CreteTelemetryService(out var telemetryHandler, out var telemetryService);
        var knownUiContexts = Substitute.For<IKnownUIContexts>();
        var telemetryManager = CreateTestSubject(telemetryHandler, knownUiContexts);

        knownUiContexts.CSharpProjectContextChanged += Raise.EventWith(new UIContextChangedEventArgs(activated));
        
        telemetryService.Received(activated ? 1 : 0).AnalysisDoneOnSingleLanguage(Arg.Is<AnalysisDoneOnSingleLanguageParams>(a => a.language == Language.CS));
    }
    
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void VBNetUIContext_SendsVBNetAnalysisUpdate(bool activated)
    {
        CreteTelemetryService(out var telemetryHandler, out var telemetryService);
        var knownUiContexts = Substitute.For<IKnownUIContexts>();
        var telemetryManager = CreateTestSubject(telemetryHandler, knownUiContexts);

        knownUiContexts.VBProjectContextChanged += Raise.EventWith(new UIContextChangedEventArgs(activated));
        
        telemetryService.Received(activated ? 1 : 0).AnalysisDoneOnSingleLanguage(Arg.Is<AnalysisDoneOnSingleLanguageParams>(a => a.language == Language.VBNET));
    }

    private static void CreteTelemetryService(out ITelemetryChangeHandler telemetryHandler, out ITelemetrySLCoreService telemetryService)
    {
        telemetryHandler = Substitute.For<ITelemetryChangeHandler>();
        var telemetryServiceMock = Substitute.For<ITelemetrySLCoreService>();
        telemetryService = telemetryServiceMock;
        telemetryHandler
            .When(x => x.SendTelemetry(Arg.Any<Action<ITelemetrySLCoreService>>()))
            .Do(callInfo => callInfo.Arg<Action<ITelemetrySLCoreService>>()(telemetryServiceMock));
    }

    private static TelemetryManager CreateTestSubject(ITelemetryChangeHandler telemetryHandler, IKnownUIContexts uiContexts = null)
    {
        uiContexts ??= Substitute.For<IKnownUIContexts>();
        return new TelemetryManager(telemetryHandler, uiContexts);
    }
}
