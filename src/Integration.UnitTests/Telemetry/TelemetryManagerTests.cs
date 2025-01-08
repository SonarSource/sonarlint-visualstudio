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

using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;
using SonarLint.VisualStudio.TestInfrastructure;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests.Telemetry;

[TestClass]
public class TelemetryManagerTests
{
    private TelemetryManager telemetryManager;
    private ISlCoreTelemetryHelper telemetryHandler;
    private ITelemetrySLCoreService telemetryService;
    private IKnownUIContexts knownUiContexts;

    [TestInitialize]
    public void TestInitialize()
    {
        telemetryHandler = Substitute.For<ISlCoreTelemetryHelper>();
        MockTelemetryService();
        knownUiContexts = Substitute.For<IKnownUIContexts>();

        telemetryManager = new TelemetryManager(telemetryHandler, knownUiContexts);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<TelemetryManager, ITelemetryManager>(
            MefTestHelpers.CreateExport<ISlCoreTelemetryHelper>(),
            MefTestHelpers.CreateExport<IKnownUIContexts>());

        MefTestHelpers.CheckTypeCanBeImported<TelemetryManager, IQuickFixesTelemetryManager>(
            MefTestHelpers.CreateExport<ISlCoreTelemetryHelper>(),
            MefTestHelpers.CreateExport<IKnownUIContexts>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<TelemetryManager>();
    }

    [DataTestMethod]
    [DataRow(SlCoreTelemetryStatus.Unavailable)]
    [DataRow(SlCoreTelemetryStatus.Disabled)]
    [DataRow(SlCoreTelemetryStatus.Enabled)]
    public void GetStatus_CallsRpcService(SlCoreTelemetryStatus status)
    {
        telemetryHandler.GetStatus().Returns(status);
        
        telemetryManager.GetStatus().Should().Be(status);
    }

    [TestMethod]
    public void OptOut_CallsRpcService()
    {
        telemetryManager.OptOut();

        Received.InOrder(() =>
        {
            telemetryHandler.Notify(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.DisableTelemetry();
        });
    }

    [TestMethod]
    public void OptIn_CallsRpcService()
    {
        telemetryManager.OptIn();

        Received.InOrder(() =>
        {
            telemetryHandler.Notify(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.EnableTelemetry();
        });
    }

    [TestMethod]
    public void TaintIssueInvestigatedLocally_CallsRpcService()
    {
        telemetryManager.TaintIssueInvestigatedLocally();

        Received.InOrder(() =>
        {
            telemetryHandler.Notify(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.TaintVulnerabilitiesInvestigatedLocally();
        });
    }

    [TestMethod]
    public void TaintVulnerabilitiesInvestigatedRemotely_CallsRpcService()
    {
        telemetryManager.TaintIssueInvestigatedRemotely();

        Received.InOrder(() =>
        {
            telemetryHandler.Notify(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.TaintVulnerabilitiesInvestigatedRemotely();
        });
    }

    [TestMethod]
    public void QuickFixApplied_CallsRpcService()
    {
        telemetryManager.QuickFixApplied("myrule");

        Received.InOrder(() =>
        {
            telemetryHandler.Notify(Arg.Any<Action<ITelemetrySLCoreService>>());
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
        telemetryManager.LanguageAnalyzed(languageKey, TimeSpan.FromMilliseconds(analysisTimeMs));

        Received.InOrder(() =>
        {
            telemetryHandler.Notify(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.AnalysisDoneOnSingleLanguage(Arg.Is<AnalysisDoneOnSingleLanguageParams>(a => a.language == language && a.analysisTimeMs == analysisTimeMs));
        });
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CSharpUIContext_SendsCSharpAnalysisUpdate(bool activated)
    {
        knownUiContexts.CSharpProjectContextChanged += Raise.EventWith(new UIContextChangedEventArgs(activated));

        telemetryService.Received(activated ? 1 : 0).AnalysisDoneOnSingleLanguage(Arg.Is<AnalysisDoneOnSingleLanguageParams>(a => a.language == Language.CS));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void VBNetUIContext_SendsVBNetAnalysisUpdate(bool activated)
    {
        knownUiContexts.VBProjectContextChanged += Raise.EventWith(new UIContextChangedEventArgs(activated));

        telemetryService.Received(activated ? 1 : 0).AnalysisDoneOnSingleLanguage(Arg.Is<AnalysisDoneOnSingleLanguageParams>(a => a.language == Language.VBNET));
    }

    [TestMethod]
    public void LinkClicked_CallsRpcService()
    {
        var linkId = "anId";

        telemetryManager.LinkClicked(linkId);

        Received.InOrder(() =>
        {
            telemetryHandler.Notify(Arg.Any<Action<ITelemetrySLCoreService>>());
            telemetryService.HelpAndFeedbackLinkClicked(Arg.Is<HelpAndFeedbackClickedParams>(a => a.itemId == linkId));
        });
    }

    private void MockTelemetryService()
    {
        telemetryService = Substitute.For<ITelemetrySLCoreService>();
        telemetryHandler
            .When(x => x.Notify(Arg.Any<Action<ITelemetrySLCoreService>>()))
            .Do(callInfo => callInfo.Arg<Action<ITelemetrySLCoreService>>()(telemetryService));
    }
}
