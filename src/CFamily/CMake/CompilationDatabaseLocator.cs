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

using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    internal interface ICompilationDatabaseLocator
    {
        /// <summary>
        /// Returns absolute path to the compilation database file of the currently active build configuration.
        /// Returns null if the file was not found.
        /// </summary>
        string Locate();
    }

    [Export(typeof(ICompilationDatabaseLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CompilationDatabaseLocator : ICompilationDatabaseLocator
    {
        internal const string CompilationDatabaseFileName = "compile_commands.json";
        internal const string CMakeSettingsFileName = "CMakeSettings.json";
        internal const string VSDefaultConfiguration = "x64-Debug";
        internal const string DefaultLocationFormat = "{0}\\out\\build\\{1}";

        private readonly IFolderWorkspaceService folderWorkspaceService;
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        [ImportingConstructor]
        public CompilationDatabaseLocator(IFolderWorkspaceService folderWorkspaceService, ILogger logger)
            : this(folderWorkspaceService, new FileSystem(), logger)
        {
        }

        public CompilationDatabaseLocator(IFolderWorkspaceService folderWorkspaceService, IFileSystem fileSystem, ILogger logger)
        {
            this.folderWorkspaceService = folderWorkspaceService;
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public string Locate()
        {
            var databaseFolderPath = GetCompilationDatabaseFolderPath();

            if (string.IsNullOrEmpty(databaseFolderPath))
            {
                return null;
            }

            var compilationDatabaseFilePath = Path.Combine(databaseFolderPath, CompilationDatabaseFileName);

            if (!fileSystem.File.Exists(compilationDatabaseFilePath))
            {
                logger.WriteLine(Resources.NoCompilationDatabaseFile, compilationDatabaseFilePath);

                return null;
            }

            logger.WriteLine(Resources.FoundCompilationDatabaseFile, compilationDatabaseFilePath);

            return compilationDatabaseFilePath;
        }

        public string GetCompilationDatabaseFolderPath()
        {
            var rootDirectory = folderWorkspaceService.FindRootDirectory();

            if (string.IsNullOrEmpty(rootDirectory))
            {
                logger.WriteLine(Resources.NoRootDirectory);
                return null;
            }

            var cmakeSettingsFullPath = Path.GetFullPath(Path.Combine(rootDirectory, CMakeSettingsFileName));
            var activeConfiguration = VSDefaultConfiguration;

            if (!fileSystem.File.Exists(cmakeSettingsFullPath))
            {
                var defaultLocation = Path.GetFullPath(string.Format(DefaultLocationFormat, rootDirectory, activeConfiguration));

                logger.WriteLine(Resources.NoCMakeSettings, CMakeSettingsFileName, rootDirectory, defaultLocation);

                return defaultLocation;
            }

            logger.WriteLine(Resources.ReadingCMakeSettings, cmakeSettingsFullPath);

            var settingsString = fileSystem.File.ReadAllText(cmakeSettingsFullPath);
            var settings = JsonConvert.DeserializeObject<CMakeSettings>(settingsString);
            var buildConfiguration = settings.Configurations?.FirstOrDefault(x => x.Name == activeConfiguration);

            if (buildConfiguration == null)
            {
                logger.WriteLine(Resources.NoBuildConfigInCMakeSettings, activeConfiguration, CMakeSettingsFileName);
                return null;
            }

            var databaseFolderPath = Path.GetFullPath(buildConfiguration.BuildRoot
                .Replace("${projectDir}", rootDirectory)
                .Replace("${name}", buildConfiguration.Name));

            return databaseFolderPath;
        }
    }
}
