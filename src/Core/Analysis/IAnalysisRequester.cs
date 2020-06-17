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

namespace SonarLint.VisualStudio.Core
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
        /// <param name="analyzerOptions">Any analyzer-specific options. Can be null.</param>
        /// <param name="filePaths">List of specific files to analyze. Can be null, in which case all files will be analyzed.</param>
        /// <remarks>There are no guarantees about whether the analysis is performed before the method
        /// returns or not.</remarks>
        void RequestAnalysis(IAnalyzerOptions analyzerOptions, params string[] filePaths);
    }

    public class AnalysisRequestEventArgs : EventArgs
    {
        public AnalysisRequestEventArgs(IAnalyzerOptions analyzerOptions, IEnumerable<string> filePaths)
        {
            Options = analyzerOptions;
            FilePaths = filePaths;
        }

        /// <summary>
        /// Analyzer-specific options (optional)
        /// </summary>
        public IAnalyzerOptions Options { get; }

        /// <summary>
        /// The list of files to analyze. Null/empty = analyze all files
        /// </summary>
        public IEnumerable<string> FilePaths { get; }
    }

    public static class AnalysisRequesterExtensions
    {
        public static void RequestAnalysis(this IAnalysisRequester analysisRequester, params string[] filePaths)
             => analysisRequester.RequestAnalysis(null, filePaths);
    }

}
