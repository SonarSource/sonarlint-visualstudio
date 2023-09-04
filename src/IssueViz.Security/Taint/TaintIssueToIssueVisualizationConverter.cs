/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarQube.Client.Models.ServerSentEvents.ClientContract;
using ITaintIssue = SonarQube.Client.Models.ServerSentEvents.ClientContract.ITaintIssue;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{
    internal interface ITaintIssueToIssueVisualizationConverter
    {
        IAnalysisIssueVisualization Convert(SonarQubeIssue sonarQubeIssue);

        IAnalysisIssueVisualization Convert(ITaintIssue sonarQubeTaintIssue);
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
            var issueViz = CreateAnalysisIssueVisualization(analysisIssue);
            issueViz.IsSuppressed = sonarQubeIssue.IsResolved;

            return issueViz;
        }

        public IAnalysisIssueVisualization Convert(ITaintIssue sonarQubeTaintIssue)
        {
            var analysisIssue = ConvertToAnalysisIssue(sonarQubeTaintIssue);

            return CreateAnalysisIssueVisualization(analysisIssue);
        }

        private IAnalysisIssueVisualization CreateAnalysisIssueVisualization(IAnalysisIssueBase analysisIssue)
        {
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

        private static IAnalysisIssueBase ConvertToAnalysisIssue(SonarQubeIssue sonarQubeIssue)
        {
            if (sonarQubeIssue.TextRange == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeIssue.TextRange));
            }

            return new TaintIssue(
                sonarQubeIssue.IssueKey,
                sonarQubeIssue.RuleId,
                primaryLocation: new AnalysisIssueLocation(
                    sonarQubeIssue.Message,
                    sonarQubeIssue.FilePath,
                    textRange: new TextRange(
                        sonarQubeIssue.TextRange.StartLine,
                        sonarQubeIssue.TextRange.EndLine,
                        sonarQubeIssue.TextRange.StartOffset,
                        sonarQubeIssue.TextRange.EndOffset,
                        sonarQubeIssue.Hash)),
                Convert(sonarQubeIssue.Severity),
                ConvertToHighestSeverity(sonarQubeIssue.DefaultImpacts),
                sonarQubeIssue.CreationTimestamp,
                sonarQubeIssue.LastUpdateTimestamp,
                Convert(sonarQubeIssue.Flows),
                sonarQubeIssue.Context
            );
        }

        private static IAnalysisIssueBase ConvertToAnalysisIssue(ITaintIssue sonarQubeTaintIssue)
        {
            return new TaintIssue(
                sonarQubeTaintIssue.Key,
                sonarQubeTaintIssue.RuleKey,
                primaryLocation: new AnalysisIssueLocation(
                    sonarQubeTaintIssue.MainLocation.Message,
                    sonarQubeTaintIssue.MainLocation.FilePath,
                    textRange: new TextRange(
                        sonarQubeTaintIssue.MainLocation.TextRange.StartLine,
                        sonarQubeTaintIssue.MainLocation.TextRange.EndLine,
                        sonarQubeTaintIssue.MainLocation.TextRange.StartLineOffset,
                        sonarQubeTaintIssue.MainLocation.TextRange.EndLineOffset,
                        sonarQubeTaintIssue.MainLocation.TextRange.Hash)),
                Convert(sonarQubeTaintIssue.Severity),
                null, // todo: add after implemented in SonarQubeService
                sonarQubeTaintIssue.CreationDate,
                default,
                Convert(sonarQubeTaintIssue.Flows),
                sonarQubeTaintIssue.Context
            );
        }

        private static IReadOnlyList<IAnalysisIssueFlow> Convert(IEnumerable<IssueFlow> flows) =>
            flows.Select(x => new AnalysisIssueFlow(Convert(x.Locations))).ToArray();

        private static IReadOnlyList<IAnalysisIssueFlow> Convert(IEnumerable<IFlow> flows) =>
            flows.Select(x => new AnalysisIssueFlow(Convert(x.Locations))).ToArray();

        private static IReadOnlyList<IAnalysisIssueLocation> Convert(IEnumerable<IssueLocation> locations) =>
            locations.Reverse().Select(location =>
            {
                if (location.TextRange == null)
                {
                    throw new ArgumentNullException(nameof(location.TextRange));
                }

                return new AnalysisIssueLocation(location.Message,
                    location.FilePath,
                    textRange: new TextRange(
                        location.TextRange.StartLine,
                        location.TextRange.EndLine,
                        location.TextRange.StartOffset,
                        location.TextRange.EndOffset,
                        null));
            }).ToArray();

        private static IReadOnlyList<IAnalysisIssueLocation> Convert(IEnumerable<ILocation> locations) =>
            locations.Reverse().Select(location =>
            {
                if (location.TextRange == null)
                {
                    throw new ArgumentNullException(nameof(location.TextRange));
                }

                return new AnalysisIssueLocation(location.Message,
                    location.FilePath,
                    textRange: new TextRange(
                        location.TextRange.StartLine,
                        location.TextRange.EndLine,
                        location.TextRange.StartLineOffset,
                        location.TextRange.EndLineOffset,
                        location.TextRange.Hash));
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

        internal /* for testing */ static SoftwareQualitySeverity? ConvertToHighestSeverity(
            Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity> sonarQubeSoftwareQualitySeverities)
        {
            if (sonarQubeSoftwareQualitySeverities == null || sonarQubeSoftwareQualitySeverities.Count == 0)
            {
                return null;
            }

            return sonarQubeSoftwareQualitySeverities
                .Select(kvp => kvp.Value)
                .Select(sqSeverity =>
                {
                    switch (sqSeverity)
                    {
                        case SonarQubeSoftwareQualitySeverity.Low:
                            return SoftwareQualitySeverity.Low;
                        case SonarQubeSoftwareQualitySeverity.Medium:
                            return SoftwareQualitySeverity.Medium;
                        case SonarQubeSoftwareQualitySeverity.High:
                            return SoftwareQualitySeverity.High;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(sqSeverity));
                    }
                })
                .Max();
        }
    }
}
