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
        /// Used by callers to notify the service that the locations in the specified
        /// files have been updated
        /// </summary>
        void LocationsUpdated(IEnumerable<string> affectedFilePaths);
    }

    public class IssuesChangedEventArgs : EventArgs
    {
        public IssuesChangedEventArgs(IEnumerable<string> affectedFiles)
        {
            AffectedFiles = affectedFiles ?? Array.Empty<string>();
        }

        /// <summary>
        /// The set of files impacted by the new issues
        /// </summary>
        public IEnumerable<string> AffectedFiles { get; }
    }
}
