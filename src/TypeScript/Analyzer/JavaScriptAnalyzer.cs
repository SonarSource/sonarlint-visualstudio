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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.Analyzer
{
    [Export(typeof(IAnalyzer))]
    internal sealed class JavaScriptAnalyzer : IAnalyzer, IDisposable
    {
        private readonly ITelemetryManager telemetryManager;
        private readonly IAnalysisStatusNotifier analysisStatusNotifier;
        private readonly IEslintBridgeAnalyzer eslintBridgeAnalyzer;

        [ImportingConstructor]
        public JavaScriptAnalyzer(IJavaScriptEslintBridgeClient eslintBridgeClient,
            IRulesProviderFactory rulesProviderFactory,
            ITelemetryManager telemetryManager,
            IAnalysisStatusNotifier analysisStatusNotifier,
            IEslintBridgeAnalyzerFactory eslintBridgeAnalyzerFactory)
        {
            this.telemetryManager = telemetryManager;
            this.analysisStatusNotifier = analysisStatusNotifier;

            var rulesProvider = rulesProviderFactory.Create("javascript");
            eslintBridgeAnalyzer = eslintBridgeAnalyzerFactory.Create(rulesProvider, eslintBridgeClient);
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
                var stopwatch = Stopwatch.StartNew();
                var issues = await eslintBridgeAnalyzer.Analyze(filePath, null, cancellationToken);
                analysisStatusNotifier.AnalysisFinished(filePath, issues.Count, stopwatch.Elapsed);

                if (issues.Any())
                {
                    consumer.Accept(filePath, issues);
                }
            }
            catch (TaskCanceledException)
            {
                analysisStatusNotifier.AnalysisCancelled(filePath);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                analysisStatusNotifier.AnalysisFailed(filePath, ex);
            }
        }

        public void Dispose()
        {
            eslintBridgeAnalyzer.Dispose();
        }
    }
}
