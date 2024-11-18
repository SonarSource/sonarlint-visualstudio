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

using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Helpers
{
    public interface IAnalysisSeverityToVsSeverityConverter
    {
        __VSERRORCATEGORY Convert(AnalysisIssueSeverity? severity);
        __VSERRORCATEGORY ConvertFromCct(SoftwareQualitySeverity severity);
    }

    public class AnalysisSeverityToVsSeverityConverter : IAnalysisSeverityToVsSeverityConverter
    {
        private readonly IEnvironmentSettings environmentSettings;

        public AnalysisSeverityToVsSeverityConverter()
            : this(new EnvironmentSettings())
        {
        }

        internal AnalysisSeverityToVsSeverityConverter(IEnvironmentSettings environmentSettings)
        {
            this.environmentSettings = environmentSettings;
        }

        public __VSERRORCATEGORY ConvertFromCct(SoftwareQualitySeverity severity)
        {
            switch (severity)
            {
                case SoftwareQualitySeverity.Medium:
                case SoftwareQualitySeverity.High:
                case SoftwareQualitySeverity.Blocker:
                    return __VSERRORCATEGORY.EC_WARNING;

                case SoftwareQualitySeverity.Info:
                case SoftwareQualitySeverity.Low:
                    return __VSERRORCATEGORY.EC_MESSAGE;

                default:
                    // We don't want to throw here - we're being called by VS to populate
                    // the columns in the error list, and if we're on a UI thread then
                    // we'll crash VS
                    return __VSERRORCATEGORY.EC_MESSAGE;
            }
        }

        public __VSERRORCATEGORY Convert(AnalysisIssueSeverity? severity)
        {
            switch (severity)
            {
                case AnalysisIssueSeverity.Info:
                case AnalysisIssueSeverity.Minor:
                    return __VSERRORCATEGORY.EC_MESSAGE;

                case AnalysisIssueSeverity.Major:
                case AnalysisIssueSeverity.Critical:
                    return __VSERRORCATEGORY.EC_WARNING;

                case AnalysisIssueSeverity.Blocker:
                    return environmentSettings.TreatBlockerSeverityAsError() ? __VSERRORCATEGORY.EC_ERROR : __VSERRORCATEGORY.EC_WARNING;

                default:
                    // We don't want to throw here - we're being called by VS to populate
                    // the columns in the error list, and if we're on a UI thread then
                    // we'll crash VS
                    return __VSERRORCATEGORY.EC_MESSAGE;
            }
        }
    }

    public static class AnalysisSeverityToVsSeverityConverterExtensions
    {
        public static __VSERRORCATEGORY GetVsSeverity(
            this IAnalysisSeverityToVsSeverityConverter converter,
            IAnalysisIssue issue) =>
            issue.HighestSoftwareQualitySeverity.HasValue
                ? converter.ConvertFromCct(issue.HighestSoftwareQualitySeverity.Value)
                : converter.Convert(issue.Severity);
    }
}
