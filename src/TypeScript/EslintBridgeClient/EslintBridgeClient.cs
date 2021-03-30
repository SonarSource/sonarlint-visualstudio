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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    internal interface IEslintBridgeClient : IDisposable
    {
        Task InitLinter(IEnumerable<Rule> rules, CancellationToken cancellationToken);

        Task<AnalysisResponse> AnalyzeJs(string filePath, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Matching Java implementation: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/sonar-javascript-plugin/src/main/java/org/sonar/plugins/javascript/eslint/JavaScriptEslintBasedSensor.java#L51
    /// Eslint-bridge methods: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/eslint-bridge/src/server.ts
    /// </summary>
    [Export(typeof(IEslintBridgeClient))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class EslintBridgeClient : IEslintBridgeClient
    {
        private readonly IEslintBridgeHttpWrapper httpWrapper;
        private readonly IAnalysisConfiguration analysisConfiguration;
        private readonly ILogger logger;

        [ImportingConstructor]
        public EslintBridgeClient(IEslintBridgeHttpWrapper httpWrapper, ILogger logger)
            : this(httpWrapper, new AnalysisConfiguration(), logger)
        {
        }

        internal EslintBridgeClient(IEslintBridgeHttpWrapper httpWrapper, 
            IAnalysisConfiguration analysisConfiguration,
            ILogger logger)
        {
            this.httpWrapper = httpWrapper;
            this.analysisConfiguration = analysisConfiguration;
            this.logger = logger;
        }

        public Task InitLinter(IEnumerable<Rule> rules, CancellationToken cancellationToken)
        {
            var initLinterRequest = new InitLinterRequest
            {
                Rules = rules.ToArray(),
                Globals = analysisConfiguration.GetGlobals(),
                Environments = analysisConfiguration.GetEnvironments()
            };

            return httpWrapper.PostAsync("init-linter", initLinterRequest, cancellationToken);
        }

        public async Task<AnalysisResponse> AnalyzeJs(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                var analysisRequest = new AnalysisRequest
                {
                    FilePath = filePath,
                    IgnoreHeaderComments = true,
                    TSConfigFilePaths = Array.Empty<string>() // eslint-bridge generates a default tsconfig for JS analysis
                };

                var responseString = await httpWrapper.PostAsync("analyze-js", analysisRequest, cancellationToken);

                return responseString == null ? null : JsonConvert.DeserializeObject<AnalysisResponse>(responseString);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.ERR_AnalyzeJsFailure, filePath, ex);
                return null;
            }
        }

        private Task Close()
        {
            return httpWrapper.PostAsync("close", null, CancellationToken.None);
        }

        public async void Dispose()
        {
            await Close();
            httpWrapper.Dispose();
        }
    }
}
