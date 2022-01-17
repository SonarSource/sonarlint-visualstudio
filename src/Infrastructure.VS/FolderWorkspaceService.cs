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
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Infrastructure.VS
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

    [Export(typeof(IFolderWorkspaceService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class FolderWorkspaceService : IFolderWorkspaceService
    {
        /// <summary>
        /// See https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.__vspropid7?view=visualstudiosdk-2019
        /// The enum is not available in VS 2015 API.
        /// </summary>
        internal const int VSPROPID_IsInOpenFolderMode = -8044;

        private readonly IVsSolution vsSolution;

        [ImportingConstructor]
        public FolderWorkspaceService([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
        }

        public bool IsFolderWorkspace()
        {
            // "Open as Folder" was introduced in VS2017, so in VS2015 the hr result will be `E_NOTIMPL` and `isOpenAsFolder` will be null.
            var hr = vsSolution.GetProperty(VSPROPID_IsInOpenFolderMode, out var isOpenAsFolder);
            Debug.Assert(hr == VSConstants.S_OK || hr == VSConstants.E_NOTIMPL, "Failed to retrieve VSPROPID_IsInOpenFolderMode");

            return isOpenAsFolder != null && (bool)isOpenAsFolder;
        }

        public string FindRootDirectory()
        {
            if (!IsFolderWorkspace())
            {
                return null;
            }

            // For projects that are opened as folder, the root IVsHierarchy is the "Miscellaneous Files" folder.
            // This folder doesn't have a directory path so we need to take the directory path from IVsSolution.
            var hr = vsSolution.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out var solutionDirectory);

            Debug.Assert(hr == VSConstants.S_OK || hr == VSConstants.E_NOTIMPL,
                "Failed to retrieve VSPROPID_SolutionDirectory");

            return (string)solutionDirectory;
        }
    }
}
