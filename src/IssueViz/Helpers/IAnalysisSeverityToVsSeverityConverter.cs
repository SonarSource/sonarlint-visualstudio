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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Helpers
{
    public interface IAnalysisSeverityToVsSeverityConverter
    {
        __VSERRORCATEGORY Convert(AnalysisIssueSeverity? severity, string projectName);

        __VSERRORCATEGORY ConvertFromCct(SoftwareQualitySeverity severity, string projectName);
    }

    [Export(typeof(IAnalysisSeverityToVsSeverityConverter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class AnalysisSeverityToVsSeverityConverter : IAnalysisSeverityToVsSeverityConverter
    {
        private readonly IEnvironmentSettings environmentSettings;
        private readonly ITreatWarningsAsErrorsCache treatWarningsAsErrorsCache;

        [ImportingConstructor]
        public AnalysisSeverityToVsSeverityConverter(ITreatWarningsAsErrorsCache treatWarningsAsErrorsCache) : this(new EnvironmentSettings(), treatWarningsAsErrorsCache)
        {
        }

        internal AnalysisSeverityToVsSeverityConverter(IEnvironmentSettings environmentSettings, ITreatWarningsAsErrorsCache treatWarningsAsErrorsCache)

        {
            this.environmentSettings = environmentSettings;
            this.treatWarningsAsErrorsCache = treatWarningsAsErrorsCache;
        }

        public __VSERRORCATEGORY ConvertFromCct(SoftwareQualitySeverity severity, string projectName)
        {
            var result = severity switch
            {
                SoftwareQualitySeverity.Medium
                    or SoftwareQualitySeverity.High
                    or SoftwareQualitySeverity.Blocker => __VSERRORCATEGORY.EC_WARNING,
                SoftwareQualitySeverity.Info
                    or SoftwareQualitySeverity.Low => __VSERRORCATEGORY.EC_MESSAGE,
                // We don't want to throw here - we're being called by VS to populate
                // the columns in the error list, and if we're on a UI thread then
                // we'll crash VS
                _ => __VSERRORCATEGORY.EC_MESSAGE
            };

            return ApplyTreatWarningsAsErrors(result, projectName);
        }

        public __VSERRORCATEGORY Convert(AnalysisIssueSeverity? severity, string projectName)
        {
            var result = severity switch
            {
                AnalysisIssueSeverity.Info
                    or AnalysisIssueSeverity.Minor => __VSERRORCATEGORY.EC_MESSAGE,
                AnalysisIssueSeverity.Major
                    or AnalysisIssueSeverity.Critical => __VSERRORCATEGORY.EC_WARNING,
                AnalysisIssueSeverity.Blocker => environmentSettings.TreatBlockerSeverityAsError() ? __VSERRORCATEGORY.EC_ERROR : __VSERRORCATEGORY.EC_WARNING,
                // We don't want to throw here - we're being called by VS to populate
                // the columns in the error list, and if we're on a UI thread then
                // we'll crash VS
                _ => __VSERRORCATEGORY.EC_MESSAGE
            };

            return ApplyTreatWarningsAsErrors(result, projectName);
        }

        private __VSERRORCATEGORY ApplyTreatWarningsAsErrors(__VSERRORCATEGORY result, string projectName) =>
            result == __VSERRORCATEGORY.EC_WARNING && (treatWarningsAsErrorsCache?.IsTreatWarningsAsErrorsEnabled(projectName) ?? false)
                ? __VSERRORCATEGORY.EC_ERROR
                : result;
    }

    public static class AnalysisSeverityToVsSeverityConverterExtensions
    {
        public static __VSERRORCATEGORY GetVsSeverity(
            this IAnalysisSeverityToVsSeverityConverter converter,
            IAnalysisIssue issue,
            string projectName) => // todo
            issue.HighestImpact?.Severity != null
                ? converter.ConvertFromCct(issue.HighestImpact.Severity, projectName)
                : converter.Convert(issue.Severity, projectName);
    }
}
