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
using System.Linq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    [Export(typeof(ICompilationDatabaseLocator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CompilationDatabaseLocator : ICompilationDatabaseLocator
    {
        internal const string CompilationDatabaseFileName = "compile_commands.json";
        internal const string DefaultLocationFormat = "{0}\\out\\build\\{1}";

        private readonly IFolderWorkspaceService folderWorkspaceService;
        private readonly IFileSystem fileSystem;
        private readonly IBuildConfigProvider buildConfigProvider;
        private readonly ICMakeSettingsProvider cMakeSettingsProvider;
        private readonly IMacroEvaluationService macroEvaluationService;
        private readonly ILogger logger;

        [ImportingConstructor]
        public CompilationDatabaseLocator(IFolderWorkspaceService folderWorkspaceService, ILogger logger)
            : this(folderWorkspaceService,
                new BuildConfigProvider(logger),
                new CMakeSettingsProvider(logger), 
                new MacroEvaluationService(logger),
                new FileSystem(),
                logger)
        {
        }

        public CompilationDatabaseLocator(IFolderWorkspaceService folderWorkspaceService, 
            IBuildConfigProvider buildConfigProvider,
            ICMakeSettingsProvider cMakeSettingsProvider,
            IMacroEvaluationService macroEvaluationService,
            IFileSystem fileSystem, 
            ILogger logger)
        {
            this.folderWorkspaceService = folderWorkspaceService;
            this.fileSystem = fileSystem;
            this.buildConfigProvider = buildConfigProvider;
            this.cMakeSettingsProvider = cMakeSettingsProvider;
            this.macroEvaluationService = macroEvaluationService;
            this.logger = logger;
        }

        public string Locate()
        {
            var rootDirectory = folderWorkspaceService.FindRootDirectory();

            if (string.IsNullOrEmpty(rootDirectory))
            {
                logger.LogVerbose("[CompilationDatabaseLocator] Could not find project root directory");
                return null;
            }

            var cmakeSettings = cMakeSettingsProvider.Find(rootDirectory);
            var activeConfiguration = buildConfigProvider.GetActiveConfig(rootDirectory);

            var compilationDatabaseLocation = cmakeSettings != null
                ? GetConfiguredLocation(cmakeSettings, activeConfiguration, rootDirectory)
                : GetDefaultLocation(rootDirectory, activeConfiguration);

            if (fileSystem.File.Exists(compilationDatabaseLocation))
            {
                return compilationDatabaseLocation;
            }

            logger.WriteLine(Resources.NoCompilationDatabaseFile, compilationDatabaseLocation);

            return null;
        }

        private string GetDefaultLocation(string rootDirectory, string activeConfiguration)
        {
            var defaultDirectory = Path.GetFullPath(string.Format(DefaultLocationFormat, rootDirectory, activeConfiguration));
            var defaultLocation = Path.Combine(defaultDirectory, CompilationDatabaseFileName);

            logger.LogVerbose($"[CompilationDatabaseLocator] No CMakeSettings file was found under {rootDirectory}, returning default location: {defaultLocation}");

            return defaultLocation;
        }

        private string GetConfiguredLocation(CMakeSettingsSearchResult cMakeSettings, 
            string activeConfiguration,
            string rootDirectory)
        {
            var buildConfiguration = cMakeSettings.Settings.Configurations?.FirstOrDefault(x => x.Name == activeConfiguration);

            if (buildConfiguration == null)
            {
                logger.WriteLine(Resources.NoBuildConfigInCMakeSettings, activeConfiguration);
                return null;
            }

            if (buildConfiguration.BuildRoot == null)
            {
                logger.WriteLine(Resources.NoBuildRootInCMakeSettings, activeConfiguration);
                return null;
            }

            var evaluationContext = new EvaluationContext(buildConfiguration.Name,
                rootDirectory,
                buildConfiguration.Generator,
                cMakeSettings.RootCMakeListsFilePath,
                cMakeSettings.CMakeSettingsFilePath);

            var evaluatedBuildRoot = macroEvaluationService.Evaluate(buildConfiguration.BuildRoot, evaluationContext);
            
            if (evaluatedBuildRoot == null)
            {
                logger.WriteLine(Resources.UnableToEvaluateBuildRootProperty, buildConfiguration.BuildRoot);
                return null;
            }

            var databaseFilePath = Path.Combine(Path.GetFullPath(evaluatedBuildRoot), CompilationDatabaseFileName);

            return databaseFilePath;
        }
    }
}
