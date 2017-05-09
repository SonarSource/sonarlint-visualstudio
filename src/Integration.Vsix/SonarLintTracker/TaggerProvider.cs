/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Sonarlint;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Factory for the <see cref="ITagger{T}"/>. There will be one instance of this class/VS session.
    ///
    /// It is also the <see cref="ITableDataSource"/> that reports Sonar errors.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class TaggerProvider : IViewTaggerProvider, ITableDataSource, IIssueConsumer
    {
        internal readonly ITableManager ErrorTableManager;
        internal readonly ITextDocumentFactoryService TextDocumentFactoryService;

        private readonly List<SinkManager> managers = new List<SinkManager>();
        private readonly TrackerManager trackers = new TrackerManager();

        private readonly ISonarLintDaemon daemon;

        [ImportingConstructor]
        internal TaggerProvider([Import] ITableManagerProvider provider, [Import] ITextDocumentFactoryService textDocumentFactoryService, [Import] ISonarLintDaemon daemon)
        {
            this.ErrorTableManager = provider.GetTableManager(StandardTables.ErrorsTable);
            this.TextDocumentFactoryService = textDocumentFactoryService;

            this.ErrorTableManager.AddSource(this, StandardTableColumnDefinitions.DetailsExpander,
                                                   StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.ErrorCategory,
                                                   StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName,
                                                   StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column,
                                                   StandardTableColumnDefinitions.ProjectName);

            this.daemon = daemon;
        }

        /// <summary>
        /// Create a tagger that will track Sonar issues on the view/buffer combination.
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (!daemon.IsRunning)
            {
                return null;
            }

            // Only attempt to track the view's edit buffer.
            // Multiple views could have that buffer open simultaneously, so only create one instance of the tracker.
            if (buffer != textView.TextBuffer || typeof(T) != typeof(IErrorTag))
            {
                return null;
            }

            ITextDocument document;
            if (!TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                return null;
            }

            var path = document.FilePath;
            // TODO find a better way to detect JavaScript
            if (!path.ToLowerInvariant().EndsWith(".js"))
            {
                return null;
            }

            lock (trackers)
            {
                if (!trackers.ExistsForFile(path))
                {
                    var tracker = new IssueTracker(this, buffer, document);
                    return tracker as ITagger<T>;
                }
            }

            return null;
        }

        internal void Rename(string oldPath, string newPath)
        {
            trackers.Rename(oldPath, newPath);
        }

        #region ITableDataSource members

        public string DisplayName => "SonarLint";

        public string Identifier => "SonarLint";

        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

        // Note: Error List is the only expected subscriber
        public IDisposable Subscribe(ITableDataSink sink) => new SinkManager(this, sink);

        #endregion

        private class TrackerManager
        {
            private readonly IDictionary<string, IssueTracker> trackers = new Dictionary<string, IssueTracker>();

            public bool ExistsForFile(string path)
            {
                var key = Key(path);
                return trackers.ContainsKey(key);
            }

            public void Add(IssueTracker tracker)
            {
                trackers.Add(Key(tracker.FilePath), tracker);
            }

            public void Remove(IssueTracker tracker)
            {
                trackers.Remove(Key(tracker.FilePath));
            }

            public bool TryGetValue(string path, out IssueTracker tracker)
            {
                return trackers.TryGetValue(Key(path), out tracker);
            }

            public IEnumerable<IssueTracker> Values => trackers.Values;

            private string Key(string path)
            {
                return path.ToLowerInvariant();
            }

            internal void Rename(string oldPath, string newPath)
            {
                string oldKey = Key(oldPath);
                IssueTracker tracker;
                if (trackers.TryGetValue(oldKey, out tracker))
                {
                    trackers.Add(Key(newPath), tracker);
                    trackers.Remove(oldKey);
                }
            }
        }

        public void RequestAnalysis(string path, string charset)
        {
            daemon.RequestAnalysis(path, charset, this);
        }

        public void Accept(string path, IEnumerable<Issue> issues)
        {
            UpdateIssues(path, issues);
        }

        private void UpdateIssues(string path, IEnumerable<Issue> issues)
        {
            IssueTracker tracker;
            if (trackers.TryGetValue(path, out tracker))
            {
                tracker.UpdateIssues(issues);
            }
        }

        public void AddSinkManager(SinkManager manager)
        {
            lock (managers)
            {
                managers.Add(manager);

                foreach (var tracker in trackers.Values)
                {
                    manager.AddFactory(tracker.Factory);
                }
            }
        }

        public void RemoveSinkManager(SinkManager manager)
        {
            lock (managers)
            {
                managers.Remove(manager);
            }
        }

        public void AddIssueTracker(IssueTracker tracker)
        {
            lock (managers)
            {
                trackers.Add(tracker);

                foreach (var manager in managers)
                {
                    manager.AddFactory(tracker.Factory);
                }
            }
        }

        public void RemoveIssueTracker(IssueTracker tracker)
        {
            lock (managers)
            {
                trackers.Remove(tracker);

                foreach (var manager in managers)
                {
                    manager.RemoveFactory(tracker.Factory);
                }
            }
        }

        public void UpdateAllSinks()
        {
            lock (managers)
            {
                foreach (var manager in managers)
                {
                    manager.UpdateSink();
                }
            }
        }
    }
}
