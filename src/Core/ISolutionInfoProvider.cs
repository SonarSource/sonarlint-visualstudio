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

using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Returns information about the current solution if there is one
    /// </summary>
    /// <remarks>The caller can assume that the component follows VS threading rules
    /// i.e. the implementing class is responsible for switching to the UI thread if necessary.
    /// The caller doesn't need to worry about it.
    /// </remarks>
    public interface ISolutionInfoProvider
    {
        /// <summary>
        /// Returns the full directory path for the current solution file, or null if there is not a current solution
        /// </summary>
        Task<string> GetSolutionDirectoryAsync();

        /// <summary>
        /// Returns the full directory path for the current solution file, or null if there is not a current solution
        /// </summary>
        /// <remarks>If possible, use the asynchronous version of this method. This synchronous version was added
        /// to be used only by existing non-thread-aware code that cannot easily be refactored.</remarks>
        string GetSolutionDirectory();

        /// <summary>
        /// Returns the full file path for the current solution file, or null if there is not a current solution
        /// </summary>
        Task<string> GetFullSolutionFilePathAsync();

        /// <summary>
        /// Returns the full file path for the current solution file, or null if there is not a current solution
        /// </summary>
        /// <remarks>If possible, use the asynchronous version of this method. This synchronous version was added
        /// to be used only by existing non-thread-aware code that cannot easily be refactored.</remarks>
        string GetFullSolutionFilePath();

        /// <summary>
        /// Returns true if the Solution is fully opened or false if it is not.
        /// </summary>
        Task<bool> IsSolutionFullyOpenedAsync();

        /// <summary>
        /// Returns true if the Solution is fully opened or false if it is not.
        /// </summary>
        ///  <remarks>If possible, use the asynchronous version of this method. This synchronous version was added
        /// to be used only by existing non-thread-aware code that cannot easily be refactored.</remarks>
        bool IsSolutionFullyOpened();

        /// <summary>
        /// Returns true/false if the workspace is in Open-As-Folder mode
        /// </summary>
        /// <remarks>Will always return false in VS2015 as that mode is not supported in 2015.</remarks>
        Task<bool> IsFolderWorkspaceAsync();

        /// <summary>
        /// Returns true/false if the workspace is in Open-As-Folder mode
        /// </summary>
        /// <remarks>Will always return false in VS2015 as that mode is not supported in 2015.</remarks>
        /// to be used only by existing non-thread-aware code that cannot easily be refactored.</remarks>
        bool IsFolderWorkspace();
    }
}
