/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.ConnectedMode.Shared
{
    [Export(typeof(ISharedBindingConfigProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    internal class SharedBindingConfigProvider(
        IGitWorkspaceService gitWorkspaceService,
        ISharedFolderProvider sharedFolderProvider,
        ISolutionInfoProvider solutionInfoProvider,
        ISharedBindingConfigFileProvider sharedBindingConfigFileProvider,
        ILogger logger,
        IFileSystemService fileSystem)
        : ISharedBindingConfigProvider
    {
        private const string SharedFolderName = ".sonarlint";

        private readonly ILogger logger = logger.ForContext(Resources.SharedBindingConfigProvider_LogContext);

        public SharedBindingConfigModel GetSharedBinding()
        {
            if (GetSharedBindingFilePathOrNull() is { } sharedBindingFilePath)
            {
                return sharedBindingConfigFileProvider.ReadSharedBindingConfigFile(sharedBindingFilePath);
            }

            return null;
        }

        public string GetSharedBindingFilePathOrNull()
        {
            var sharedBindingFilePath = CalculateSharedBindingPathUnderExistingFolder();

            if (sharedBindingFilePath == null)
            {
                return null;
            }

            if (!fileSystem.File.Exists(sharedBindingFilePath))
            {
                logger.WriteLine(Resources.SharedBindingConfigProvider_SharedFileNotFound, sharedBindingFilePath);
                return null;
            }

            return sharedBindingFilePath;
        }

        public bool HasSharedBinding() => GetSharedBindingFilePathOrNull() != null;

        public string SaveSharedBinding(SharedBindingConfigModel sharedBindingConfigModel)
        {
            var fileSavePath = ChooseNewSharedBindingLocation();

            if (fileSavePath == null)
            {
                return null;
            }
            return sharedBindingConfigFileProvider.WriteSharedBindingConfigFile(fileSavePath, sharedBindingConfigModel) ? fileSavePath : null;
        }

        private string ChooseNewSharedBindingLocation() =>
            CalculateSharedBindingPathUnderExistingFolder() ?? CalculateSharedBindingPathUnderGitRoot();

        private string CalculateSharedBindingPathUnderExistingFolder()
        {
            var sonarLintFolder = sharedFolderProvider.GetSharedFolderPath();
            if (sonarLintFolder == null)
            {
                logger.WriteLine(Resources.SharedBindingConfigProvider_SharedFolderNotFound);
                return null;
            }

            var sharedBindingFilePath = Path.Combine(sonarLintFolder, SharedFileName);
            return sharedBindingFilePath;
        }

        private string CalculateSharedBindingPathUnderGitRoot()
        {
            var gitRepoDir = gitWorkspaceService.GetRepoRoot();

            if (gitRepoDir == null)
            {
                logger.WriteLine(Resources.SharedBindingConfigProvider_SavePathNotFound);
                return null;
            }

            return Path.Combine(gitRepoDir, SharedFolderName, SharedFileName);
        }

        private string SharedFileName => solutionInfoProvider.GetSolutionName() + ".json";
    }
}
