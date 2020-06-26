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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class AccumulatingIssueConsumer : IIssueConsumer
    {
        private readonly ITextSnapshot analysedSnapshot;
        private readonly string filePath;
        private readonly IIssuesFilter issuesFilter;
        private readonly ILogger logger;
        private readonly IIssueMarkerFactory issueMarkerFactory;

        private readonly List<IAnalysisIssue> accummulatedIssues;
        private readonly UpdateMarkers updateMarkers;

        public delegate void UpdateMarkers(IEnumerable<IssueMarker> issueMarkers);

        public AccumulatingIssueConsumer(ITextSnapshot analysedSnapshot, string filePath, IIssuesFilter issuesFilter, UpdateMarkers updateMarkers, ILogger logger)
            : this(analysedSnapshot, filePath, issuesFilter, updateMarkers, logger, new IssueMarkerFactory())
        {
        }

        internal /* for testing */ AccumulatingIssueConsumer(ITextSnapshot analysedSnapshot, string filePath, IIssuesFilter issuesFilter,
            UpdateMarkers updateMarkers, ILogger logger, IIssueMarkerFactory issueMarkerFactory)
        {
            this.analysedSnapshot = analysedSnapshot;
            this.filePath = filePath;
            this.issuesFilter = issuesFilter;
            this.updateMarkers = updateMarkers;
            this.logger = logger;

            this.issueMarkerFactory = issueMarkerFactory;
            accummulatedIssues = new List<IAnalysisIssue>();
        }

        void IIssueConsumer.Accept(string path, IEnumerable<IAnalysisIssue> issues)
        {
//duncanp - TODO remove
            //System.Threading.Thread.Sleep(100);

            // Callback from the daemon when new results are available
            if (path != filePath)
            {
                Debug.Fail("Issues returned for an unexpected file path");
                return;
            }

            accummulatedIssues.AddRange(issues);

            var filteredIssues = RemoveSuppressedIssues(accummulatedIssues);


            var newMarkers = filteredIssues.Where(IsValidIssueTextRange)
                .Select(CreateIssueMarker)
                .ToArray();

            updateMarkers(newMarkers);
        }

        private IEnumerable<IAnalysisIssue> RemoveSuppressedIssues(IEnumerable<IAnalysisIssue> issues)
        {
            var filterableIssues = IssueToFilterableIssueConverter.Convert(issues, analysedSnapshot);

            var filteredIssues = issuesFilter.Filter(filterableIssues);
            Debug.Assert(filteredIssues.All(x => x is FilterableIssueAdapter), "Not expecting the issue filter to change the list item type");

            var suppressedCount = filterableIssues.Count() - filteredIssues.Count();
            logger.WriteLine(Strings.Daemon_SuppressedIssuesInfo, suppressedCount);

            return filteredIssues.OfType<FilterableIssueAdapter>()
                .Select(x => x.SonarLintIssue);
        }

        private bool IsValidIssueTextRange(IAnalysisIssue issue) =>
            1 <= issue.StartLine && issue.EndLine <= analysedSnapshot.LineCount;

        private IssueMarker CreateIssueMarker(IAnalysisIssue issue) =>
            issueMarkerFactory.Create(issue, analysedSnapshot);
    }
}
