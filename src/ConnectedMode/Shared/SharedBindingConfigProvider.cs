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

using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.Shared
{
    [Export(typeof(ISharedBindingConfigProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SharedBindingConfigProvider : ISharedBindingConfigProvider
    {
        private const string sharedFolderName = ".sonarlint";

        private readonly IGitWorkspaceService gitWorkspaceService;
        private readonly ISharedFolderProvider sharedFolderProvider;
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly ISharedBindingConfigFileProvider sharedBindingConfigFileProvider;
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        [ImportingConstructor]
        public SharedBindingConfigProvider(IGitWorkspaceService gitWorkspaceService,
            ISharedFolderProvider sharedFolderProvider,
            ISolutionInfoProvider solutionInfoProvider,
            ISharedBindingConfigFileProvider sharedBindingConfigFileProvider,
            ILogger logger) : this(gitWorkspaceService, sharedFolderProvider, solutionInfoProvider, sharedBindingConfigFileProvider, logger, new FileSystem())
        { }

        internal SharedBindingConfigProvider(IGitWorkspaceService gitWorkspaceService,
            ISharedFolderProvider sharedFolderProvider,
            ISolutionInfoProvider solutionInfoProvider,
            ISharedBindingConfigFileProvider sharedBindingConfigFileProvider,
            ILogger logger,
            IFileSystem fileSystem)
        {
            this.gitWorkspaceService = gitWorkspaceService;
            this.sharedFolderProvider = sharedFolderProvider;
            this.solutionInfoProvider = solutionInfoProvider;
            this.sharedBindingConfigFileProvider = sharedBindingConfigFileProvider;
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        public SharedBindingConfigModel GetSharedBinding()
        {
            var sonarLintFolder = sharedFolderProvider.GetSharedFolderPath();
            if (sonarLintFolder == null)
            {
                logger.WriteLine(Resources.SharedBindingConfigProvider_SharedFolderNotFound);
                return null;
            }

            return sharedBindingConfigFileProvider.ReadSharedBindingConfigFile(SharedFilePath);
        }

        public bool HasSharedBinding()
        {
            return sharedFolderProvider.GetSharedFolderPath() != null && fileSystem.File.Exists(SharedFilePath);
        }

        public bool SaveSharedBinding(SharedBindingConfigModel sharedBindingConfigModel)
        {
            string fileSavePath = null;
            if (sharedFolderProvider.GetSharedFolderPath() != null)
            {
                fileSavePath = SharedFilePath;
            }
            else
            {
                var gitRepoDir = gitWorkspaceService.GetRepoRoot();
                if (gitRepoDir != null)
                {
                    fileSavePath = Path.Combine(gitRepoDir, sharedFolderName, SharedFileName);
                }
            }

            if (fileSavePath == null)
            {
                logger.WriteLine(Resources.SharedBindingConfigProvider_SavePathNotFound);
                return false;
            }
            return sharedBindingConfigFileProvider.WriteSharedBindingConfigFile(fileSavePath, sharedBindingConfigModel);
        }

        private string SharedFilePath => Path.Combine(sharedFolderProvider.GetSharedFolderPath(), SharedFileName);

        private string SharedFileName => solutionInfoProvider.GetSolutionName() + ".json";
    }
}
