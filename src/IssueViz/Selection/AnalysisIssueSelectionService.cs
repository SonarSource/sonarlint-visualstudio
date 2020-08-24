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
        public IAnalysisIssueVisualization SelectedIssue { get; private set; }
        public IAnalysisIssueFlowVisualization SelectedFlow { get; private set; }
        public IAnalysisIssueLocationVisualization SelectedLocation { get; private set; }

        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        public void Select(IAnalysisIssueVisualization issueVisualization)
        {
            SelectedIssue = issueVisualization;
            SelectedFlow = GetFirstFlowOrDefault();
            SelectedLocation = GetFirstLocationOrDefault();

            RaiseSelectionChanged(SelectionChangeLevel.Issue);
        }

        public void Select(IAnalysisIssueFlowVisualization flowVisualization)
        {
            SelectedFlow = flowVisualization;
            SelectedLocation = GetFirstLocationOrDefault();

            RaiseSelectionChanged(SelectionChangeLevel.Flow);
        }

        public void Select(IAnalysisIssueLocationVisualization locationVisualization)
        {
            SelectedLocation = locationVisualization;

            RaiseSelectionChanged(SelectionChangeLevel.Location);
        }

        private void RaiseSelectionChanged(SelectionChangeLevel changeLevel)
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(changeLevel, SelectedIssue, SelectedFlow, SelectedLocation));
        }

        private IAnalysisIssueFlowVisualization GetFirstFlowOrDefault()
        {
            return SelectedIssue?.Flows?.FirstOrDefault();
        }

        private IAnalysisIssueLocationVisualization GetFirstLocationOrDefault()
        {
            return SelectedFlow?.Locations?.FirstOrDefault();
        }

        public void Dispose()
        {
            SelectionChanged = null;
        }
    }
}
