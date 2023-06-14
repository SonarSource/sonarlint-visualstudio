/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots
{
    /// <summary>
    /// Methods for modification of <see cref="ILocalHotspotsStore"/>
    /// </summary>
    public interface ILocalHotspotsStoreUpdater
    {
        void AddOrUpdate(string filePath, IEnumerable<IAnalysisIssueVisualization> hotspots);
        void RemoveForFile(string filePath);
    }

    /// <summary>
    /// Represents the storage for locally analyzed hotspots and matching them against hotspots from the server
    /// </summary>
    internal interface ILocalHotspotsStore : ILocalHotspotsStoreUpdater, IIssuesStore
    {
        List<LocalHotspot> GetAllLocalHotspots();
    }
    
    [Export(typeof(ILocalHotspotsStoreUpdater))]
    [Export(typeof(ILocalHotspotsStore))]
    [Export(typeof(IIssuesStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class LocalHotspotsStore : ILocalHotspotsStore
    {
        private static readonly List<IAnalysisIssueVisualization> EmptyList = new List<IAnalysisIssueVisualization>();

        private readonly object lockObject = new object();
        private readonly IHotspotMatcher hotspotMatcher;
        private readonly IThreadHandling threadHandling;
        private readonly IServerHotspotStore serverHotspotStore;
        private Dictionary<string, List<LocalHotspot>> fileToHotspotsMapping =
            new Dictionary<string, List<LocalHotspot>>();

        private List<SonarQubeHotspot> serverHotspots = new List<SonarQubeHotspot>();
        private HashSet<SonarQubeHotspot> unmatchedHotspots = new HashSet<SonarQubeHotspot>();

        [ImportingConstructor]
        public LocalHotspotsStore(IServerHotspotStore serverHotspotStore, IHotspotMatcher hotspotMatcher, IThreadHandling threadHandling)
        {
            this.serverHotspotStore = serverHotspotStore;
            this.hotspotMatcher = hotspotMatcher;
            this.threadHandling = threadHandling;
            InitializeServerHotspots(this.serverHotspotStore.GetAll());
            serverHotspotStore.Refreshed += (sender, args) => RematchAll();
        }

        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        public IReadOnlyCollection<IAnalysisIssueVisualization> GetAll()
        {
            threadHandling.ThrowIfOnUIThread();
            
            lock (lockObject)
            {
                return FlattenMapping().Select(x => x.Visualization).ToList();
            }
        }

        public List<LocalHotspot> GetAllLocalHotspots()
        {
            threadHandling.ThrowIfOnUIThread();

            lock (lockObject)
            {
                return FlattenMapping().ToList();
            }
        }

        public void AddOrUpdate(string filePath, IEnumerable<IAnalysisIssueVisualization> hotspots)
        {
            threadHandling.ThrowIfOnUIThread();

            lock (lockObject)
            {
                List<IAnalysisIssueVisualization> oldIssueVisualizations;
                if (fileToHotspotsMapping.TryGetValue(filePath, out var oldHotspots))
                {
                    // possible optimization point. it's unlikely we need to re-match every single hotspot
                    UnmatchServerHotspots(oldHotspots);
                    oldIssueVisualizations = oldHotspots
                        .Select(x => x.Visualization)
                        .ToList();
                }
                else
                {
                    oldIssueVisualizations = EmptyList;
                }

                var hotspotsList = hotspots.ToList();
                
                if (!hotspotsList.Any() && !oldIssueVisualizations.Any())
                {
                    return;
                }

                fileToHotspotsMapping[filePath] = GetLocalHotspots(hotspotsList);

                NotifyIssuesChanged(new IssuesChangedEventArgs(oldIssueVisualizations, hotspotsList));
            }
        }

        public void RemoveForFile(string filePath)
        {
            threadHandling.ThrowIfOnUIThread();

            lock (lockObject)
            {
                if (!fileToHotspotsMapping.TryGetValue(filePath, out var localHotspots))
                {
                    return;
                }
                
                fileToHotspotsMapping.Remove(filePath);
                UnmatchServerHotspots(localHotspots);

                NotifyIssuesChanged(new IssuesChangedEventArgs(localHotspots.Select(x => x.Visualization).ToList(),
                    EmptyList));
            }
        }

        private List<LocalHotspot> GetLocalHotspots(IEnumerable<IAnalysisIssueVisualization> hotspots)
        {
            return hotspots.Select(visualization => MatchAndConvert(visualization)).ToList();
        }

        private void RematchAll()
        {
            lock (lockObject)
            {
                InitializeServerHotspots(serverHotspotStore.GetAll());

                fileToHotspotsMapping = fileToHotspotsMapping.ToDictionary(kvp => kvp.Key,
                    kvp => GetLocalHotspots(kvp.Value.Select(localHotspot => localHotspot.Visualization)));

                var visualizations = GetAll();
                
                if (!visualizations.Any())
                {
                    return;
                }
                
                NotifyIssuesChanged(new IssuesChangedEventArgs(visualizations, visualizations));
            }
        }

        private void InitializeServerHotspots(IList<SonarQubeHotspot> sonarQubeHotspots)
        {
            serverHotspots = sonarQubeHotspots?.ToList() ?? new List<SonarQubeHotspot>();
            unmatchedHotspots = serverHotspots.ToHashSet();
        }

        private void UnmatchServerHotspots(List<LocalHotspot> oldHotspots)
        {
            foreach (var localHotspot in oldHotspots.Where(x => x.ServerHotspot != null))
            {
                unmatchedHotspots.Add(localHotspot.ServerHotspot);
            }
        }

        private LocalHotspot MatchAndConvert(IAnalysisIssueVisualization visualization)
        {
            foreach (var serverHotspot in unmatchedHotspots)
            {
                if (!hotspotMatcher.IsMatch(visualization, serverHotspot))
                {
                    continue;
                }

                unmatchedHotspots.Remove(serverHotspot);
                return new LocalHotspot(visualization, serverHotspot);
            }

            return new LocalHotspot(visualization);
        }

        private IEnumerable<LocalHotspot> FlattenMapping() => 
            fileToHotspotsMapping.SelectMany(kvp => kvp.Value);

        private void NotifyIssuesChanged(IssuesChangedEventArgs args)
        {
            IssuesChanged?.Invoke(this, args);
        }
    }
}
