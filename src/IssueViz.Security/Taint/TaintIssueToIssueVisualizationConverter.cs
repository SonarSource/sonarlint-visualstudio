/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{
    interface ITaintIssueToIssueVisualizationConverter
    {
        IAnalysisIssueVisualization Convert(SonarQubeIssue sonarQubeIssue);
    }

    [Export(typeof(ITaintIssueToIssueVisualizationConverter))]
    internal class TaintIssueToIssueVisualizationConverter : ITaintIssueToIssueVisualizationConverter
    {
        private readonly IAnalysisIssueVisualizationConverter issueVisualizationConverter;
        private readonly IAbsoluteFilePathLocator absoluteFilePathLocator;

        [ImportingConstructor]
        public TaintIssueToIssueVisualizationConverter(IAnalysisIssueVisualizationConverter issueVisualizationConverter, IAbsoluteFilePathLocator absoluteFilePathLocator)
        {
            this.issueVisualizationConverter = issueVisualizationConverter;
            this.absoluteFilePathLocator = absoluteFilePathLocator;
        }

        public IAnalysisIssueVisualization Convert(SonarQubeIssue sonarQubeIssue)
        {
            var analysisIssue = ConvertToAnalysisIssue(sonarQubeIssue);
            var issueViz = issueVisualizationConverter.Convert(analysisIssue);

            CalculateLocalFilePaths(issueViz);

            return issueViz;
        }

        private void CalculateLocalFilePaths(IAnalysisIssueVisualization issueViz)
        {
            var allLocations = issueViz.GetAllLocations();

            foreach (var location in allLocations)
            {
                location.CurrentFilePath = absoluteFilePathLocator.Locate(location.Location.FilePath);
            }
        }

        private IAnalysisIssueBase ConvertToAnalysisIssue(SonarQubeIssue sonarQubeIssue)
        {
            if (sonarQubeIssue.TextRange == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeIssue.TextRange));
            } 

            return new TaintIssue(
                sonarQubeIssue.IssueKey,
                sonarQubeIssue.FilePath,
                sonarQubeIssue.RuleId,
                sonarQubeIssue.Message,
                sonarQubeIssue.TextRange.StartLine,
                sonarQubeIssue.TextRange.EndLine,
                sonarQubeIssue.TextRange.StartOffset,
                sonarQubeIssue.TextRange.EndOffset,
                sonarQubeIssue.Hash,
                Convert(sonarQubeIssue.Severity),
                sonarQubeIssue.CreationTimestamp,
                sonarQubeIssue.LastUpdateTimestamp,
                Convert(sonarQubeIssue.Flows)
            );
        }

        private IReadOnlyList<IAnalysisIssueFlow> Convert(IEnumerable<IssueFlow> flows) =>
            flows.Select(x => new AnalysisIssueFlow(Convert(x.Locations))).ToArray();

        private IReadOnlyList<IAnalysisIssueLocation> Convert(IEnumerable<IssueLocation> locations) =>
            locations.Select(location =>
            {
                if (location.TextRange == null)
                {
                    throw new ArgumentNullException(nameof(location.TextRange));
                }

                return new AnalysisIssueLocation(location.Message,
                    location.FilePath,
                    location.TextRange.StartLine,
                    location.TextRange.EndLine,
                    location.TextRange.StartOffset,
                    location.TextRange.EndOffset,
                    null);
            }).ToArray();

        /// <summary>
        /// Converts from the sonarqube issue severity enum to the standard AnalysisIssueSeverity
        /// </summary>
        internal /* for testing */ static AnalysisIssueSeverity Convert(SonarQubeIssueSeverity issueSeverity)
        {
            switch (issueSeverity)
            {
                case SonarQubeIssueSeverity.Blocker:
                    return AnalysisIssueSeverity.Blocker;
                case SonarQubeIssueSeverity.Critical:
                    return AnalysisIssueSeverity.Critical;
                case SonarQubeIssueSeverity.Info:
                    return AnalysisIssueSeverity.Info;
                case SonarQubeIssueSeverity.Major:
                    return AnalysisIssueSeverity.Major;
                case SonarQubeIssueSeverity.Minor:
                    return AnalysisIssueSeverity.Minor;

                default:
                    throw new ArgumentOutOfRangeException(nameof(issueSeverity));
            }
        }
    }
}
