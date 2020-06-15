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
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    // Interface introduced to simplify testing.
    internal interface ISinkManagerRegister
    {
        void AddSinkManager(ISinkManager manager);
        void RemoveSinkManager(ISinkManager manager);
    }

    [Export(typeof(ISonarErrorListDataSource))]
    internal class SonarErrorListDataSource : ITableDataSource, ISonarErrorListDataSource, ISinkManagerRegister
    {
        private readonly ISet<ISinkManager> managers = new HashSet<ISinkManager>();
        private readonly ISet<ITableEntriesSnapshotFactory> factories = new HashSet<ITableEntriesSnapshotFactory>();

        [ImportingConstructor]
        internal SonarErrorListDataSource(ITableManagerProvider tableManagerProvider)
        {
            if (tableManagerProvider == null)
            {
                throw new ArgumentNullException(nameof(tableManagerProvider));
            }

            var errorTableManager = tableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
            errorTableManager.AddSource(this, StandardTableColumnDefinitions.DetailsExpander,
                                                   StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.ErrorCategory,
                                                   StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName,
                                                   StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column,
                                                   StandardTableColumnDefinitions.ProjectName);
        }

        #region ITableDataSource members

        public string DisplayName => "SonarLint";

        public string Identifier => "SonarLint";

        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

        // Note: Error List is the only expected subscriber
        public IDisposable Subscribe(ITableDataSink sink) => new SinkManager(this, sink);

        #endregion

        #region ISinkManagerRegister

        public void AddSinkManager(ISinkManager manager)
        {
            lock (managers)
            {
                managers.Add(manager);

                foreach (var factory in factories)
                {
                    manager.AddFactory(factory);
                }
            }
        }

        public void RemoveSinkManager(ISinkManager manager)
        {
            lock (managers)
            {
                managers.Remove(manager);
            }
        }

        #endregion

        #region ISonarErrorListDataSource implementation

        public void RefreshErrorList()
        {
            // Mark all the sinks as dirty (so, as a side-effect, they will start an update pass that will get the new snapshot
            // from the snapshot factories).
            lock (managers)
            {
                foreach (var manager in managers)
                {
                    manager.UpdateSink();
                }
            }
        }

        public void AddFactory(ITableEntriesSnapshotFactory factory)
        {
            lock (managers)
            {
                factories.Add(factory);
                foreach (var manager in managers)
                {
                    manager.AddFactory(factory);
                }
            }
        }

        public void RemoveFactory(ITableEntriesSnapshotFactory factory)
        {
            lock (managers)
            {
                factories.Remove(factory);
                foreach (var manager in managers)
                {
                    manager.RemoveFactory(factory);
                }
            }
        }

        #endregion ISonarErrorListDataSource implementation
    }
}
