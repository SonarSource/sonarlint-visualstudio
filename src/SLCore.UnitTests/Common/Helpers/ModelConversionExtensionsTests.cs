/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SoftwareQuality = SonarLint.VisualStudio.Core.Analysis.SoftwareQuality;
using CleanCodeAttribute = SonarLint.VisualStudio.Core.Analysis.CleanCodeAttribute;
using CoreHotspotStatus = SonarLint.VisualStudio.Core.Analysis.HotspotStatus;
using SlCoreDependencyRiskSeverity = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskSeverity;
using CoreDependencyRiskSeverity = SonarLint.VisualStudio.Core.Analysis.DependencyRiskImpactSeverity;
using SlCoreDependencyRiskStatus = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskStatus;
using CoreDependencyRiskStatus = SonarLint.VisualStudio.Core.Analysis.DependencyRiskStatus;
using SlCoreDependencyRiskTransition = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskTransition;
using CoreDependencyRiskTransition = SonarLint.VisualStudio.Core.Analysis.DependencyRiskTransition;
using SlCoreDependencyRiskType = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskType;
using CoreDependencyRiskType = SonarLint.VisualStudio.Core.Analysis.DependencyRiskType;
using SlCoreHotspotStatus = SonarLint.VisualStudio.SLCore.Common.Models.HotspotStatus;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Helpers;

[TestClass]
public class ModelConversionExtensionsTests
{
    [DataRow(IssueSeverity.BLOCKER, AnalysisIssueSeverity.Blocker)]
    [DataRow(IssueSeverity.CRITICAL, AnalysisIssueSeverity.Critical)]
    [DataRow(IssueSeverity.MAJOR, AnalysisIssueSeverity.Major)]
    [DataRow(IssueSeverity.MINOR, AnalysisIssueSeverity.Minor)]
    [DataRow(IssueSeverity.INFO, AnalysisIssueSeverity.Info)]
    [TestMethod]
    public void ToAnalysisIssueSeverity_ConvertsCorrectly(IssueSeverity issueSeverity, AnalysisIssueSeverity excpectedAnalysisIssueSeverity) =>
        issueSeverity.ToAnalysisIssueSeverity().Should().Be(excpectedAnalysisIssueSeverity);

    [TestMethod]
    public void ToAnalysisIssueSeverity_DoesNotThrow()
    {
        foreach (var issueSeverity in Enum.GetValues(typeof(IssueSeverity)))
        {
            var act = () => ((IssueSeverity)issueSeverity).ToAnalysisIssueSeverity();
            act.Should().NotThrow();
        }
    }

    [TestMethod]
    public void ToAnalysisIssueSeverity_ValueOutOfRange_Throws()
    {
        var act = () => ((IssueSeverity)1000).ToAnalysisIssueSeverity();
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: issueSeverity
                                                                      Actual value was 1000.
                                                                      """);
    }

    [DataRow(RuleType.BUG, AnalysisIssueType.Bug)]
    [DataRow(RuleType.CODE_SMELL, AnalysisIssueType.CodeSmell)]
    [DataRow(RuleType.SECURITY_HOTSPOT, AnalysisIssueType.SecurityHotspot)]
    [DataRow(RuleType.VULNERABILITY, AnalysisIssueType.Vulnerability)]
    [TestMethod]
    public void ToAnalysisIssueType_ConvertsCorrectly(RuleType ruleType, AnalysisIssueType excpectedAnalysisIssueType) => ruleType.ToAnalysisIssueType().Should().Be(excpectedAnalysisIssueType);

    [TestMethod]
    public void ToAnalysisIssueType_DoesNotThrow()
    {
        foreach (var ruleType in Enum.GetValues(typeof(RuleType)))
        {
            var act = () => ((RuleType)ruleType).ToAnalysisIssueType();
            act.Should().NotThrow();
        }
    }

    [TestMethod]
    public void ToAnalysisIssueType_ValueOutOfRange_Throws()
    {
        var act = () => ((RuleType)1000).ToAnalysisIssueType();
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: ruleType
                                                                      Actual value was 1000.
                                                                      """);
    }

    [DataRow(ImpactSeverity.LOW, SoftwareQualitySeverity.Low)]
    [DataRow(ImpactSeverity.MEDIUM, SoftwareQualitySeverity.Medium)]
    [DataRow(ImpactSeverity.HIGH, SoftwareQualitySeverity.High)]
    [TestMethod]
    public void ToSoftwareQualitySeverity_ConvertsCorrectly(ImpactSeverity impactSeverity, SoftwareQualitySeverity excpectedSoftwareQualitySeverity) =>
        impactSeverity.ToSoftwareQualitySeverity().Should().Be(excpectedSoftwareQualitySeverity);

    [TestMethod]
    public void ToSoftwareQualitySeverity_DoesNotThrow()
    {
        foreach (var impactSeverity in Enum.GetValues(typeof(ImpactSeverity)))
        {
            var act = () => ((ImpactSeverity)impactSeverity).ToSoftwareQualitySeverity();
            act.Should().NotThrow();
        }
    }

    [TestMethod]
    public void ToSoftwareQualitySeverity_ValueOutOfRange_Throws()
    {
        var act = () => ((ImpactSeverity)1000).ToSoftwareQualitySeverity();
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: impactSeverity
                                                                      Actual value was 1000.
                                                                      """);
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow(VulnerabilityProbability.HIGH, HotspotPriority.High)]
    [DataRow(VulnerabilityProbability.MEDIUM, HotspotPriority.Medium)]
    [DataRow(VulnerabilityProbability.LOW, HotspotPriority.Low)]
    public void GetHotspotPriority_HotspotHasVulnerabilityProbability_ConvertsCorrectly(VulnerabilityProbability? vulnerabilityProbability, HotspotPriority? expectedHotspotPriority)
    {
        var result = vulnerabilityProbability.GetHotspotPriority();

        result.Should().Be(expectedHotspotPriority);
    }

    [TestMethod]
    public void GetHotspotPriority_ValueOutOfRange_Throws()
    {
        var vulnerabilityProbability = (VulnerabilityProbability?)1000;

        var act = () => vulnerabilityProbability.GetHotspotPriority();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    [DataRow(SLCore.Common.Models.SoftwareQuality.MAINTAINABILITY, SoftwareQuality.Maintainability)]
    [DataRow(SLCore.Common.Models.SoftwareQuality.RELIABILITY, SoftwareQuality.Reliability)]
    [DataRow(SLCore.Common.Models.SoftwareQuality.SECURITY, SoftwareQuality.Security)]
    public void ToSoftwareQuality_ConvertsCorrectly(SLCore.Common.Models.SoftwareQuality softwareQuality, SoftwareQuality expectedSoftwareQuality) =>
        softwareQuality.ToSoftwareQuality().Should().Be(expectedSoftwareQuality);

    [TestMethod]
    public void ToSoftwareQuality_ValueOutOfRange_Throws()
    {
        var act = () => ((SLCore.Common.Models.SoftwareQuality)1000).ToSoftwareQuality();

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: softwareQuality
                                                                      Actual value was 1000.
                                                                      """);
    }

    [TestMethod]
    [DataRow(SLCore.Common.Models.SoftwareQuality.MAINTAINABILITY, SoftwareQuality.Maintainability, ImpactSeverity.LOW, SoftwareQualitySeverity.Low)]
    [DataRow(SLCore.Common.Models.SoftwareQuality.RELIABILITY, SoftwareQuality.Reliability, ImpactSeverity.MEDIUM, SoftwareQualitySeverity.Medium)]
    [DataRow(SLCore.Common.Models.SoftwareQuality.SECURITY, SoftwareQuality.Security, ImpactSeverity.HIGH, SoftwareQualitySeverity.High)]
    public void ToImpact_ConvertsCorrectly(
        SLCore.Common.Models.SoftwareQuality softwareQuality,
        SoftwareQuality expectedSoftwareQuality,
        ImpactSeverity severity,
        SoftwareQualitySeverity expectedSeverity)
    {
        var impactDto = new ImpactDto(softwareQuality, severity);

        impactDto.ToImpact().Should().BeEquivalentTo(new Impact(expectedSoftwareQuality, expectedSeverity));
    }

    [TestMethod]
    public void ToImpact_SeverityValueOutOfRange_Throws()
    {
        var act = () => new ImpactDto(SLCore.Common.Models.SoftwareQuality.MAINTAINABILITY, (ImpactSeverity)1000).ToImpact();

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: impactSeverity
                                                                      Actual value was 1000.
                                                                      """);
    }

    [TestMethod]
    public void ToImpact_SoftwareQualityValueOutOfRange_Throws()
    {
        var act = () => new ImpactDto((SLCore.Common.Models.SoftwareQuality)1000, ImpactSeverity.LOW).ToImpact();

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: softwareQuality
                                                                      Actual value was 1000.
                                                                      """);
    }

    [TestMethod]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.CONVENTIONAL, CleanCodeAttribute.Conventional)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.FORMATTED, CleanCodeAttribute.Formatted)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.IDENTIFIABLE, CleanCodeAttribute.Identifiable)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.CLEAR, CleanCodeAttribute.Clear)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.COMPLETE, CleanCodeAttribute.Complete)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.EFFICIENT, CleanCodeAttribute.Efficient)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.LOGICAL, CleanCodeAttribute.Logical)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.DISTINCT, CleanCodeAttribute.Distinct)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.FOCUSED, CleanCodeAttribute.Focused)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.MODULAR, CleanCodeAttribute.Modular)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.TESTED, CleanCodeAttribute.Tested)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.LAWFUL, CleanCodeAttribute.Lawful)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.RESPECTFUL, CleanCodeAttribute.Respectful)]
    [DataRow(SLCore.Common.Models.CleanCodeAttribute.TRUSTWORTHY, CleanCodeAttribute.Trustworthy)]
    [DataRow(null, null)]
    public void ToCleanCodeAttribute_ConvertsCorrectly(SLCore.Common.Models.CleanCodeAttribute? slCoreCleanCodeAttribute, CleanCodeAttribute? expected)
    {
        var result = slCoreCleanCodeAttribute.ToCleanCodeAttribute();

        result.Should().Be(expected);
    }

    [TestMethod]
    public void ToCleanCodeAttribute_ValueOutOfRange_Throws()
    {
        var act = () => ((SLCore.Common.Models.CleanCodeAttribute?)1000).ToCleanCodeAttribute();

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: cleanCodeAttribute
                                                                      Actual value was 1000.
                                                                      """);
    }

    [TestMethod]
    [DataRow(SlCoreHotspotStatus.TO_REVIEW, CoreHotspotStatus.ToReview)]
    [DataRow(SlCoreHotspotStatus.ACKNOWLEDGED, CoreHotspotStatus.Acknowledged)]
    [DataRow(SlCoreHotspotStatus.FIXED, CoreHotspotStatus.Fixed)]
    [DataRow(SlCoreHotspotStatus.SAFE, CoreHotspotStatus.Safe)]
    public void ToHotspotStatus_ConvertsCorrectly(SlCoreHotspotStatus slCoreStatus, CoreHotspotStatus coreStatus) => slCoreStatus.ToHotspotStatus().Should().Be(coreStatus);

    [TestMethod]
    public void ToHotspotStatus_ConvertsCorrectly()
    {
        var act = () => ((SlCoreHotspotStatus)1000).ToHotspotStatus();

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: hotspotStatus
                                                                      Actual value was 1000.
                                                                      """);
    }

    [TestMethod]
    [DataRow(SlCoreDependencyRiskStatus.FIXED, CoreDependencyRiskStatus.Fixed)]
    [DataRow(SlCoreDependencyRiskStatus.OPEN, CoreDependencyRiskStatus.Open)]
    [DataRow(SlCoreDependencyRiskStatus.CONFIRM, CoreDependencyRiskStatus.Confirmed)]
    [DataRow(SlCoreDependencyRiskStatus.ACCEPT, CoreDependencyRiskStatus.Accepted)]
    [DataRow(SlCoreDependencyRiskStatus.SAFE, CoreDependencyRiskStatus.Safe)]
    public void ToDependencyRiskStatus_ConvertsCorrectly(SlCoreDependencyRiskStatus dependencyRiskStatus, CoreDependencyRiskStatus expectedStatus) =>
        dependencyRiskStatus.ToDependencyRiskStatus().Should().Be(expectedStatus);

    [TestMethod]
    public void ToDependencyRiskStatus_ValueOutOfRange_Throws()
    {
        var act = () => ((SlCoreDependencyRiskStatus)1000).ToDependencyRiskStatus();

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: dependencyRiskStatus
                                                                      Actual value was 1000.
                                                                      """);
    }

    [TestMethod]
    [DataRow(SlCoreDependencyRiskType.VULNERABILITY, CoreDependencyRiskType.Vulnerability)]
    [DataRow(SlCoreDependencyRiskType.PROHIBITED_LICENSE, CoreDependencyRiskType.ProhibitedLicense)]
    public void ToDependencyRiskType_ConvertsCorrectly(SlCoreDependencyRiskType dependencyRiskType, CoreDependencyRiskType expectedType) =>
        dependencyRiskType.ToDependencyRiskType().Should().Be(expectedType);

    [TestMethod]
    public void ToDependencyRiskType_ValueOutOfRange_Throws()
    {
        var act = () => ((SlCoreDependencyRiskType)1000).ToDependencyRiskType();

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: dependencyRiskType
                                                                      Actual value was 1000.
                                                                      """);
    }

    [TestMethod]
    [DataRow(SlCoreDependencyRiskSeverity.INFO, CoreDependencyRiskSeverity.Info)]
    [DataRow(SlCoreDependencyRiskSeverity.LOW, CoreDependencyRiskSeverity.Low)]
    [DataRow(SlCoreDependencyRiskSeverity.MEDIUM, CoreDependencyRiskSeverity.Medium)]
    [DataRow(SlCoreDependencyRiskSeverity.HIGH, CoreDependencyRiskSeverity.High)]
    [DataRow(SlCoreDependencyRiskSeverity.BLOCKER, CoreDependencyRiskSeverity.Blocker)]
    public void ToDependencyRiskSeverity_ConvertsCorrectly(SlCoreDependencyRiskSeverity dependencyRiskSeverity, CoreDependencyRiskSeverity expectedSeverity) =>
        dependencyRiskSeverity.ToDependencyRiskSeverity().Should().Be(expectedSeverity);

    [TestMethod]
    public void ToDependencyRiskSeverity_ValueOutOfRange_Throws()
    {
        var act = () => ((SlCoreDependencyRiskSeverity)1000).ToDependencyRiskSeverity();

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: dependencyRiskSeverity
                                                                      Actual value was 1000.
                                                                      """);
    }

    [TestMethod]
    [DataRow(SlCoreDependencyRiskTransition.CONFIRM, CoreDependencyRiskTransition.Confirm)]
    [DataRow(SlCoreDependencyRiskTransition.REOPEN, CoreDependencyRiskTransition.Reopen)]
    [DataRow(SlCoreDependencyRiskTransition.SAFE, CoreDependencyRiskTransition.Safe)]
    [DataRow(SlCoreDependencyRiskTransition.FIXED, CoreDependencyRiskTransition.Fixed)]
    [DataRow(SlCoreDependencyRiskTransition.ACCEPT, CoreDependencyRiskTransition.Accept)]
    public void ToDependencyRiskTransition_ConvertsCorrectly(SlCoreDependencyRiskTransition dependencyRiskTransition, CoreDependencyRiskTransition expectedTransition) =>
        dependencyRiskTransition.ToDependencyRiskTransition().Should().Be(expectedTransition);

    [TestMethod]
    public void ToDependencyRiskTransition_ValueOutOfRange_Throws()
    {
        var act = () => ((SlCoreDependencyRiskTransition)1000).ToDependencyRiskTransition();

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: dependencyRiskTransition
                                                                      Actual value was 1000.
                                                                      """);
    }

    [TestMethod]
    [DataRow(CoreDependencyRiskTransition.Confirm, SlCoreDependencyRiskTransition.CONFIRM)]
    [DataRow(CoreDependencyRiskTransition.Reopen, SlCoreDependencyRiskTransition.REOPEN)]
    [DataRow(CoreDependencyRiskTransition.Safe, SlCoreDependencyRiskTransition.SAFE)]
    [DataRow(CoreDependencyRiskTransition.Fixed, SlCoreDependencyRiskTransition.FIXED)]
    [DataRow(CoreDependencyRiskTransition.Accept, SlCoreDependencyRiskTransition.ACCEPT)]
    public void ToSlCoreDependencyRiskTransition_ConvertsCorrectly(CoreDependencyRiskTransition dependencyRiskTransition, SlCoreDependencyRiskTransition expectedTransition) =>
        dependencyRiskTransition.ToSlCoreDependencyRiskTransition().Should().Be(expectedTransition);

    [TestMethod]
    public void ToSlCoreDependencyRiskTransition_ValueOutOfRange_Throws()
    {
        var act = () => ((CoreDependencyRiskTransition)1000).ToSlCoreDependencyRiskTransition();

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("""
                                                                      Unexpected enum value
                                                                      Parameter name: dependencyRiskTransition
                                                                      Actual value was 1000.
                                                                      """);
    }
}
