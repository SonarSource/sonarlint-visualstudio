﻿/*
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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api
{
    internal interface IHotspotToIssueVisualizationConverter
    {
        /// <summary>
        /// Converts given SQ hotspot into AnalysisIssueVisualization with IHotspot.
        /// </summary>
        /// <remarks>
        /// Attempts to resolve <see cref="SonarQubeHotspot.ComponentPath"/> into an absolute file path.
        /// If path cannot be resolved, <see cref="IHotspot.FilePath"/> will be null and <see cref="IHotspot.ServerFilePath"/> will contain the original SQ hotspot path.
        /// </remarks>
        IAnalysisIssueVisualization Convert(SonarQubeHotspot sonarQubeHotspot);
    }

    [Export(typeof(IHotspotToIssueVisualizationConverter))]
    internal class HotspotToIssueVisualizationConverter : IHotspotToIssueVisualizationConverter
    {
        private readonly IAnalysisIssueVisualizationConverter issueVisualizationConverter;
        private readonly IAbsoluteFilePathLocator absoluteFilePathLocator;

        [ImportingConstructor]
        public HotspotToIssueVisualizationConverter(IAnalysisIssueVisualizationConverter issueVisualizationConverter, IAbsoluteFilePathLocator absoluteFilePathLocator)
        {
            this.issueVisualizationConverter = issueVisualizationConverter;
            this.absoluteFilePathLocator = absoluteFilePathLocator;
        }

        public IAnalysisIssueVisualization Convert(SonarQubeHotspot sonarQubeHotspot)
        {
            var hotspot = ConvertToHotspot(sonarQubeHotspot);
            var issueViz = issueVisualizationConverter.Convert(hotspot);

            return issueViz;
        }

        private Hotspot ConvertToHotspot(SonarQubeHotspot sonarQubeHotspot)
        {
            var localFilePath = absoluteFilePathLocator.Locate(sonarQubeHotspot.FilePath);
            var priority = GetPriority(sonarQubeHotspot.Rule.VulnerabilityProbability);

            var sqRule = sonarQubeHotspot.Rule;
            var rule = new HotspotRule(
                sqRule.RuleKey,
                sqRule.RuleName,
                sqRule.SecurityCategory,
                priority,
                sqRule.RiskDescription,
                sqRule.VulnerabilityDescription,
                sqRule.FixRecommendations
                );

            var hotspot = new Hotspot(
                hotspotKey: sonarQubeHotspot.HotspotKey,
                serverFilePath: sonarQubeHotspot.FilePath,
                primaryLocation: new AnalysisIssueLocation(
                    filePath: localFilePath,
                    message: sonarQubeHotspot.Message,
                    textRange: new TextRange(
                        startLine: sonarQubeHotspot.TextRange.StartLine,
                        endLine: sonarQubeHotspot.TextRange.EndLine,
                        startLineOffset: sonarQubeHotspot.TextRange.StartOffset,
                        endLineOffset: sonarQubeHotspot.TextRange.EndOffset,
                        lineHash: sonarQubeHotspot.LineHash)),
                rule: rule,
                createTimestamp: sonarQubeHotspot.CreationTimestamp,
                lastUpdateTimestamp: sonarQubeHotspot.LastUpdateTimestamp,
                flows: null);

            return hotspot;
        }

        private HotspotPriority GetPriority(string vulnerabilityProbability)
        {
            if (vulnerabilityProbability == null)
            {
                throw new ArgumentNullException(nameof(vulnerabilityProbability));
            }

            switch (vulnerabilityProbability.ToLowerInvariant())
            {
                case "high":
                    return HotspotPriority.High;
                case "medium":
                    return HotspotPriority.Medium;
                case "low":
                    return HotspotPriority.Low;
                default:
                    throw new ArgumentOutOfRangeException(nameof(vulnerabilityProbability), vulnerabilityProbability, "Invalid hotspot probability");
            }
        }
    }
}
