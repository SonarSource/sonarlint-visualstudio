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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.TableControls;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(ISonarErrorListDataSource))]
    [Export(typeof(IIssueLocationStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class SonarErrorListDataSource :
        ITableDataSource,           // Allows us to provide entries to the Error List
        ISonarErrorListDataSource,  // Used by analyzers to push new analysis results to the data source
        IIssueLocationStore,        // Used by the taggers to get/update locations for specific files
        IDisposable
    {
        private readonly IFileRenamesEventSource fileRenamesEventSource;
        private readonly ISet<ITableDataSink> sinks = new HashSet<ITableDataSink>();
        private readonly ISet<SnapshotFactory> factories = new HashSet<SnapshotFactory>();

        [ImportingConstructor]
        internal SonarErrorListDataSource(ITableManagerProvider tableManagerProvider, IFileRenamesEventSource fileRenamesEventSource)
        {
            if (tableManagerProvider == null)
            {
                throw new ArgumentNullException(nameof(tableManagerProvider));
            }

            this.fileRenamesEventSource = fileRenamesEventSource ?? throw new ArgumentNullException(nameof(fileRenamesEventSource));
            fileRenamesEventSource.FilesRenamed += OnFilesRenamed;

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

        public string Identifier => SonarLintTableControlConstants.ErrorListDataSourceIdentifier;

        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

        // Note: Error List is the only expected subscriber
        public IDisposable Subscribe(ITableDataSink sink)
        {
            lock (sinks)
            {
                sinks.Add(sink);

                foreach (var factory in factories)
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

        public void RefreshErrorList(SnapshotFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            lock (sinks)
            {
                // Guard against potential race condition - factory could have been removed
                if (!factories.Contains(factory))
                {
                    return;
                }

                InternalRefreshErrorList(factory);
                NotifyLocationServiceListeners(factory);
            }
        }

        private void InternalRefreshErrorList(ITableEntriesSnapshotFactory factory)
        {
            // Mark all the sinks as dirty (so, as a side-effect, they will start an update pass that will get the new snapshot
            // from the snapshot factory).
            foreach (var sink in sinks)
            {
                SafeOperation(sink, "FactorySnapshotChanged", () => sink.FactorySnapshotChanged(factory));
            }
        }

        private void NotifyLocationServiceListeners(SnapshotFactory factory)
        {
            IssuesChanged?.Invoke(this, new IssuesChangedEventArgs(factory.CurrentSnapshot.FilesInSnapshot));
        }

        public void AddFactory(SnapshotFactory factory)
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

        public void RemoveFactory(SnapshotFactory factory)
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

        #region IIssueLocationStore implementation

        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        public IEnumerable<IAnalysisIssueLocationVisualization> GetLocations(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            // Guard against factories changing while iterating
            var currentFactories = factories.ToArray();

            // There should be only one factory that has primary locations for the specified file path,
            // but any factory could have secondary locations
            var locVizs = new List<IAnalysisIssueLocationVisualization>();
            foreach (var factory in currentFactories)
            {
                locVizs.AddRange(factory.CurrentSnapshot.GetLocationsVizsForFile(filePath));
            }
            return locVizs;
        }

        public void Refresh(IEnumerable<string> affectedFilePaths)
        {
            if (affectedFilePaths == null)
            {
                throw new ArgumentNullException(nameof(affectedFilePaths));
            }

            lock (sinks)
            {
                foreach (var factory in factories)
                {
                    var snapshot = factory.CurrentSnapshot;
                    if (snapshot.FilesInSnapshot.Any(snapshotPath => affectedFilePaths.Any(affected => PathHelper.IsMatchingPath(snapshotPath, affected))))
                    {
                        snapshot.IncrementVersion();
                        InternalRefreshErrorList(factory);
                    }
                }
            }
        }

        #endregion IIssueLocationStore implementation

        private static void SafeOperation(ITableDataSink sink, string operationName, Action op)
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

        private void OnFilesRenamed(object sender, FilesRenamedEventArgs e)
        {
            lock (sinks)
            {
                var currentFactories = factories.ToArray();

                foreach (var oldFilePath in e.OldNewFilePaths.Keys)
                {
                    var newFilePath = e.OldNewFilePaths[oldFilePath];

                    foreach (var factory in currentFactories)
                    {
                        var locationsInOldFile = factory.CurrentSnapshot.GetLocationsVizsForFile(oldFilePath);

                        foreach (var location in locationsInOldFile)
                        {
                            location.CurrentFilePath = newFilePath;
                        }

                        var factoryChanged = true;
                        var renamedAnalyzedFile = e.OldNewFilePaths.ContainsKey(factory.CurrentSnapshot.AnalyzedFilePath);

                        if (renamedAnalyzedFile)
                        {
                            factory.UpdateSnapshot(factory.CurrentSnapshot.CreateUpdatedSnapshot(newFilePath));
                        }
                        else if (locationsInOldFile.Any())
                        {
                            factory.CurrentSnapshot.IncrementVersion();
                        }
                        else
                        {
                            factoryChanged = false;
                        }

                        if (factoryChanged)
                        {
                            InternalRefreshErrorList(factory);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            fileRenamesEventSource.FilesRenamed -= OnFilesRenamed;
        }
    }
}
