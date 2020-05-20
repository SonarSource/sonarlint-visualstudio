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
using System.Collections;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
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
            var projectRootDirectory = Path.GetDirectoryName(project.FullName);
            var conflictingFileFullPath = Path.Combine(projectRootDirectory, additionalFileName);

            if (Exists(conflictingFileFullPath))
            {
                conflictedAdditionalFilePath = conflictingFileFullPath;
                return true;
            }

            return IsReferencedUnderProjectFolder(project, additionalFileName, out conflictedAdditionalFilePath);
        }

        private bool Exists(string fileName)
        {
            // For old-style SDK projects, the file can exist on disk but not referenced in the project, so we check using the file system
            return fileSystem.File.Exists(fileName);
        }

        private bool IsReferencedUnderProjectFolder(Project project, string additionalFileName, out string conflictedAdditionalFilePath)
        {
            // When the file is under a folder, VS API cannot detect it: IVsProject.IsDocumentInProject/project.ProjectItems do not work.
            // And we cannot detect using the file system, since the file can be referenced as a link.

            var projectXml = fileSystem.File.ReadAllText(project.FullName);
            var xDocument = XDocument.Load(new StringReader(projectXml));
            var xPathEvaluate = xDocument.XPathEvaluate("//*[local-name() = 'Project']//*[local-name() = 'ItemGroup']//*[local-name() = 'AdditionalFiles']/@Include") as IEnumerable;

            var hasAdditionalFile = xPathEvaluate
                .Cast<XAttribute>()
                .FirstOrDefault(x => additionalFileName.Equals(Path.GetFileName(x.Value), StringComparison.OrdinalIgnoreCase));

            if (hasAdditionalFile != null)
            {
                conflictedAdditionalFilePath = hasAdditionalFile.Value;
                return true;
            }

            conflictedAdditionalFilePath = string.Empty;
            return false;
        }
    }
}
