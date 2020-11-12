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
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource
{
    internal interface IHotspotsStore
    {
        void Add(IAnalysisIssueVisualization hotspot);
        void Remove(IAnalysisIssueVisualization hotspot);
    }

    [Export(typeof(IHotspotsStore))]
    [Export(typeof(IIssueLocationStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class HotspotsTableDataSource : IHotspotsStore, IIssueLocationStore, ITableDataSource, IDisposable
    {
        private readonly IHotspotTableEntryFactory tableEntryFactory;
        private readonly ITableManager tableManager;
        private readonly ISet<ITableDataSink> sinks = new HashSet<ITableDataSink>();
        private readonly List<ITableEntry> tableEntries = new List<ITableEntry>();

        public string SourceTypeIdentifier { get; } = HotspotsTableConstants.TableSourceTypeIdentifier;
        public string Identifier { get; } = HotspotsTableConstants.TableIdentifier;
        public string DisplayName { get; } = HotspotsTableConstants.TableDisplayName;

        [ImportingConstructor]
        public HotspotsTableDataSource(ITableManagerProvider tableManagerProvider, IHotspotTableEntryFactory tableEntryFactory)
        {
            this.tableEntryFactory = tableEntryFactory;
            tableManager = tableManagerProvider.GetTableManager(HotspotsTableConstants.TableManagerIdentifier);
            tableManager.AddSource(this, HotspotsTableColumns.Names);
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            sink.AddEntries(tableEntries, true);

            lock (sinks)
            {
                sinks.Add(sink);

                return new ExecuteOnDispose(() => Unsubscribe(sink));
            }
        }

        private void Unsubscribe(ITableDataSink sink)
        {
            sink.RemoveAllEntries();

            lock (sinks)
            {
                sinks.Remove(sink);
            }
        }

        public void Dispose()
        {
            tableManager.RemoveSource(this);
        }

        public void Add(IAnalysisIssueVisualization hotspot)
        {
            var entry = tableEntryFactory.Create(hotspot);
            tableEntries.Add(entry);

            lock (sinks)
            {
                foreach (var sink in sinks)
                {
                    sink.AddEntries(new[] {entry});
                }
            }

            NotifyHotspotFilesChanged(hotspot);
        }

        public void Remove(IAnalysisIssueVisualization hotspot)
        {
            var entries = tableEntries.Where(x => x.Identity == hotspot).ToArray();

            if (entries.Length == 0)
            {
                return;
            }

            tableEntries.RemoveAll(x=> x.Identity == hotspot);

            lock (sinks)
            {
                foreach (var sink in sinks)
                {
                    sink.RemoveEntries(entries);
                }
            }

            NotifyHotspotFilesChanged(hotspot);
        }

        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        public IEnumerable<IAnalysisIssueLocationVisualization> GetLocations(string filePath)
        {
            var matchingLocations = tableEntries
                .Select(entry => (IAnalysisIssueVisualization) entry.Identity)
                .SelectMany(hotspotViz => hotspotViz.GetAllLocations())
                .Where(locationViz => PathHelper.IsMatchingPath(locationViz.CurrentFilePath, filePath));

            return matchingLocations;
        }

        public void Refresh(IEnumerable<string> affectedFilePaths)
        {
            var changedEntries = tableEntries
                .Where(entry => affectedFilePaths.Any(p =>
                    PathHelper.IsMatchingPath(p, ((IAnalysisIssueVisualization) entry.Identity).CurrentFilePath)))
                .ToList();

            if (changedEntries.Count == 0)
            {
                return;
            }

            lock (sinks)
            {
                foreach (var sink in sinks)
                {
                    sink.ReplaceEntries(changedEntries, changedEntries);
                }
            }
        }

        private void NotifyHotspotFilesChanged(IAnalysisIssueVisualization hotspot)
        {
            var hotspotFilePaths = hotspot
                .GetAllLocations()
                .Select(x => x.CurrentFilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            IssuesChanged?.Invoke(this, new IssuesChangedEventArgs(hotspotFilePaths));
        }
    }
}
