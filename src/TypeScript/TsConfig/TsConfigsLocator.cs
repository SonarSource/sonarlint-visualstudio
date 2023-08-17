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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.TypeScript.TsConfig
{
    internal interface ITsConfigsLocator
    {
        /// <summary>
        /// Returns all the tsconfig files in the project directory of the given <see cref="sourceFilePath"/>.
        /// </summary>
        IReadOnlyList<string> Locate(string sourceFilePath);
    }

    [Export(typeof(ITsConfigsLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class TsConfigsLocator : ITsConfigsLocator
    {
        private readonly IFolderWorkspaceService folderWorkspaceService;
        private readonly IVsHierarchyLocator vsHierarchyLocator;
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        [ImportingConstructor]
        public TsConfigsLocator(IFolderWorkspaceService folderWorkspaceService, 
            IVsHierarchyLocator vsHierarchyLocator, 
            ILogger logger)
            : this(folderWorkspaceService, vsHierarchyLocator, new FileSystem(), logger)
        {
        }

        internal TsConfigsLocator(IFolderWorkspaceService folderWorkspaceService,
            IVsHierarchyLocator vsHierarchyLocator,
            IFileSystem fileSystem, 
            ILogger logger)
        {
            this.folderWorkspaceService = folderWorkspaceService;
            this.vsHierarchyLocator = vsHierarchyLocator;
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public IReadOnlyList<string> Locate(string sourceFilePath)
        {
            var projectDirectory = GetProjectDirectory(sourceFilePath);

            if (projectDirectory == null)
            {
                return Array.Empty<string>();
            }

            // This search behavior matches the observed behavior of VS itself:
            // the tsconfig doesn't have to be included in the project but it needs to be physically under the project's directory
            var foundTsConfigs = fileSystem
                .Directory
                .EnumerateFiles(projectDirectory, "tsconfig.json", SearchOption.AllDirectories)
                .Where(x=> !x.Contains("\\node_modules\\"));

            return foundTsConfigs.ToArray();
        }

        private string GetProjectDirectory(string sourceFilePath)
        {
            var folderRootDirectory = folderWorkspaceService.FindRootDirectory();

            if (!string.IsNullOrEmpty(folderRootDirectory))
            {
                return folderRootDirectory;
            }

            var vsProject = vsHierarchyLocator.GetVsHierarchyForFile(sourceFilePath);

            if (vsProject == null)
            {
                logger.WriteLine(Resources.ERR_NoVsProject, sourceFilePath);
                return null;
            }

            var hr = vsProject.GetProperty(
                (uint) VSConstants.VSITEMID.Root,
                (int) __VSHPROPID.VSHPROPID_ProjectDir,
                out var projectDirectory);

            Debug.Assert(hr == VSConstants.S_OK || hr == VSConstants.E_NOTIMPL, "Failed to retrieve VSHPROPID_ProjectDir");

            return (string) projectDirectory;
        }
    }
}
