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

namespace SonarLint.VisualStudio.Core
{
    public interface IFolderWorkspaceService
    {
        /// <summary>
        /// Returns true/false if the workspace is in Open-As-Folder mode
        /// </summary>
        /// <remarks>Will always return false in VS2015 as that mode is not supported in 2015.</remarks>
        bool IsFolderWorkspace();

        /// <summary>
        /// Returns the root directory for Open-As-Folder projects.
        /// Will return null if the root directory could not be retrieved,
        /// or if the workspace is not in Open-As-Folder mode.
        /// </summary>
        string FindRootDirectory();
    }
}
