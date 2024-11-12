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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{
    public interface ITaintStore : IIssuesStore
    {
        /// <summary>
        /// Removes all existing visualizations and initializes the store to the given collection.
        /// Can be called multiple times.
        /// </summary>
        void Set(IReadOnlyCollection<IAnalysisIssueVisualization> issueVisualizations, string newConfigurationScope);

        string ConfigurationScope { get; }

        /// <summary>
        /// Add the given issue to the existing list of visualizations.
        /// If <see cref="GetAnalysisInformation"/> is null, the operation is ignored.
        /// </summary>
        void Add(IAnalysisIssueVisualization issueVisualization);

        /// <summary>
        /// Removes an issue with the given key from the existing list of visualizations.
        /// If no matching issue is found, the operation is ignored.
        /// </summary>
        void Remove(Guid id);
    }

    [Export(typeof(ITaintStore))]
    [Export(typeof(IIssuesStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TaintStore : ITaintStore
    {
        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        private readonly object locker = new object();

        private string configurationScope;
        private List<IAnalysisIssueVisualization> taintVulnerabilities = new List<IAnalysisIssueVisualization>();

        public IReadOnlyCollection<IAnalysisIssueVisualization> GetAll()
        {
            lock (locker)
            {
                return taintVulnerabilities.ToList();
            }
        }

        public void Add(IAnalysisIssueVisualization issueVisualization)
        {
            if (issueVisualization == null)
            {
                throw new ArgumentNullException(nameof(issueVisualization));
            }

            lock (locker)
            {
                if (configurationScope == null)
                {
                    return;
                }

                if (taintVulnerabilities.Contains(issueVisualization, TaintAnalysisIssueVisualizationByIssueKeyEqualityComparer.Instance))
                {
                    return;
                }

                taintVulnerabilities.Add(issueVisualization);

            }

            NotifyIssuesChanged([], [issueVisualization]);
        }

        public void Remove(Guid id)
        {
            IAnalysisIssueVisualization valueToRemove;

            lock (locker)
            {
                if (configurationScope == null)
                {
                    return;
                }

                var indexToRemove =
                    taintVulnerabilities.FindIndex(issueViz => ((ITaintIssue)issueViz.Issue).Id.Equals(id));

                if (indexToRemove == -1)
                {
                    return;
                }

                valueToRemove = taintVulnerabilities[indexToRemove];
                taintVulnerabilities.RemoveAt(indexToRemove);

            }

            NotifyIssuesChanged([valueToRemove], []);
        }

        public void Set(IReadOnlyCollection<IAnalysisIssueVisualization> issueVisualizations, string newConfigurationScope)
        {
            if (issueVisualizations == null)
            {
                throw new ArgumentNullException(nameof(issueVisualizations));
            }

            if (issueVisualizations.Count > 0 && newConfigurationScope == null)
            {
                throw new ArgumentNullException(nameof(newConfigurationScope));
            }

            IAnalysisIssueVisualization[] removedIssues;
            IAnalysisIssueVisualization[] addedIssues;

            lock (locker)
            {
                var oldIssues = taintVulnerabilities;
                taintVulnerabilities = issueVisualizations.ToList();
                configurationScope = newConfigurationScope;


                removedIssues = oldIssues.Except(taintVulnerabilities, TaintAnalysisIssueVisualizationByIssueKeyEqualityComparer.Instance).ToArray();
                addedIssues = taintVulnerabilities.Except(oldIssues, TaintAnalysisIssueVisualizationByIssueKeyEqualityComparer.Instance).ToArray();
            }

            NotifyIssuesChanged(removedIssues, addedIssues);
        }

        public string ConfigurationScope
        {
            get
            {
                lock (locker)
                {
                    return configurationScope;
                }
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

                return firstTaintIssue.Id.Equals(secondTaintIssue.Id);
            }


            public int GetHashCode(IAnalysisIssueVisualization obj)
            {
                return (obj.Issue != null ? ((ITaintIssue)obj.Issue).Id.GetHashCode() : 0);
            }
        }
    }
}
