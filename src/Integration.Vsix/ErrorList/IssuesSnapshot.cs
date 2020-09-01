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
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.TableControls;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal interface IIssuesSnapshot : ITableEntriesSnapshot
    {
        /// <summary>
        /// Logical identifier for a set of issues produced by an analysis run.
        /// Every snapshot that has the same analysis id describes the same set of
        /// issues in the same order. The implementation of "IndexOf" depends on this.
        /// </summary>
        Guid AnalysisRunId { get; }

        /// <summary>
        /// The list of issues returned by the analyzer run
        /// </summary>
        IEnumerable<IssueMarker> IssueMarkers { get; }

        /// <summary>
        /// Returns the set of files for which this snapshot contains location information (primary and secondary)
        /// </summary>
        IEnumerable<string> FilesInSnapshot { get; }

        /// <summary>
        /// Returns all of the location visualizations (primary and secondary) that are in the specified file
        /// </summary>
        /// <returns></returns>
        IEnumerable<IAnalysisIssueLocationVisualization> GetLocationsVizsForFile(string filePath);
    }

    /// <summary>
    /// ErrorList plumbing. Contains the issues data for a single analyzed file,
    /// and overrides methods called by the Error List to populate rows with
    /// that data.
    /// </summary>
    /// <remarks>
    /// See the README.md in this folder for more information
    /// </remarks>
    internal class IssuesSnapshot : WpfTableEntriesSnapshotBase, IIssuesSnapshot
    {
        private readonly string projectName;
        private readonly string filePath;
        private readonly int versionNumber;
        private readonly IAnalysisSeverityToVsSeverityConverter toVsSeverityConverter;
        private readonly IRuleHelpLinkProvider ruleHelpLinkProvider;

        private readonly IList<IssueMarker> issueMarkers;
        private readonly IReadOnlyCollection<IssueMarker> readonlyIssueMarkers;

        // Every snapshot has a unique version number. It doesn't matter what it is, as
        // long as it increments (if two snapshots have the same number or lower, the ErrorList
        // will assume the data hasn't changed and won't update the rows).
        private static int nextVersionNumber = 0;
        private static int GetNextVersionNumber() => ++nextVersionNumber;

        #region Construction methods

        /// <summary>
        /// Create a snapshot with new set of issues from a new analysis run
        /// </summary>
        public static IssuesSnapshot CreateNew(string projectName, string filePath, IEnumerable<IssueMarker> issueMarkers) =>
            new IssuesSnapshot(Guid.NewGuid(), projectName, filePath, issueMarkers);

        /// <summary>
        /// Create and return an updated version of an existing snapshot, where the
        /// issues are the same but the source file has been renamed
        /// </summary>
        public IssuesSnapshot CreateUpdatedSnapshot(string newFilePath) =>
            new IssuesSnapshot(AnalysisRunId, projectName, newFilePath, issueMarkers);

        /// <summary>
        /// Create and return an updated version of an existing snapshot, where the
        /// set of issues are the same but their locations have changed (by the user editing the file)
        /// </summary>
        /// <remarks>The number and ordering of issues must be the same</remarks>
        public IssuesSnapshot CreateUpdatedSnapshot(IEnumerable<IssueMarker> updatedIssueMarkers)
        {
            if (updatedIssueMarkers.Count() != IssueMarkers.Count())
            {
                throw new ArgumentException("Number of issues should not change when updating a snapshot", nameof(updatedIssueMarkers));
            }

            return new IssuesSnapshot(AnalysisRunId, projectName, filePath, updatedIssueMarkers);
        }

        private IssuesSnapshot(Guid snapshotId, string projectName, string filePath, IEnumerable<IssueMarker> issueMarkers)
            : this(snapshotId, projectName, filePath, issueMarkers,  new AnalysisSeverityToVsSeverityConverter(), new RuleHelpLinkProvider())
        {
        }

        private IssuesSnapshot(Guid snapshotId, string projectName, string filePath, IEnumerable<IssueMarker> issueMarkers, IAnalysisSeverityToVsSeverityConverter toVsSeverityConverter, IRuleHelpLinkProvider ruleHelpLinkProvider)
        {
            this.AnalysisRunId = snapshotId;
            this.projectName = projectName;
            this.filePath = filePath;
            this.versionNumber = GetNextVersionNumber();
            this.toVsSeverityConverter = toVsSeverityConverter;
            this.ruleHelpLinkProvider = ruleHelpLinkProvider;
            this.issueMarkers = new List<IssueMarker>(issueMarkers);
            this.readonlyIssueMarkers = new ReadOnlyCollection<IssueMarker>(this.issueMarkers);

            // Optimistation:
            // Most rules only have a single location, and most multi-location rules only produce locations
            // for a single file. So most snapshots will only have issues relating to a single file.
            // However, we still need to notify every tagger when any snapshot has updated, in case there
            // any of the issues have secondary locations that the tagger needs to handle.
            // This optimisation gives the tagger a quick way to check that it does not need to do any work
            // i.e. if the file handled by the tagger is not in the list, do nothing.
            this.FilesInSnapshot = CalculateFilesInSnapshot(issueMarkers);
        }

        #endregion

        #region Overrides

        public override int Count => this.issueMarkers.Count;

        public override int VersionNumber => this.versionNumber;

        public override bool TryGetValue(int index, string keyName, out object content)
        {
            if (index < 0 || this.issueMarkers.Count <= index)
            {
                content = null;
                return false;
            }

            switch (keyName)
            {
                case StandardTableKeyNames.DocumentName:
                    content = filePath;
                    return true;

                case StandardTableKeyNames.Line:
                    // Note: the line and column numbers are taken from the SnapshotSpan, not the Issue.
                    // The SnapshotSpan represents the live document, so the text positions could have
                    // changed from those reported from the Issue.
                    content = this.issueMarkers[index].Span.Start.GetContainingLine().LineNumber;
                    return true;

                case StandardTableKeyNames.Column:
                    // Use the span, not the issue. See comment immediately above.
                    var position = this.issueMarkers[index].Span.Start;
                    var line = position.GetContainingLine();
                    content = position.Position - line.Start.Position;
                    return true;

                case StandardTableKeyNames.Text:
                    content = this.issueMarkers[index].Issue.Message;
                    return true;

                case StandardTableKeyNames.ErrorSeverity:
                    content = toVsSeverityConverter.Convert(this.issueMarkers[index].Issue.Severity);
                    return true;

                case StandardTableKeyNames.BuildTool:
                    content = "SonarLint";
                    return true;

                case StandardTableKeyNames.ErrorCode:
                    content = this.issueMarkers[index].Issue.RuleKey;
                    return true;

                case StandardTableKeyNames.ErrorRank:
                    content = ErrorRank.Other;
                    return true;

                case StandardTableKeyNames.ErrorCategory:
                    content = $"{issueMarkers[index].Issue.Severity} {ToString(issueMarkers[index].Issue.Type)}";
                    return true;

                case StandardTableKeyNames.ErrorCodeToolTip:
                    content = $"Open description of rule {this.issueMarkers[index].Issue.RuleKey}";
                    return true;

                case StandardTableKeyNames.HelpLink:
                    string ruleKey = this.issueMarkers[index].Issue.RuleKey;
                    content = ruleHelpLinkProvider.GetHelpLink(ruleKey);
                    return true;

                case StandardTableKeyNames.ProjectName:
                    content = projectName;
                    return true;

                // Not a visible field - returns the issue object
                case SonarLintTableControlConstants.IssueVizColumnName:
                    content = this.issueMarkers[index].IssueViz;
                    return true;
                default:
                    content = null;
                    return false;
            }
        }

        private object ToString(AnalysisIssueType type)
        {
            switch (type)
            {
                case AnalysisIssueType.Vulnerability: return "Vulnerability";
                case AnalysisIssueType.Bug: return "Bug";
                case AnalysisIssueType.CodeSmell:
                default:
                    return "Code Smell";
            }
        }

        public override bool CanCreateDetailsContent(int index)
        {
            // TODO flip to true when detailed description is ready
            return false;
        }

        public override bool TryCreateDetailsStringContent(int index, out string content)
        {
            // TODO use the detailed description
            content = this.issueMarkers[index].Issue.Message;
            return true;
        }

        public override int IndexOf(int currentIndex, ITableEntriesSnapshot newSnapshot)
        {
            // Called when a new snapshot has been created and the Error List is trying to
            // map from a selected item in the previous snapshot to the corresponding item
            // in the new snapshot.
            // We can only do this if the snapshots represent the same set of issues i.e.
            // the AnalysisRunIds are the same.
            // The Error List will still raise two "selection changed" events - firstly to 
            // "null", then to the corresponding issue in the new snapshot.
            if (newSnapshot is IssuesSnapshot newIssuesSnapshot &&
                newIssuesSnapshot.AnalysisRunId == AnalysisRunId)
            {
                return currentIndex;
            }
            return base.IndexOf(currentIndex, newSnapshot);
        }

        #endregion

        #region IIssuesSnapshot implementation

        public Guid AnalysisRunId { get; }

        public IEnumerable<IssueMarker> IssueMarkers => readonlyIssueMarkers;

        public IEnumerable<string> FilesInSnapshot { get; }

        public IEnumerable<IAnalysisIssueLocationVisualization> GetLocationsVizsForFile(string filePath)
        {
            if (!FilesInSnapshot.Any(x => IsMatchingPath(filePath, x)))
            {
                return Array.Empty<IAnalysisIssueLocationVisualization>();
            }

            var locVizs = GetAllLocationVisualizations(IssueMarkers)
                .Where(locViz => IsMatchingPath(filePath, locViz.Location.FilePath));

            return new List<IAnalysisIssueLocationVisualization>(locVizs);
        }

        private static IEnumerable<string> CalculateFilesInSnapshot(IEnumerable<IssueMarker> markers)
        {
            var allFilePaths = GetAllLocationVisualizations(markers)
                .Select(locViz => locViz.Location.FilePath);

            return new HashSet<string>(allFilePaths, StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<IAnalysisIssueLocationVisualization> GetAllLocationVisualizations(IEnumerable<IssueMarker> markers) =>
                markers.Select(x => x.IssueViz) // primary locations
                .Union(markers.SelectMany(x => x.IssueViz.Flows.SelectMany(f => f.Locations))); // secondary locations


        private static bool IsMatchingPath(string path1, string path2) =>
            path1.Equals(path2, StringComparison.OrdinalIgnoreCase);

        #endregion IIssuesSnapshot implementation
    }
}
