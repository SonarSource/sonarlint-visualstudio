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

        public AdditionalFileConflictChecker()
            : this(new FileSystem())
        {
        }

        internal AdditionalFileConflictChecker(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public bool HasConflictingAdditionalFile(Project project, string additionalFileName, out string conflictingAdditionalFilePath)
        {
            return ExistsUnderRootFolder(project, additionalFileName, out conflictingAdditionalFilePath) ||
                   ExistsInProject(project, additionalFileName, out conflictingAdditionalFilePath);
        }

        private bool ExistsUnderRootFolder(Project project, string additionalFileName, out string conflictingAdditionalFilePath)
        {
            var projectRootDirectory = Path.GetDirectoryName(project.FullName);
            conflictingAdditionalFilePath = Path.Combine(projectRootDirectory, additionalFileName);

            // For old-style MSBuild projects, the file can exist on disk but not referenced in the project, so we check using the file system
            return fileSystem.File.Exists(conflictingAdditionalFilePath);
        }

        private bool ExistsInProject(Project project, string additionalFileName, out string conflictingAdditionalFilePath)
        {
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                if (HasAdditionalFile(projectItem, additionalFileName, out conflictingAdditionalFilePath))
                {
                    return true;
                }
            }

            conflictingAdditionalFilePath = string.Empty;
            return false;
        }

        private bool HasAdditionalFile(ProjectItem projectItem, string additionalFileName, out string conflictingAdditionalFilePath)
        {
            if (projectItem.FileNames[0].EndsWith(additionalFileName))
            {
                var itemTypeProperty = VsShellUtils.FindProperty(projectItem.Properties, Constants.ItemTypePropertyKey);
                var isMarkedAsAdditionalFile = Constants.AdditionalFilesItemTypeName.Equals(itemTypeProperty.Value?.ToString(), StringComparison.OrdinalIgnoreCase);

                if (isMarkedAsAdditionalFile)
                {
                    conflictingAdditionalFilePath = projectItem.FileNames[0];
                    return true;
                }
            }

            foreach (ProjectItem subItem in projectItem.ProjectItems)
            {
                if (HasAdditionalFile(subItem, additionalFileName, out conflictingAdditionalFilePath))
                {
                    return true;
                }
            }

            conflictingAdditionalFilePath = string.Empty;
            return false;
        }
    }
}
