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
        /// <summary>
        /// Configures the linter with the set of rules to execute
        /// </summary>
        /// <remarks>This method should be called whenever the set of active rules or
        /// their configuration changes.</remarks>
        Task InitLinter(IEnumerable<Rule> rules, CancellationToken cancellationToken);

        /// <summary>
        /// Analyzes the specified file and returns the detected issues.
        /// Throws <see cref="EslintBridgeClientNotInitializedException"/> if <seealso cref="InitLinter"/> should be called.
        /// </summary>
        Task<AnalysisResponse> Analyze(string filePath, string tsConfigFilePath, CancellationToken cancellationToken);

        /// <summary>
        /// Closes running eslint-bridge server.
        /// </summary>
        Task Close();
    }

    [Serializable]
    public class EslintBridgeClientNotInitializedException : Exception
    {
        public EslintBridgeClientNotInitializedException()
        {
        }

        protected EslintBridgeClientNotInitializedException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Matching Java implementation: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/sonar-javascript-plugin/src/main/java/org/sonar/plugins/javascript/eslint/JavaScriptEslintBasedSensor.java#L51
    /// Eslint-bridge methods: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/eslint-bridge/src/server.ts
    /// </summary>
    internal class EslintBridgeClient : IEslintBridgeClient
    {
        private readonly string analyzeEndpoint;
        private readonly IEslintBridgeProcess eslintBridgeProcess;
        private readonly IEslintBridgeHttpWrapper httpWrapper;
        private readonly IAnalysisConfiguration analysisConfiguration;
        private bool isDisposed;

        public EslintBridgeClient(string analyzeEndpoint, IEslintBridgeProcess eslintBridgeProcess, ILogger logger)
            : this(analyzeEndpoint, eslintBridgeProcess, new EslintBridgeHttpWrapper(logger), new AnalysisConfiguration())
        {
        }

        internal EslintBridgeClient(string analyzeEndpoint, 
            IEslintBridgeProcess eslintBridgeProcess,
            IEslintBridgeHttpWrapper httpWrapper,
            IAnalysisConfiguration analysisConfiguration)
        {
            this.analyzeEndpoint = analyzeEndpoint;
            this.eslintBridgeProcess = eslintBridgeProcess;
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

            return MakeCall("init-linter", initLinterRequest, cancellationToken);
        }

        public async Task<AnalysisResponse> Analyze(string filePath, string tsConfigFilePath, CancellationToken cancellationToken)
        {
            var tsConfigFilePaths = tsConfigFilePath == null ? Array.Empty<string>() : new[] {tsConfigFilePath};

            var analysisRequest = new AnalysisRequest
            {
                FilePath = filePath,
                IgnoreHeaderComments = true,
                TSConfigFilePaths = tsConfigFilePaths
            };

            var result = await eslintBridgeProcess.Start();

            if (result.IsNewProcess)
            {
                throw new EslintBridgeClientNotInitializedException();
            }

            var fullServerUrl = BuildServerUri(result.Port, analyzeEndpoint);
            var responseString = await httpWrapper.PostAsync(fullServerUrl, analysisRequest, cancellationToken);

            if (string.IsNullOrEmpty(responseString))
            {
                throw new InvalidOperationException(Resources.ERR_InvalidResponse);
            }

            return JsonConvert.DeserializeObject<AnalysisResponse>(responseString);
        }

        public async Task Close()
        {
            try
            {
                await MakeCall("close", null, CancellationToken.None);
            }
            catch
            {
                // nothing to do if the call failed
            }
            eslintBridgeProcess.Stop();
        }

        #region IDisposable

        public async void Dispose()
        {
            await Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async Task Dispose(bool disposing)
        {
            if (disposing && !isDisposed)
            {
                try
                {
                    await Close();
                }
                catch
                {
                    // nothing to do if the call failed
                }

                eslintBridgeProcess.Dispose();
                httpWrapper.Dispose();
                isDisposed = true;
            }
        }

        #endregion

        protected async Task<string> MakeCall(string endpoint, object request, CancellationToken cancellationToken)
        {
            var result = await eslintBridgeProcess.Start();
            var fullServerUrl = BuildServerUri(result.Port, endpoint);
            var response = await httpWrapper.PostAsync(fullServerUrl, request, cancellationToken);

            return response;
        }

        private Uri BuildServerUri(int port, string endpoint) => new Uri($"http://localhost:{port}/{endpoint}");
    }
}
