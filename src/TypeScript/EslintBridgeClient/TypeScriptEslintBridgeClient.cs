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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    internal interface ITypeScriptEslintBridgeClient : IEslintBridgeClient
    {
        /// <summary>
        /// Notifies eslint-bridge that a different config file will be used.
        /// </summary>
        /// <remarks>Resource optimisation - tells the eslintbridge that it can discard some cached data</remarks>
        Task NewTsConfig(CancellationToken cancellationToken);

        /// <summary>
        /// Returns the source files and projects referenced in the tsconfig file
        /// </summary>
        Task<TSConfigResponse> TsConfigFiles(string tsConfigFilePath, CancellationToken cancellationToken);
    }

    [Export(typeof(ITypeScriptEslintBridgeClient))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class TypeScriptEslintBridgeClient : JsTsEslintBridgeClientBase, ITypeScriptEslintBridgeClient
    {
        [ImportingConstructor]
        public TypeScriptEslintBridgeClient(IEslintBridgeProcessFactory eslintBridgeProcessFactory, ILogger logger)
            : this(eslintBridgeProcessFactory.Create(), new EslintBridgeHttpWrapper(logger), logger)
        {
        }

        internal /* for testing */ TypeScriptEslintBridgeClient(IEslintBridgeProcess eslintBridgeProcess, IEslintBridgeHttpWrapper httpWrapper, ILogger logger)
            : base("analyze-ts", eslintBridgeProcess, httpWrapper, new AnalysisConfiguration(), new EslintBridgeKeepAlive(eslintBridgeProcess, logger))
        {
        }

        public async Task NewTsConfig(CancellationToken cancellationToken)
        {
            var responseString = await EslintBridgeHttpHelper.MakeCallAsync(eslintBridgeProcess, httpWrapper, "new-tsconfig", null, cancellationToken);

            if (!responseString.Equals("OK!", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(string.Format(Resources.ERR_InvalidResponse, responseString));
            }
        }

        public async Task<TSConfigResponse> TsConfigFiles(string tsConfigFilePath, CancellationToken cancellationToken)
        {
            var request = new TsConfigRequest { TsConfig = tsConfigFilePath };
            var responseString = await EslintBridgeHttpHelper.MakeCallAsync(eslintBridgeProcess, httpWrapper, "tsconfig-files", request, cancellationToken);

            if (string.IsNullOrEmpty(responseString))
            {
                throw new InvalidOperationException(string.Format(Resources.ERR_InvalidResponse, responseString));
            }

            return JsonConvert.DeserializeObject<TSConfigResponse>(responseString);
        }
    }
}
