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
using System.Diagnostics;
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.IssueVisualization;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    [Export(typeof(IClientSuppressionSynchronizer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ClientSuppressionSynchronizer : IClientSuppressionSynchronizer
    {
        private readonly IClientIssueStore clientIssueStore;
        private readonly IIssuesFilter issueFilter;

        [ImportingConstructor]
        public ClientSuppressionSynchronizer(IClientIssueStore clientSideIssueStore, IIssuesFilter issueFilter)
        {
            this.clientIssueStore = clientSideIssueStore;
            this.issueFilter = issueFilter;
        }

        public void SynchronizeSuppressedIssues()
        {
            var filterableIssues = clientIssueStore.Get().OfType<IFilterableIssue>().ToArray();

            var matches = issueFilter.GetMatches(filterableIssues);

            Debug.Assert(matches.All(x => x is IAnalysisIssueVisualization), "Not expecting the issue filter to change the list item type");

            foreach (var issue in filterableIssues)
            {
                var issueViz = issue as IAnalysisIssueVisualization;
                issueViz.IsSuppressed = matches.Contains(issue);
            }
        }
    }
}
