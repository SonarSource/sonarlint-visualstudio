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

        public void Refresh(IEnumerable<string> affectedFilePaths)
        {
            // Implementation is not required:
            // This method was originally added in order to notify the error list sinks that the taggers changed the span of the issue visualizations.
            // The newer issue stores display their data in plain xaml lists, and the span changes are tracked via NotifyPropertyChanged mechanism in IAnalysisIssueVisualization.
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
