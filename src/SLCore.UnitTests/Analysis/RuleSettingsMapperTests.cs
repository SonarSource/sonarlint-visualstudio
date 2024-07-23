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

using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore.Analysis;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Analysis;

[TestClass]
public class RuleSettingsMapperTests
{
    private const string RuleId = "dummyRule";
    private const string RuleId2 = "dummyRule2";
    private readonly RulesSettings rulesSettings = new();
    private SlCoreIslCoreRuleSettings islCoreRuleSettingsMapper;

    [TestInitialize]
    public void TestInitialize()
    {
        islCoreRuleSettingsMapper = new SlCoreIslCoreRuleSettings();
    }

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<SlCoreIslCoreRuleSettings, ISLCoreRuleSettings>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SlCoreIslCoreRuleSettings>();
    }

    [TestMethod]
    public void MapRuleSettingsToSlCoreSettings_RuleSettingsHaveOneRule_ShouldCreateDictionaryWithOneRule()
    {
        AddRule(RuleId);

        var slCoreSettings = islCoreRuleSettingsMapper.MapRuleSettingsToSlCoreSettings(rulesSettings);

        slCoreSettings.Should().NotBeNull();
        slCoreSettings.Keys.Count.Should().Be(1);
        slCoreSettings.Keys.First().Should().Be(RuleId);
    }

    [TestMethod]
    public void MapRuleSettingsToSlCoreSettings_RuleSettingsHaveTwoRules_ShouldCreateDictionaryWithTwoRules()
    {
        AddRule(RuleId);
        AddRule(RuleId2);

        var slCoreSettings = islCoreRuleSettingsMapper.MapRuleSettingsToSlCoreSettings(rulesSettings);

        slCoreSettings.Should().NotBeNull();
        slCoreSettings.Keys.Count.Should().Be(2);
        slCoreSettings.Keys.First().Should().Be(RuleId);
        slCoreSettings.Keys.Last().Should().Be(RuleId2);
    }

    [TestMethod]
    [DataRow(RuleLevel.Off, false)]
    [DataRow(RuleLevel.On, true)]
    public void MapRuleSettingsToSlCoreSettings_ShouldSetCorrectlySqlCoreIsActiveValue(RuleLevel ruleLevel, bool expectedIsActive)
    {
        AddRule(RuleId, ruleLevel);

        var slCoreSettings = islCoreRuleSettingsMapper.MapRuleSettingsToSlCoreSettings(rulesSettings);

        var ruleConfigDto = slCoreSettings.Values.First();
        ruleConfigDto.isActive.Should().Be(expectedIsActive);
    }

    [TestMethod]
    public void MapRuleSettingsToSlCoreSettings_RuleSettingHaveOneRuleWithParametersSetToNull_ShouldInitializeSqlCoreParametersToEmpty()
    {
        AddRule(RuleId, ruleParameters:null);

        var slCoreSettings = islCoreRuleSettingsMapper.MapRuleSettingsToSlCoreSettings(rulesSettings);

        slCoreSettings.Values.Count.Should().Be(1);
        var ruleConfigDto = slCoreSettings.Values.First();
        ruleConfigDto.paramValueByKey.Should().BeEmpty();
    }

    [TestMethod]
    public void MapRuleSettingsToSlCoreSettings_RuleSettingHaveOneRuleWithOneParameter_ShouldCreateSqlCoreParameterWithSameValues()
    {
        var parameters = new Dictionary<string, string> { { "threshold", "15" } };
        AddRule(RuleId, RuleLevel.On, parameters);

        var slCoreSettings = islCoreRuleSettingsMapper.MapRuleSettingsToSlCoreSettings(rulesSettings);

        slCoreSettings.Values.Count.Should().Be(1);
        var ruleConfigDto = slCoreSettings.Values.First();
        ruleConfigDto.paramValueByKey.Should().Equal(parameters);
    }

    private void AddRule(string ruleId, RuleLevel ruleLevel = RuleLevel.On, Dictionary<string, string> ruleParameters = null)
    {
        rulesSettings.Rules.Add(ruleId, new RuleConfig { Level = ruleLevel, Parameters = ruleParameters});
    }
}
