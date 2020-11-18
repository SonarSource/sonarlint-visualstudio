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
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Store
{
    internal interface IHotspotsStore
    {
        ReadOnlyObservableCollection<IAnalysisIssueVisualization> GetAll();

        void Add(IAnalysisIssueVisualization hotspot);
    }

    [Export(typeof(IHotspotsStore))]
    [Export(typeof(IIssueLocationStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class HotspotsStore : IHotspotsStore, IIssueLocationStore
    {
        private ObservableCollection<IAnalysisIssueVisualization> Hotspots { get; } = new ObservableCollection<IAnalysisIssueVisualization>();

        public void Add(IAnalysisIssueVisualization hotspot)
        {
            Hotspots.Add(hotspot);

            var hotspotFilePaths = hotspot
                .GetAllLocations()
                .Select(x => x.CurrentFilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            IssuesChanged?.Invoke(this, new IssuesChangedEventArgs(hotspotFilePaths));
        }

        public ReadOnlyObservableCollection<IAnalysisIssueVisualization> GetAll()
        {
            return new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(Hotspots);
        }

        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        public IEnumerable<IAnalysisIssueLocationVisualization> GetLocations(string filePath)
        {
            var matchingLocations = Hotspots
                .SelectMany(hotspotViz => hotspotViz.GetAllLocations())
                .Where(locationViz => PathHelper.IsMatchingPath(locationViz.CurrentFilePath, filePath));

            return matchingLocations;
        }

        public void Refresh(IEnumerable<string> affectedFilePaths)
        {
        }
    }
}
