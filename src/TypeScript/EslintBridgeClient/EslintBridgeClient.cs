/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;
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
        /// </summary>
        Task<AnalysisResponse> Analyze(string filePath, string tsConfigFilePath, CancellationToken cancellationToken);

        /// <summary>
        /// Closes running eslint-bridge server.
        /// </summary>
        Task Close();
    }

    /// <summary>
    /// Matching Java implementation: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/sonar-javascript-plugin/src/main/java/org/sonar/plugins/javascript/eslint/JavaScriptEslintBasedSensor.java#L51
    /// Eslint-bridge methods: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/eslint-bridge/src/server.ts
    /// </summary>
    internal class EslintBridgeClient : IEslintBridgeClient
    {
        // The eslintbridge process will shutdown after 15 seconds.
        // We send a keep alive every 5 seconds.
        private const double MillisecondsToWaitBetweenKeepAlives = 5000;

        private readonly string analyzeEndpoint;
        private readonly IEslintBridgeProcess eslintBridgeProcess;
        private readonly IEslintBridgeHttpWrapper httpWrapper;
        private readonly IAnalysisConfiguration analysisConfiguration;
        private readonly ITimer keepAliveTimer;
        private readonly ILogger logger;
        private bool isDisposed;

        public EslintBridgeClient(string analyzeEndpoint, IEslintBridgeProcess eslintBridgeProcess, ILogger logger)
            : this(analyzeEndpoint, eslintBridgeProcess, new EslintBridgeHttpWrapper(logger), new AnalysisConfiguration(),
                  new TimerWrapper(), logger)
        {
        }

        internal EslintBridgeClient(string analyzeEndpoint,
            IEslintBridgeProcess eslintBridgeProcess,
            IEslintBridgeHttpWrapper httpWrapper,
            IAnalysisConfiguration analysisConfiguration,
            ITimer keepAliveTimer,
            ILogger logger)
        {
            this.analyzeEndpoint = analyzeEndpoint;
            this.eslintBridgeProcess = eslintBridgeProcess;
            this.httpWrapper = httpWrapper;
            this.analysisConfiguration = analysisConfiguration;
            this.logger = logger;

            this.keepAliveTimer = keepAliveTimer;
            this.keepAliveTimer.AutoReset = true;
            this.keepAliveTimer.Interval = MillisecondsToWaitBetweenKeepAlives;
            this.keepAliveTimer.Elapsed += OnKeepAliveTimerElapsed;
        }

        public async Task InitLinter(IEnumerable<Rule> rules, CancellationToken cancellationToken)
        {
            var initLinterRequest = new InitLinterRequest
            {
                Rules = rules.ToArray(),
                Globals = analysisConfiguration.GetGlobals(),
                Environments = analysisConfiguration.GetEnvironments()
            };

            var responseString = await MakeCall("init-linter", initLinterRequest, cancellationToken);

            if (!"OK!".Equals(responseString))
            {
                throw new InvalidOperationException(string.Format(Resources.ERR_InvalidResponse, responseString));
            }
        }

        public async Task<AnalysisResponse> Analyze(string filePath, string tsConfigFilePath, CancellationToken cancellationToken)
        {
            var tsConfigFilePaths = tsConfigFilePath == null ? Array.Empty<string>() : new[] { tsConfigFilePath };

            var analysisRequest = new AnalysisRequest
            {
                FilePath = filePath,
                IgnoreHeaderComments = true,
                TSConfigFilePaths = tsConfigFilePaths,
                FileType = "MAIN"
            };

            var responseString = await MakeCall(analyzeEndpoint, analysisRequest, cancellationToken);

            if (string.IsNullOrEmpty(responseString))
            {
                throw new InvalidOperationException(string.Format(Resources.ERR_InvalidResponse, responseString));
            }

            return JsonConvert.DeserializeObject<AnalysisResponse>(responseString);
        }

        public async Task Close()
        {
            try
            {
                if (eslintBridgeProcess.IsRunning)
                {
                    await MakeCall("close", null, CancellationToken.None);
                }
            }
            catch
            {
                // nothing to do if the call failed
            }
            finally
            {
                keepAliveTimer.Stop();
                eslintBridgeProcess.Stop();
            }
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
                keepAliveTimer.Dispose();
                isDisposed = true;
            }
        }

        #endregion

        protected async Task<string> MakeCall(string endpoint, object request, CancellationToken cancellationToken)
        {
            var port = await EnsureProcessIsRunningAsync();
            var fullServerUrl = BuildServerUri(port, endpoint);
            var response = await httpWrapper.PostAsync(fullServerUrl, request, cancellationToken);

            return response;
        }

        private async Task<int> EnsureProcessIsRunningAsync()
        {
            // Both "Start" methods can be called multiple times safely
            var port = await eslintBridgeProcess.Start();
            keepAliveTimer.Start();
            return port;
        }

        private Uri BuildServerUri(int port, string endpoint) => new Uri($"http://localhost:{port}/{endpoint}");

        private void OnKeepAliveTimerElapsed(object sender, TimerEventArgs e)
            => HandleKeepAliveTimerElapsedAsync().Forget();

        internal /* for testing */async Task HandleKeepAliveTimerElapsedAsync()
        {
            try
            {
                // Stopping the timer here means we won't send multiple keep-alives
                // if the server is busy analysing and doesn't respond to the first
                // call. It also makes debugging this method simpler.
                keepAliveTimer.Stop();
                if (eslintBridgeProcess.IsRunning)
                {
                    await SendKeepAliveAsync();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogDebug($"[EsLintBridgeClient] Error sending keep-alive: {ex}");
            }
            finally
            {
                keepAliveTimer.Start();
            }
        }

        private async Task SendKeepAliveAsync()
        {
            var port = await eslintBridgeProcess.Start();
            var fullServerUrl = BuildServerUri(port, "status");
            await httpWrapper.GetAsync(fullServerUrl, CancellationToken.None);
        }
    }
}
