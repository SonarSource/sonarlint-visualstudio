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

using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
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
public class RuleInfoConverterTests
{
    private RuleInfoConverter testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new RuleInfoConverter();

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<RuleInfoConverter, IRuleInfoConverter>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RuleInfoConverter>();

    [DataTestMethod]
    [DataRow(IssueSeverity.INFO, RuleIssueSeverity.Info)]
    [DataRow(IssueSeverity.MAJOR, RuleIssueSeverity.Major)]
    [DataRow(IssueSeverity.BLOCKER, RuleIssueSeverity.Blocker)]
    [DataRow(IssueSeverity.CRITICAL, RuleIssueSeverity.Critical)]
    [DataRow(IssueSeverity.MINOR, RuleIssueSeverity.Minor)]
    public void Convert_RuleDetails_CorrectlyConvertsSeverity(IssueSeverity slCore, RuleIssueSeverity expected)
    {
        var ruleDetails = new EffectiveRuleDetailsDto(
            default,
            default,
            default,
            new StandardModeDetails(slCore, default),
            default,
            default,
            default);

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.Severity.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow(RuleType.CODE_SMELL, RuleIssueType.CodeSmell)]
    [DataRow(RuleType.VULNERABILITY, RuleIssueType.Vulnerability)]
    [DataRow(RuleType.BUG, RuleIssueType.Bug)]
    [DataRow(RuleType.SECURITY_HOTSPOT, RuleIssueType.Hotspot)]
    public void Convert_RuleDetails_CorrectlyConvertsType(RuleType slCore, RuleIssueType expected)
    {
        var ruleDetails = new EffectiveRuleDetailsDto(
            default,
            default,
            default,
            new StandardModeDetails(default, slCore),
            default,
            default,
            default);

        var ruleInfo = testSubject.Convert(ruleDetails);

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
    public void Convert_RuleDetails_CorrectlyConvertsCleanCodeAttribute(CleanCodeAttribute slCore, RuleCleanCodeAttribute expected)
    {
        var ruleDetails = new EffectiveRuleDetailsDto(
            default,
            default,
            default,
            new MQRModeDetails(slCore, default),
            default,
            default,
            default);

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.CleanCodeAttribute.Should().Be(expected);
    }

    [TestMethod]
    public void Convert_RuleDetails_CorrectlyConvertsImpacts()
    {
        var ruleDetails = new EffectiveRuleDetailsDto(
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
            default);

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.DefaultImpacts.Should().BeEquivalentTo(new Dictionary<RuleSoftwareQuality, RuleSoftwareQualitySeverity>
        {
            { RuleSoftwareQuality.Security, RuleSoftwareQualitySeverity.High },
            { RuleSoftwareQuality.Reliability, RuleSoftwareQualitySeverity.Low },
            { RuleSoftwareQuality.Maintainability, RuleSoftwareQualitySeverity.Medium }
        });
    }

    [TestMethod]
    public void Convert_RuleDetails_Standard_SimpleRuleDescription()
    {
        const string rulekey = "rule:key1";
        var ruleDetails = new EffectiveRuleDetailsDto(
            rulekey,
            "name",
            Language.JS,
            new StandardModeDetails(IssueSeverity.CRITICAL, RuleType.VULNERABILITY),
            VulnerabilityProbability.MEDIUM,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateLeft(
                new RuleMonolithicDescriptionDto("content")),
            new List<EffectiveRuleParamDto>());

        var ruleInfo = testSubject.Convert(ruleDetails);

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
    public void Convert_RuleDetails_MQR_SimpleRuleDescription()
    {
        const string rulekey = "rule:key1";
        var ruleDetails = new EffectiveRuleDetailsDto(
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
            new List<EffectiveRuleParamDto>());

        var ruleInfo = testSubject.Convert(ruleDetails);

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
    public void Convert_RuleDetails_Standard_RichRuleDescription()
    {
        const string rulekey = "rule:key1";
        var ruleSplitDescriptionDto = new RuleSplitDescriptionDto("intro", new List<RuleDescriptionTabDto>());
        var ruleDetails = new EffectiveRuleDetailsDto(
            rulekey,
            "name",
            Language.CPP,
            new StandardModeDetails(IssueSeverity.MINOR, RuleType.BUG),
            null,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateRight(ruleSplitDescriptionDto),
            new List<EffectiveRuleParamDto> { new("ignored", default, default, default) });

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(rulekey,
            null,
            "name",
            RuleIssueSeverity.Minor,
            RuleIssueType.Bug,
            ruleSplitDescriptionDto,
            null,
            null));
    }

    [TestMethod]
    public void Convert_RuleDetails_MQR_RichRuleDescription()
    {
        const string rulekey = "rule:key1";
        var ruleSplitDescriptionDto = new RuleSplitDescriptionDto("intro", new List<RuleDescriptionTabDto>());
        var ruleDetails = new EffectiveRuleDetailsDto(
            rulekey,
            "name",
            Language.CPP,
            new MQRModeDetails(CleanCodeAttribute.RESPECTFUL, [
                new ImpactDto(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.MEDIUM)
            ]),
            null,
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateRight(ruleSplitDescriptionDto),
            new List<EffectiveRuleParamDto> { new("ignored", default, default, default) });

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(rulekey,
            null,
            "name",
            null,
            null,
            ruleSplitDescriptionDto,
            RuleCleanCodeAttribute.Respectful,
            new Dictionary<RuleSoftwareQuality, RuleSoftwareQualitySeverity> { { RuleSoftwareQuality.Maintainability, RuleSoftwareQualitySeverity.Medium } }));
    }

    [DataTestMethod]
    [DataRow(IssueSeverity.INFO, RuleIssueSeverity.Info)]
    [DataRow(IssueSeverity.MAJOR, RuleIssueSeverity.Major)]
    [DataRow(IssueSeverity.BLOCKER, RuleIssueSeverity.Blocker)]
    [DataRow(IssueSeverity.CRITICAL, RuleIssueSeverity.Critical)]
    [DataRow(IssueSeverity.MINOR, RuleIssueSeverity.Minor)]
    public void Convert_IssueDetails_CorrectlyConvertsSeverity(IssueSeverity slCore, RuleIssueSeverity expected)
    {
        Guid.NewGuid();
        var ruleDetails = CreateEffectiveIssueDetailsDto(new StandardModeDetails(slCore, default));

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.Severity.Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow(RuleType.CODE_SMELL, RuleIssueType.CodeSmell)]
    [DataRow(RuleType.VULNERABILITY, RuleIssueType.Vulnerability)]
    [DataRow(RuleType.BUG, RuleIssueType.Bug)]
    [DataRow(RuleType.SECURITY_HOTSPOT, RuleIssueType.Hotspot)]
    public void Convert_IssueDetails_CorrectlyConvertsType(RuleType slCore, RuleIssueType expected)
    {
        Guid.NewGuid();
        var ruleDetails = CreateEffectiveIssueDetailsDto(new StandardModeDetails(default, slCore));

        var ruleInfo = testSubject.Convert(ruleDetails);

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
    public void Convert_IssueDetails_CorrectlyConvertsCleanCodeAttribute(CleanCodeAttribute slCore, RuleCleanCodeAttribute expected)
    {
        Guid.NewGuid();
        var ruleDetails = CreateEffectiveIssueDetailsDto(new MQRModeDetails(slCore, default));

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.CleanCodeAttribute.Should().Be(expected);
    }

    [TestMethod]
    public void Convert_IssueDetails_CorrectlyConvertsImpacts()
    {
        Guid.NewGuid();
        var ruleDetails = CreateEffectiveIssueDetailsDto(new MQRModeDetails(default, [
            new ImpactDto(SoftwareQuality.SECURITY, ImpactSeverity.HIGH),
            new ImpactDto(SoftwareQuality.RELIABILITY, ImpactSeverity.LOW),
            new ImpactDto(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.MEDIUM)
        ]));

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.DefaultImpacts.Should().BeEquivalentTo(new Dictionary<RuleSoftwareQuality, RuleSoftwareQualitySeverity>
        {
            { RuleSoftwareQuality.Security, RuleSoftwareQualitySeverity.High },
            { RuleSoftwareQuality.Reliability, RuleSoftwareQualitySeverity.Low },
            { RuleSoftwareQuality.Maintainability, RuleSoftwareQualitySeverity.Medium }
        });
    }

    [TestMethod]
    public void Convert_IssueDetails_Standard_SimpleRuleDescription()
    {
        Guid.NewGuid();
        var ruleDetails = CreateEffectiveIssueDetailsDto(new StandardModeDetails(IssueSeverity.CRITICAL, RuleType.VULNERABILITY),
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateLeft(
                new RuleMonolithicDescriptionDto("content")));

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(null,
            "content",
            null,
            RuleIssueSeverity.Critical,
            RuleIssueType.Vulnerability,
            null,
            null,
            null));
    }

    [TestMethod]
    public void Convert_IssueDetails_MQR_SimpleRuleDescription()
    {
        Guid.NewGuid();
        var ruleDetails = CreateEffectiveIssueDetailsDto(new MQRModeDetails(CleanCodeAttribute.MODULAR, default), Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateLeft(
            new RuleMonolithicDescriptionDto("content")));

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(null,
            "content",
            null,
            null,
            null,
            null,
            RuleCleanCodeAttribute.Modular,
            null));
    }

    [TestMethod]
    public void Convert_IssueDetails_Standard_RichRuleDescription()
    {
        Guid.NewGuid();
        var ruleSplitDescriptionDto = new RuleSplitDescriptionDto("intro", new List<RuleDescriptionTabDto>());
        var ruleDetails = CreateEffectiveIssueDetailsDto(new StandardModeDetails(IssueSeverity.MINOR, RuleType.BUG),
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateRight(ruleSplitDescriptionDto));

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(null,
            null,
            null,
            RuleIssueSeverity.Minor,
            RuleIssueType.Bug,
            ruleSplitDescriptionDto,
            null,
            null));
    }

    [TestMethod]
    public void Convert_IssueDetails_MQR_RichRuleDescription()
    {
        Guid.NewGuid();
        var ruleSplitDescriptionDto = new RuleSplitDescriptionDto("intro", new List<RuleDescriptionTabDto>());
        var ruleDetails = CreateEffectiveIssueDetailsDto(new MQRModeDetails(CleanCodeAttribute.RESPECTFUL, default),
            Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto>.CreateRight(ruleSplitDescriptionDto));

        var ruleInfo = testSubject.Convert(ruleDetails);

        ruleInfo.Should().BeEquivalentTo(new RuleInfo(null,
            null,
            null,
            null,
            null,
            ruleSplitDescriptionDto,
            RuleCleanCodeAttribute.Respectful,
            null));
    }

    private static EffectiveIssueDetailsDto CreateEffectiveIssueDetailsDto(
        Either<StandardModeDetails, MQRModeDetails> severityDetails,
        Either<RuleMonolithicDescriptionDto, RuleSplitDescriptionDto> description = default) =>
        new(
            default,
            default,
            default,
            default,
            description,
            default,
            severityDetails,
            default);
}
