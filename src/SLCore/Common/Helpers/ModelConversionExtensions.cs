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
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.SLCore.Common.Helpers
{
    public static class ModelConversionExtensions
    {
        public static AnalysisIssueSeverity ToAnalysisIssueSeverity(this IssueSeverity issueSeverity)
        {
            return issueSeverity switch
            {
                IssueSeverity.BLOCKER => AnalysisIssueSeverity.Blocker,
                IssueSeverity.CRITICAL => AnalysisIssueSeverity.Critical,
                IssueSeverity.MAJOR => AnalysisIssueSeverity.Major,
                IssueSeverity.MINOR => AnalysisIssueSeverity.Minor,
                IssueSeverity.INFO => AnalysisIssueSeverity.Info,
                _ => throw new ArgumentOutOfRangeException(nameof(issueSeverity), issueSeverity, SLCoreStrings.ModelExtensions_UnexpectedValue),
            };
        }

        public static AnalysisIssueType ToAnalysisIssueType(this RuleType ruleType)
        {
            return ruleType switch
            {
                RuleType.CODE_SMELL => AnalysisIssueType.CodeSmell,
                RuleType.BUG => AnalysisIssueType.Bug,
                RuleType.VULNERABILITY => AnalysisIssueType.Vulnerability,
                RuleType.SECURITY_HOTSPOT => AnalysisIssueType.SecurityHotspot,
                _ => throw new ArgumentOutOfRangeException(nameof(ruleType), ruleType, SLCoreStrings.ModelExtensions_UnexpectedValue),
            };
        }

        public static SoftwareQualitySeverity ToSoftwareQualitySeverity(this ImpactSeverity impactSeverity)
        {
            return impactSeverity switch
            {
                ImpactSeverity.INFO => SoftwareQualitySeverity.Info,
                ImpactSeverity.LOW => SoftwareQualitySeverity.Low,
                ImpactSeverity.MEDIUM => SoftwareQualitySeverity.Medium,
                ImpactSeverity.HIGH => SoftwareQualitySeverity.High,
                ImpactSeverity.BLOCKER => SoftwareQualitySeverity.Blocker,
                _ => throw new ArgumentOutOfRangeException(nameof(impactSeverity), impactSeverity, SLCoreStrings.ModelExtensions_UnexpectedValue),
            };
        }

        public static HotspotPriority? GetHotspotPriority(this VulnerabilityProbability? vulnerabilityProbability)
        {
            return vulnerabilityProbability switch
            {
                null => null,
                VulnerabilityProbability.HIGH => HotspotPriority.High,
                VulnerabilityProbability.MEDIUM => HotspotPriority.Medium,
                VulnerabilityProbability.LOW => HotspotPriority.Low,
                _ => throw new ArgumentOutOfRangeException(nameof(vulnerabilityProbability), vulnerabilityProbability, SLCoreStrings.ModelExtensions_UnexpectedValue),
            };
        }
    }
}
