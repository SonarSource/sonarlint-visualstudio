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
        private readonly ISolutionInfoProvider solutionInfoProvider;

        [ImportingConstructor]
        public FolderWorkspaceService(ISolutionInfoProvider solutionInfoProvider)
        {
            this.solutionInfoProvider = solutionInfoProvider;
        }

        public bool IsFolderWorkspace()
        {
            return solutionInfoProvider.IsFolderWorkspace();
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
