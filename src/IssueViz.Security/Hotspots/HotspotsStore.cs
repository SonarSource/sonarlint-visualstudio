/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots
{
    internal interface IHotspotsStore : IIssuesStore
    {
        /// <summary>
        /// Adds a given visualization to the list if it does not already exist.
        /// Returns the given visualization, or the existing visualization with the same hotspot key.
        /// </summary>
        IAnalysisIssueVisualization GetOrAdd(IAnalysisIssueVisualization hotspotViz);

        void Remove(IAnalysisIssueVisualization hotspotViz);
    }

    [Export(typeof(IHotspotsStore))]
    [Export(typeof(IIssuesStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class HotspotsStore : IHotspotsStore
    {
        private readonly List<IAnalysisIssueVisualization> hotspots = new List<IAnalysisIssueVisualization>();

        public IReadOnlyCollection<IAnalysisIssueVisualization> GetAll() => hotspots;

        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        public IAnalysisIssueVisualization GetOrAdd(IAnalysisIssueVisualization hotspotViz)
        {
            var existingHotspot = FindExisting(hotspotViz);

            if (existingHotspot != null)
            {
                return existingHotspot;
            }

            hotspots.Add(hotspotViz);

            NotifyIssuesChanged(new IssuesChangedEventArgs(
                Array.Empty<IAnalysisIssueVisualization>(),
                new[] {hotspotViz}));

            return hotspotViz;
        }

        public void Remove(IAnalysisIssueVisualization hotspotViz)
        {
            hotspots.Remove(hotspotViz);

            NotifyIssuesChanged(new IssuesChangedEventArgs(
                new[] {hotspotViz},
                Array.Empty<IAnalysisIssueVisualization>()));
        }

        private IAnalysisIssueVisualization FindExisting(IAnalysisIssueVisualization hotspotViz)
        {
            var key = ((IHotspot)hotspotViz.Issue).HotspotKey;

            return hotspots.FirstOrDefault(x => ((IHotspot)x.Issue).HotspotKey == key);
        }

        private void NotifyIssuesChanged(IssuesChangedEventArgs args)
        {
            IssuesChanged?.Invoke(this, args);
        }
    }
}
