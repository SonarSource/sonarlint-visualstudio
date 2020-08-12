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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(ISonarErrorListDataSource))]
    internal class SonarErrorListDataSource : ITableDataSource, ISonarErrorListDataSource
    {
        public const string DataSourceIdentifier = "SonarLint";

        private readonly ISet<ITableDataSink> sinks = new HashSet<ITableDataSink>();
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

        public string Identifier => DataSourceIdentifier;

        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

        // Note: Error List is the only expected subscriber
        public IDisposable Subscribe(ITableDataSink sink)
        {
            lock(sinks)
            {
                sinks.Add(sink);

                foreach(var factory in factories)
                {
                    sink.AddFactory(factory);
                }

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

        #endregion

        #region ISonarErrorListDataSource implementation

        public void RefreshErrorList()
        {
            // Mark all the sinks as dirty (so, as a side-effect, they will start an update pass that will get the new snapshot
            // from the snapshot factories).
            lock (sinks)
            {
                foreach (var sink in sinks)
                {
                    SafeOperation(sink, "FactorySnapshotChanged", () => sink.FactorySnapshotChanged(null));
                }
            }
        }

        public void AddFactory(ITableEntriesSnapshotFactory factory)
        {
            lock (sinks)
            {
                factories.Add(factory);
                foreach (var sink in sinks)
                {
                    SafeOperation(sink, "AddFactory", () => sink.AddFactory(factory));
                }
            }
        }

        public void RemoveFactory(ITableEntriesSnapshotFactory factory)
        {
            lock (sinks)
            {
                factories.Remove(factory);
                foreach (var sink in sinks)
                {
                    SafeOperation(sink, "RemoveFactory", () => sink.RemoveFactory(factory));
                }
            }
        }

        #endregion ISonarErrorListDataSource implementation

        private void SafeOperation(ITableDataSink sink, string operationName, Action op)
        {
            try
            {
                op();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Suppress non-critical exception.
                // We are not logging the errors to the output window because it might be too noisy e.g. if
                // bug #1055 mentioned above occurs then the faulty sink will throw an exception each
                // time a character is typed in the editor.
                System.Diagnostics.Debug.WriteLine($"Error in sink {sink.GetType().FullName}.{operationName}: {ex.Message}");
            }
        }
    }
}
