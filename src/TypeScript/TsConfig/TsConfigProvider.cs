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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;

namespace SonarLint.VisualStudio.TypeScript.TsConfig
{
    internal interface ITsConfigProvider
    {
        /// <summary>
        /// Returns the tsconfig file for the given source file, or null if an applicable tsconfig could not be found.
        /// </summary>
        Task<string> GetConfigForFile(string sourceFilePath, CancellationToken cancellationToken);
    }

    [Export(typeof(ITsConfigProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class TsConfigProvider : ITsConfigProvider
    {
        private readonly ITsConfigsLocator tsConfigsLocator;
        private readonly ITypeScriptEslintBridgeClient typeScriptEslintBridgeClient;
        private readonly IFileSystem fileSystem;
        private readonly ILogger logger;

        [ImportingConstructor]
        public TsConfigProvider(ITsConfigsLocator tsConfigsLocator,
            ITypeScriptEslintBridgeClient typeScriptEslintBridgeClient,
            ILogger logger)
            : this(tsConfigsLocator, typeScriptEslintBridgeClient, new FileSystem(), logger)
        {
        }

        internal TsConfigProvider(ITsConfigsLocator tsConfigsLocator,
            ITypeScriptEslintBridgeClient typeScriptEslintBridgeClient,
            IFileSystem fileSystem,
            ILogger logger)
        {
            this.tsConfigsLocator = tsConfigsLocator;
            this.typeScriptEslintBridgeClient = typeScriptEslintBridgeClient;
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        public async Task<string> GetConfigForFile(string sourceFilePath, CancellationToken cancellationToken)
        {
            var allTsConfigsFilePaths = tsConfigsLocator.Locate(sourceFilePath);

            logger.LogVerbose(Resources.INFO_FoundTsConfigs, string.Join(Path.PathSeparator.ToString(), allTsConfigsFilePaths));

            var tsConfigFile = await GetConfigForFile(sourceFilePath,
                allTsConfigsFilePaths,
                visited: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                cancellationToken);

            return tsConfigFile;
        }

        private async Task<string> GetConfigForFile(string sourceFilePath,
            IEnumerable<string> candidateTsConfigs,
            HashSet<string> visited,
            CancellationToken cancellationToken)
        {
            foreach (var tsConfigFilePath in candidateTsConfigs)
            {
                Debug.Assert(Path.IsPathRooted(tsConfigFilePath), "Path should be absolute");

                var tsConfigToCheck = tsConfigFilePath;
                var isDirectory = fileSystem.Directory.Exists(tsConfigFilePath);

                if (isDirectory)
                {
                    tsConfigToCheck = Path.GetFullPath(Path.Combine(tsConfigToCheck, "tsconfig.json"));
                }

                if (visited.Contains(tsConfigToCheck))
                {
                    continue;
                }

                visited.Add(tsConfigToCheck);

                var response = await typeScriptEslintBridgeClient.TsConfigFiles(tsConfigToCheck, cancellationToken);

                if (response.Error != null)
                {
                    logger.WriteLine(Resources.ERR_FailedToProcessTsConfig, tsConfigToCheck, response.Error);
                    continue;
                }

                if (response.ParsingError != null)
                {
                    logger.WriteLine(Resources.ERR_FailedToProcessTsConfig_ParsingError, 
                        tsConfigToCheck, 
                        response.ParsingError.Code,
                        response.ParsingError.Line,
                        response.ParsingError.Message);
                    continue;
                }

                if (response.ProjectReferences != null && response.ProjectReferences.Any())
                {
                    logger.LogVerbose(Resources.INFO_CheckingReferencedTsConfigs, tsConfigToCheck, string.Join(Path.DirectorySeparatorChar.ToString(), response.ProjectReferences));

                    var matchingConfig = await GetConfigForFile(sourceFilePath,
                        response.ProjectReferences,
                        visited,
                        cancellationToken);

                    if (!string.IsNullOrEmpty(matchingConfig))
                    {
                        return matchingConfig;
                    }
                }

                if (response.Files != null &&
                    response.Files.Any(x => IsMatchingPath(x, sourceFilePath, tsConfigToCheck)))
                {
                    logger.WriteLine(Resources.INFO_MatchingTsConfig, sourceFilePath, tsConfigToCheck);

                    return tsConfigToCheck;
                }
            }

            return null;
        }

        private bool IsMatchingPath(string fileInTsConfig, string sourceFilePath, string tsConfigsFilePath)
        {
            try
            {
                return PathHelper.IsMatchingPath(fileInTsConfig, sourceFilePath);
            }
            catch (Exception)
            {
                logger.WriteLine(Resources.ERR_InvalidFileInTsConfig, tsConfigsFilePath, fileInTsConfig);
                return false;
            }
        }
    }
}
