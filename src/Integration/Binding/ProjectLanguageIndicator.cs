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
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using EnvDTE;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.Binding
{
    [Export(typeof(IProjectLanguageIndicator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ProjectLanguageIndicator : IProjectLanguageIndicator
    {
        private readonly ISonarLanguageRecognizer sonarLanguageRecognizer;
        private readonly IFolderWorkspaceService folderWorkspaceService;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public ProjectLanguageIndicator(ISonarLanguageRecognizer sonarLanguageRecognizer, 
            IFolderWorkspaceService folderWorkspaceService,
            ILogger logger)
            : this(sonarLanguageRecognizer, folderWorkspaceService, logger, new FileSystem())
        {

        }

        internal ProjectLanguageIndicator(ISonarLanguageRecognizer sonarLanguageRecognizer,
            IFolderWorkspaceService folderWorkspaceService,
            ILogger logger, 
            IFileSystem fileSystem)
        {
            this.sonarLanguageRecognizer = sonarLanguageRecognizer;
            this.folderWorkspaceService = folderWorkspaceService;
            this.logger = logger;
            this.fileSystem = fileSystem;
        }
        
        public bool HasOneOfTargetLanguages(Project dteProject, params AnalysisLanguage[] targetLanguages)
        {
            //When opened as folder there can be a dteProject if a file is open
            //If there is a dteProject and it's opened as a folder
            //Folder search takes precedence for consistency
            if (folderWorkspaceService.IsFolderWorkspace())
            {
                return HasFileOnDisk(targetLanguages);
            }

            Debug.Assert(dteProject != null, "When it's not folder workspace we expect dteProject not to be null");

            try
            {
                var hasAnyOfLanguages = HasFileInProject(dteProject.ProjectItems, targetLanguages);

                return hasAnyOfLanguages;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(BindingStrings.FailedToIdentifyLanguage, string.Join(",", targetLanguages), dteProject.Name, ex);

                return false;
            }
        }

        private bool HasFileOnDisk(AnalysisLanguage[] analysisLanguages)
        {
            var root = folderWorkspaceService.FindRootDirectory();

            var fileList = fileSystem.Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Where(x => !x.Contains("\\node_modules\\"));

            return fileList.Any(fileName => IsTargetLanguage(fileName, analysisLanguages));
        }

        private bool IsTargetLanguage(string fileName, AnalysisLanguage[] targetLanguages)
        {
            var analysisLanguage = sonarLanguageRecognizer.GetAnalysisLanguageFromExtension(fileName);
            return analysisLanguage.HasValue && targetLanguages.Contains(analysisLanguage.Value);
        }

        private bool HasFileInProject(ProjectItems projectItems, AnalysisLanguage[] analysisLanguages)
        {
            foreach (ProjectItem item in projectItems)
            {
                if (IsTargetLanguage(item.Name, analysisLanguages))
                {
                    return true;
                }

                if (item.ProjectItems?.Count > 0)
                {
                    if (HasFileInProject(item.ProjectItems, analysisLanguages))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
