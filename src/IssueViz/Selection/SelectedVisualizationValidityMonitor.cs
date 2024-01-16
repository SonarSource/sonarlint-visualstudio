/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;

namespace SonarLint.VisualStudio.IssueVisualization.Selection
{
    /// <summary>
    /// Monitors changes to issue visualizations in <see cref="IIssueLocationStoreAggregator"/> and
    /// clears the selection in <see cref="IAnalysisIssueSelectionService"/> when the selected visualization no longer exists  
    /// </summary>
    internal interface ISelectedVisualizationValidityMonitor : IDisposable
    {
    }

    [Export(typeof(ISelectedVisualizationValidityMonitor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SelectedVisualizationValidityMonitor : ISelectedVisualizationValidityMonitor
    {
        private readonly IIssueSelectionService selectionService;
        private readonly IIssueLocationStoreAggregator locationStoreAggregator;

        [ImportingConstructor]
        public SelectedVisualizationValidityMonitor(IIssueSelectionService selectionService, IIssueLocationStoreAggregator locationStoreAggregator)
        {
            this.selectionService = selectionService;
            this.locationStoreAggregator = locationStoreAggregator;

            locationStoreAggregator.IssuesChanged += LocationStoreAggregator_IssuesChanged;
        }

        private void LocationStoreAggregator_IssuesChanged(object sender, IssuesChangedEventArgs e)
        {
            if (selectionService.SelectedIssue == null)
            {
                return;
            }

            var issuesChangedInSelectedFile = e.AnalyzedFiles.Any(x => PathHelper.IsMatchingPath(x, selectionService.SelectedIssue.CurrentFilePath));

            if (issuesChangedInSelectedFile)
            {
                var selectedIssueNoLongerExists = !locationStoreAggregator.Contains(selectionService.SelectedIssue);

                if (selectedIssueNoLongerExists)
                {
                    selectionService.SelectedIssue = null;
                }
            }
        }

        public void Dispose()
        {
            locationStoreAggregator.IssuesChanged -= LocationStoreAggregator_IssuesChanged;
        }
    }
}
