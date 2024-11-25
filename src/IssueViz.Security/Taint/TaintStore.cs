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
        /// Removes all existing visualizations and initializes the store to the given collection & configuration scope.
        /// Can be called multiple times.
        /// </summary>
        void Set(IReadOnlyCollection<IAnalysisIssueVisualization> issueVisualizations, string newConfigurationScope);

        /// <summary>
        /// Removes all existing visualizations and resets the configurations scope.
        /// Can be called multiple times.
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns current configuration scope id. Null if store is Reset
        /// </summary>
        string ConfigurationScope { get; }

        /// <summary>
        /// Applies updates to current store. If store is Reset or configuration scope is different, update is ignored.
        /// </summary>
        void Update(TaintVulnerabilitiesUpdate taintVulnerabilitiesUpdate);
    }

    public class TaintVulnerabilitiesUpdate(
        string configurationScope,
        IEnumerable<IAnalysisIssueVisualization> added,
        IEnumerable<IAnalysisIssueVisualization> updated,
        IEnumerable<Guid> closed)
    {
        public string ConfigurationScope { get; } = !string.IsNullOrEmpty(configurationScope) ? configurationScope : throw new ArgumentNullException(nameof(configurationScope));
        public IEnumerable<IAnalysisIssueVisualization> Added { get; } = added ?? throw new ArgumentNullException(nameof(added));
        public IEnumerable<IAnalysisIssueVisualization> Updated { get; } = updated ?? throw new ArgumentNullException(nameof(updated));
        public IEnumerable<Guid> Closed { get; } = closed ?? throw new ArgumentNullException(nameof(closed));
    }

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

        public void Reset() =>
            SetInternal([], null);

        public void Set(IReadOnlyCollection<IAnalysisIssueVisualization> issueVisualizations, string newConfigurationScope)
        {
            ValidateSet(issueVisualizations, newConfigurationScope);
            SetInternal(issueVisualizations, newConfigurationScope);
        }

        private void SetInternal(IReadOnlyCollection<IAnalysisIssueVisualization> issueVisualizations, string newConfigurationScope)
        {
            List<IAnalysisIssueVisualization> diffRemoved = [];
            List<IAnalysisIssueVisualization> diffAdded = [];

            lock (locker)
            {
                var oldVulnerabilities = taintVulnerabilities;
                taintVulnerabilities = issueVisualizations.ToDictionary(x => x.IssueId!.Value, x => x);
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

            Debug.Assert(issueVisualizations.All(x => x.IssueId.HasValue));

            if (string.IsNullOrEmpty(newConfigurationScope))
            {
                throw new ArgumentNullException(nameof(newConfigurationScope));
            }
        }

        public void Update(TaintVulnerabilitiesUpdate taintVulnerabilitiesUpdate)
        {
            ValidateUpdate(taintVulnerabilitiesUpdate);

            List<IAnalysisIssueVisualization> diffAdded = [];
            List<IAnalysisIssueVisualization> diffRemoved = [];
            lock (locker)
            {
                if (taintVulnerabilitiesUpdate.ConfigurationScope != configurationScope)
                {
                    return;
                }

                HandleClosed(taintVulnerabilitiesUpdate.Closed, diffRemoved);
                HandleUpdated(taintVulnerabilitiesUpdate.Updated, diffRemoved, diffAdded);
                HandleAdded(taintVulnerabilitiesUpdate.Added, diffAdded);
            }

            NotifyIfIssuesChanged(diffAdded, diffRemoved);
        }

        private void NotifyIfIssuesChanged(List<IAnalysisIssueVisualization> diffAdded, List<IAnalysisIssueVisualization> diffRemoved)
        {
            if (diffAdded.Count != 0 || diffRemoved.Count != 0)
            {
                NotifyIssuesChanged(diffRemoved, diffAdded);
            }
        }

        private static void ValidateUpdate(TaintVulnerabilitiesUpdate taintVulnerabilitiesUpdate)
        {
            if (taintVulnerabilitiesUpdate == null)
            {
                throw new ArgumentNullException(nameof(taintVulnerabilitiesUpdate));
            }

            Debug.Assert(taintVulnerabilitiesUpdate.Added.All(x => x.IssueId.HasValue));
            Debug.Assert(taintVulnerabilitiesUpdate.Updated.All(x => x.IssueId.HasValue));
        }

        private void HandleAdded(IEnumerable<IAnalysisIssueVisualization> added, List<IAnalysisIssueVisualization> diffAdded)
        {
            foreach (var addedVulnerability in added)
            {
                if (taintVulnerabilities.ContainsKey(addedVulnerability.IssueId!.Value))
                {
                    Debug.Fail("Taint Update: attempting to add a Vulnerability with the same id that already exists");
                    continue;
                }
                taintVulnerabilities[addedVulnerability.IssueId!.Value] = addedVulnerability;
                diffAdded.Add(addedVulnerability);
            }
        }

        private void HandleUpdated(IEnumerable<IAnalysisIssueVisualization> updated, List<IAnalysisIssueVisualization> diffRemoved, List<IAnalysisIssueVisualization> diffAdded)
        {
            foreach (var updatedVulnerability in updated)
            {
                var outdatedVulnerability = MatchToCached(updatedVulnerability);
                if (outdatedVulnerability == null)
                {
                    continue;
                }
                taintVulnerabilities[updatedVulnerability.IssueId!.Value] = updatedVulnerability;
                diffRemoved.Add(outdatedVulnerability);
                diffAdded.Add(updatedVulnerability);
            }
        }

        private void HandleClosed(IEnumerable<Guid> removed, List<IAnalysisIssueVisualization> diffRemoved)
        {
            foreach (var removedId in removed)
            {
                if (!taintVulnerabilities.TryGetValue(removedId, out var removedVulnerability))
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

        private IAnalysisIssueVisualization MatchToCached(IAnalysisIssueVisualization taintVulnerability)
        {
            if (taintVulnerabilities.TryGetValue(taintVulnerability.IssueId!.Value, out var outdatedVulnerabilityByIssueId))
            {
                return outdatedVulnerabilityByIssueId;
            }

            var issueKey = ((ITaintIssue) taintVulnerability.Issue).IssueKey;
            var outdatedVulnerabilityByServerKey = MatchByServerKey(issueKey);
            if (outdatedVulnerabilityByServerKey != null)
            {
                // Taint was not found by issue id, but was found by server key
                // This is a workaround for situations when the issue id randomly changes
                // In this case, remove the cached non-existing one to re-align the cache with the new one
                taintVulnerabilities.Remove(outdatedVulnerabilityByServerKey.IssueId!.Value);
                return outdatedVulnerabilityByServerKey;
            }

            Debug.Fail("Taint Update: attempting to update a non-existent Vulnerability");
            return null;
        }

        private IAnalysisIssueVisualization MatchByServerKey(string issueKey) =>
            taintVulnerabilities
                .Where(x => ((ITaintIssue) x.Value.Issue).IssueKey == issueKey)
                .Select(x => x.Value)
                .FirstOrDefault();
    }
}
