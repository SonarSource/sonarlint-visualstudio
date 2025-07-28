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
using SlCoreDependencyRiskSeverity = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskSeverity;
using CoreDependencyRiskSeverity = SonarLint.VisualStudio.Core.Analysis.DependencyRiskImpactSeverity;
using SlCoreDependencyRiskStatus = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskStatus;
using CoreDependencyRiskStatus = SonarLint.VisualStudio.Core.Analysis.DependencyRiskStatus;
using SlCoreDependencyRiskTransition = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskTransition;
using CoreDependencyRiskTransition = SonarLint.VisualStudio.Core.Analysis.DependencyRiskTransition;
using SlCoreDependencyRiskType = SonarLint.VisualStudio.SLCore.Common.Models.DependencyRiskType;
using CoreDependencyRiskType = SonarLint.VisualStudio.Core.Analysis.DependencyRiskType;
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

    public static CoreDependencyRiskStatus ToDependencyRiskStatus(this SlCoreDependencyRiskStatus dependencyRiskStatus) =>
        dependencyRiskStatus switch
        {
            SlCoreDependencyRiskStatus.OPEN => CoreDependencyRiskStatus.Open,
            SlCoreDependencyRiskStatus.CONFIRM => CoreDependencyRiskStatus.Confirmed,
            SlCoreDependencyRiskStatus.ACCEPT => CoreDependencyRiskStatus.Accepted,
            SlCoreDependencyRiskStatus.SAFE => CoreDependencyRiskStatus.Safe,
            _ => throw new ArgumentOutOfRangeException(nameof(dependencyRiskStatus), dependencyRiskStatus, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static CoreDependencyRiskType ToDependencyRiskType(this SlCoreDependencyRiskType dependencyRiskType) =>
        dependencyRiskType switch
        {
            SlCoreDependencyRiskType.VULNERABILITY => CoreDependencyRiskType.Vulnerability,
            SlCoreDependencyRiskType.PROHIBITED_LICENSE => CoreDependencyRiskType.ProhibitedLicense,
            _ => throw new ArgumentOutOfRangeException(nameof(dependencyRiskType), dependencyRiskType, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static CoreDependencyRiskSeverity ToDependencyRiskSeverity(this SlCoreDependencyRiskSeverity dependencyRiskSeverity) =>
        dependencyRiskSeverity switch
        {
            SlCoreDependencyRiskSeverity.INFO => CoreDependencyRiskSeverity.Info,
            SlCoreDependencyRiskSeverity.LOW => CoreDependencyRiskSeverity.Low,
            SlCoreDependencyRiskSeverity.MEDIUM => CoreDependencyRiskSeverity.Medium,
            SlCoreDependencyRiskSeverity.HIGH => CoreDependencyRiskSeverity.High,
            SlCoreDependencyRiskSeverity.BLOCKER => CoreDependencyRiskSeverity.Blocker,
            _ => throw new ArgumentOutOfRangeException(nameof(dependencyRiskSeverity), dependencyRiskSeverity, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };


    public static CoreDependencyRiskTransition ToDependencyRiskTransition(this SlCoreDependencyRiskTransition dependencyRiskTransition) =>
        dependencyRiskTransition switch
        {
            SlCoreDependencyRiskTransition.CONFIRM => CoreDependencyRiskTransition.Confirm,
            SlCoreDependencyRiskTransition.REOPEN => CoreDependencyRiskTransition.Reopen,
            SlCoreDependencyRiskTransition.SAFE => CoreDependencyRiskTransition.Safe,
            SlCoreDependencyRiskTransition.FIXED => CoreDependencyRiskTransition.Fixed,
            SlCoreDependencyRiskTransition.ACCEPT => CoreDependencyRiskTransition.Accept,
            _ => throw new ArgumentOutOfRangeException(nameof(dependencyRiskTransition), dependencyRiskTransition, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };

    public static SlCoreDependencyRiskTransition ToSlCoreDependencyRiskTransition(this CoreDependencyRiskTransition dependencyRiskTransition) =>
        dependencyRiskTransition switch
        {
            CoreDependencyRiskTransition.Confirm => SlCoreDependencyRiskTransition.CONFIRM,
            CoreDependencyRiskTransition.Reopen => SlCoreDependencyRiskTransition.REOPEN,
            CoreDependencyRiskTransition.Safe => SlCoreDependencyRiskTransition.SAFE,
            CoreDependencyRiskTransition.Fixed => SlCoreDependencyRiskTransition.FIXED,
            CoreDependencyRiskTransition.Accept => SlCoreDependencyRiskTransition.ACCEPT,
            _ => throw new ArgumentOutOfRangeException(nameof(dependencyRiskTransition), dependencyRiskTransition, SLCoreStrings.ModelExtensions_UnexpectedValue)
        };
}
