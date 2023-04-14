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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
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

        private readonly IRulesProvider rulesProvider;
        private readonly IEslintBridgeClient eslintBridgeClient;
        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly IAnalysisConfigMonitor analysisConfigMonitor;
        private readonly IEslintBridgeIssueConverter issueConverter;
        private readonly IThreadHandling threadHandling;
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
            : this(rulesProvider, eslintBridgeClient, activeSolutionTracker, analysisConfigMonitor, issueConverter, ThreadHandling.Instance, logger)
        {
        }

        internal EslintBridgeAnalyzer(
            IRulesProvider rulesProvider,
            IEslintBridgeClient eslintBridgeClient,
            IActiveSolutionTracker activeSolutionTracker,
            IAnalysisConfigMonitor analysisConfigMonitor,
            IEslintBridgeIssueConverter issueConverter,
            IThreadHandling threadHandling,
            ILogger logger)
        {
            this.rulesProvider = rulesProvider;
            this.eslintBridgeClient = eslintBridgeClient;
            this.activeSolutionTracker = activeSolutionTracker;
            this.analysisConfigMonitor = analysisConfigMonitor;
            this.issueConverter = issueConverter;
            this.threadHandling = threadHandling;
            this.logger = logger;

            activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTracker_ActiveSolutionChanged;
            analysisConfigMonitor.ConfigChanged += AnalysisConfigMonitor_ConfigChanged;
        }

        public async Task<IReadOnlyCollection<IAnalysisIssue>> Analyze(string filePath, string tsConfig, CancellationToken cancellationToken)
        {
            await EnsureEslintBridgeClientIsInitialized(cancellationToken);
            var analysisResponse = await eslintBridgeClient.Analyze(filePath, tsConfig, cancellationToken);

            if (LinterNotInitializedResponse(analysisResponse))
            {
                // The call to `EnsureEslintBridgeClientIsInitialized` above doesn't guarantee the client is correctly initialized (e.g. the external process might have crashed).
                // So we still need to handle the "not initialised" case here.
                RequireLinterUpdate();
                await EnsureEslintBridgeClientIsInitialized(cancellationToken);
                analysisResponse = await eslintBridgeClient.Analyze(filePath, tsConfig, cancellationToken);
            }

            if (analysisResponse.ParsingError != null)
            {
                LogParsingError(filePath, analysisResponse.ParsingError);
                return Array.Empty<IAnalysisIssue>();
            }

            if (analysisResponse.Issues == null)
            {
                return Array.Empty<IAnalysisIssue>();
            }

            var issues = ConvertIssues(filePath, analysisResponse.Issues);

            return issues;
        }

        private static bool LinterNotInitializedResponse(JsTsAnalysisResponse analysisResponse)
        {
            return analysisResponse.ParsingError != null && 
                   analysisResponse.ParsingError.Code == ParsingErrorCode.LINTER_INITIALIZATION;
        }

        private async Task EnsureEslintBridgeClientIsInitialized(CancellationToken cancellationToken)
        {
            try
            {
                serverInitLocker.WaitOne();

                if (shouldInitLinter)
                {
                    await eslintBridgeClient.InitLinter(rulesProvider.GetActiveRulesConfiguration(), cancellationToken);
                    shouldInitLinter = false;
                }
            }
            finally
            {
                serverInitLocker.Set();
            }
        }

        public void Dispose()
        {
            activeSolutionTracker.ActiveSolutionChanged -= ActiveSolutionTracker_ActiveSolutionChanged;
            analysisConfigMonitor.ConfigChanged -= AnalysisConfigMonitor_ConfigChanged;

            serverInitLocker?.Dispose();
        }

        private async void ActiveSolutionTracker_ActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
        {
            if (!e.IsSolutionOpen)
            {
                // We only need to shut down the server after a solution is closed.
                // See https://github.com/SonarSource/sonarlint-visualstudio/issues/2438 for more info.
                await StopServer();
            }
        }

        private void AnalysisConfigMonitor_ConfigChanged(object sender, EventArgs e)
        {
            RequireLinterUpdate();
        }

        private async Task StopServer()
        {
            await threadHandling.SwitchToBackgroundThread();
            RequireLinterUpdate();
            await eslintBridgeClient.Close();
        }

        private void RequireLinterUpdate()
        {
            threadHandling.ThrowIfOnUIThread();

            serverInitLocker.WaitOne();
            shouldInitLinter = true;
            serverInitLocker.Set();
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
    }
}
