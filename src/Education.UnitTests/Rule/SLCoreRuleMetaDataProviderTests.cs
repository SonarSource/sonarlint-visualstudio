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

using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;
using CleanCodeAttribute = SonarLint.VisualStudio.SLCore.Common.Models.CleanCodeAttribute;
using IssueSeverity = SonarLint.VisualStudio.SLCore.Common.Models.IssueSeverity;
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;
using SoftwareQuality = SonarLint.VisualStudio.SLCore.Common.Models.SoftwareQuality;
using RuleCleanCodeAttribute = SonarLint.VisualStudio.Core.Analysis.CleanCodeAttribute;
using RuleSoftwareQuality = SonarLint.VisualStudio.Core.Analysis.SoftwareQuality;
using RuleSoftwareQualitySeverity = SonarLint.VisualStudio.Core.Analysis.SoftwareQualitySeverity;

namespace SonarLint.VisualStudio.Education.UnitTests.Rule;

[TestClass]
public class SLCoreRuleMetaDataProviderTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SLCoreRuleMetaDataProvider, IRuleMetaDataProvider>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SLCoreRuleMetaDataProvider>();

    [DataTestMethod]
    [DataRow(IssueSeverity.INFO, RuleIssueSeverity.Info)]
    [DataRow(IssueSeverity.MAJOR, RuleIssueSeverity.Major)]
    [DataRow(IssueSeverity.BLOCKER, RuleIssueSeverity.Blocker)]
    [DataRow(IssueSeverity.CRITICAL, RuleIssueSeverity.Critical)]
    [DataRow(IssueSeverity.MINOR, RuleIssueSeverity.Minor)]
    public async Task GetRuleInfoAsync_CorrectlyConvertsSeverity(IssueSeverity slCore, RuleIssueSeverity expected)
    {
        const string rulekey = "rule:key1";
        const string configScopeId = "configscope";

        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out _);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope(configScopeId));
        SetupRulesService(rulesServiceMock, rulekey, configScopeId, new EffectiveRuleDetailsDto(
            default,
            default,
            default,
            new StandardModeDetails(slCore, default),
            default,
            default,
            default));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.Severity.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow(RuleType.CODE_SMELL, RuleIssueType.CodeSmell)]
    [DataRow(RuleType.VULNERABILITY, RuleIssueType.Vulnerability)]
    [DataRow(RuleType.BUG, RuleIssueType.Bug)]
    [DataRow(RuleType.SECURITY_HOTSPOT, RuleIssueType.Hotspot)]
    public async Task GetRuleInfoAsync_CorrectlyConvertsType(RuleType slCore, RuleIssueType expected)
    {
        const string rulekey = "rule:key1";
        const string configScopeId = "configscope";

        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out _);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope(configScopeId));
        SetupRulesService(rulesServiceMock, rulekey, configScopeId, new EffectiveRuleDetailsDto(
            default,
            default,
            default,
            new StandardModeDetails(default, slCore),
            default,
            default,
            default));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.IssueType.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow(CleanCodeAttribute.CONVENTIONAL, RuleCleanCodeAttribute.Conventional)]
    [DataRow(CleanCodeAttribute.FORMATTED, RuleCleanCodeAttribute.Formatted)]
    [DataRow(CleanCodeAttribute.IDENTIFIABLE, RuleCleanCodeAttribute.Identifiable)]
    [DataRow(CleanCodeAttribute.CLEAR, RuleCleanCodeAttribute.Clear)]
    [DataRow(CleanCodeAttribute.COMPLETE, RuleCleanCodeAttribute.Complete)]
    [DataRow(CleanCodeAttribute.EFFICIENT, RuleCleanCodeAttribute.Efficient)]
    [DataRow(CleanCodeAttribute.LOGICAL, RuleCleanCodeAttribute.Logical)]
    [DataRow(CleanCodeAttribute.DISTINCT, RuleCleanCodeAttribute.Distinct)]
    [DataRow(CleanCodeAttribute.FOCUSED, RuleCleanCodeAttribute.Focused)]
    [DataRow(CleanCodeAttribute.MODULAR, RuleCleanCodeAttribute.Modular)]
    [DataRow(CleanCodeAttribute.TESTED, RuleCleanCodeAttribute.Tested)]
    [DataRow(CleanCodeAttribute.LAWFUL, RuleCleanCodeAttribute.Lawful)]
    [DataRow(CleanCodeAttribute.RESPECTFUL, RuleCleanCodeAttribute.Respectful)]
    [DataRow(CleanCodeAttribute.TRUSTWORTHY, RuleCleanCodeAttribute.Trustworthy)]
    public async Task GetRuleInfoAsync_CorrectlyConvertsCleanCodeAttribute(CleanCodeAttribute slCore, RuleCleanCodeAttribute expected)
    {
        const string rulekey = "rule:key1";
        const string configScopeId = "configscope";

        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out _);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope(configScopeId));
        SetupRulesService(rulesServiceMock, rulekey, configScopeId, new EffectiveRuleDetailsDto(
            default,
            default,
            default,
            new MQRModeDetails(slCore, default),
            default,
            default,
            default));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.CleanCodeAttribute.Should().Be(expected);
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_CorrectlyConvertsImpacts()
    {
        const string rulekey = "rule:key1";
        const string configScopeId = "configscope";

        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out _);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope(configScopeId));
        SetupRulesService(rulesServiceMock, rulekey, configScopeId, new EffectiveRuleDetailsDto(
            default,
            default,
            default,
            new MQRModeDetails(default, [
                new ImpactDto(SoftwareQuality.SECURITY, ImpactSeverity.HIGH),
                new ImpactDto(SoftwareQuality.RELIABILITY, ImpactSeverity.LOW),
                new ImpactDto(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.MEDIUM)
            ]),
            default,
            default,
            default));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.DefaultImpacts.Should().BeEquivalentTo(new Dictionary<RuleSoftwareQuality, RuleSoftwareQualitySeverity>
        {
            { RuleSoftwareQuality.Security, RuleSoftwareQualitySeverity.High },
            { RuleSoftwareQuality.Reliability, RuleSoftwareQualitySeverity.Low },
            { RuleSoftwareQuality.Maintainability, RuleSoftwareQualitySeverity.Medium }
        });
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_Standard_SimpleRuleDescription()
    {
        const string rulekey = "rule:key1";
        const string configScopeId = "configscope";

        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out var logger);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope(configScopeId));
        SetupRulesService(rulesServiceMock, rulekey, configScopeId, new EffectiveRuleDetailsDto(
            rulekey,
            "name",
            Language.JS,
            new StandardModeDetails(IssueSeverity.CRITICAL, RuleType.VULNERABILITY),
            VulnerabilityProbability.MEDIUM,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateLeft(
                new RuleMonolithicDescriptionDto("content")),
            new List<EffectiveRuleParamDto>()));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(rulekey,
            "content",
            "name",
            RuleIssueSeverity.Critical,
            RuleIssueType.Vulnerability,
            null,
            null,
            null));
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_MQR_SimpleRuleDescription()
    {
        const string rulekey = "rule:key1";
        const string configScopeId = "configscope";

        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out var logger);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope(configScopeId));
        SetupRulesService(rulesServiceMock, rulekey, configScopeId, new EffectiveRuleDetailsDto(
            rulekey,
            "name",
            Language.JS,
            new MQRModeDetails(CleanCodeAttribute.MODULAR, [
                new ImpactDto(SoftwareQuality.SECURITY, ImpactSeverity.HIGH),
                new ImpactDto(SoftwareQuality.RELIABILITY, ImpactSeverity.LOW)
            ]),
            VulnerabilityProbability.MEDIUM,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateLeft(
                new RuleMonolithicDescriptionDto("content")),
            new List<EffectiveRuleParamDto>()));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(rulekey,
            "content",
            "name",
            null,
            null,
            null,
            RuleCleanCodeAttribute.Modular,
            new Dictionary<RuleSoftwareQuality, RuleSoftwareQualitySeverity>
            {
                { RuleSoftwareQuality.Security, RuleSoftwareQualitySeverity.High }, { RuleSoftwareQuality.Reliability, RuleSoftwareQualitySeverity.Low }
            }));
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_Standard_RichRuleDescription()
    {
        const string rulekey = "rule:key1";
        const string configScopeId = "configscope";

        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out var logger);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope(configScopeId));
        var ruleSplitDescriptionDto = new RuleSplitDescriptionDto("intro", new List<RuleDescriptionTabDto>());
        SetupRulesService(rulesServiceMock, rulekey, configScopeId, new EffectiveRuleDetailsDto(
            rulekey,
            "name",
            Language.CPP,
            new StandardModeDetails(IssueSeverity.MINOR, RuleType.BUG),
            null,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateRight(ruleSplitDescriptionDto),
            new List<EffectiveRuleParamDto> { new("ignored", default, default, default) }));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(rulekey,
            null,
            "name",
            RuleIssueSeverity.Minor,
            RuleIssueType.Bug,
            ruleSplitDescriptionDto,
            null,
            null));
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_MQR_RichRuleDescription()
    {
        const string rulekey = "rule:key1";
        const string configScopeId = "configscope";

        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out var logger);
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope(configScopeId));
        var ruleSplitDescriptionDto = new RuleSplitDescriptionDto("intro", new List<RuleDescriptionTabDto>());
        SetupRulesService(rulesServiceMock, rulekey, configScopeId, new EffectiveRuleDetailsDto(
            rulekey,
            "name",
            Language.CPP,
            new MQRModeDetails(CleanCodeAttribute.RESPECTFUL, [
                new ImpactDto(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.MEDIUM)
            ]),
            null,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateRight(ruleSplitDescriptionDto),
            new List<EffectiveRuleParamDto> { new("ignored", default, default, default) }));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(rulekey,
            null,
            "name",
            null,
            null,
            ruleSplitDescriptionDto,
            RuleCleanCodeAttribute.Respectful,
            new Dictionary<RuleSoftwareQuality, RuleSoftwareQualitySeverity> { { RuleSoftwareQuality.Maintainability, RuleSoftwareQualitySeverity.Medium } }));
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_NoActiveScope_ReturnsNull()
    {
        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out var logger);
        SetUpServiceProvider(serviceProviderMock, out _);
        SetUpConfigScopeTracker(configScopeTrackerMock, null);

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.Should().BeNull();
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_ServiceUnavailable_ReturnsNull()
    {
        var testSubject = CreateTestSubject(out _, out var configScopeTrackerMock, out var logger);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope("id"));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.Should().BeNull();
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void GetRuleInfoAsync_ServiceThrows_ReturnsNullAndLogs()
    {
        var testSubject =
            CreateTestSubject(out var serviceProviderMock, out var configScopeTrackerMock, out var logger);
        SetUpConfigScopeTracker(configScopeTrackerMock, new ConfigurationScope("id"));
        SetUpServiceProvider(serviceProviderMock, out var rulesServiceMock);
        rulesServiceMock
            .Setup(x => x.GetEffectiveRuleDetailsAsync(It.IsAny<GetEffectiveRuleDetailsParams>()))
            .ThrowsAsync(new Exception("my message"));

        var act = () => testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists("my message");
    }

    private static void SetUpConfigScopeTracker(
        Mock<IActiveConfigScopeTracker> configScopeTrackerMock,
        ConfigurationScope scope) =>
        configScopeTrackerMock.SetupGet(x => x.Current).Returns(scope);

    private static void SetupRulesService(
        Mock<IRulesSLCoreService> rulesServiceMock,
        string rulekey,
        string configScopeId,
        EffectiveRuleDetailsDto response) =>
        rulesServiceMock
            .Setup(r => r.GetEffectiveRuleDetailsAsync(It.Is<GetEffectiveRuleDetailsParams>(p =>
                p.ruleKey == rulekey && p.configurationScopeId == configScopeId)))
            .ReturnsAsync(new GetEffectiveRuleDetailsResponse(response));

    private static void SetUpServiceProvider(
        Mock<ISLCoreServiceProvider> serviceProviderMock,
        out Mock<IRulesSLCoreService> rulesServiceMock)
    {
        rulesServiceMock = new Mock<IRulesSLCoreService>();
        var rulesService = rulesServiceMock.Object;
        serviceProviderMock.Setup(x => x.TryGetTransientService(out rulesService)).Returns(true);
    }

    private static SLCoreRuleMetaDataProvider CreateTestSubject(
        out Mock<ISLCoreServiceProvider> serviceProviderMock,
        out Mock<IActiveConfigScopeTracker> configScopeTrackerMock,
        out TestLogger logger)
    {
        serviceProviderMock = new Mock<ISLCoreServiceProvider>();
        configScopeTrackerMock = new Mock<IActiveConfigScopeTracker>();
        logger = new TestLogger();
        return new SLCoreRuleMetaDataProvider(serviceProviderMock.Object, configScopeTrackerMock.Object, logger);
    }
}
