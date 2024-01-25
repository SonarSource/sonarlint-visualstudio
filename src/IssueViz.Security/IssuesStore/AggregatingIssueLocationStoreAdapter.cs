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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore
{
    /// <summary>
    /// MEF-imports <see cref="IIssuesStore"/> and implements <see cref="IIssueLocationStore"/> on top of the given stores.
    /// </summary>
    [Export(typeof(IIssueLocationStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class AggregatingIssueLocationStoreAdapter : IIssueLocationStore, IDisposable
    {
        private readonly IEnumerable<IIssuesStore> issueStores;

        [ImportingConstructor]
        public AggregatingIssueLocationStoreAdapter([ImportMany] IEnumerable<IIssuesStore> issueStores)
        {
            this.issueStores = issueStores;

            foreach (var store in issueStores)
            {
                store.IssuesChanged += Store_IssuesChanged;
            }
        }

        public event EventHandler<Editor.LocationTagging.IssuesChangedEventArgs> IssuesChanged;

        public IEnumerable<IAnalysisIssueLocationVisualization> GetLocations(string filePath)
        {
            var matchingLocations = issueStores
                .SelectMany(store => store.GetAll())
                .SelectMany(issueViz => issueViz.GetAllLocations())
                .Where(locationViz => !string.IsNullOrEmpty(locationViz.CurrentFilePath) &&
                                      PathHelper.IsMatchingPath(locationViz.CurrentFilePath, filePath));

            return matchingLocations;
        }

        public IEnumerable<IAnalysisIssueVisualization> GetIssues()
        {
            return issueStores.SelectMany(x => x.GetAll());
        }

        public void RefreshOnBufferChanged(string affectedFilePath)
        {
            // Implementation is not required:
            // The assumption is that the set of issues in the aggregated stores (taint and hotspot) is not affected
            // by changes in the buffer. The taggers will already have updated the spans in the IAnalysisIssueVisualization,
            // and those changes will have been propagated to the taint and hotspot UI lists via NotifyPropertyChanged mechanism

            // NOTE: we're making assumptions about all of the implementations of IIssueStore.
            // A better approach would be to propagate the call and allow individual store implementations to ignore
            // it if appropriate.
        }

        public void Refresh(IEnumerable<string> affectedFilePaths)
        {
            // Currently, this is called when the set of suppressed issues has changed.
            // We don't support suppressions for Taint or Hotspots yet.

            // NOTE: same comment as RefreshOnBufferChanged - we're making assumptions about the implementations of IIssueStore.
        }

        public bool Contains(IAnalysisIssueVisualization issueVisualization)
        {
            if (issueVisualization == null)
            {
                throw new ArgumentNullException(nameof(issueVisualization));
            }

            return issueStores
                .SelectMany(store => store.GetAll())
                .Contains(issueVisualization);
        }

        private void Store_IssuesChanged(object sender, IssuesChangedEventArgs e)
        {
            var changedItems = e.RemovedIssues.Union(e.AddedIssues);

            NotifyIssuesChanged(changedItems);
        }

        private void NotifyIssuesChanged(IEnumerable<IAnalysisIssueVisualization> changedIssueVizs)
        {
            if (IssuesChanged == null)
            {
                return;
            }

            var filePaths = changedIssueVizs
                .SelectMany(issueViz =>
                    issueViz.GetAllLocations()
                        .Select(x => x.CurrentFilePath)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase));

            if (filePaths.Any())
            {
                IssuesChanged.Invoke(this, new Editor.LocationTagging.IssuesChangedEventArgs(filePaths));
            }
        }

        public void Dispose()
        {
            foreach (var store in issueStores)
            {
                store.IssuesChanged -= Store_IssuesChanged;
            }
        }
    }
}
