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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    [Export(typeof(IFolderWorkspaceService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class FolderWorkspaceService : IFolderWorkspaceService
    {
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public FolderWorkspaceService(ISolutionInfoProvider solutionInfoProvider) : this(solutionInfoProvider, new FileSystem())
        {
        }

        public FolderWorkspaceService(ISolutionInfoProvider solutionInfoProvider, IFileSystem fileSystem)
        {
            this.solutionInfoProvider = solutionInfoProvider;
            this.fileSystem = fileSystem;
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

        public IReadOnlyCollection<string> ListFiles()
        {
            var root = FindRootDirectory();

            return root is not null ?
                fileSystem.Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Where(x => !x.Contains("\\node_modules\\")).ToList() :
                [];
        }
    }
}
