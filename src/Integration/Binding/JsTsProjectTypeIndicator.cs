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
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using EnvDTE;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.Binding
{
    [Export(typeof(IJsTsProjectTypeIndicator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class JsTsProjectTypeIndicator : IJsTsProjectTypeIndicator
    {
        private readonly ISonarLanguageRecognizer sonarLanguageRecognizer;
        private readonly IFolderWorkspaceService folderWorkspaceService;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public JsTsProjectTypeIndicator(ISonarLanguageRecognizer sonarLanguageRecognizer,
            IFolderWorkspaceService folderWorkspaceService,
            ILogger logger)
            : this(sonarLanguageRecognizer, folderWorkspaceService, logger, new FileSystem())
        {
        }

        internal JsTsProjectTypeIndicator(ISonarLanguageRecognizer sonarLanguageRecognizer,
            IFolderWorkspaceService folderWorkspaceService,
            ILogger logger,
            IFileSystem fileSystem)
        {
            this.sonarLanguageRecognizer = sonarLanguageRecognizer;
            this.folderWorkspaceService = folderWorkspaceService;
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        public bool IsJsTs(Project dteProject)
        {
            //When opened as folder there can be a dteProject if a file is open
            //If there is a dteProject and it's opened as a folder
            //Folder search takes precedense for consistency
            if (folderWorkspaceService.IsFolderWorkspace())
            {
                return HasJsTsFileOnDisk();
            }

            if (dteProject == null)
            {
                logger.LogDebug("[JsTsProjectTypeIndicator] dteProject is null");
                return false;
            }

            logger.LogDebug("[JsTsProjectTypeIndicator] Processing dteProject... ");

            LogProperty("dteProject.Name", ()=> dteProject.Name);
            LogProperty("dteProject.Kind", () => dteProject.Kind);
            LogProperty("dteProject.FileName", () => dteProject.FileName);
            LogProperty("dteProject.FullName", () => dteProject.FullName);
            LogProperty("dteProject.ExtenderCATID", () => dteProject.ExtenderCATID);
            LogProperty("dteProject.UniqueName", () => dteProject.UniqueName);

            var result = HasJsTsFileInProject(dteProject.ProjectItems);

            logger.LogDebug("[JsTsProjectTypeIndicator] Finished processing dteProject.");

            return result;
        }

        private void LogProperty(string propertyName, Func<string> getValue)
        {
            try
            {
                var propertyValue = getValue();
                logger.LogDebug("[JsTsProjectTypeIndicator] Property {0}={1}", propertyName, propertyValue);
            }
            catch (Exception e)
            {
                logger.LogDebug("[JsTsProjectTypeIndicator] Failed to log property {0} of dteProject: {1}", propertyName, e);
            }
        }

        private bool HasJsTsFileOnDisk()
        {
            var root = folderWorkspaceService.FindRootDirectory();
            var fileList = fileSystem.Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Where(x => !x.Contains("\\node_modules\\"));

            return fileList.Any(IsFileJsTs);
        }

        private bool IsFileJsTs(string fileName)
        {
            var analysisLanguage = sonarLanguageRecognizer.GetAnalysisLanguageFromExtension(fileName);
            return analysisLanguage == Core.Analysis.AnalysisLanguage.Javascript || analysisLanguage == Core.Analysis.AnalysisLanguage.TypeScript;
        }

        private bool HasJsTsFileInProject(ProjectItems projectItems)
        {
            if (projectItems == null)
            {
                logger.LogDebug("[JsTsProjectTypeIndicator] projectItems is null");
                return false;
            }
            try
            {
                LogProperty("projectItems.Count", () => projectItems.Count.ToString());

                foreach (ProjectItem item in projectItems)
                {
                    if (item == null)
                    {
                        logger.LogDebug("[JsTsProjectTypeIndicator] item is null");
                        return false;
                    }

                    if (string.IsNullOrEmpty(item.Name))
                    {
                        logger.LogDebug("[JsTsProjectTypeIndicator] item.Name is null");
                        LogProperty("item.Kind", () => item.Kind);
                        LogProperty("item.FileNames[0]", () => item.FileNames[0]);
                        return false;
                    }

                    LogProperty("item.Name", () => item.Name);

                    if (IsFileJsTs(item.Name))
                    {
                        return true;
                    }

                    if (item.ProjectItems?.Count > 0)
                    {
                        if (HasJsTsFileInProject(item.ProjectItems))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                logger.LogDebug("[JsTsProjectTypeIndicator] Exception occurred: {0} ", e);
                return false;
            }
        }
    }
}
