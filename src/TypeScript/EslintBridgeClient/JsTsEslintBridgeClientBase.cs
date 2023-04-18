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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    /// <summary>
    /// Matching Java implementation: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/sonar-javascript-plugin/src/main/java/org/sonar/plugins/javascript/eslint/JavaScriptEslintBasedSensor.java#L51
    /// Eslint-bridge methods: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/eslint-bridge/src/server.ts
    /// </summary>
    internal class JsTsEslintBridgeClientBase : EslintBridgeClientBase, IEslintBridgeClient
    {
        private readonly IAnalysisConfiguration analysisConfiguration;

        public JsTsEslintBridgeClientBase(string analyzeEndpoint, IEslintBridgeProcess eslintBridgeProcess, ILogger logger)
            : this(analyzeEndpoint, eslintBridgeProcess, new EslintBridgeHttpWrapper(logger), new AnalysisConfiguration(),
                  new EslintBridgeKeepAlive(eslintBridgeProcess, logger))
        {
        }

        internal JsTsEslintBridgeClientBase(string analyzeEndpoint,
            IEslintBridgeProcess eslintBridgeProcess,
            IEslintBridgeHttpWrapper httpWrapper,
            IAnalysisConfiguration analysisConfiguration,
            IEslintBridgeKeepAlive keepAlive)
            : base(analyzeEndpoint, eslintBridgeProcess, httpWrapper, keepAlive)
        {
            this.analysisConfiguration = analysisConfiguration;
        }

        public async Task InitLinter(IEnumerable<Rule> rules, CancellationToken cancellationToken)
        {
            var initLinterRequest = new InitLinterRequest
            {
                Rules = rules.ToArray(),
                Globals = analysisConfiguration.GetGlobals(),
                Environments = analysisConfiguration.GetEnvironments()
            };

            var responseString = await EslintBridgeHttpHelper.MakeCallAsync(eslintBridgeProcess, httpWrapper, "init-linter", initLinterRequest, cancellationToken);

            if (!"OK!".Equals(responseString))
            {
                throw new InvalidOperationException(string.Format(Resources.ERR_InvalidResponse, responseString));
            }
        }

        public async Task<AnalysisResponse> Analyze(string filePath, string tsConfigFilePath, CancellationToken cancellationToken)
        {
            var tsConfigFilePaths = tsConfigFilePath == null ? Array.Empty<string>() : new[] { tsConfigFilePath };

            var analysisRequest = new JsTsAnalysisRequest
            {
                FilePath = filePath,
                IgnoreHeaderComments = true,
                TSConfigFilePaths = tsConfigFilePaths,
                FileType = "MAIN"
            };

            var responseString = await EslintBridgeHttpHelper.MakeCallAsync(eslintBridgeProcess, httpWrapper, analyzeEndpoint, analysisRequest, cancellationToken);

            if (string.IsNullOrEmpty(responseString))
            {
                throw new InvalidOperationException(string.Format(Resources.ERR_InvalidResponse, responseString));
            }

            return JsonConvert.DeserializeObject<JsTsAnalysisResponse>(responseString);
        }
    }
}
