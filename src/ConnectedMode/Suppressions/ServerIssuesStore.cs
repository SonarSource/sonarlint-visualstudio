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
using SonarLint.VisualStudio.Integration;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    [Export(typeof(IServerIssuesStore))]
    [Export(typeof(IServerIssuesStoreWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ServerIssuesStore : IServerIssuesStoreWriter
    {
        private readonly ILogger logger;
        private readonly object serverIssuesLock = new object();
        private readonly Dictionary<string, SonarQubeIssue> serverIssues = new Dictionary<string, SonarQubeIssue>(StringComparer.Ordinal);

        public event EventHandler ServerIssuesChanged;

        [ImportingConstructor]
        public ServerIssuesStore(ILogger logger)
        {
            this.logger = logger;
        }

        #region IServerIssuesStore implementation

        public IEnumerable<SonarQubeIssue> Get()
        {
            lock (serverIssuesLock)
            {
                return serverIssues?.Values.ToArray() ?? Enumerable.Empty<SonarQubeIssue>();
            }
        }

        #endregion IServerIssuesStore implementation

        #region IServerIssueStoreWriter implementation

        public void AddIssues(IEnumerable<SonarQubeIssue> issues, bool clearAllExistingIssues)
        {
            if (issues == null) { return; }
            
            lock (serverIssuesLock)
            {
                if (clearAllExistingIssues)
                {
                    serverIssues.Clear();
                }

                foreach(var issue in issues)
                {
                    serverIssues[issue.IssueKey] = issue;
                }
            }

            RaiseEventIfRequired();
        }

        public void UpdateIssues(bool isResolved, IEnumerable<string> issueKeys)
        {
            bool issuesChanged = false;
            lock (serverIssuesLock)
            {
                foreach (var issueKey in issueKeys)
                {
                    serverIssues.TryGetValue(issueKey, out var issue);

                    if (issue == null)
                    {
                        logger.LogVerbose(Resources.Store_UpdateIssue_NoMatch, issueKey);
                        return;
                    }

                    if (issue.IsResolved == isResolved)
                    {
                        logger.LogVerbose(Resources.Store_UpdateIssue_UpdateNotRequired, issueKey);
                    }
                    else
                    {
                        logger.LogVerbose(Resources.Store_UpdateIssue_UpdateRequired, issueKey, isResolved);
                        issue.IsResolved = isResolved;
                        issuesChanged = true;
                    }
                }
            }

            if (issuesChanged)
            {
                RaiseEventIfRequired();
            }
        }

        private void RaiseEventIfRequired()
        {
            if (ServerIssuesChanged != null)
            {
                // Note: we don't need to raise the event inside the lock
                // - there is no data about what changed in the event args, it's just a
                // "something changed" notification, so if there are multiple threads
                // trying to update the store concurrently it doesn't matter which order
                // the events are raised in.
                // This is different from the TaintStore, where the event does contain information
                // about what changed, so the order of event processing matters.
                logger.WriteLine(Resources.Store_UpdateIssue_RaisingEvent);
                ServerIssuesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion IServerIssueStoreWriter implementation
    }
}
