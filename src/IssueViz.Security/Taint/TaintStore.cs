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

        void Update(TaintVulnerabilityUpdate taintVulnerabilityUpdate);
    }

    public record TaintVulnerabilityUpdate(
        IEnumerable<IAnalysisIssueVisualization> Added,
        IEnumerable<IAnalysisIssueVisualization> Updated,
        IEnumerable<Guid> Closed,
        string ConfigurationScope);

    [Export(typeof(ITaintStore))]
    [Export(typeof(IIssuesStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TaintStore : ITaintStore
    {
        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        private readonly object locker = new();

        private string configurationScope;
        private Dictionary<Guid, IAnalysisIssueVisualization> taintVulnerabilities = new();

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

        public IReadOnlyCollection<IAnalysisIssueVisualization> GetAll()
        {
            lock (locker)
            {
                return taintVulnerabilities.Values.ToList();
            }
        }

        public void Set(IReadOnlyCollection<IAnalysisIssueVisualization> issueVisualizations, string newConfigurationScope)
        {
            ValidateSet(issueVisualizations, newConfigurationScope);

            List<IAnalysisIssueVisualization> diffRemoved = new();
            List<IAnalysisIssueVisualization> diffAdded = new();

            lock (locker)
            {
                var oldVulnerabilities = taintVulnerabilities;
                taintVulnerabilities = issueVisualizations.ToDictionary(x => x.Issue.Id!.Value, x => x);
                configurationScope = newConfigurationScope;

                diffRemoved.AddRange(oldVulnerabilities.Values);
                diffAdded.AddRange(taintVulnerabilities.Values);
            }

            NotifyIssuesChanged(diffRemoved, diffAdded);
        }

        private static void ValidateSet(IReadOnlyCollection<IAnalysisIssueVisualization> issueVisualizations, string newConfigurationScope)
        {
            if (issueVisualizations == null)
            {
                throw new ArgumentNullException(nameof(issueVisualizations));
            }

            if (issueVisualizations.Count > 0 && string.IsNullOrEmpty(newConfigurationScope))
            {
                throw new ArgumentNullException(nameof(newConfigurationScope));
            }
        }

        public void Update(TaintVulnerabilityUpdate taintVulnerabilityUpdate)
        {
            ValidateUpdate(taintVulnerabilityUpdate);

            List<IAnalysisIssueVisualization> diffAdded = new();
            List<IAnalysisIssueVisualization> diffRemoved = new();
            lock (locker)
            {
                if (taintVulnerabilityUpdate.ConfigurationScope != configurationScope)
                {
                    return;
                }

                HandleClosed(taintVulnerabilityUpdate.Closed, diffAdded);
                HandleUpdated(taintVulnerabilityUpdate.Updated, diffAdded, diffRemoved);
                HandleAdded(taintVulnerabilityUpdate.Added, diffRemoved);
            }

            NotifyIssuesChanged(diffAdded, diffRemoved);
        }

        private static void ValidateUpdate(TaintVulnerabilityUpdate taintVulnerabilityUpdate)
        {
            if (taintVulnerabilityUpdate.Added == null)
            {
                throw new ArgumentNullException(nameof(taintVulnerabilityUpdate.Added));
            }

            if (taintVulnerabilityUpdate.Updated == null)
            {
                throw new ArgumentNullException(nameof(taintVulnerabilityUpdate.Updated));
            }

            if (taintVulnerabilityUpdate.Closed == null)
            {
                throw new ArgumentNullException(nameof(taintVulnerabilityUpdate.Closed));
            }

            if (string.IsNullOrEmpty(taintVulnerabilityUpdate.ConfigurationScope))
            {
                throw new ArgumentNullException(nameof(taintVulnerabilityUpdate.ConfigurationScope));
            }
        }

        private void HandleAdded(IEnumerable<IAnalysisIssueVisualization> added, List<IAnalysisIssueVisualization> diffAdded)
        {
            foreach (var addedVulnerability in added)
            {
                if (taintVulnerabilities.ContainsKey(addedVulnerability.Issue.Id!.Value))
                {
                    Debug.Fail("Taint Update: attemting to add a Vulnerability with the same id that already exists");
                    continue;
                }
                taintVulnerabilities[addedVulnerability.Issue.Id!.Value] = addedVulnerability;
                diffAdded.Add(addedVulnerability);
            }
        }

        private void HandleUpdated(IEnumerable<IAnalysisIssueVisualization> updated, List<IAnalysisIssueVisualization> diffRemoved, List<IAnalysisIssueVisualization> diffAdded)
        {
            foreach (var updatedVulnerability in updated)
            {
                if (!taintVulnerabilities.TryGetValue(updatedVulnerability.Issue.Id!.Value, out IAnalysisIssueVisualization outdatedVulnerability))
                {
                    Debug.Fail("Taint Update: attempting to update a non-existent Vulnerability");
                    continue;
                }
                taintVulnerabilities[updatedVulnerability.Issue.Id!.Value] = updatedVulnerability;
                diffRemoved.Add(outdatedVulnerability);
                diffAdded.Add(updatedVulnerability);
            }
        }

        private void HandleClosed(IEnumerable<Guid> removed, List<IAnalysisIssueVisualization> diffRemoved)
        {
            foreach (var removedId in removed)
            {
                if (!taintVulnerabilities.TryGetValue(removedId, out IAnalysisIssueVisualization removedVulnerability))
                {
                    Debug.Fail("Taint Update: attempting to remove a non-existent Vulnerability");
                    continue;
                }
                taintVulnerabilities.Remove(removedId);
                diffRemoved.Add(removedVulnerability);
            }
        }

        private void NotifyIssuesChanged(
            IReadOnlyCollection<IAnalysisIssueVisualization> removedIssues,
            IReadOnlyCollection<IAnalysisIssueVisualization> addedIssues) =>
            // Hacky workaround for #4066 - always raise the event, even if
            // the set of added/removed files is empty.
            // See also #4070.
            IssuesChanged?.Invoke(this, new IssuesChangedEventArgs(removedIssues, addedIssues));
    }
}
