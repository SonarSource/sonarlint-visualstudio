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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
    internal sealed class EslintBridgeClient : IEslintBridgeClient
    {
        private readonly Uri baseServerUri;
        private readonly IEslintBridgeHttpWrapper httpWrapper;
        private readonly IAnalysisConfiguration analysisConfiguration;

        public EslintBridgeClient(Uri baseServerUri, ILogger logger)
            : this(baseServerUri, new EslintBridgeHttpWrapper(logger), new AnalysisConfiguration())
        {
        }

        internal EslintBridgeClient(Uri baseServerUri, 
            IEslintBridgeHttpWrapper httpWrapper,
            IAnalysisConfiguration analysisConfiguration)
        {
            this.baseServerUri = baseServerUri;
            this.httpWrapper = httpWrapper;
            this.analysisConfiguration = analysisConfiguration;
        }

        public Task InitLinter(IEnumerable<Rule> rules, CancellationToken cancellationToken)
        {
            var initLinterRequest = new InitLinterRequest
            {
                Rules = rules.ToArray(),
                Globals = analysisConfiguration.GetGlobals(),
                Environments = analysisConfiguration.GetEnvironments()
            };

            return httpWrapper.PostAsync(BuildServerUri("init-linter"), initLinterRequest, cancellationToken);
        }

        public async Task<AnalysisResponse> AnalyzeJs(string filePath, CancellationToken cancellationToken)
        {
            var analysisRequest = new AnalysisRequest
            {
                FilePath = filePath,
                IgnoreHeaderComments = true,
                TSConfigFilePaths = Array.Empty<string>() // eslint-bridge generates a default tsconfig for JS analysis
            };

            var responseString = await httpWrapper.PostAsync(BuildServerUri("analyze-js"), analysisRequest, cancellationToken);

            if (string.IsNullOrEmpty(responseString))
            {
                throw new ArgumentException("no analysis response");
            }

            return JsonConvert.DeserializeObject<AnalysisResponse>(responseString);
        }

        private Task Close()
        {
            return httpWrapper.PostAsync(BuildServerUri("close"), null, CancellationToken.None);
        }

        public async void Dispose()
        {
            try
            {
                await Close();
            }
            catch
            {
                // nothing to do if the call failed
            }

            httpWrapper.Dispose();
        }

        private Uri BuildServerUri(string endpoint) => new Uri(baseServerUri, endpoint);
    }
}
