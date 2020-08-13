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

using System.Collections.Generic;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Models
{
    internal interface IAnalysisIssueFlowVisualization
    {
        int FlowNumber { get; }

        IReadOnlyList<IAnalysisIssueLocationVisualization> Locations { get; }

        IAnalysisIssueFlow Flow { get; }
    }

    internal class AnalysisIssueFlowVisualization : IAnalysisIssueFlowVisualization
    {
        public AnalysisIssueFlowVisualization(int flowNumber, IReadOnlyList<IAnalysisIssueLocationVisualization> locations, IAnalysisIssueFlow flow)
        {
            FlowNumber = flowNumber;
            Locations = locations;
            Flow = flow;
        }

        public int FlowNumber { get; }
        public IReadOnlyList<IAnalysisIssueLocationVisualization> Locations { get; }
        public IAnalysisIssueFlow Flow { get; }
    }
}
