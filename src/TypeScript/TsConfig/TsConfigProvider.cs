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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;

namespace SonarLint.VisualStudio.TypeScript.TsConfig
{
    internal interface ITsConfigProvider
    {
        /// <summary>
        /// Returns the tsconfig file for the given source file, or null if an applicable tsconfig could not be found.
        /// </summary>
        Task<string> GetConfigForFile(string sourceFilePath, IEslintBridgeClient eslintBridgeClient, CancellationToken cancellationToken);
    }

    [Export(typeof(ITsConfigProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class TsConfigProvider : ITsConfigProvider
    {
        private readonly ITsConfigsLocator tsConfigsLocator;

        [ImportingConstructor]
        public TsConfigProvider(ITsConfigsLocator tsConfigsLocator)
        {
            this.tsConfigsLocator = tsConfigsLocator;
        }

        public async Task<string> GetConfigForFile(string sourceFilePath, IEslintBridgeClient eslintBridgeClient, CancellationToken cancellationToken)
        {
            var allTsConfigsFilePaths = tsConfigsLocator.Locate();

            foreach (var tsConfigsFilePath in allTsConfigsFilePaths)
            {
                var response = await eslintBridgeClient.TsConfigFiles(tsConfigsFilePath, cancellationToken);

                if (response.Files != null && response.Files.Any(x=> PathHelper.IsMatchingPath(x, sourceFilePath)))
                {
                    return tsConfigsFilePath;
                }
            }

            return null;
        }
    }
}
