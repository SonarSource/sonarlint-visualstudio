// /*
//  * SonarLint for Visual Studio
//  * Copyright (C) 2016-2024 SonarSource SA
//  * mailto:info AT sonarsource DOT com
//  *
//  * This program is free software; you can redistribute it and/or
//  * modify it under the terms of the GNU Lesser General Public
//  * License as published by the Free Software Foundation; either
//  * version 3 of the License, or (at your option) any later version.
//  *
//  * This program is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  * Lesser General Public License for more details.
//  *
//  * You should have received a copy of the GNU Lesser General Public License
//  * along with this program; if not, write to the Free Software Foundation,
//  * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//  */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
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

namespace SonarLint.VisualStudio.Education.UnitTests.Rule;

[TestClass]
public class SLCoreRuleMetaDataProviderTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SLCoreRuleMetaDataProvider, IRuleMetaDataProvider>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SLCoreRuleMetaDataProvider>();
    }


    [TestMethod]
    public async Task GetRuleInfoAsync_SimpleRuleDescription()
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
            IssueSeverity.CRITICAL,
            RuleType.SECURITY_HOTSPOT,
            CleanCodeAttribute.MODULAR,
            CleanCodeAttributeCategory.INTENTIONAL,
            new List<ImpactDto>
            {
                new(SoftwareQuality.SECURITY, ImpactSeverity.HIGH),
                new(SoftwareQuality.RELIABILITY, ImpactSeverity.LOW)
            },
            Language.JS,
            VulnerabilityProbability.MEDIUM,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateLeft(
                new RuleMonolithicDescriptionDto("content")),
            new List<EffectiveRuleParamDto>()));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(rulekey, 
            "content",
            "name", 
            RuleIssueSeverity.Critical,
            RuleIssueType.Hotspot, 
            null,
            Core.Analysis.CleanCodeAttribute.Modular,
            new Dictionary<Core.Analysis.SoftwareQuality, SoftwareQualitySeverity>
            {
                { Core.Analysis.SoftwareQuality.Security, SoftwareQualitySeverity.High },
                { Core.Analysis.SoftwareQuality.Reliability, SoftwareQualitySeverity.Low }
            }));
    }

    [TestMethod]
    public async Task GetRuleInfoAsync_RichRuleDescription()
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
            IssueSeverity.MINOR,
            RuleType.BUG,
            CleanCodeAttribute.RESPECTFUL,
            CleanCodeAttributeCategory.ADAPTABLE,
            new List<ImpactDto>
            {
                new(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.MEDIUM)
            },
            Language.CPP,
            null,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateRight(ruleSplitDescriptionDto),
            new List<EffectiveRuleParamDto>
            {
                new("ignored", default, default, default)
            }));

        var ruleInfo = await testSubject.GetRuleInfoAsync(new SonarCompositeRuleId("rule", "key1"));

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(rulekey, 
            null, 
            "name", 
            RuleIssueSeverity.Minor, 
            RuleIssueType.Bug,
            ruleSplitDescriptionDto,
            Core.Analysis.CleanCodeAttribute.Respectful,
            new Dictionary<Core.Analysis.SoftwareQuality, SoftwareQualitySeverity>
            {
                { Core.Analysis.SoftwareQuality.Maintainability, SoftwareQualitySeverity.Medium }
            }));
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
    public async Task GetRuleInfoAsync_ServiceThrows_ReturnsNullAndLogs()
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

    private static void SetUpConfigScopeTracker(Mock<IActiveConfigScopeTracker> configScopeTrackerMock,
        ConfigurationScope scope)
    {
        configScopeTrackerMock.SetupGet(x => x.Current).Returns(scope);
    }

    private static void SetupRulesService(Mock<IRulesRpcService> rulesServiceMock, string rulekey, string configScopeId,
        EffectiveRuleDetailsDto response)
    {
        rulesServiceMock
            .Setup(r => r.GetEffectiveRuleDetailsAsync(It.Is<GetEffectiveRuleDetailsParams>(p =>
                p.ruleKey == rulekey && p.configurationScopeId == configScopeId)))
            .ReturnsAsync(new GetEffectiveRuleDetailsResponse(response));
    }

    private static void SetUpServiceProvider(Mock<ISLCoreServiceProvider> serviceProviderMock,
        out Mock<IRulesRpcService> rulesServiceMock)
    {
        rulesServiceMock = new Mock<IRulesRpcService>();
        var rulesService = rulesServiceMock.Object;
        serviceProviderMock.Setup(x => x.TryGetTransientService(out rulesService)).Returns(true);
    }

    private static SLCoreRuleMetaDataProvider CreateTestSubject(out Mock<ISLCoreServiceProvider> serviceProviderMock,
        out Mock<IActiveConfigScopeTracker> configScopeTrackerMock,
        out TestLogger logger)
    {
        serviceProviderMock = new Mock<ISLCoreServiceProvider>();
        configScopeTrackerMock = new Mock<IActiveConfigScopeTracker>();
        logger = new TestLogger();
        return new SLCoreRuleMetaDataProvider(serviceProviderMock.Object, configScopeTrackerMock.Object, logger);
    }
}
