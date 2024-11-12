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


using System;

namespace SonarLint.VisualStudio.Core
{
    public interface IFolderWorkspaceMonitor
    {
        /// <summary>
        /// The event is raised whenever a folder workspace is initialized by VS to have an underlying VsHierarchy.
        /// </summary>
        /// <remarks>
        /// When a folder is opened, before any file is actually opened, VS does not structure a proper VsHierarchy and DTE.
        /// Only when a file is actually opened that VS initializes it to be "Miscellaneous Files" project.
        /// We need this event since certain parts of our code, i.e. <see cref="IAbsoluteFilePathLocator"/>,
        /// rely on VsHierarchy being initialized by the time they're called.
        /// </remarks>
        event EventHandler FolderWorkspaceInitialized;
    }
}
