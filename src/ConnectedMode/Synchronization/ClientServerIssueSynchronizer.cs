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
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Synchronization
{
    [Export(typeof(IClientServerIssueSynchronizer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ClientServerIssueSynchronizer : IClientServerIssueSynchronizer
    {
        private readonly IIssueLocationStoreAggregator issuesStore;
        private readonly IIssueMatcher issueMatcher;

        public event EventHandler<ClientServerIssueMatchChangedEventArgs> ClientServerIssueMatchChanged;

        [ImportingConstructor]
        public ClientServerIssueSynchronizer(IIssueLocationStoreAggregator issuesStore, IIssueMatcher issueMatcher)
        {
            this.issuesStore = issuesStore;
            this.issueMatcher = issueMatcher;
        }

        public void SynchronizeIssues()
        {
            var changedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var filterableIssues = issuesStore.GetIssues().OfType<IFilterableIssue>().ToArray();

            foreach (var issue in filterableIssues)
            {
                var issueViz = issue as IAnalysisIssueVisualization;

                // If the object was matched then it is suppressed on the server
                var newIsSuppressedValue = issueMatcher.Match(issueViz)?.IsResolved ?? false;

                if (issueViz.IsSuppressed != newIsSuppressedValue)
                {
                    issueViz.IsSuppressed = newIsSuppressedValue;
                    changedFiles.Add(issueViz.CurrentFilePath);
                }
            }

            if (ClientServerIssueMatchChanged != null && changedFiles.Count > 0)
            {
                var eventArgs = new ClientServerIssueMatchChangedEventArgs(changedFiles);
                ClientServerIssueMatchChanged?.Invoke(this, eventArgs);
            }
        }
    }
}
