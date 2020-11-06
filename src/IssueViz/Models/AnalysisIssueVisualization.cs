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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppression;

namespace SonarLint.VisualStudio.IssueVisualization.Models
{
    public interface IAnalysisIssueVisualization : IAnalysisIssueLocationVisualization, IFilterableIssue
    {
        IReadOnlyList<IAnalysisIssueFlowVisualization> Flows { get; }

        IAnalysisIssueBase Issue { get; }
    }

    internal class AnalysisIssueVisualization : IAnalysisIssueVisualization
    {
        private string currentFilePath;
        private SnapshotSpan? span;

        public AnalysisIssueVisualization(IReadOnlyList<IAnalysisIssueFlowVisualization> flows, IAnalysisIssueBase issue, SnapshotSpan? span)
        {
            Flows = flows;
            Issue = issue;
            CurrentFilePath = issue.FilePath;
            Span = span;
        }

        public IReadOnlyList<IAnalysisIssueFlowVisualization> Flows { get; }
        public IAnalysisIssueBase Issue { get; }
        public int StepNumber => 0;
        public IAnalysisIssueLocation Location => Issue;

        public SnapshotSpan? Span
        {
            get => span;
            set
            {
                span = value;
                NotifyPropertyChanged();
            }
        }

        public string CurrentFilePath
        {
            get => currentFilePath;
            set
            {
                currentFilePath = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        string IFilterableIssue.RuleId => Issue.RuleKey;

        string IFilterableIssue.FilePath => CurrentFilePath;

        string IFilterableIssue.LineHash => Issue.LineHash;

        string IFilterableIssue.ProjectGuid => null; // not used for non-Roslyn issues

        int? IFilterableIssue.StartLine => Issue.StartLine;
    }

    public static class AnalysisIssueVisualizationExtensions
    {
        /// <summary>
        /// Returns primary and secondary locations of IssueVisualization
        /// </summary>
        public static IEnumerable<IAnalysisIssueLocationVisualization> GetAllLocations(this IAnalysisIssueVisualization issueVisualization)
        {
            var primaryLocation = issueVisualization;
            var secondaryLocations = issueVisualization.Flows.SelectMany(x => x.Locations);

            var allLocations = new List<IAnalysisIssueLocationVisualization> {primaryLocation};
            allLocations.AddRange(secondaryLocations);

            return allLocations;
        }
    }
}
