﻿/*
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.Analyzer
{
    [Export(typeof(IAnalyzer))]
    internal sealed class JavaScriptAnalyzer : IAnalyzer, IDisposable
    {
        private readonly EventWaitHandle serverInitLocker = new EventWaitHandle(true, EventResetMode.AutoReset);

        private readonly IRulesProvider rulesProvider;
        private readonly IEslintBridgeIssueConverter issuesConverter;
        private readonly ITelemetryManager telemetryManager;
        private readonly IAnalysisStatusNotifier analysisStatusNotifier;
        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly IAnalysisConfigMonitor analysisConfigMonitor;
        private readonly ILogger logger;
        private readonly IEslintBridgeClient eslintBridgeClient;

        private bool shouldInitLinter = true;

        [ImportingConstructor]
        public JavaScriptAnalyzer(IEslintBridgeClientFactory eslintBridgeClientFactory,
            IRulesProviderFactory rulesProviderFactory,
            ITelemetryManager telemetryManager,
            IAnalysisStatusNotifier analysisStatusNotifier,
            IActiveSolutionTracker activeSolutionTracker,
            IAnalysisConfigMonitor analysisConfigMonitor,
            ILogger logger)
            : this(eslintBridgeClientFactory,
                  rulesProviderFactory.Create("javascript"),
                  telemetryManager,
                  analysisStatusNotifier,
                  activeSolutionTracker,
                  analysisConfigMonitor,
                  logger)
        {
        }

        internal /* for testing */ JavaScriptAnalyzer(IEslintBridgeClientFactory eslintBridgeClientFactory,
            IRulesProvider rulesProvider,
            ITelemetryManager telemetryManager,
            IAnalysisStatusNotifier analysisStatusNotifier,
            IActiveSolutionTracker activeSolutionTracker,
            IAnalysisConfigMonitor analysisConfigMonitor,
            ILogger logger,
            IEslintBridgeIssueConverter issuesConverter = null // settable for testing
            )
        {
            this.rulesProvider = rulesProvider;
            this.issuesConverter = issuesConverter ?? new EslintBridgeIssueConverter(rulesProvider);
            this.telemetryManager = telemetryManager;
            this.analysisStatusNotifier = analysisStatusNotifier;
            this.activeSolutionTracker = activeSolutionTracker;
            this.analysisConfigMonitor = analysisConfigMonitor;
            this.logger = logger;

            eslintBridgeClient = eslintBridgeClientFactory.Create();

            activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTracker_ActiveSolutionChanged;
            analysisConfigMonitor.ConfigChanged += AnalysisConfigMonitor_ConfigChanged;
        }

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            return languages.Contains(AnalysisLanguage.Javascript);
        }

        public void ExecuteAnalysis(string path,
            string charset,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            IAnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken)
        {
            Debug.Assert(IsAnalysisSupported(detectedLanguages));

            ExecuteAnalysis(path, consumer, cancellationToken).Forget(); // fire and forget
        }

        internal async Task ExecuteAnalysis(string filePath, IIssueConsumer consumer, CancellationToken cancellationToken)
        {
            telemetryManager.LanguageAnalyzed("js");

            // Switch to a background thread
            await TaskScheduler.Default;

            analysisStatusNotifier.AnalysisStarted(filePath);

            try
            {
                await EnsureEslintBridgeClientIsInitialized(cancellationToken);
                var stopwatch = Stopwatch.StartNew();
                var analysisResponse = await eslintBridgeClient.AnalyzeJs(filePath, cancellationToken);

                var numberOfIssues = analysisResponse.Issues?.Count() ?? 0;
                analysisStatusNotifier.AnalysisFinished(filePath, numberOfIssues, stopwatch.Elapsed);

                if (analysisResponse.ParsingError != null)
                {
                    LogParsingError(filePath, analysisResponse.ParsingError);
                    // TODO: bug, doesn't clear the progress bar from "analysis started"
                    return;
                }

                var issues = ConvertIssues(filePath, analysisResponse.Issues);

                if (issues.Any())
                {
                    consumer.Accept(filePath, issues);
                }
            }
            catch (EslintBridgeClientNotInitializedException)
            {
                RequireLinterUpdate();
                // TODO: bug, doesn't clear the progress bar from "analysis started"
            }
            catch (TaskCanceledException)
            {
                analysisStatusNotifier.AnalysisCancelled(filePath);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                analysisStatusNotifier.AnalysisFailed(filePath, ex);
                RequireLinterUpdate();
            }
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

        private IEnumerable<IAnalysisIssue> ConvertIssues(string filePath, IEnumerable<Issue> analysisResponseIssues) =>
            analysisResponseIssues.Select(x => issuesConverter.Convert(filePath, x));

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

        public void Dispose()
        {
            activeSolutionTracker.ActiveSolutionChanged -= ActiveSolutionTracker_ActiveSolutionChanged;
            analysisConfigMonitor.ConfigChanged -= AnalysisConfigMonitor_ConfigChanged;

            eslintBridgeClient?.Dispose();
            serverInitLocker?.Dispose();
        }

        private void ActiveSolutionTracker_ActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e)
        {
            RequireLinterUpdate();
        }

        private void AnalysisConfigMonitor_ConfigChanged(object sender, EventArgs e)
        {
            RequireLinterUpdate();
        }

        private void RequireLinterUpdate()
        {
            serverInitLocker.WaitOne();
            shouldInitLinter = true;
            serverInitLocker.Set();
        }
    }
}
