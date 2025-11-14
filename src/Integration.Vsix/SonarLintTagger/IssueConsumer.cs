/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;

/*
 * Instancing: a new instance of this class should be created for each analysis request.
 *
 * The class is initialized with the text snapshot representing the state of the text buffer
 * at the point the analysis was triggered.
 *
 * The job of the class is to:
 * 1) map the issue start/end positions supplied by the analyzer to spans in the supplied text snapshot, and
 * 2) replace the previous list of issues
 *
 * Each time IIssueConsumer.Set is called, the new issues will be mapped back to the
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
    internal class IssueConsumer : IIssueConsumer
    {
        // See bug #1487: this text snapshot should match the content of the file being analysed
        private readonly ITextSnapshot analysisSnapshot;
        private readonly IAnalysisIssueVisualizationConverter issueToIssueVisualizationConverter;
        private readonly string analysisFilePath;
        private readonly IssueConsumerFactory.IIssueHandler issueHandler;

        public IssueConsumer(ITextSnapshot analysisSnapshot, string analysisFilePath, IssueConsumerFactory.IIssueHandler issueHandler, IAnalysisIssueVisualizationConverter issueToIssueVisualizationConverter)
        {
            this.analysisSnapshot = analysisSnapshot ?? throw new ArgumentNullException(nameof(analysisSnapshot));
            this.analysisFilePath = analysisFilePath ?? throw new ArgumentNullException(nameof(analysisFilePath));
            this.issueHandler = issueHandler ?? throw new ArgumentNullException(nameof(issueHandler));
            this.issueToIssueVisualizationConverter = issueToIssueVisualizationConverter ?? throw new ArgumentNullException(nameof(issueToIssueVisualizationConverter));
        }

        public void SetIssues(string path, IEnumerable<IAnalysisIssue> issues)
        {
            if (!ValidatePath(path))
            {
                return;
            }

            issueHandler.HandleNewIssues(PrepareFindings(issues));
        }

        public void SetHotspots(string path, IEnumerable<IAnalysisIssue> hotspots)
        {
            if (!ValidatePath(path))
            {
                return;
            }

            issueHandler.HandleNewHotspots(PrepareFindings(hotspots));
        }

        private List<IAnalysisIssueVisualization> PrepareFindings(IEnumerable<IAnalysisIssue> findings)
        {
            Debug.Assert(findings.All(IsIssueFileLevelOrInAnalysisSnapshot), "Not all reported findings could be mapped to the analysis snapshot");

            var analysisIssueVisualizations = findings
                .Where(IsIssueFileLevelOrInAnalysisSnapshot)
                .Select(x => issueToIssueVisualizationConverter.Convert(x, analysisSnapshot))
                .ToList();
            return analysisIssueVisualizations;
        }

        private bool ValidatePath(string path)
        {
            // Callback from the daemon when new results are available
            if (path != analysisFilePath)
            {
                Debug.Fail("Findings returned for an unexpected file path");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks that the analysis issue can be mapped to location in the text snapshot or file level.
        /// </summary>
        private bool IsIssueFileLevelOrInAnalysisSnapshot(IAnalysisIssue issue)
        {
            return issue.IsFileLevel() ||
            (1 <= issue.PrimaryLocation.TextRange.StartLine && issue.PrimaryLocation.TextRange.EndLine <= analysisSnapshot.LineCount);
        }
    }
}
