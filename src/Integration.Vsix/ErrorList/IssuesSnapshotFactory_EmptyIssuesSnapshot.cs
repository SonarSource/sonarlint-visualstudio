/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using Microsoft.VisualStudio.Shell.TableManager;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Integration.Vsix.ErrorList
{
    partial class IssuesSnapshotFactory
    {
        internal /* for testing */ static IIssuesSnapshot CreateEmptySnapshot(string filePath)
            => new EmptyIssuesSnapshot(filePath);

        /// <summary>
        /// Represents a snapshot with no issues
        /// </summary>
        /// <remarks>
        /// There is a <see cref="TableEntriesSnapshotFactoryBase.EmptySnapshot"/>, which provides an empty
        /// snapshot implementation, but we can't use it because we used inheritance rather than composition.
        /// </remarks>
        private sealed class EmptyIssuesSnapshot : IIssuesSnapshot
        {
            public EmptyIssuesSnapshot(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentNullException(nameof(filePath));
                }
                
                AnalysisRunId = Guid.NewGuid();
                AnalyzedFilePath = filePath;
                FilesInSnapshot = new string[] { filePath };
                Issues = Array.Empty<IAnalysisIssueVisualization>();
            }

            #region IIssuesSnaphost members

            public Guid AnalysisRunId { get; }

            public string AnalyzedFilePath { get; private set; }

            public IEnumerable<IAnalysisIssueVisualization> Issues { get; }

            public IEnumerable<string> FilesInSnapshot { get; }

            public IEnumerable<IAnalysisIssueLocationVisualization> GetLocationsVizsForFile(string filePath)
                => Array.Empty<IAnalysisIssueLocationVisualization>();

            public IIssuesSnapshot CreateUpdatedSnapshot(string analyzedFilePath)
            {
                this.AnalyzedFilePath = analyzedFilePath;
                return this;
            }

            public IIssuesSnapshot GetUpdatedSnapshot() => this;

            #endregion IIssuesSnaphost members

            #region ITableEntriesSnapshot members

            public int Count => 0;

            public int VersionNumber => 0;

            public int IndexOf(int currentIndex, ITableEntriesSnapshot newSnapshot) => -1;

            public void StartCaching() { /* no-op */ }

            public void StopCaching() { /* no-op */ }

            public bool TryGetValue(int index, string keyName, out object content)
            {
                content = null;
                return false;
            }

            public void Dispose() { /* no-op */ }

            #endregion ITableEntriesSnapshot
        }
    }
}
