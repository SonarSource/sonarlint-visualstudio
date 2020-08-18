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

        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        public IAnalysisIssueVisualization SelectedIssue
        {
            get => selectedIssue;
            set
            {
                selectedIssue = value;
                selectedFlow = GetFirstFlowOrDefault();
                selectedLocation = GetFirstLocationOrDefault();

                RaiseSelectionChanged(SelectionChangeLevel.Issue);
            }
        }

        public IAnalysisIssueFlowVisualization SelectedFlow
        {
            get => selectedFlow;
            set
            {
                selectedFlow = value;
                selectedLocation = GetFirstLocationOrDefault();

                RaiseSelectionChanged(SelectionChangeLevel.Flow);
            }
        }

        public IAnalysisIssueLocationVisualization SelectedLocation
        {
            get => selectedLocation;
            set
            {
                selectedLocation = value;

                RaiseSelectionChanged(SelectionChangeLevel.Location);
            }
        }

        private void RaiseSelectionChanged(SelectionChangeLevel changeLevel)
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(changeLevel, SelectedIssue, SelectedFlow, SelectedLocation));
        }

        private IAnalysisIssueFlowVisualization GetFirstFlowOrDefault()
        {
            return selectedIssue?.Flows?.FirstOrDefault();
        }

        private IAnalysisIssueLocationVisualization GetFirstLocationOrDefault()
        {
            return selectedFlow?.Locations?.FirstOrDefault();
        }

        public void Dispose()
        {
            SelectionChanged = null;
        }
    }
}
