﻿/*
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

namespace SonarLint.VisualStudio.Core.Analysis
{
    public interface IAnalysisRequester
    {
        /// <summary>
        /// Notification that analysis has been requested
        /// </summary>
        event EventHandler<AnalysisRequestEventArgs> AnalysisRequested;

        /// <summary>
        /// Called to request that analysis is performed
        /// </summary>
        /// <param name="filePaths">List of specific files to analyze. Can be null, in which case all opened files will be analyzed.</param>
        /// <remarks>There are no guarantees about whether the analysis is performed before the method
        /// returns or not.</remarks>
        void RequestAnalysis(params string[] filePaths);
    }

    public class AnalysisRequestEventArgs(IEnumerable<string> filePaths) : EventArgs
    {
        /// <summary>
        /// The list of files to analyze. Null/empty = analyze all files
        /// </summary>
        public IEnumerable<string> FilePaths { get; } = filePaths;
    }
}
