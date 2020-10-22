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
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource
{
    internal interface IHotspotsTableDataSource : ITableDataSource, IDisposable
    {
    }

    [Export(typeof(IHotspotsTableDataSource))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class HotspotsTableDataSource : IHotspotsTableDataSource
    {
        private readonly ITableManager tableManager;
        private readonly ISet<ITableDataSink> sinks = new HashSet<ITableDataSink>();

        public string SourceTypeIdentifier { get; } = HotspotsTableConstants.TableSourceTypeIdentifier;
        public string Identifier { get; } = HotspotsTableConstants.TableIdentifier;
        public string DisplayName { get; } = HotspotsTableConstants.TableDisplayName;

        [ImportingConstructor]
        public HotspotsTableDataSource(ITableManagerProvider tableManagerProvider)
        {
            tableManager = tableManagerProvider.GetTableManager(HotspotsTableConstants.TableManagerIdentifier);
            tableManager.AddSource(this, HotspotsTableColumns.Names);
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            lock (sinks)
            {
                sinks.Add(sink);

                return new ExecuteOnDispose(() => Unsubscribe(sink));
            }
        }

        private void Unsubscribe(ITableDataSink sink)
        {
            lock (sinks)
            {
                sinks.Remove(sink);
            }
        }

        public void Dispose()
        {
            tableManager.RemoveSource(this);
        }
    }
}
