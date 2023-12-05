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
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.IssueVisualization.TableControls;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Vsix.ErrorList
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
        private readonly IIssueSelectionService issueSelectionService;
        private readonly ISet<ITableDataSink> sinks = new HashSet<ITableDataSink>();
        private readonly ISet<IIssuesSnapshotFactory> factories = new HashSet<IIssuesSnapshotFactory>();

        [ImportingConstructor]
        internal SonarErrorListDataSource(ITableManagerProvider tableManagerProvider,
            IFileRenamesEventSource fileRenamesEventSource,
            IIssueSelectionService issueSelectionService)
        {
            if (tableManagerProvider == null)
            {
                throw new ArgumentNullException(nameof(tableManagerProvider));
            }

            this.fileRenamesEventSource = fileRenamesEventSource ?? throw new ArgumentNullException(nameof(fileRenamesEventSource));
            this.issueSelectionService = issueSelectionService ?? throw new ArgumentNullException(nameof(issueSelectionService));
            fileRenamesEventSource.FilesRenamed += OnFilesRenamed;

            var errorTableManager = tableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
            errorTableManager.AddSource(this, StandardTableColumnDefinitions.DetailsExpander,
                                                   StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.ErrorCategory,
                                                   StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName,
                                                   StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column,
                                                   StandardTableColumnDefinitions.ProjectName,
                                                   SuppressionsColumnHelper.SuppressionStateColumnName);
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

        public void RefreshErrorList(IIssuesSnapshotFactory factory)
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
                NotifyIssuesChanged(factory);
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

        private void NotifyIssuesChanged(IIssuesSnapshotFactory factory)
        {
            IssuesChanged?.Invoke(this, new IssuesChangedEventArgs(factory.CurrentSnapshot.FilesInSnapshot));
        }

        public void AddFactory(IIssuesSnapshotFactory factory)
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

        public void RemoveFactory(IIssuesSnapshotFactory factory)
        {
            lock (sinks)
            {
                var wasRemoved = factories.Remove(factory);

                foreach (var sink in sinks)
                {
                    SafeOperation(sink, "RemoveFactory", () => sink.RemoveFactory(factory));
                }

                if (wasRemoved)
                {
                    NotifyIssuesChanged(factory);
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
            IIssuesSnapshotFactory[] currentFactories;

            lock (sinks)
            {
                currentFactories = factories.ToArray();
            }

            // There should be only one factory that has primary locations for the specified file path,
            // but any factory could have secondary locations
            var locVizs = new List<IAnalysisIssueLocationVisualization>();

            foreach (var factory in currentFactories)
            {
                locVizs.AddRange(factory.CurrentSnapshot.GetLocationsVizsForFile(filePath));
            }

            return locVizs;
        }

        public void RefreshOnBufferChanged(string affectedFilePath)
        {
            if (affectedFilePath == null)
            {
                throw new ArgumentNullException(nameof(affectedFilePath));
            }

            InternalRefreshAffectedFiles(new[] { affectedFilePath }, notifyListeners: false);
        }

        public void Refresh(IEnumerable<string> affectedFilePaths)
        {
            if (affectedFilePaths == null)
            {
                throw new ArgumentNullException(nameof(affectedFilePaths));
            }

            InternalRefreshAffectedFiles(affectedFilePaths, notifyListeners: true);
        }

        private void InternalRefreshAffectedFiles(IEnumerable<string> affectedFilePaths, bool notifyListeners)
        {
            lock (sinks)
            {
                var affectedSnapshotFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var factory in factories)
                {
                    var oldSnapshot = factory.CurrentSnapshot;
                    var isSnapshotAffected = oldSnapshot.FilesInSnapshot.Any(snapshotPath => affectedFilePaths.Any(affected => PathHelper.IsMatchingPath(snapshotPath, affected)));

                    if (isSnapshotAffected)
                    {
                        factory.UpdateSnapshot(factory.CurrentSnapshot.GetUpdatedSnapshot());
                        InternalRefreshErrorList(factory);

                        if (notifyListeners)
                        {
                            // Aggregate the list of affected snapshot files and raise one event at the end e.g.
                            // * Factory1 refers to files A and B
                            // * Factory2 refers to files C and B
                            // * Factory3 refers to file D
                            // -> we want to notify changes to files [A, B, C, D] once only
                            foreach (var file in factory.CurrentSnapshot.FilesInSnapshot)
                            {
                                affectedSnapshotFiles.Add(file);
                            }
                        }
                    }

                    var selectedIssue = issueSelectionService.SelectedIssue;

                    // If the issue became non-navigable, it would not exist in the new snapshot.
                    // Hence, the selection should be checked based on the old snapshot's contents
                    if (oldSnapshot.Issues.Contains(selectedIssue) && !selectedIssue.IsNavigable())
                    {
                        issueSelectionService.SelectedIssue = null;
                    }
                }

                if (notifyListeners && affectedSnapshotFiles.Count > 0)
                {
                    IssuesChanged?.Invoke(this, new IssuesChangedEventArgs(affectedSnapshotFiles));
                }
            }
        }

        public bool Contains(IAnalysisIssueVisualization issueVisualization)
        {
            if (issueVisualization == null)
            {
                throw new ArgumentNullException(nameof(issueVisualization));
            }

            lock (sinks)
            {
                return factories.Any(factory => factory.CurrentSnapshot.Issues.Contains(issueVisualization));
            }
        }

        #endregion IIssueLocationStore implementation

        #region IClientIssueStore implementation

        public IEnumerable<IAnalysisIssueVisualization> GetIssues()
        {
            IIssuesSnapshotFactory[] currentFactories;

            lock (sinks)
            {
                currentFactories = factories.ToArray();
            }

            foreach (var factory in currentFactories)
            {
                foreach (var issue in factory.CurrentSnapshot.Issues)
                {
                    yield return issue;
                }
            }
        }

        #endregion IClientIssueStore implementation

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

                foreach (var factory in currentFactories)
                {
                    var factoryChanged = factory.HandleFileRenames(e.OldNewFilePaths);

                    if (factoryChanged)
                    {
                        InternalRefreshErrorList(factory);
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
