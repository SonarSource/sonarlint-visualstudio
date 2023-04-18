﻿/*
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.Analyzer
{
    internal abstract class AnalyzerBase : IDisposable
    {
        protected readonly ITelemetryManager telemetryManager;
        protected readonly IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;
        protected readonly IEslintBridgeAnalyzer eslintBridgeAnalyzer;

        protected AnalyzerBase(ITelemetryManager telemetryManager,
            IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
            IEslintBridgeAnalyzerFactory eslintBridgeAnalyzerFactory,
            IRulesProviderFactory rulesProviderFactory,
            IEslintBridgeClient eslintBridgeClient,
            string repoKey,
            Language language)
        {
            this.telemetryManager = telemetryManager;
            this.analysisStatusNotifierFactory = analysisStatusNotifierFactory;

            var rulesProvider = rulesProviderFactory.Create(repoKey, language);
            eslintBridgeAnalyzer = eslintBridgeAnalyzerFactory.Create(rulesProvider, eslintBridgeClient);
        }

        protected async Task ExecuteAsync(IAnalysisStatusNotifier analysisStatusNotifier, string filePath, IIssueConsumer consumer, CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var issues = await eslintBridgeAnalyzer.Analyze(filePath, null, cancellationToken);
                analysisStatusNotifier.AnalysisFinished(issues.Count, stopwatch.Elapsed);

                if (issues.Any())
                {
                    consumer.Accept(filePath, issues);
                }
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
        }

        public void Dispose()
        {
            eslintBridgeAnalyzer.Dispose();
        }
    }
}
