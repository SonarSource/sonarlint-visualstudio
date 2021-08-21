/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using EnvDTE;
using SonarLint.VisualStudio.Infrastructure.VS;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration
{
    public interface IProjectToLanguageMapper
    {
        /// <summary>
        /// Returns all of the supported Sonar languages for the specified project or Unknown
        /// if no languages are supported
        /// </summary>
        IEnumerable<Language> GetAllBindingLanguagesForProject(Project dteProject);

        /// <summary>
        /// Returns true/false if the project has at least one supported Sonar language
        /// </summary>
        bool HasSupportedLanguage(Project project);
    }

    [Export(typeof(IProjectToLanguageMapper))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ProjectToLanguageMapper : IProjectToLanguageMapper
    {
        private readonly IFolderWorkspaceService folderWorkspaceService;
        private readonly IFileSystem fileSystem;

        internal static readonly IDictionary<Guid, Language> KnownProjectTypes = new Dictionary<Guid, Language>()
        {
            { new Guid(ProjectSystemHelper.CSharpProjectKind), Language.CSharp },
            { new Guid(ProjectSystemHelper.VbProjectKind), Language.VBNET },
            { new Guid(ProjectSystemHelper.CSharpCoreProjectKind),  Language.CSharp },
            { new Guid(ProjectSystemHelper.VbCoreProjectKind), Language.VBNET },
            { new Guid(ProjectSystemHelper.CppProjectKind), Language.Cpp }
        };

        [ImportingConstructor]
        public ProjectToLanguageMapper(IFolderWorkspaceService folderWorkspaceService)
            : this(folderWorkspaceService, new FileSystem())
        {
        }

        internal ProjectToLanguageMapper(IFolderWorkspaceService folderWorkspaceService, IFileSystem fileSystem)
        {
            this.folderWorkspaceService = folderWorkspaceService;
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Returns the supported Sonar language for the specified project or Unknown
        /// if no languages are supported
        /// </summary>
        /// <returns>
        /// Previously the code assumed a one-to-one mapping between project types and languages.
        /// The worked when the only supported languages were C# and VB. It doesn't work now that
        /// connected mode is supported for C++ projects (which can have both C++ and C files).
        /// New code should call <see cref="GetAllBindingLanguagesForProject(EnvDTE.Project)"/> instead
        /// and handle the fact that there could be multiple supported languages.
        /// </returns>
        private Language GetLanguageForProject(Project dteProject)
        {
            if (dteProject == null)
            {
                throw new ArgumentNullException(nameof(dteProject));
            }

            if (!Guid.TryParse(dteProject.Kind, out var projectKind))
            {
                return Language.Unknown;
            }

            if (KnownProjectTypes.TryGetValue(projectKind, out var language))
            {
                return language;
            }

            var isOpenAsFolder = folderWorkspaceService.IsFolderWorkspace();

            if (isOpenAsFolder)
            {
                var rootDirectory = folderWorkspaceService.FindRootDirectory();
                var isCMake = fileSystem.Directory.EnumerateFiles(rootDirectory, "CMakeLists.txt", SearchOption.AllDirectories).Any();

                if (isCMake)
                {
                    return Language.Cpp;
                }
            }

            return Language.Unknown;
        }

        public IEnumerable<Language> GetAllBindingLanguagesForProject(Project dteProject)
        {
            var language = GetLanguageForProject(dteProject);

            if (Language.Cpp.Equals(language))
            {
                return new[] { Language.Cpp, Language.C };
            }

            return new[] { language };
        }

        public bool HasSupportedLanguage(Project project)
        {
            var languages = GetAllBindingLanguagesForProject(project);

            return languages.Any(x => x.IsSupported);
        }
    }
}
