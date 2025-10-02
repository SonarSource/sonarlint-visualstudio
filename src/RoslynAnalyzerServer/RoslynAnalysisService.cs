/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer;

[Export(typeof(IRoslynAnalysisService))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynAnalysisService(
    IRoslynAnalysisEngine analysisEngine,
    IRoslynAnalysisConfigurationProvider analysisConfigurationProvider,
    IRoslynSolutionAnalysisCommandProvider analysisCommandProvider) : IRoslynAnalysisService
{
    private readonly object locker = new();
    private readonly Dictionary<Guid, CancellationTokenSource> cancellationTokensForAnalysis = new();

    public async Task<IEnumerable<RoslynIssue>> AnalyzeAsync(
        AnalysisRequest analysisRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            return await analysisEngine.AnalyzeAsync(
                analysisCommandProvider.GetAnalysisCommandsForCurrentSolution(analysisRequest.FileNames.Select(x => x.LocalPath).ToArray()),
                await analysisConfigurationProvider.GetConfigurationAsync(analysisRequest.ActiveRules, analysisRequest.AnalysisProperties, analysisRequest.AnalyzerInfo),
                SetUpCancellationTokenForAnalysis(analysisRequest, cancellationToken));
        }
        finally
        {
            CancelAndCleanUpToken(analysisRequest.AnalysisId);
        }
    }

    public bool Cancel(AnalysisCancellationRequest analysisCancellationRequest)
    {
        return CancelAndCleanUpToken(analysisCancellationRequest.AnalysisId);
    }

    private CancellationToken SetUpCancellationTokenForAnalysis(
        AnalysisRequest analysisRequest,
        CancellationToken cancellationToken)
    {
        lock (locker)
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokensForAnalysis[analysisRequest.AnalysisId] = cancellationTokenSource;
            cancellationToken = cancellationTokenSource.Token;
        }
        return cancellationToken;
    }

    private bool CancelAndCleanUpToken(Guid analysisId)
    {
        CancellationTokenSource? cancellationTokenSource;
        lock (locker)
        {
            if (!cancellationTokensForAnalysis.TryGetValue(analysisId, out cancellationTokenSource))
            {
                return false;
            }
            cancellationTokensForAnalysis.Remove(analysisId);
        }

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        return true;
    }
}
