/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using System.IO;
using EnvDTE;
using Microsoft.Alm.Authentication;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal class SolutionBindingSerializer : FileBindingSerializer
    {
        private readonly IServiceProvider serviceProvider;

        public const string SonarQubeSolutionBindingConfigurationFileName = "SolutionBinding.sqconfig";
        public const string StoreNamespace = "SonarLint.VisualStudio.Integration";

        public SolutionBindingSerializer(IServiceProvider serviceProvider)
            : this(serviceProvider,
                  serviceProvider?.GetService<ISourceControlledFileSystem>(),
                  new SecretStore(StoreNamespace),
                  serviceProvider?.GetMefService<ILogger>(),
                  new FileWrapper())
        {
        }

        internal /*for testing purposes*/ SolutionBindingSerializer(
            IServiceProvider serviceProvider,
            ISourceControlledFileSystem sccFileSystem,
            ICredentialStore store,
            ILogger logger, IFile fileWrapper)
            : base(sccFileSystem, store, logger, fileWrapper)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            this.serviceProvider = serviceProvider;
        }

        protected override WriteMode Mode
        {
            get { return WriteMode.Queued; }
        }

        protected override bool OnSuccessfulFileWrite(string filePath)
        {
            this.AddSolutionItemFile(filePath);
            this.RemoveSolutionItemFile(filePath);
            return true;
        }

        private void AddSolutionItemFile(string configFile)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(configFile), "Invalid configuration file");

            var projectSystemHelper = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystemHelper.AssertLocalServiceIsNotNull();

            Project solutionItemsProject = projectSystemHelper.GetSolutionFolderProject(Constants.SonarQubeManagedFolderName, true);
            if (solutionItemsProject == null)
            {
                Debug.Fail("Could not find the solution items project"); // Should never happen
            }
            else
            {
                projectSystemHelper.AddFileToProject(solutionItemsProject, configFile);
            }
        }

        private void RemoveSolutionItemFile(string configFile)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(configFile), "Invalid configuration file");

            var projectSystemHelper = this.serviceProvider.GetService<IProjectSystemHelper>();
            projectSystemHelper.AssertLocalServiceIsNotNull();

            Project solutionItemsProject = projectSystemHelper.GetSolutionItemsProject(false);
            if (solutionItemsProject != null)
            {
                // Remove file from project and if project is empty, remove project from solution
                var fileName = Path.GetFileName(configFile);
                projectSystemHelper.RemoveFileFromProject(solutionItemsProject, fileName);
            }
        }

        protected override string GetFullConfigurationFilePath()
        {
            var solutionRuleSetsInfoProvider = this.serviceProvider.GetService<ISolutionRuleSetsInformationProvider>();
            string rootFolder = solutionRuleSetsInfoProvider.GetSolutionSonarQubeRulesFolder();

            // When the solution is closed return null
            if (rootFolder == null)
            {
                return null;
            }

            return Path.Combine(rootFolder, SonarQubeSolutionBindingConfigurationFileName);
        }
    }
}
