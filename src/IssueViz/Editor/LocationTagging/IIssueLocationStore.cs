/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging
{
    public interface IIssueLocationStore
    {
        /// <summary>
        /// Notifies listeners that the set of issues has changed
        /// </summary>
        event EventHandler<IssuesChangedEventArgs> IssuesChanged;

        /// <summary>
        /// Returns all primary and secondary locations for the specified file
        /// </summary>
        IEnumerable<IAnalysisIssueLocationVisualization> GetLocations(string filePath);

        /// <summary>
        /// Returns all the primary issues
        /// </summary>
        IEnumerable<IAnalysisIssueVisualization> GetIssues();

        /// <summary>
        /// Notifies the service that one or more issues in the specified files have changed
        /// independently of the content of the document changing
        /// </summary>
        /// <remarks>
        /// The set of issues can be changed without the content changing if e.g.
        /// - a new analysis has been performed
        /// - issues have been suppressed/unsuppressed
        /// <seealso cref="RefreshOnBufferChanged(string)"/>
        /// </remarks>
        void Refresh(IEnumerable<string> affectedFilePaths);

        /// <summary>
        /// Notifies the service that an underlying file has been updated
        /// </summary>
        /// <remarks>
        /// Changing the contents of a file can affect the issues in the file:
        /// - if content has been deleted then some issues may no longer exist.
        /// - if content has been added or removed then the location of issues may have changed.
        /// This method allows the service to update necessary action to update the UI.
        /// The assumption is that Editor-related components (e.g. taggers) do not need
        /// to be updated as they will already be aware of the change.
        /// <seealso cref="Refresh(IEnumerable{string})"/>
        /// </remarks>
        void RefreshOnBufferChanged(string affectedFilePath);

        /// <summary>
        /// Returns true/false if the given <see cref="issueVisualization"/> exists in the store
        /// </summary>
        bool Contains(IAnalysisIssueVisualization issueVisualization);
    }

    public class IssuesChangedEventArgs : EventArgs
    {
        public IssuesChangedEventArgs(IEnumerable<string> analyzedFiles)
        {
            AnalyzedFiles = analyzedFiles ?? Array.Empty<string>();
        }

        /// <summary>
        /// The set of files impacted by the new issues
        /// </summary>
        public IEnumerable<string> AnalyzedFiles { get; }
    }
}
