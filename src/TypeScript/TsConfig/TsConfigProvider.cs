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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;
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
        private readonly ILogger logger;

        [ImportingConstructor]
        public TsConfigProvider(ITsConfigsLocator tsConfigsLocator, ITypeScriptEslintBridgeClient typeScriptEslintBridgeClient, ILogger logger)
        {
            this.tsConfigsLocator = tsConfigsLocator;
            this.typeScriptEslintBridgeClient = typeScriptEslintBridgeClient;
            this.logger = logger;
        }

        public async Task<string> GetConfigForFile(string sourceFilePath, CancellationToken cancellationToken)
        {
            var allTsConfigsFilePaths = tsConfigsLocator.Locate(sourceFilePath);

            logger.LogDebug(Resources.INFO_FoundTsConfigs, string.Join(Path.PathSeparator.ToString(), allTsConfigsFilePaths));

            foreach (var tsConfigsFilePath in allTsConfigsFilePaths)
            {
                var response = await typeScriptEslintBridgeClient.TsConfigFiles(tsConfigsFilePath, cancellationToken);

                if (response.Files != null &&
                    response.Files.Any(x => IsMatchingPath(x, sourceFilePath, tsConfigsFilePath)))
                {
                    logger.WriteLine(Resources.INFO_MatchingTsConfig, sourceFilePath, tsConfigsFilePath);

                    return tsConfigsFilePath;
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
