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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SoftwareQuality = SonarLint.VisualStudio.Core.Analysis.SoftwareQuality;
using CleanCodeAttribute = SonarLint.VisualStudio.Core.Analysis.CleanCodeAttribute;
using CoreHotspotStatus = SonarLint.VisualStudio.Core.Analysis.HotspotStatus;
using SlCoreHotspotStatus = SonarLint.VisualStudio.SLCore.Common.Models.HotspotStatus;

namespace SonarLint.VisualStudio.SLCore.Common.Helpers;

public static class ModelConversionExtensions
{
    public static AnalysisIssueSeverity ToAnalysisIssueSeverity(this IssueSeverity issueSeverity) =>
        issueSeverity switch
        {
            IssueSeverity.BLOCKER => AnalysisIssueSeverity.Blocker,
            IssueSeverity.CRITICAL => AnalysisIssueSeverity.Critical,
            IssueSeverity.MAJOR => AnalysisIssueSeverity.Major,
            IssueSeverity.MINOR => AnalysisIssueSeverity.Minor,
            IssueSeverity.INFO => AnalysisIssueSeverity.Info,
            _ => throw new ArgumentOutOfRangeException(nameof(issueSeverity), issueSeverity, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static AnalysisIssueType ToAnalysisIssueType(this RuleType ruleType) =>
        ruleType switch
        {
            RuleType.CODE_SMELL => AnalysisIssueType.CodeSmell,
            RuleType.BUG => AnalysisIssueType.Bug,
            RuleType.VULNERABILITY => AnalysisIssueType.Vulnerability,
            RuleType.SECURITY_HOTSPOT => AnalysisIssueType.SecurityHotspot,
            _ => throw new ArgumentOutOfRangeException(nameof(ruleType), ruleType, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static SoftwareQualitySeverity ToSoftwareQualitySeverity(this ImpactSeverity impactSeverity) =>
        impactSeverity switch
        {
            ImpactSeverity.INFO => SoftwareQualitySeverity.Info,
            ImpactSeverity.LOW => SoftwareQualitySeverity.Low,
            ImpactSeverity.MEDIUM => SoftwareQualitySeverity.Medium,
            ImpactSeverity.HIGH => SoftwareQualitySeverity.High,
            ImpactSeverity.BLOCKER => SoftwareQualitySeverity.Blocker,
            _ => throw new ArgumentOutOfRangeException(nameof(impactSeverity), impactSeverity, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static HotspotPriority? GetHotspotPriority(this VulnerabilityProbability? vulnerabilityProbability) =>
        vulnerabilityProbability switch
        {
            null => null,
            VulnerabilityProbability.HIGH => HotspotPriority.High,
            VulnerabilityProbability.MEDIUM => HotspotPriority.Medium,
            VulnerabilityProbability.LOW => HotspotPriority.Low,
            _ => throw new ArgumentOutOfRangeException(nameof(vulnerabilityProbability), vulnerabilityProbability, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static SoftwareQuality ToSoftwareQuality(this Models.SoftwareQuality softwareQuality) =>
        softwareQuality switch
        {
            Models.SoftwareQuality.MAINTAINABILITY => SoftwareQuality.Maintainability,
            Models.SoftwareQuality.RELIABILITY => SoftwareQuality.Reliability,
            Models.SoftwareQuality.SECURITY => SoftwareQuality.Security,
            _ => throw new ArgumentOutOfRangeException(nameof(softwareQuality), softwareQuality, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static CleanCodeAttribute? ToCleanCodeAttribute(this SLCore.Common.Models.CleanCodeAttribute? cleanCodeAttribute) =>
        cleanCodeAttribute switch
        {
            SLCore.Common.Models.CleanCodeAttribute.CONVENTIONAL => CleanCodeAttribute.Conventional,
            SLCore.Common.Models.CleanCodeAttribute.FORMATTED => CleanCodeAttribute.Formatted,
            SLCore.Common.Models.CleanCodeAttribute.IDENTIFIABLE => CleanCodeAttribute.Identifiable,
            SLCore.Common.Models.CleanCodeAttribute.CLEAR => CleanCodeAttribute.Clear,
            SLCore.Common.Models.CleanCodeAttribute.COMPLETE => CleanCodeAttribute.Complete,
            SLCore.Common.Models.CleanCodeAttribute.EFFICIENT => CleanCodeAttribute.Efficient,
            SLCore.Common.Models.CleanCodeAttribute.LOGICAL => CleanCodeAttribute.Logical,
            SLCore.Common.Models.CleanCodeAttribute.DISTINCT => CleanCodeAttribute.Distinct,
            SLCore.Common.Models.CleanCodeAttribute.FOCUSED => CleanCodeAttribute.Focused,
            SLCore.Common.Models.CleanCodeAttribute.MODULAR => CleanCodeAttribute.Modular,
            SLCore.Common.Models.CleanCodeAttribute.TESTED => CleanCodeAttribute.Tested,
            SLCore.Common.Models.CleanCodeAttribute.LAWFUL => CleanCodeAttribute.Lawful,
            SLCore.Common.Models.CleanCodeAttribute.RESPECTFUL => CleanCodeAttribute.Respectful,
            SLCore.Common.Models.CleanCodeAttribute.TRUSTWORTHY => CleanCodeAttribute.Trustworthy,
            null => null,
            _ => throw new ArgumentOutOfRangeException(nameof(cleanCodeAttribute), cleanCodeAttribute, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static Impact ToImpact(this ImpactDto impact) => new(impact.softwareQuality.ToSoftwareQuality(), impact.impactSeverity.ToSoftwareQualitySeverity());

    public static CoreHotspotStatus ToHotspotStatus(this SlCoreHotspotStatus hotspotStatus) =>
        hotspotStatus switch
        {
            SlCoreHotspotStatus.TO_REVIEW => CoreHotspotStatus.ToReview,
            SlCoreHotspotStatus.ACKNOWLEDGED => CoreHotspotStatus.Acknowledged,
            SlCoreHotspotStatus.FIXED => CoreHotspotStatus.Fixed,
            SlCoreHotspotStatus.SAFE => CoreHotspotStatus.Safe,
            _ => throw new ArgumentOutOfRangeException(nameof(hotspotStatus), hotspotStatus, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static DependencyRiskStatus ToDependencyRiskStatus(this ScaStatus scaStatus) =>
        scaStatus switch
        {
            ScaStatus.OPEN => DependencyRiskStatus.Open,
            ScaStatus.CONFIRM => DependencyRiskStatus.Confirmed,
            ScaStatus.ACCEPT => DependencyRiskStatus.Accepted,
            ScaStatus.SAFE => DependencyRiskStatus.Safe,
            _ => throw new ArgumentOutOfRangeException(nameof(scaStatus), scaStatus, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static DependencyRiskType ToDependencyRiskType(this ScaType scaType) =>
        scaType switch
        {
            ScaType.VULNERABILITY => DependencyRiskType.Vulnerability,
            ScaType.PROHIBITED_LICENSE => DependencyRiskType.ProhibitedLicense,
            _ => throw new ArgumentOutOfRangeException(nameof(scaType), scaType, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static DependencyRiskImpactSeverity ToDependencyRiskSeverity(this ScaSeverity scaSeverity) =>
        scaSeverity switch
        {
            ScaSeverity.INFO => DependencyRiskImpactSeverity.Info,
            ScaSeverity.LOW => DependencyRiskImpactSeverity.Low,
            ScaSeverity.MEDIUM => DependencyRiskImpactSeverity.Medium,
            ScaSeverity.HIGH => DependencyRiskImpactSeverity.High,
            ScaSeverity.BLOCKER => DependencyRiskImpactSeverity.Blocker,
            _ => throw new ArgumentOutOfRangeException(nameof(scaSeverity), scaSeverity, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };


    public static DependencyRiskTransition ToDependencyRiskTransition(this ScaTransition scaTransition) =>
        scaTransition switch
        {
            ScaTransition.CONFIRM => DependencyRiskTransition.Confirm,
            ScaTransition.REOPEN => DependencyRiskTransition.Reopen,
            ScaTransition.SAFE => DependencyRiskTransition.Safe,
            ScaTransition.FIXED => DependencyRiskTransition.Fixed,
            ScaTransition.ACCEPT => DependencyRiskTransition.Accept,
            _ => throw new ArgumentOutOfRangeException(nameof(scaTransition), scaTransition, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };
}
