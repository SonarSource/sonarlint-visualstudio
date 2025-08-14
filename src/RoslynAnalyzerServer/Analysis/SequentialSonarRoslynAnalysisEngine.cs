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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

[Export(typeof(ISonarRoslynAnalysisEngine))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SequentialSonarRoslynAnalysisEngine(
    IRoslynDiagnosticsConverter diagnosticsConverter,
    ISonarRoslynProjectCompilationProvider projectCompilationProvider,
    ILogger logger) : ISonarRoslynAnalysisEngine
{
    private readonly ILogger logger = logger.ForContext("Roslyn Analysis", "Engine");

    public async Task<IEnumerable<SonarDiagnostic>> AnalyzeAsync(
        List<SonarRoslynProjectAnalysisRequest> projectsAnalysis,
        ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
    {
        var uniqueDiagnostics = new HashSet<SonarDiagnostic>(DiagnosticDuplicatesComparer.Instance);
        foreach (var projectAnalysisCommands in projectsAnalysis)
        {
            var compilationWithAnalyzers = await projectCompilationProvider.GetProjectCompilationAsync(projectAnalysisCommands.Project, sonarRoslynAnalysisConfigurations, token);

            // todo SLVS-2467 issue streaming
            foreach (var analysisCommand in projectAnalysisCommands.AnalysisCommands)
            {
                var diagnostics = await analysisCommand.ExecuteAsync(compilationWithAnalyzers, token);

                foreach (var diagnostic in diagnostics.Select(d => diagnosticsConverter.ConvertToSonarDiagnostic(d, compilationWithAnalyzers.Language)))
                {
                    // todo SLVS-2468 improve issue merging
                    if (!uniqueDiagnostics.Add(diagnostic))
                    {
                        logger.LogVerbose("Duplicate diagnostic discarded ID: {0}, File: {1}, Line: {2}", diagnostic.RuleKey, Path.GetFileName(diagnostic.PrimaryLocation.FilePath), diagnostic.PrimaryLocation.TextRange.StartLine);
                    }
                }
            }
        }

        return uniqueDiagnostics;
    }
}
