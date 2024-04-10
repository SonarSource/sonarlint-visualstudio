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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Helpers;
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

        [ImportingConstructor]
        public ProjectLanguageIndicator(ISonarLanguageRecognizer sonarLanguageRecognizer,
            IFolderWorkspaceService folderWorkspaceService,
            ILogger logger)
        {
            this.sonarLanguageRecognizer = sonarLanguageRecognizer;
            this.folderWorkspaceService = folderWorkspaceService;
            this.logger = logger;
        }

        public bool HasTargetLanguage(Project dteProject, ITargetLanguagePredicate targetLanguagePredicate)
        {
            //When opened as folder there can be a dteProject if a file is open
            //If there is a dteProject and it's opened as a folder
            //Folder search takes precedence for consistency
            if (folderWorkspaceService.IsFolderWorkspace())
            {
                return HasFileOnDisk(targetLanguagePredicate);
            }

            Debug.Assert(dteProject != null, "When it's not folder workspace we expect dteProject not to be null");

            try
            {
                var hasAnyOfLanguages = HasFileInProject(dteProject.ProjectItems, targetLanguagePredicate);

                return hasAnyOfLanguages;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(BindingStrings.FailedToIdentifyLanguage, dteProject.Name, ex);

                return false;
            }
        }

        private bool HasFileOnDisk(ITargetLanguagePredicate targetLanguagePredicate) =>
            folderWorkspaceService.ListFiles().Any(fileName => IsTargetLanguage(fileName, targetLanguagePredicate));

        private bool IsTargetLanguage(string fileName, ITargetLanguagePredicate targetLanguagePredicate)
        {
            var normalizedExtension = FileExtensionExtractor.GetNormalizedExtension(fileName);

            if (string.IsNullOrEmpty(normalizedExtension))
            {
                return false;
            }

            var analysisLanguage = sonarLanguageRecognizer.GetAnalysisLanguageFromExtension(normalizedExtension);
            return analysisLanguage.HasValue && targetLanguagePredicate.IsTargetLanguage(analysisLanguage.Value, normalizedExtension);
        }

        private bool HasFileInProject(ProjectItems projectItems, ITargetLanguagePredicate analysisTargetLanguages)
        {
            foreach (ProjectItem item in projectItems)
            {
                if (IsTargetLanguage(item.Name, analysisTargetLanguages))
                {
                    return true;
                }

                if (item.ProjectItems?.Count > 0)
                {
                    if (HasFileInProject(item.ProjectItems, analysisTargetLanguages))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
