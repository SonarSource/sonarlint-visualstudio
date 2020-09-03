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
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;

/* 
 * Instancing: a new instance of this class should be created for each analysis request.
 * 
 * The class is initialized with the text snapshot representing the state of the text buffer
 * at the point the analysis was triggered.
 * 
 * The job of the class is to:
 * 1) map the issue start/end positions supplied by the analyzer to spans in the supplied text snapshot, and 
 * 2) accumulate the list of issues that have return by the analyzer so far
 * 
 * Each time IIssueConsumer.Accept is called, the new issues will be mapped back to the
 * supplied snapshot and decorated with the additional data required for filtering and tagging.
 * Then, all of the issues that have been received so far will be passed to the OnIssuesChanged delegate.
 * 
 * However, it's possible that the text buffer could have been edited since the analysis
 * was triggered. It is the responsibility of the callback to translate the supplied IssueVisualizations
 * to the current text buffer snapshot, if necessary.
 * 
 * 
 * Mapping from reported issue line/char positions to the analysis snapshot
 * ------------------------------------------------------------------------
 * Currently, the JS/CFamily analyzers run against the file on disc, not the content of 
 * the snapshot. We're assuming that analysis is triggered on save so the state of the
 * text snapshot matches the file on disc which is being analyzed.
 * We expect always to be able to map back from the reported issue to the snapshot.
 * However, we code defensively and strip out any issues that can't be mapped.
 * 
 */

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Handles processing the issues for a single analysis request
    /// </summary>
    internal class AccumulatingIssueConsumer : IIssueConsumer
    {
        // See bug #1487: this text snapshot should match the content of the file being analysed
        private readonly ITextSnapshot analysisSnapshot;
        private readonly IAnalysisIssueVisualizationConverter issueToIssueVisualizationConverter;
        private readonly List<IAnalysisIssueVisualization> allIssues;
        private readonly string analysisFilePath;
        private readonly OnIssuesChanged onIssuesChanged;

        public delegate void OnIssuesChanged(IEnumerable<IAnalysisIssueVisualization> issues);

        public AccumulatingIssueConsumer(ITextSnapshot analysisSnapshot, string analysisFilePath, OnIssuesChanged onIssuesChangedCallback, IAnalysisIssueVisualizationConverter issueToIssueVisualizationConverter)
        {
            this.analysisSnapshot = analysisSnapshot ?? throw new ArgumentNullException(nameof(analysisSnapshot));
            this.analysisFilePath = analysisFilePath ?? throw new ArgumentNullException(nameof(analysisFilePath));
            this.onIssuesChanged = onIssuesChangedCallback ?? throw new ArgumentNullException(nameof(onIssuesChangedCallback));
            this.issueToIssueVisualizationConverter = issueToIssueVisualizationConverter ?? throw new ArgumentNullException(nameof(issueToIssueVisualizationConverter));

            allIssues = new List<IAnalysisIssueVisualization>();
        }

        public void Accept(string path, IEnumerable<IAnalysisIssue> issues)
        {
            // Callback from the daemon when new results are available
            if (path != analysisFilePath)
            {
                Debug.Fail("Issues returned for an unexpected file path");
                return;
            }

            Debug.Assert(issues.All(IsIssueInAnalysisSnapshot), "Not all reported issues could be mapped to the analysis snapshot");

            var newIssues = issues
                .Where(IsIssueInAnalysisSnapshot)
                .Select(x => issueToIssueVisualizationConverter.Convert(x, analysisSnapshot))
                .ToArray();

            allIssues.AddRange(newIssues);

            onIssuesChanged.Invoke(allIssues);
        }

        /// <summary>
        /// Checks that the analysis issue can be mapped to location in the text snapshot.
        /// </summary>
        private bool IsIssueInAnalysisSnapshot(IAnalysisIssue issue) =>
            // Sonar issues line numbers are 1-based; 0 = file-level issue
            // Spans lines are 0-based
            1 <= issue.StartLine && issue.EndLine <= analysisSnapshot.LineCount;
    }
}
