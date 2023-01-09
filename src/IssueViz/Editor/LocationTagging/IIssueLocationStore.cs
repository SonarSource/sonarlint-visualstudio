﻿/*
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
        /// Used by callers to notify the service that the data for issues in the specified
        /// files has been updated
        /// </summary>
        /// <remarks>Allows the service to take any necessary action to update the UI.
        /// This is a batch notification. We don't want to notify for each individual location change as that would be
        /// too noisy.
        /// </remarks>
        void Refresh(IEnumerable<string> affectedFilePaths);

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
