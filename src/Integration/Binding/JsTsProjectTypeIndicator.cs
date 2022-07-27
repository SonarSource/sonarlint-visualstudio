﻿/*
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using EnvDTE;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.Binding
{
    [Export(typeof(IJsTsProjectTypeIndicator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class JsTsProjectTypeIndicator : IJsTsProjectTypeIndicator
    {
        readonly ISonarLanguageRecognizer sonarLanguageRecognizer;
        readonly IFolderWorkspaceService folderWorkspaceService;
        readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public JsTsProjectTypeIndicator(ISonarLanguageRecognizer sonarLanguageRecognizer, IFolderWorkspaceService folderWorkspaceService)
            : this(sonarLanguageRecognizer, folderWorkspaceService, new FileSystem())
        {

        }

        internal JsTsProjectTypeIndicator(ISonarLanguageRecognizer sonarLanguageRecognizer, IFolderWorkspaceService folderWorkspaceService, IFileSystem fileSystem)
        {
            this.sonarLanguageRecognizer = sonarLanguageRecognizer;
            this.folderWorkspaceService = folderWorkspaceService;
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
            else
            {
                Debug.Assert(dteProject != null, "When it's not folder workspace we expect dteProject not to be null");

                return HasJsTsFileInProject(dteProject.ProjectItems);
            }            
        }

        private bool HasJsTsFileOnDisk()
        {
            string root = folderWorkspaceService.FindRootDirectory();

            var fileList = fileSystem.Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Where(x => !x.Contains("\\node_modules\\"));

            return (fileList.Any(IsFileJsTs));
        }

        private bool IsFileJsTs(string fileName)
        {
            var analysisLanguage = sonarLanguageRecognizer.GetAnalysisLanguageFromExtension(fileName);
            return analysisLanguage == Core.Analysis.AnalysisLanguage.Javascript || analysisLanguage == Core.Analysis.AnalysisLanguage.TypeScript;
        }

        private bool HasJsTsFileInProject(ProjectItems projectItems)
        {

            foreach (ProjectItem item in projectItems)
            {
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
    }
}
