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
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Selection
{
    [Export(typeof(IAnalysisIssueSelectionService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class AnalysisIssueSelectionService : IAnalysisIssueSelectionService
    {
        private IAnalysisIssueVisualization selectedIssue;
        private IAnalysisIssueFlowVisualization selectedFlow;
        private IAnalysisIssueLocationVisualization selectedLocation;

        public event EventHandler<IssueChangedEventArgs> SelectedIssueChanged;
        public event EventHandler<FlowChangedEventArgs> SelectedFlowChanged;
        public event EventHandler<LocationChangedEventArgs> SelectedLocationChanged;

        public IAnalysisIssueVisualization SelectedIssue
        {
            get => selectedIssue;
            set
            {
                selectedIssue = value;
                SelectedIssueChanged?.Invoke(this, new IssueChangedEventArgs(value));

                SelectedFlow = selectedIssue?.Flows?.FirstOrDefault();
            }
        }

        public IAnalysisIssueFlowVisualization SelectedFlow
        {
            get => selectedFlow;
            set
            {
                selectedFlow = value;
                SelectedFlowChanged?.Invoke(this, new FlowChangedEventArgs(value));

                SelectedLocation = selectedFlow?.Locations?.FirstOrDefault();
            }
        }

        public IAnalysisIssueLocationVisualization SelectedLocation
        {
            get => selectedLocation;
            set
            {
                selectedLocation = value;
                SelectedLocationChanged?.Invoke(this, new LocationChangedEventArgs(value));
            }
        }

        public void Dispose()
        {
            SelectedIssueChanged = null;
            SelectedFlowChanged = null;
            SelectedLocationChanged = null;
        }
    }
}
