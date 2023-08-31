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
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
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
        private readonly ISolutionInfoProvider solutionInfoProvider;

        [ImportingConstructor]
        public FolderWorkspaceService([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, ISolutionInfoProvider solutionInfoProvider)
        {
            vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            this.solutionInfoProvider = solutionInfoProvider;
        }

        public bool IsFolderWorkspace()
        {
            // "Open as Folder" was introduced in VS2017, so in VS2015 the hr result will be `E_NOTIMPL` and `isOpenAsFolder` will be null.
            var hr = vsSolution.GetProperty(VSPROPID_IsInOpenFolderMode, out var isOpenAsFolder);
            Debug.Assert(hr == VSConstants.S_OK, "Failed to retrieve VSPROPID_IsInOpenFolderMode");

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
            return solutionInfoProvider.GetSolutionDirectory();
        }
    }
}
