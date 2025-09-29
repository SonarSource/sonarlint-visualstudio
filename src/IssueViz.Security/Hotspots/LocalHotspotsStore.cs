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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots
{
    /// <summary>
    /// Methods for modification of <see cref="ILocalHotspotsStore"/>
    /// </summary>
    public interface ILocalHotspotsStoreUpdater
    {
        void UpdateForFile(string filePath, IEnumerable<IAnalysisIssueVisualization> hotspots);

        void RemoveForFile(string filePath);

        void Clear();
    }

    /// <summary>
    /// Represents the storage for locally analyzed hotspots and matching them against hotspots from the server
    /// </summary>
    internal interface ILocalHotspotsStore : ILocalHotspotsStoreUpdater, IIssuesStore
    {
        IReadOnlyCollection<LocalHotspot> GetAllLocalHotspots();
    }

    [Export(typeof(ILocalHotspotsStoreUpdater))]
    [Export(typeof(ILocalHotspotsStore))]
    [Export(typeof(IIssuesStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class LocalHotspotsStore : ILocalHotspotsStore
    {
        private static readonly List<IAnalysisIssueVisualization> EmptyList = [];

        private readonly object lockObject = new();
        private readonly IThreadHandling threadHandling;

        private readonly Dictionary<string, List<LocalHotspot>> fileToHotspotsMapping = [];

        [ImportingConstructor]
        public LocalHotspotsStore(IThreadHandling threadHandling)
        {
            this.threadHandling = threadHandling;
        }

        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        public IReadOnlyCollection<IAnalysisIssueVisualization> GetAll()
        {
            lock (lockObject)
            {
                return GetOpenHotspots().Select(x => x.Visualization).ToList();
            }
        }

        public IReadOnlyCollection<LocalHotspot> GetAllLocalHotspots()
        {
            lock (lockObject)
            {
                return GetOpenHotspots().ToList();
            }
        }

        public void UpdateForFile(string filePath, IEnumerable<IAnalysisIssueVisualization> hotspots)
        {
            List<IAnalysisIssueVisualization> oldIssueVisualizations;
            var hotspotsList = hotspots.ToList();
            lock (lockObject)
            {
                if (fileToHotspotsMapping.TryGetValue(filePath, out var oldHotspots))
                {
                    oldIssueVisualizations = oldHotspots
                        .Select(x => x.Visualization)
                        .ToList();
                }
                else
                {
                    oldIssueVisualizations = EmptyList;
                }

                if (!hotspotsList.Any() && !oldIssueVisualizations.Any())
                {
                    return;
                }

                fileToHotspotsMapping[filePath] = hotspotsList.Select(LocalHotspot.ToLocalHotspot).ToList();
            }
            NotifyIssuesChanged(new IssuesChangedEventArgs(oldIssueVisualizations, hotspotsList));
        }

        public void RemoveForFile(string filePath)
        {
            threadHandling.ThrowIfOnUIThread();

            List<LocalHotspot> localHotspots;
            lock (lockObject)
            {
                if (!fileToHotspotsMapping.TryGetValue(filePath, out localHotspots))
                {
                    return;
                }

                fileToHotspotsMapping.Remove(filePath);
            }
            NotifyIssuesChanged(new IssuesChangedEventArgs(localHotspots.Select(x => x.Visualization).ToList(), EmptyList));
        }

        public void Clear()
        {
            threadHandling.ThrowIfOnUIThread();

            IssuesChangedEventArgs removedIssues;
            lock (lockObject)
            {
                removedIssues = new IssuesChangedEventArgs(fileToHotspotsMapping
                        .SelectMany(x =>
                            x.Value.Select(y => y.Visualization))
                        .ToList(),
                    EmptyList);

                fileToHotspotsMapping.Clear();
            }
            NotifyIssuesChanged(removedIssues);
        }

        private IEnumerable<LocalHotspot> GetOpenHotspots() => fileToHotspotsMapping.SelectMany(kvp => kvp.Value).Where(hs => !hs.Visualization.IsResolved);

        private void NotifyIssuesChanged(IssuesChangedEventArgs args)
        {
            IssuesChanged?.Invoke(this, args);
        }
    }
}
