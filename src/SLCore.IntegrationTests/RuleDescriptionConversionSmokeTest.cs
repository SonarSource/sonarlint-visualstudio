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
[Ignore] // https://sonarsource.atlassian.net/browse/SLVS-1428
public class RuleDescriptionConversionSmokeTest
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task CheckAllEmbeddedRules()
    {
        const string configScope = "configscope1";
        var failedRuleDescriptions = new List<string>();
        var testLogger = new TestLogger();
        var slCoreErrorLogger = new TestLogger();
        using var slCoreTestRunner = new SLCoreTestRunner(testLogger, slCoreErrorLogger, TestContext.TestName);
        slCoreTestRunner.AddListener(new LoggerListener(testLogger));
        slCoreTestRunner.Start();

        var ruleHelpXamlBuilder = CreateRuleHelpXamlBuilder();
        var activeConfigScopeTracker = CreateActiveConfigScopeTracker(slCoreTestRunner);
        var slCoreRuleMetaDataProvider = CreateSlCoreRuleMetaDataProvider(slCoreTestRunner, activeConfigScopeTracker, testLogger);
        activeConfigScopeTracker.SetCurrentConfigScope(configScope);
        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out IRulesSLCoreService rulesSlCoreService).Should().BeTrue();

        // no hotspots are returned from ListAllStandaloneRulesDefinitionsAsync
        var ruleDescriptions = await GetAllRuleDescriptions(await rulesSlCoreService.ListAllStandaloneRulesDefinitionsAsync(), slCoreRuleMetaDataProvider);
        CheckRuleDescriptionsOnSTAThread(ruleDescriptions, ruleHelpXamlBuilder, failedRuleDescriptions);

        failedRuleDescriptions.Should().BeEquivalentTo(
            new List<string>
            {
                "cpp:S1232", // unsupported <caption> tag https://github.com/SonarSource/sonarlint-visualstudio/issues/5014
                "csharpsquid:S6932", // unsupported <dl> and <dt> tag https://github.com/SonarSource/sonarlint-visualstudio/issues/5414
                "csharpsquid:S6966", // unsupported <dl> and <dt> tag https://github.com/SonarSource/sonarlint-visualstudio/issues/5414
                "typescript:S6811", // unsupported <caption> tag https://github.com/SonarSource/sonarlint-visualstudio/issues/5014
                "javascript:S6811" // unsupported <caption> tag https://github.com/SonarSource/sonarlint-visualstudio/issues/5014
            });
    }


    private static async Task<List<IRuleInfo>> GetAllRuleDescriptions(ListAllStandaloneRulesDefinitionsResponse ruleDefinitions,
        IRuleMetaDataProvider slCoreRuleMetaDataProvider)
    {
        ruleDefinitions.rulesByKey.Count.Should().BeGreaterThan(1500);
        
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
        new(slCoreTestRunner.SLCoreServiceProvider,
            activeConfigScopeTracker,
            testLogger);

    private static ActiveConfigScopeTracker CreateActiveConfigScopeTracker(SLCoreTestRunner slCoreTestRunner) =>
        new(slCoreTestRunner.SLCoreServiceProvider,
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
