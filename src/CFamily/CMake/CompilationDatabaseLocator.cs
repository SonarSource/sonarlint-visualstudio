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
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.VisualStudio;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;

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
        internal const string DefaultLocationFormat = "{0}\\out\\build\\{1}";

        private readonly IFolderWorkspaceService folderWorkspaceService;
        private readonly IFileSystem fileSystem;
        private readonly IBuildConfigProvider buildConfigProvider;
        private readonly ILogger logger;

        [ImportingConstructor]
        public CompilationDatabaseLocator(IFolderWorkspaceService folderWorkspaceService, ILogger logger)
            : this(folderWorkspaceService,
                new BuildConfigProvider(logger),
                new FileSystem(),
                logger)
        {
        }

        public CompilationDatabaseLocator(IFolderWorkspaceService folderWorkspaceService, 
            IBuildConfigProvider buildConfigProvider,
            IFileSystem fileSystem, 
            ILogger logger)
        {
            this.folderWorkspaceService = folderWorkspaceService;
            this.fileSystem = fileSystem;
            this.buildConfigProvider = buildConfigProvider;
            this.logger = logger;
        }

        public string Locate()
        {
            var rootDirectory = folderWorkspaceService.FindRootDirectory();

            if (string.IsNullOrEmpty(rootDirectory))
            {
                logger.LogDebug("[CompilationDatabaseLocator] Could not find project root directory");
                return null;
            }

            var cmakeSettingsFullPath = Path.GetFullPath(Path.Combine(rootDirectory, CMakeSettingsFileName));
            var activeConfiguration = buildConfigProvider.GetActiveConfig(rootDirectory);

            return fileSystem.File.Exists(cmakeSettingsFullPath)
                ? GetConfiguredLocation(cmakeSettingsFullPath, activeConfiguration, rootDirectory)
                : GetDefaultLocation(rootDirectory, activeConfiguration);
        }

        private string GetDefaultLocation(string rootDirectory, string activeConfiguration)
        {
            var defaultDirectory = Path.GetFullPath(string.Format(DefaultLocationFormat, rootDirectory, activeConfiguration));
            var defaultLocation = Path.Combine(defaultDirectory, CompilationDatabaseFileName);

            logger.LogDebug($"[CompilationDatabaseLocator] No {CMakeSettingsFileName} was found under {rootDirectory}, returning default location: {defaultLocation}");

            return defaultLocation;
        }

        private string GetConfiguredLocation(string cmakeSettingsFullPath, string activeConfiguration, string rootDirectory)
        {
            logger.LogDebug($"[CompilationDatabaseLocator] Reading {cmakeSettingsFullPath}...");
            CMakeSettings settings;

            try
            {
                var settingsString = fileSystem.File.ReadAllText(cmakeSettingsFullPath);
                settings = JsonConvert.DeserializeObject<CMakeSettings>(settingsString);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.BadCMakeSettings, ex);
                return null;
            }

            var buildConfiguration = settings.Configurations?.FirstOrDefault(x => x.Name == activeConfiguration);

            if (buildConfiguration == null)
            {
                logger.WriteLine(Resources.NoBuildConfigInCMakeSettings, activeConfiguration, CMakeSettingsFileName);
                return null;
            }

            var databaseFolderPath = Path.GetFullPath(buildConfiguration.BuildRoot
                .Replace("${projectDir}", rootDirectory)
                .Replace("${name}", buildConfiguration.Name));

            var databaseFilePath = Path.Combine(databaseFolderPath, CompilationDatabaseFileName);

            return databaseFilePath;
        }
    }
}
