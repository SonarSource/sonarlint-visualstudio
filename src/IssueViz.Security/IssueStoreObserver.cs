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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security
{
    internal interface IIssueStoreObserver : IDisposable
    {
        /// <summary>
        /// Begins to observe the given collection if it was not already observed.
        /// Returns a disposable handle that unregisters the collection.
        /// </summary>
        IDisposable Register(ReadOnlyObservableCollection<IAnalysisIssueVisualization> issueVisualizations);
    }

    [Export(typeof(IIssueStoreObserver))]
    [Export(typeof(IIssueLocationStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class IssueStoreObserver : IIssueStoreObserver, IIssueLocationStore
    {
        private HashSet<ReadOnlyObservableCollection<IAnalysisIssueVisualization>> ObservableIssueVisualizations { get; } = new HashSet<ReadOnlyObservableCollection<IAnalysisIssueVisualization>>();

        IDisposable IIssueStoreObserver.Register(ReadOnlyObservableCollection<IAnalysisIssueVisualization> issueVisualizations)
        {
            if (issueVisualizations == null)
            {
                throw new ArgumentNullException(nameof(issueVisualizations));
            }

            var unregisterCallback = new ExecuteOnDispose(() =>
            {
                ObservableIssueVisualizations.Remove(issueVisualizations);
                ((INotifyCollectionChanged)issueVisualizations).CollectionChanged -= IssueVisualizations_CollectionChanged;
                NotifyIssuesChanged(issueVisualizations);
            });

            if (ObservableIssueVisualizations.Contains(issueVisualizations))
            {
                Debug.Assert(!ObservableIssueVisualizations.Contains(issueVisualizations), "Not expecting the collection to be registered twice");
                return unregisterCallback;
            }

            ObservableIssueVisualizations.Add(issueVisualizations);
            ((INotifyCollectionChanged)issueVisualizations).CollectionChanged += IssueVisualizations_CollectionChanged;
            NotifyIssuesChanged(issueVisualizations);

            return unregisterCallback;
        }

        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        IEnumerable<IAnalysisIssueLocationVisualization> IIssueLocationStore.GetLocations(string filePath)
        {
            var matchingLocations = ObservableIssueVisualizations
                .SelectMany(issueVizsCollection => issueVizsCollection)
                .SelectMany(issueViz => issueViz.GetAllLocations())
                .Where(locationViz => !string.IsNullOrEmpty(locationViz.CurrentFilePath) &&
                                      PathHelper.IsMatchingPath(locationViz.CurrentFilePath, filePath));

            return matchingLocations;
        }

        void IIssueLocationStore.Refresh(IEnumerable<string> affectedFilePaths)
        {
            // Implementation is not required:
            // This method was originally added in order to notify the error list sinks that the taggers changed the span of the issue visualizations.
            // The newer issue stores display their data in plain xaml lists, and the span changes are tracked via NotifyPropertyChanged mechanism in IAnalysisIssueVisualization.
        }

        bool IIssueLocationStore.Contains(IAnalysisIssueVisualization issueVisualization)
        {
            if (issueVisualization == null)
            {
                throw new ArgumentNullException(nameof(issueVisualization));
            }

            return ObservableIssueVisualizations
                .SelectMany(issueVizsCollection => issueVizsCollection)
                .Contains(issueVisualization);
        }

        private void IssueVisualizations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var addedItems = e.NewItems ?? Array.Empty<IAnalysisIssueVisualization>();
            var removedItems = e.OldItems ?? Array.Empty<IAnalysisIssueVisualization>();
            var changedItems = addedItems.Cast<IAnalysisIssueVisualization>().Union(removedItems.Cast<IAnalysisIssueVisualization>());

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
                IssuesChanged.Invoke(this, new IssuesChangedEventArgs(filePaths));
            }
        }

        public void Dispose()
        {
            foreach (var collection in ObservableIssueVisualizations)
            {
                ((INotifyCollectionChanged) collection).CollectionChanged -= IssueVisualizations_CollectionChanged;
            }

            ObservableIssueVisualizations.Clear();
        }
    }
}
