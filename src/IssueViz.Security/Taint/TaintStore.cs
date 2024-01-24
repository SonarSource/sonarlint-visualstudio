/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{
    internal interface ITaintStore : IIssuesStore
    {
        /// <summary>
        /// Removes all existing visualizations and initializes the store to the given collection.
        /// Can be called multiple times.
        /// </summary>
        void Set(IEnumerable<IAnalysisIssueVisualization> issueVisualizations, AnalysisInformation analysisInformation);

        /// <summary>
        /// Returns additional analysis information for the existing visualizations in the store.
        /// </summary>
        AnalysisInformation GetAnalysisInformation();

        /// <summary>
        /// Add the given issue to the existing list of visualizations.
        /// If <see cref="GetAnalysisInformation"/> is null, the operation is ignored.
        /// </summary>
        void Add(IAnalysisIssueVisualization issueVisualization);

        /// <summary>
        /// Removes an issue with the given key from the existing list of visualizations.
        /// If no matching issue is found, the operation is ignored.
        /// </summary>
        void Remove(string issueKey);
    }

    [Export(typeof(ITaintStore))]
    [Export(typeof(IIssuesStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TaintStore : ITaintStore
    {
        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        private static readonly object Locker = new object();

        private List<IAnalysisIssueVisualization> taintVulnerabilities = new List<IAnalysisIssueVisualization>();
        private AnalysisInformation analysisInformation;

        public IReadOnlyCollection<IAnalysisIssueVisualization> GetAll()
        {
            lock (Locker)
            {
                return taintVulnerabilities.ToList();
            }
        }

        public AnalysisInformation GetAnalysisInformation() => analysisInformation;

        public void Add(IAnalysisIssueVisualization issueVisualization)
        {
            if (issueVisualization == null)
            {
                throw new ArgumentNullException(nameof(issueVisualization));
            }

            lock (Locker)
            {
                if (analysisInformation == null)
                {
                    return;
                }

                if (taintVulnerabilities.Contains(issueVisualization, TaintAnalysisIssueVisualizationByIssueKeyEqualityComparer.Instance))
                {
                    return;
                }

                taintVulnerabilities.Add(issueVisualization);

                NotifyIssuesChanged(Array.Empty<IAnalysisIssueVisualization>(), new[] { issueVisualization });
            }
        }

        public void Remove(string issueKey)
        {
            if (issueKey == null)
            {
                throw new ArgumentNullException(nameof(issueKey));
            }

            lock (Locker)
            {
                var indexToRemove =
                    taintVulnerabilities.FindIndex(issueViz => ((ITaintIssue)issueViz.Issue).IssueKey.Equals(issueKey));

                if (indexToRemove == -1)
                {
                    return;
                }

                var valueToRemove = taintVulnerabilities[indexToRemove];
                taintVulnerabilities.RemoveAt(indexToRemove);

                NotifyIssuesChanged(new[] { valueToRemove }, Array.Empty<IAnalysisIssueVisualization>());
            }
        }

        public void Set(IEnumerable<IAnalysisIssueVisualization> issueVisualizations, AnalysisInformation analysisInformation)
        {
            if (issueVisualizations == null)
            {
                throw new ArgumentNullException(nameof(issueVisualizations));
            }

            lock (Locker)
            {
                this.analysisInformation = analysisInformation;

                var oldIssues = taintVulnerabilities;
                taintVulnerabilities = issueVisualizations.ToList();

                var removedIssues = oldIssues.Except(taintVulnerabilities, TaintAnalysisIssueVisualizationByIssueKeyEqualityComparer.Instance).ToArray();
                var addedIssues = taintVulnerabilities.Except(oldIssues, TaintAnalysisIssueVisualizationByIssueKeyEqualityComparer.Instance).ToArray();

                NotifyIssuesChanged(removedIssues, addedIssues);
            }
        }

        private void NotifyIssuesChanged(
            IReadOnlyCollection<IAnalysisIssueVisualization> removedIssues,
            IReadOnlyCollection<IAnalysisIssueVisualization> addedIssues)
        {
            // Hacky workaround for #4066 - always raise the event, even if
            // the set of added/removed files is empty.
            // See also #4070.
            IssuesChanged?.Invoke(this, new IssuesChangedEventArgs(removedIssues, addedIssues));
        }

        private sealed class TaintAnalysisIssueVisualizationByIssueKeyEqualityComparer : IEqualityComparer<IAnalysisIssueVisualization>
        {
            public static readonly TaintAnalysisIssueVisualizationByIssueKeyEqualityComparer Instance =
                new TaintAnalysisIssueVisualizationByIssueKeyEqualityComparer();

            private TaintAnalysisIssueVisualizationByIssueKeyEqualityComparer(){}

            public bool Equals(IAnalysisIssueVisualization first, IAnalysisIssueVisualization second)
            {
                if (ReferenceEquals(first, second))
                {
                    return true;
                }

                if (first == null || second == null)
                {
                    return false;
                }

                var firstTaintIssue = (ITaintIssue)first.Issue;
                var secondTaintIssue = (ITaintIssue)second.Issue;

                return firstTaintIssue.IssueKey.Equals(secondTaintIssue.IssueKey);
            }
            

            public int GetHashCode(IAnalysisIssueVisualization obj)
            {
                return (obj.Issue != null ? ((ITaintIssue)obj.Issue).IssueKey.GetHashCode() : 0);
            }
        }
    }
}
