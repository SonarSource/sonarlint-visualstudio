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
using System.IO;
using System.IO.Abstractions;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class AdditionalFileConflictChecker : IAdditionalFileConflictChecker
    {
        private readonly IFileSystem fileSystem;
        private readonly IProjectSystemHelper projectSystem;

        public AdditionalFileConflictChecker(IServiceProvider serviceProvider)
            : this(serviceProvider, new FileSystem())
        {
        }

        internal AdditionalFileConflictChecker(IServiceProvider serviceProvider, IFileSystem fileSystem)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.fileSystem = fileSystem;

            projectSystem = serviceProvider.GetService<IProjectSystemHelper>();
            projectSystem.AssertLocalServiceIsNotNull();
        }

        public bool HasAnotherAdditionalFile(Project project, string expectedAdditionalFilePath, out string conflictedAdditionalFilePath)
        {
            // If the correct file is already in the project, it means we were successful in adding it and there is no clash
            if (projectSystem.IsFileInProject(project, expectedAdditionalFilePath))
            {
                conflictedAdditionalFilePath = string.Empty;
                return false;
            }

            var additionalFileName = Path.GetFileName(expectedAdditionalFilePath);

            return ExistsUnderRootFolder(project, additionalFileName, out conflictedAdditionalFilePath) ||
                   ExistsInProject(project, additionalFileName, out conflictedAdditionalFilePath);
        }

        private bool ExistsUnderRootFolder(Project project, string additionalFileName, out string conflictedAdditionalFilePath)
        {
            var projectRootDirectory = Path.GetDirectoryName(project.FullName);
            conflictedAdditionalFilePath = Path.Combine(projectRootDirectory, additionalFileName);

            // For old-style SDK projects, the file can exist on disk but not referenced in the project, so we check using the file system
            return fileSystem.File.Exists(additionalFileName);
        }

        private bool ExistsInProject(Project project, string additionalFileName, out string conflictedAdditionalFilePath)
        {
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                if (HasAdditionalFile(projectItem, additionalFileName, out conflictedAdditionalFilePath))
                {
                    return true;
                }
            }

            conflictedAdditionalFilePath = string.Empty;
            return false;
        }

        private bool HasAdditionalFile(ProjectItem projectItem, string additionalFileName, out string conflictedAdditionalFilePath)
        {
            if (projectItem.FileNames[0].EndsWith(additionalFileName))
            {
                var itemTypeProperty = VsShellUtils.FindProperty(projectItem.Properties, Constants.ItemTypePropertyKey);
                var isMarkedAsAdditionalFile = Constants.AdditionalFilesItemTypeName.Equals(itemTypeProperty.Value?.ToString(), StringComparison.OrdinalIgnoreCase);

                if (isMarkedAsAdditionalFile)
                {
                    conflictedAdditionalFilePath = projectItem.FileNames[0];
                    return true;
                }
            }

            foreach (ProjectItem subItem in projectItem.ProjectItems)
            {
                if (HasAdditionalFile(subItem, additionalFileName, out conflictedAdditionalFilePath))
                {
                    return true;
                }
            }

            conflictedAdditionalFilePath = string.Empty;
            return false;
        }
    }
}
