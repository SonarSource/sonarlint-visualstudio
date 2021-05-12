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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.Analyzer
{
    internal interface IEslintBridgeAnalyzer : IDisposable
    {
        Task<AnalysisResponse> Analyze(string filePath, string tsConfig, CancellationToken cancellationToken);
    }

    internal sealed class EslintBridgeAnalyzer : IEslintBridgeAnalyzer
    {
        private readonly EventWaitHandle serverInitLocker = new EventWaitHandle(true, EventResetMode.AutoReset);

        private readonly IRulesProvider rulesProvider;
        private readonly IEslintBridgeClient eslintBridgeClient;
        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly IAnalysisConfigMonitor analysisConfigMonitor;

        private bool shouldInitLinter = true;

        public EslintBridgeAnalyzer(
            IRulesProvider rulesProvider,
            IEslintBridgeClient eslintBridgeClient,
            IActiveSolutionTracker activeSolutionTracker,
            IAnalysisConfigMonitor analysisConfigMonitor)
        {
            this.rulesProvider = rulesProvider;
            this.eslintBridgeClient = eslintBridgeClient;
            this.activeSolutionTracker = activeSolutionTracker;
            this.analysisConfigMonitor = analysisConfigMonitor;

            activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTracker_ActiveSolutionChanged;
            analysisConfigMonitor.ConfigChanged += AnalysisConfigMonitor_ConfigChanged;
        }

        public async Task<AnalysisResponse> Analyze(string filePath, string tsConfig, CancellationToken cancellationToken)
        {
            try
            {
                return await GetAnalysisResponse(filePath, tsConfig, cancellationToken);
            }
            catch (EslintBridgeClientNotInitializedException)
            {
                RequireLinterUpdate();
                return await GetAnalysisResponse(filePath, tsConfig, cancellationToken);
            }
        }

        private async Task<AnalysisResponse> GetAnalysisResponse(string filePath, string tsConfig, CancellationToken cancellationToken)
        {
            await EnsureEslintBridgeClientIsInitialized(rulesProvider.GetActiveRulesConfiguration(), cancellationToken);

            var analysisResponse = await eslintBridgeClient.Analyze(filePath, tsConfig, cancellationToken);

            return analysisResponse;
        }

        private async Task EnsureEslintBridgeClientIsInitialized(IEnumerable<Rule> activeRules, CancellationToken cancellationToken)
        {
            try
            {
                serverInitLocker.WaitOne();

                if (shouldInitLinter)
                {
                    await eslintBridgeClient.InitLinter(activeRules, cancellationToken);
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
            await StopServer();
        }

        private async void AnalysisConfigMonitor_ConfigChanged(object sender, EventArgs e)
        {
            await StopServer();
        }

        private async Task StopServer()
        {
            RequireLinterUpdate();
            await eslintBridgeClient.Close();
        }

        private void RequireLinterUpdate()
        {
            serverInitLocker.WaitOne();
            shouldInitLinter = true;
            serverInitLocker.Set();
        }
    }
}
