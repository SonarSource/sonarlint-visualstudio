﻿/*
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

using System.Collections.Immutable;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Rules;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Analysis;

[TestClass]
public class SLCoreRuleSettingsTests
{
    private const string RuleId = "dummyRule";
    private const string RuleId2 = "dummyRule2";
    private AnalysisSettings analysisSettings = new();
    private SlCoreRuleSettings slCoreRuleSettings;
    private IUserSettingsProvider userSettingsProvider;
    private ILogger logger;
    private ISLCoreServiceProvider sLCoreServiceProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        sLCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        userSettingsProvider = Substitute.For<IUserSettingsProvider>();
        slCoreRuleSettings = new SlCoreRuleSettings(logger, userSettingsProvider, sLCoreServiceProvider);
    }

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<SlCoreRuleSettings, ISLCoreRuleSettingsUpdater>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IUserSettingsProvider>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SlCoreRuleSettings>();
    }

    [TestMethod]
    public void GetSLCoreRuleSettings_RuleSettingsHaveOneRule_ShouldCreateDictionaryWithOneRule()
    {
        AddRule(RuleId);

        var slCoreSettings = slCoreRuleSettings.GetSLCoreRuleSettings();

        slCoreSettings.Should().NotBeNull();
        slCoreSettings.Keys.Count.Should().Be(1);
        slCoreSettings.Keys.First().Should().Be(RuleId);
    }

    [TestMethod]
    public void GetSLCoreRuleSettings_RuleSettingsHaveTwoRules_ShouldCreateDictionaryWithTwoRules()
    {
        AddRule(RuleId);
        AddRule(RuleId2);

        var slCoreSettings = slCoreRuleSettings.GetSLCoreRuleSettings();

        slCoreSettings.Should().NotBeNull();
        slCoreSettings.Keys.Count.Should().Be(2);
        slCoreSettings[RuleId].Should().NotBeNull();
        slCoreSettings[RuleId2].Should().NotBeNull();
    }

    [TestMethod]
    [DataRow(RuleLevel.Off, false)]
    [DataRow(RuleLevel.On, true)]
    public void GetSLCoreRuleSettings_ShouldSetCorrectlySqlCoreIsActiveValue(RuleLevel ruleLevel, bool expectedIsActive)
    {
        AddRule(RuleId, ruleLevel);

        var slCoreSettings = slCoreRuleSettings.GetSLCoreRuleSettings();

        var ruleConfigDto = slCoreSettings.Values.First();
        ruleConfigDto.isActive.Should().Be(expectedIsActive);
    }

    [TestMethod]
    public void GetSLCoreRuleSettings_RuleSettingHaveOneRuleWithParametersSetToNull_ShouldInitializeSqlCoreParametersToEmpty()
    {
        AddRule(RuleId, ruleParameters: null);

        var slCoreSettings = slCoreRuleSettings.GetSLCoreRuleSettings();

        slCoreSettings.Values.Count.Should().Be(1);
        var ruleConfigDto = slCoreSettings.Values.First();
        ruleConfigDto.paramValueByKey.Should().BeEmpty();
    }

    [TestMethod]
    public void GetSLCoreRuleSettings_RuleSettingHaveOneRuleWithOneParameter_ShouldCreateSqlCoreParameterWithSameValues()
    {
        var parameters = new Dictionary<string, string> { { "threshold", "15" } };
        AddRule(RuleId, RuleLevel.On, parameters);

        var slCoreSettings = slCoreRuleSettings.GetSLCoreRuleSettings();

        slCoreSettings.Values.Count.Should().Be(1);
        var ruleConfigDto = slCoreSettings.Values.First();
        ruleConfigDto.paramValueByKey.Should().Equal(parameters);
    }

    [TestMethod]
    public void UpdateStandaloneRulesConfiguration_GettingRulesSLCoreServiceSucceeds_CallsUpdateStandaloneRulesConfigurationWithRuleSettings()
    {
        MockUserSettings();
        var rulesSlCoreService = MockGetRulesSlCoreService();

        slCoreRuleSettings.UpdateStandaloneRulesConfiguration();

        rulesSlCoreService.Received(1)
            .UpdateStandaloneRulesConfiguration(Arg.Is<UpdateStandaloneRulesConfigurationParams>(param => param.ruleConfigByKey.SequenceEqual(slCoreRuleSettings.GetSLCoreRuleSettings())));
    }

    [TestMethod]
    public void UpdateStandaloneRulesConfiguration_GettingRulesSLCoreServiceServiceFails_WritesALog()
    {
        sLCoreServiceProvider.TryGetTransientService(out Arg.Any<IRulesSLCoreService>()).Returns(_ => false);

        slCoreRuleSettings.UpdateStandaloneRulesConfiguration();

        logger.Received(1).WriteLine(Arg.Is<string>(msg => msg.Contains(nameof(SlCoreRuleSettings)) && msg.Contains(SLCoreStrings.ServiceProviderNotInitialized)));
    }

    [TestMethod]
    public void UpdateStandaloneRulesConfiguration_UpdatingStandaloneRulesConfigurationInSlCoreFails_WritesLog()
    {
        MockUserSettings();
        var rulesSlCoreService = MockGetRulesSlCoreService();
        rulesSlCoreService.When(x => x.UpdateStandaloneRulesConfiguration(Arg.Any<UpdateStandaloneRulesConfigurationParams>()))
            .Do(x => throw new Exception("update failed"));

        slCoreRuleSettings.UpdateStandaloneRulesConfiguration();

        logger.Received(1).WriteLine(Arg.Is<string>(msg => msg.Contains("update failed")));
    }

    private void AddRule(string ruleId, RuleLevel ruleLevel = RuleLevel.On, Dictionary<string, string> ruleParameters = null)
    {
        var newRules = analysisSettings.Rules.Add(ruleId, new RuleConfig(ruleLevel, ruleParameters));
        analysisSettings = new AnalysisSettings(newRules, ImmutableArray<string>.Empty);

        MockUserSettings();
    }

    private void MockUserSettings()
    {
        userSettingsProvider.UserSettings.Returns(new UserSettings(analysisSettings, "any"));
    }

    private IRulesSLCoreService MockGetRulesSlCoreService()
    {
        var rulesSlCoreService = Substitute.For<IRulesSLCoreService>();
        sLCoreServiceProvider.TryGetTransientService(out Arg.Any<IRulesSLCoreService>()).Returns(callInfo =>
        {
            callInfo[0] = rulesSlCoreService;
            return true;
        });

        return rulesSlCoreService;
    }
}
