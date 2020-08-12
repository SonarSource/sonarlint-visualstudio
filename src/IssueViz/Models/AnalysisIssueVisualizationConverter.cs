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
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Models
{
    internal interface IAnalysisIssueVisualizationConverter
    {
        IAnalysisIssueVisualization Convert(IAnalysisIssue issue);

    }

    [Export(typeof(IAnalysisIssueVisualizationConverter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AnalysisIssueVisualizationConverter : IAnalysisIssueVisualizationConverter
    {
        private static readonly IReadOnlyList<IAnalysisIssueFlowVisualization> EmptyConvertedFlows = Array.Empty<IAnalysisIssueFlowVisualization>();

        private readonly ILocationNavigationChecker navigationChecker;

        [ImportingConstructor]
        public AnalysisIssueVisualizationConverter(ILocationNavigationChecker navigationChecker)
        {
            this.navigationChecker = navigationChecker;
        }

        public IAnalysisIssueVisualization Convert(IAnalysisIssue issue)
        {
            var flows = Convert(issue.Flows);

            return new AnalysisIssueVisualization(flows, issue);
        }

        private IReadOnlyList<IAnalysisIssueFlowVisualization> Convert(IEnumerable<IAnalysisIssueFlow> flows)
        {
            if (!flows.Any())
            {
                return EmptyConvertedFlows;
            }

            var flowNumber = 1;

            var convertedFlows = flows
                .Select(x => new AnalysisIssueFlowVisualization(flowNumber++, Convert(x.Locations), x))
                .Cast<IAnalysisIssueFlowVisualization>()
                .ToArray();

            return convertedFlows;
        }

        private IReadOnlyList<IAnalysisIssueLocationVisualization> Convert(IEnumerable<IAnalysisIssueLocation> locations)
        {
            var locationNumber = 1;

            var convertedLocations = locations
                .Select(location =>
                {
                    var isNavigable = navigationChecker.IsNavigable(location);

                    return new AnalysisIssueLocationVisualization(locationNumber++, isNavigable, location);
                })
                .Cast<IAnalysisIssueLocationVisualization>()
                .ToArray();

            return convertedLocations;
        }
    }
}
