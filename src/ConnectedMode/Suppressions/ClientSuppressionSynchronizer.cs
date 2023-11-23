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
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.IssueVisualization.Models;
using System.Collections.Generic;
using System;
using SonarLint.VisualStudio.ConnectedMode.Synchronization;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    [Export(typeof(IClientSuppressionSynchronizer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ClientSuppressionSynchronizer : IClientSuppressionSynchronizer
    {
        private readonly IIssueLocationStoreAggregator issuesStore;
        private readonly IIssueMatcher issueMatcher;

        public event EventHandler<LocalSuppressionsChangedEventArgs> LocalSuppressionsChanged;

        [ImportingConstructor]
        public ClientSuppressionSynchronizer(IIssueLocationStoreAggregator issuesStore, IIssueMatcher issueMatcher)
        {
            this.issuesStore = issuesStore;
            this.issueMatcher = issueMatcher;
        }

        public void SynchronizeSuppressedIssues()
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

            if (LocalSuppressionsChanged != null && changedFiles.Count > 0)
            {
                var eventArgs = new LocalSuppressionsChangedEventArgs(changedFiles);
                LocalSuppressionsChanged?.Invoke(this, eventArgs);
            }
        }
    }
}
