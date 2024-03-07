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

using System.Threading;
using System.Windows.Documents;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[TestClass]
public class RuleDescriptionConversionSmokeTest
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task CheckAllEmbeddedRules()
    {
        const string configScope = "configscope1";
        var failedRuleDescriptions = new List<string>();
        var testLogger = new TestLogger();
        using var slCoreTestRunner = new SLCoreTestRunner(testLogger, TestContext.TestName);
        slCoreTestRunner.AddListener(new LoggerListener(testLogger));
        var activeConfigScopeTracker = CreateActiveConfigScopeTracker(slCoreTestRunner);
        var slCoreRuleMetaDataProvider = CreateSlCoreRuleMetaDataProvider(slCoreTestRunner, activeConfigScopeTracker, testLogger);
        var ruleHelpXamlBuilder = CreateRuleHelpXamlBuilder();

        await slCoreTestRunner.Start();
        activeConfigScopeTracker.SetCurrentConfigScope(configScope);
        slCoreTestRunner.SlCoreServiceProvider.TryGetTransientService(out IRulesRpcService rulesRpcService).Should().BeTrue();

        var ruleDescriptions = await GetAllRuleDescriptions(await rulesRpcService.ListAllStandaloneRulesDefinitionsAsync(), slCoreRuleMetaDataProvider);
        CheckRuleDescriptionsOnSTAThread(ruleDescriptions, ruleHelpXamlBuilder, failedRuleDescriptions);

        failedRuleDescriptions.Should().BeEquivalentTo(
            new List<string>
            {
                // most of these rules fail because of unclosed paragraph
                "c:S2755", "csharpsquid:S4423", "csharpsquid:S4426", "javascript:S4830", "csharpsquid:S2699", "csharpsquid:S4830", "javascript:S4423",
                "javascript:S4426", "typescript:S6811", "typescript:S6819", "typescript:S6821", "typescript:S6827", "typescript:S6824",
                "typescript:S6822", "typescript:S6823", "typescript:S5527", "typescript:S5542", "typescript:S5547", "cpp:S1232", "csharpsquid:S2187",
                "csharpsquid:S5659", "csharpsquid:S2115", "javascript:S2755", "vbnet:S4423", "javascript:S6775", "c:S4423", "javascript:S6767",
                "javascript:S1082", "c:S4426", "javascript:S5876", "typescript:S6807", "csharpsquid:S2970", "cpp:S5542", "cpp:S5547",
                "typescript:S5876", "typescript:S6767", "typescript:S1082", "typescript:S6775", "typescript:S6793", "typescript:S6317", "vbnet:S4830",
                "csharpsquid:S2053", "javascript:S6811", "javascript:S6819", "javascript:S6807", "csharpsquid:S5542", "vbnet:S5659",
                "csharpsquid:S5547", "cpp:S2755", "csharpsquid:S3329", "javascript:S5542", "typescript:S2755", "javascript:S5547", "javascript:S5527",
                "javascript:S6822", "javascript:S6823", "javascript:S6824", "javascript:S6827", "javascript:S6821", "typescript:S4423",
                "typescript:S4426", "javascript:S6317", "javascript:S6793", "vbnet:S5547", "vbnet:S5542", "vbnet:S2053", "vbnet:S3329",
                "javascript:S5659", "typescript:S4830", "csharpsquid:S2755", "javascript:S2598", "typescript:S2598", "cpp:S4426", "typescript:S5659",
                "c:S5547", "c:S5542", "cpp:S4423"
            });
    }


    private static async Task<List<IRuleInfo>> GetAllRuleDescriptions(ListAllStandaloneRulesDefinitionsResponse ruleDefinitions,
        IRuleMetaDataProvider slCoreRuleMetaDataProvider)
    {
        var ruleDescriptions = new List<IRuleInfo>();
        foreach (var ruleKey in ruleDefinitions.rulesByKey.Keys)
        {
            var strings = ruleKey.Split(':');
            var ruleInfo = await slCoreRuleMetaDataProvider.GetRuleInfoAsync(new SonarCompositeRuleId(strings[0], strings[1]));
            ruleInfo.Should().NotBeNull();
            ruleDescriptions.Add(ruleInfo);
        }

        return ruleDescriptions;
    }


    private static void CheckRuleDescriptionsOnSTAThread(List<IRuleInfo> ruleDescriptions, RuleHelpXamlBuilder ruleHelpXamlBuilder,
        List<string> failedRuleDescriptions)
    {
        var thread = new Thread(() =>
        {
            CheckRuleDescriptions(ruleDescriptions, ruleHelpXamlBuilder, failedRuleDescriptions);
        });
        // I honestly have no idea why we need this here since the old test worked without it while having the same XAMLs being generated, but w/o this it fails
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    private static void CheckRuleDescriptions(List<IRuleInfo> ruleDescriptions, RuleHelpXamlBuilder ruleHelpXamlBuilder,
        List<string> failedRuleDescriptions)
    {
        foreach (var ruleDescription in ruleDescriptions)
        {
            try
            {
                using (new AssertIgnoreScope())
                {
                    CheckRuleDescription(ruleHelpXamlBuilder.Create(ruleDescription, null));
                }
            }
            catch (Exception)
            {
                failedRuleDescriptions.Add(ruleDescription.FullRuleKey);
            }
        }
    }

    private static void CheckRuleDescription(FlowDocument doc)
    {
        // Quick sanity check that something was produced
        // Note: this is a quick way of getting the size of the document. Serializing the doc to a string
        // and checking the length takes much longer (around 25 seconds)
        var docLength = doc.ContentStart.DocumentStart.GetOffsetToPosition(doc.ContentEnd.DocumentEnd);
        docLength.Should().BeGreaterThan(30);
    }

    private static SLCoreRuleMetaDataProvider CreateSlCoreRuleMetaDataProvider(SLCoreTestRunner slCoreTestRunner,
        IActiveConfigScopeTracker activeConfigScopeTracker, ILogger testLogger) =>
        new(slCoreTestRunner.SlCoreServiceProvider,
            activeConfigScopeTracker,
            testLogger);

    private static ActiveConfigScopeTracker CreateActiveConfigScopeTracker(SLCoreTestRunner slCoreTestRunner) =>
        new(slCoreTestRunner.SlCoreServiceProvider,
            new AsyncLockFactory(),
            new NoOpThreadHandler());

    private static RuleHelpXamlBuilder CreateRuleHelpXamlBuilder()
    {
        var xamlWriterFactory = new XamlWriterFactory();
        var xamlGeneratorHelperFactory = new XamlGeneratorHelperFactory();
        var diffTranslator = new DiffTranslator(xamlWriterFactory);
        var ruleHelpXamlTranslatorFactory = new RuleHelpXamlTranslatorFactory(xamlWriterFactory,
            diffTranslator);
        return new RuleHelpXamlBuilder(
            new SimpleRuleHelpXamlBuilder(ruleHelpXamlTranslatorFactory,
                xamlGeneratorHelperFactory,
                xamlWriterFactory),
            new RichRuleHelpXamlBuilder(new RichRuleDescriptionProvider(),
                ruleHelpXamlTranslatorFactory,
                xamlGeneratorHelperFactory,
                xamlWriterFactory));
    }
}
