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

        public static List<string> filesChecked = new List<string>();

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
            //If both are true folder search takes precedense for consistency
            if (folderWorkspaceService.IsFolderWorkspace())
            {
                return HasFolderJSTS();
            }
            else
            {
                Debug.Assert(dteProject != null, "When it's not folder workspace we expect dteProject not to be null");

                return HasProjectJSTS(dteProject.ProjectItems);
            }            
        }

        private bool HasFolderJSTS()
        {
            string root = folderWorkspaceService.FindRootDirectory();

            var fileList = fileSystem.Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Where(x => !x.Contains("\\node_modules\\"));

            foreach (var file in fileList)
            {
                if(IsFileJSTS(file))
                {
                    return true;
                }    
            }
            return false;
        }

        private bool IsFileJSTS(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return sonarLanguageRecognizer.GetAnalysisLanguageFromExtension(extension) == Core.Analysis.AnalysisLanguage.Javascript;
        }

        private bool HasProjectJSTS(ProjectItems projectItems)
        {

            foreach (ProjectItem item in projectItems)
            {
                if (IsFileJSTS(item.Name))
                {
                    return true;
                }
                if (item.ProjectItems?.Count > 0)
                {
                    if (HasProjectJSTS(item.ProjectItems))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
