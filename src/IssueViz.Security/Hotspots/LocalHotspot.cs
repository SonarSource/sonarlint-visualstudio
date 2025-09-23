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
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots
{
    internal class LocalHotspot
    {
        // on the off chance we can't map the RuleId to Priority, which shouldn't happen, it's better to raise it as High
        private static readonly HotspotPriority DefaultPriority = HotspotPriority.High;

        /// <param name="visualization">Locally analyzed hotspot visualization</param>
        /// <param name="priority">Hotspot review priority</param>
        /// <param name="hotspotStatus">Hotspot review status</param>
        /// <exception cref="ArgumentNullException">Visualization can't be null</exception>
        public LocalHotspot(IAnalysisIssueVisualization visualization, HotspotPriority priority, HotspotStatus hotspotStatus)
        {
            Visualization = visualization ?? throw new ArgumentNullException(nameof(visualization));
            Priority = priority;
            HotspotStatus = hotspotStatus;
        }

        public IAnalysisIssueVisualization Visualization { get; }
        public HotspotPriority Priority { get; }
        public HotspotStatus HotspotStatus { get; }

        public static LocalHotspot ToLocalHotspot(IAnalysisIssueVisualization analysisIssueVisualization) =>
            new(analysisIssueVisualization, GetPriority(analysisIssueVisualization), ((IAnalysisHotspotIssue)analysisIssueVisualization.Issue).HotspotStatus);

        private static HotspotPriority GetPriority(IAnalysisIssueVisualization visualization)
        {
            var mappedHotspotPriority = (visualization.Issue as IAnalysisHotspotIssue)?.HotspotPriority;

            return mappedHotspotPriority ?? DefaultPriority;
        }
    }
}
