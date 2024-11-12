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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SoftwareQuality = SonarLint.VisualStudio.Core.Analysis.SoftwareQuality;

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
}
