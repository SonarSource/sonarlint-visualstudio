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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.Analyzer
{
    internal interface IEslintBridgeAnalyzer : IDisposable
    {
        Task<IReadOnlyCollection<IAnalysisIssue>> Analyze(string filePath, string tsConfig, CancellationToken cancellationToken);
    }

    internal sealed class EslintBridgeAnalyzer : IEslintBridgeAnalyzer
    {
        // todo: fix in https://github.com/SonarSource/sonarlint-visualstudio/issues/2432
        internal const string LinterIsNotInitializedError = "Linter is undefined";

        private readonly IRulesProvider rulesProvider;
        private readonly IEslintBridgeClient eslintBridgeClient;
        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly IAnalysisConfigMonitor analysisConfigMonitor;
        private readonly IEslintBridgeIssueConverter issueConverter;
        private readonly ILogger logger;

        private readonly EventWaitHandle serverInitLocker = new EventWaitHandle(true, EventResetMode.AutoReset);
        private bool shouldInitLinter = true;

        public EslintBridgeAnalyzer(
            IRulesProvider rulesProvider,
            IEslintBridgeClient eslintBridgeClient,
            IActiveSolutionTracker activeSolutionTracker,
            IAnalysisConfigMonitor analysisConfigMonitor,
            IEslintBridgeIssueConverter issueConverter,
            ILogger logger)
        {
            this.rulesProvider = rulesProvider;
            this.eslintBridgeClient = eslintBridgeClient;
            this.activeSolutionTracker = activeSolutionTracker;
            this.analysisConfigMonitor = analysisConfigMonitor;
            this.issueConverter = issueConverter;
            this.logger = logger;

            activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTracker_ActiveSolutionChanged;
            analysisConfigMonitor.ConfigChanged += AnalysisConfigMonitor_ConfigChanged;
        }

        public async Task<IReadOnlyCollection<IAnalysisIssue>> Analyze(string filePath, string tsConfig, CancellationToken cancellationToken)
        {
            var fileName = Path.GetFileName(filePath);
            LogWithFileName($"sStarting analysis...");
            var timer = Stopwatch.StartNew();

            await EnsureEslintBridgeClientIsInitialized(cancellationToken);
            var analysisResponse = await eslintBridgeClient.Analyze(filePath, tsConfig, cancellationToken);

            if (LinterNotInitializedResponse(analysisResponse))
            {
                Log($"[{fileName}] Linter not initialized response received. Triggering initialization and re-analysis...");

                LogWithFileName($"[Inner analysis] Start");
                var innerTimer = Stopwatch.StartNew();

                // The call to `EnsureEslintBridgeClientIsInitialized` above doesn't guarantee the client is correctly initialized (e.g. the external process might have crashed).
                // So we still need to handle the "not initialised" case here.
                RequireLinterUpdate();
                await EnsureEslintBridgeClientIsInitialized(cancellationToken);
                analysisResponse = await eslintBridgeClient.Analyze(filePath, tsConfig, cancellationToken);
                LogWithFileName($"[Inner analysis] Stop. Elapsed: {timer.ElapsedMilliseconds}ms");
            }

            IReadOnlyCollection<IAnalysisIssue> issues;

            if (analysisResponse.ParsingError != null)
            {
                LogParsingError(filePath, analysisResponse.ParsingError);
                issues = Array.Empty<IAnalysisIssue>();
            }
            else if (analysisResponse.Issues == null)
            {
                issues = Array.Empty<IAnalysisIssue>();
            }
            else
            {
                LogWithFileName($"Converting issues... Running total: {timer.ElapsedMilliseconds}ms");
                issues = ConvertIssues(filePath, analysisResponse.Issues);
                LogWithFileName($"Finished converting issues. Running total: {timer.ElapsedMilliseconds}ms");
            }

            Log($"[{fileName}] Finished analysis. Elapsed: {timer.ElapsedMilliseconds}ms");
            return issues;

            void LogWithFileName(string message) => Log($"[{fileName}] {message}");
        }

        private static bool LinterNotInitializedResponse(AnalysisResponse analysisResponse)
        {
            return analysisResponse.ParsingError != null && 
                   analysisResponse.ParsingError.Message.Contains(LinterIsNotInitializedError);
        }

        private async Task EnsureEslintBridgeClientIsInitialized(CancellationToken cancellationToken)
        {
            Log("Ensuring eslintbridge is initialized...");
            var timer = Stopwatch.StartNew();

            try
            {
                Log("Waiting to acquire lock...");
                serverInitLocker.WaitOne();
                Log($"Lock acquired. Running total: {timer.ElapsedMilliseconds}ms");

                if (shouldInitLinter)
                {
                    Log("Calling InitLinter...");
                    await eslintBridgeClient.InitLinter(rulesProvider.GetActiveRulesConfiguration(), cancellationToken);
                    shouldInitLinter = false;
                    Log($"InitLinter finished. Running total: {timer.ElapsedMilliseconds}ms");
                }
            }
            finally
            {
                serverInitLocker.Set();
            }
            Log($"eslintbridege is initialized. Elapsed: {timer.ElapsedMilliseconds}ms");
        }

        public void Dispose()
        {
            activeSolutionTracker.ActiveSolutionChanged -= ActiveSolutionTracker_ActiveSolutionChanged;
            analysisConfigMonitor.ConfigChanged -= AnalysisConfigMonitor_ConfigChanged;

            serverInitLocker?.Dispose();
        }

        private async void ActiveSolutionTracker_ActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
        {
            if(!e.IsSolutionOpen)
            {
                // We only need to shut down the server after a solution is closed.
                // See https://github.com/SonarSource/sonarlint-visualstudio/issues/2438 for more info.
                await StopServer();
            }
        }

        private void AnalysisConfigMonitor_ConfigChanged(object sender, EventArgs e)
        {
            Log("Handling analysis config changed event...");
            var timer = Stopwatch.StartNew();
            RequireLinterUpdate();
            Log($"Event handled. Elapsed: {timer.ElapsedMilliseconds}ms");
        }

        private async Task StopServer()
        {
            Log("Stopping server...");
            var timer = Stopwatch.StartNew();

            RequireLinterUpdate();
            await eslintBridgeClient.Close();
            Log($"Server stopped. Elapsed: {timer.ElapsedMilliseconds}ms");
        }

        private void RequireLinterUpdate()
        {
            Log("Start");
            var timer = Stopwatch.StartNew();

            Log("Waiting to acquire lock...");
            serverInitLocker.WaitOne();
            Log($"Lock acquired. Running total: {timer.ElapsedMilliseconds}ms");

            shouldInitLinter = true;
            serverInitLocker.Set();
            Log($"Stop.Elapsed: {timer.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Java version: https://github.com/SonarSource/SonarJS/blob/1916267988093cb5eb1d0b3d74bb5db5c0dbedec/sonar-javascript-plugin/src/main/java/org/sonar/plugins/javascript/eslint/AbstractEslintSensor.java#L134
        /// </summary>
        private void LogParsingError(string path, ParsingError parsingError)
        {
            if (parsingError.Code == ParsingErrorCode.MISSING_TYPESCRIPT)
            {
                logger.WriteLine(Resources.ERR_ParsingError_MissingTypescript);
            }
            else if (parsingError.Code == ParsingErrorCode.UNSUPPORTED_TYPESCRIPT)
            {
                logger.WriteLine(parsingError.Message);
                logger.WriteLine(Resources.ERR_ParsingError_UnsupportedTypescript);
            }
            else
            {
                logger.WriteLine(Resources.ERR_ParsingError_General, path, parsingError.Line, parsingError.Code, parsingError.Message);
            }
        }

        private IReadOnlyCollection<IAnalysisIssue> ConvertIssues(string filePath, IEnumerable<Issue> analysisResponseIssues) =>
            analysisResponseIssues.Select(x => issueConverter.Convert(filePath, x)).ToList();

        private void Log(string message, [CallerMemberName] string callerMemberName = null)
        {
            var text = $"[{this.GetType().Name}] [{callerMemberName}] [Thread: {Thread.CurrentThread.ManagedThreadId}, {DateTime.Now.ToString("hh:mm:ss.fff")}]  {message}";
            logger.WriteLine(text);
        }
    }
}
