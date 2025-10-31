/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Issues
{
    /// <summary>
    /// Methods for modification of <see cref="ILocalIssuesStore"/>
    /// </summary>
    public interface ILocalIssuesStoreUpdater
    {
        void UpdateForFile(string filePath, IEnumerable<IAnalysisIssueVisualization> issues);

        void RemoveForFile(string filePath);

        void Clear();
    }

    /// <summary>
    /// Represents the storage for locally analyzed issues and matching them against issues from the server
    /// </summary>
    internal interface ILocalIssuesStore : ILocalIssuesStoreUpdater, IIssuesStore;

    [Export(typeof(ILocalIssuesStoreUpdater))]
    [Export(typeof(ILocalIssuesStore))]
    [Export(typeof(IIssuesStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class LocalIssuesStore : ILocalIssuesStore
    {
        private static readonly List<IAnalysisIssueVisualization> EmptyList = [];

        private readonly object lockObject = new();
        private readonly IThreadHandling threadHandling;

        private readonly Dictionary<string, List<IAnalysisIssueVisualization>> fileToIssuesMapping = [];

        [ImportingConstructor]
        public LocalIssuesStore(IThreadHandling threadHandling)
        {
            this.threadHandling = threadHandling;
        }

        public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        public IReadOnlyCollection<IAnalysisIssueVisualization> GetAll()
        {
            lock (lockObject)
            {
                return GetOpenIssues().ToList();
            }
        }

        public void UpdateForFile(string filePath, IEnumerable<IAnalysisIssueVisualization> issues)
        {
            List<IAnalysisIssueVisualization> oldIssueVisualizations;
            var issuesList = issues.ToList();
            lock (lockObject)
            {
                if (fileToIssuesMapping.TryGetValue(filePath, out var oldIssues))
                {
                    oldIssueVisualizations = oldIssues
                        .ToList();
                }
                else
                {
                    oldIssueVisualizations = EmptyList;
                }

                if (!issuesList.Any() && !oldIssueVisualizations.Any())
                {
                    return;
                }

                fileToIssuesMapping[filePath] = issuesList;
            }
            NotifyIssuesChanged(new IssuesChangedEventArgs(oldIssueVisualizations, issuesList));
        }

        public void RemoveForFile(string filePath)
        {
            threadHandling.ThrowIfOnUIThread();

            List<IAnalysisIssueVisualization> localIssues;
            lock (lockObject)
            {
                if (!fileToIssuesMapping.TryGetValue(filePath, out localIssues))
                {
                    return;
                }

                fileToIssuesMapping.Remove(filePath);
            }
            NotifyIssuesChanged(new IssuesChangedEventArgs(localIssues.ToList(), EmptyList));
        }

        public void Clear()
        {
            threadHandling.ThrowIfOnUIThread();

            IssuesChangedEventArgs removedIssues;
            lock (lockObject)
            {
                removedIssues = new IssuesChangedEventArgs(fileToIssuesMapping
                        .SelectMany(x => x.Value)
                        .ToList(),
                    EmptyList);

                fileToIssuesMapping.Clear();
            }
            NotifyIssuesChanged(removedIssues);
        }

        private IEnumerable<IAnalysisIssueVisualization> GetOpenIssues() => fileToIssuesMapping.SelectMany(kvp => kvp.Value).Where(hs => !hs.IsResolved);

        private void NotifyIssuesChanged(IssuesChangedEventArgs args)
        {
            IssuesChanged?.Invoke(this, args);
        }
    }
}
