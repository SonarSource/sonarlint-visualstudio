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
using SonarLint.VisualStudio.TypeScript.Rules;
using SonarLint.VisualStudio.TypeScript.TsConfig;

namespace SonarLint.VisualStudio.TypeScript.Analyzer
{
    [Export(typeof(IAnalyzer))]
    internal sealed class TypeScriptAnalyzer : AnalyzerBase, IAnalyzer, IDisposable
    {
        private readonly ITsConfigProvider tsConfigProvider;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public TypeScriptAnalyzer(ITypeScriptEslintBridgeClient eslintBridgeClient,
            IRulesProviderFactory rulesProviderFactory,
            ITsConfigProvider tsConfigProvider,
            IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
            IEslintBridgeAnalyzerFactory eslintBridgeAnalyzerFactory,
            ITelemetryManager telemetryManager,
            ILogger logger,
            IThreadHandling threadHandling) : base(telemetryManager, analysisStatusNotifierFactory, eslintBridgeAnalyzerFactory, rulesProviderFactory, eslintBridgeClient, "typescript", Language.Ts)
        {
            this.tsConfigProvider = tsConfigProvider;
            this.logger = logger;
            this.threadHandling = threadHandling;
        }

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            return languages.Contains(AnalysisLanguage.TypeScript);
        }

        public void ExecuteAnalysis(string path,
            string charset,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            IAnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken)
        {
            Debug.Assert(IsAnalysisSupported(detectedLanguages));

            ExecuteAnalysisAsync(path, consumer, cancellationToken).Forget(); // fire and forget
        }

        internal async Task ExecuteAnalysisAsync(string filePath, IIssueConsumer consumer, CancellationToken cancellationToken)
        {
            telemetryManager.LanguageAnalyzed("ts");
            var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(TypeScriptAnalyzer), filePath);

            // Switch to a background thread
            await threadHandling.SwitchToBackgroundThread();

            analysisStatusNotifier.AnalysisStarted();

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var tsConfig = await tsConfigProvider.GetConfigForFile(filePath, cancellationToken);

                if (string.IsNullOrEmpty(tsConfig))
                {
                    analysisStatusNotifier.AnalysisFailed(Resources.ERR_NoTsConfig);
                    return;
                }

                logger.WriteLine("[TypescriptAnalyzer] time to find ts config: " + stopwatch.ElapsedMilliseconds);
                stopwatch.Stop();
            }
            catch (TaskCanceledException)
            {
                analysisStatusNotifier.AnalysisCancelled();
            }
            catch (EslintBridgeProcessLaunchException ex)
            {
                analysisStatusNotifier.AnalysisFailed(ex.Message);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                analysisStatusNotifier.AnalysisFailed(ex);
            }

            await ExecuteAsync(analysisStatusNotifier, filePath, consumer, cancellationToken);
        }
    }
}
