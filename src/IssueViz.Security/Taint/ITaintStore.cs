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

using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{
    public interface ITaintStoreReader : IIssuesStore
    {
        /// <summary>
        /// Returns current configuration scope id. Null if store is Reset
        /// </summary>
        string ConfigurationScope { get; }
    }

    public interface ITaintStore : ITaintStoreReader
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
        /// Applies updates to current store. If store is Reset or configuration scope is different, update is ignored.
        /// </summary>
        void Update(TaintVulnerabilitiesUpdate taintVulnerabilitiesUpdate);
    }

    /// <summary>
    /// Interface for the File aware taint store (returns only taints for open files, raises added taints event only for open files, etc.)
    /// </summary>
    internal interface IFileAwareTaintStore : ITaintStoreReader, IDisposable;

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

}
