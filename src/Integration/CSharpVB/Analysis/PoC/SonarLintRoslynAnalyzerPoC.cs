﻿/*
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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.Integration.CSharpVB.Analysis.PoC;

public interface ISonarLintRoslynAnalyzerPoC
{
    Task<ImmutableList<IAnalysisIssue>> AnalyzeAsync(string[] filePaths, CancellationToken token);
}

[Export(typeof(ISonarLintRoslynAnalyzerPoC))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SonarLintRoslynAnalyzerPoC(
    IRoslynAnalyzerProvider roslynAnalyzerProvider,
    IRoslynAnalysisConfigurationProvider configurationProvider,
    IRoslynSolutionAnalysisCommandProvider sonarRoslynSolutionAnalysisCommandProvider,
    IRoslynAnalysisEngine roslynAnalysisEngine,
    ISonarDiagnosticsConverterPoC diagnosticsConverterPoC,
    ILogger logger,
    IThreadHandling threadHandling) : ISonarLintRoslynAnalyzerPoC
{
    private Lazy<List<ActiveRuleDto>> activeRules
        = new Lazy<List<ActiveRuleDto>>(
            () => roslynAnalyzerProvider.LoadAndProcessAnalyzerAssemblies()
                .SelectMany(x => x.Value.SupportedRuleKeys, (x, y) => new ActiveRuleDto(new SonarCompositeRuleId(x.Key.RepoInfo.Key, y).ErrorListErrorCode, [])).ToList(),
            LazyThreadSafetyMode.ExecutionAndPublication);

    public async Task<ImmutableList<IAnalysisIssue>> AnalyzeAsync(string[] filePaths, CancellationToken token)
    {
        threadHandling.ThrowIfOnUIThread();

        var sonarDiagnostics = await roslynAnalysisEngine.AnalyzeAsync(sonarRoslynSolutionAnalysisCommandProvider.GetAnalysisCommandsForCurrentSolution(filePaths), configurationProvider.GetConfiguration(activeRules.Value, []), token);

        return sonarDiagnostics.Select(diagnosticsConverterPoC.ConvertToAnalysisIssue).ToImmutableList();
    }
}
