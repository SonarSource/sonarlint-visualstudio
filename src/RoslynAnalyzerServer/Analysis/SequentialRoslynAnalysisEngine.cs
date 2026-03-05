/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

[Export(typeof(IRoslynAnalysisEngine))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SequentialRoslynAnalysisEngine(
    IDiagnosticToRoslynIssueConverter issueConverter,
    IRoslynProjectCompilationProvider projectCompilationProvider,
    IRoslynQuickFixFactory quickFixFactory,
    IDiagnosticToAnalysisIssueConverter diagnosticToAnalysisIssueConverter,
    IAdditionalAnalysisIssueStorage additionalAnalysisIssueStorage,
    IAdditionalAnalysisConfigurationFactory additionalAnalysisConfigurationFactory,
    ILogger logger) : IRoslynAnalysisEngine
{
    private readonly ILogger logger = logger.ForContext(Resources.RoslynLogContext, Resources.RoslynAnalysisLogContext, Resources.RoslynAnalysisEngineLogContext);

    public async Task<IEnumerable<RoslynIssue>> AnalyzeAsync(
        List<RoslynProjectAnalysisRequest> projectsAnalysis,
        IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
    {
        var uniqueDiagnostics = new HashSet<RoslynIssue>(DiagnosticDuplicatesComparer.Instance); // todo might need non-unique diagnostics for pragma analyzer
        var nonUniqueDiagnostics = ImmutableArray.Create<Diagnostic>();
        var additionalConfigurations = additionalAnalysisConfigurationFactory.Create(() => nonUniqueDiagnostics, sonarRoslynAnalysisConfigurations);

        foreach (var projectAnalysisRequest in projectsAnalysis)
        {
            var (compilationWithAnalyzers, compilationWithAdditionalAnalyzers) = await projectCompilationProvider.GetProjectCompilationsAsync(
                projectAnalysisRequest.Scope,
                sonarRoslynAnalysisConfigurations,
                additionalConfigurations,
                token);

            // todo SLVS-2467 issue streaming
            foreach (var analysisCommand in projectAnalysisRequest.AnalysisCommands)
            {
                var diagnostics = await analysisCommand.ExecuteAsync(compilationWithAnalyzers, token);
                nonUniqueDiagnostics = nonUniqueDiagnostics.AddRange(diagnostics);

                foreach (var diagnostic in diagnostics.Where(x => !x.IsSuppressed))
                {
                    var quickFixes = await quickFixFactory.CreateQuickFixesAsync(
                        diagnostic,
                        projectAnalysisRequest.Scope.Project.Solution,
                        compilationWithAnalyzers.AnalysisConfiguration,
                        token);

                    var roslynIssue = issueConverter.ConvertToSonarDiagnostic(diagnostic, quickFixes, compilationWithAnalyzers.Language);
                    // todo SLVS-2468 improve issue merging
                    if (!uniqueDiagnostics.Add(roslynIssue))
                    {
                        logger.LogVerbose(Resources.AnalysisEngine_DuplicateDiagnostic, roslynIssue.RuleId, roslynIssue.PrimaryLocation.FileUri.LocalPath,
                            roslynIssue.PrimaryLocation.TextRange.StartLine);
                    }
                }
            }

            if (compilationWithAdditionalAnalyzers is not null)
            {
                await ProduceAdditionalDiagnosticsAsync(token, projectAnalysisRequest, compilationWithAdditionalAnalyzers);
            }
        }

        return uniqueDiagnostics;
    }

    private async Task ProduceAdditionalDiagnosticsAsync(
        CancellationToken token,
        RoslynProjectAnalysisRequest projectAnalysisCommands,
        IRoslynCompilationWithAnalyzersWrapper compilationWithAdditionalAnalyzers)
    {
        var additionalIssuesByFile = new Dictionary<string, List<IAnalysisIssue>>();

        foreach (var analysisCommand in projectAnalysisCommands.AdditionalCommands)
        {
            var diagnostics = await analysisCommand.ExecuteAsync(compilationWithAdditionalAnalyzers, token);

            foreach (var diagnostic in diagnostics.Where(x => !x.IsSuppressed))
            {
                var quickFixes = await quickFixFactory.CreateQuickFixesAsync(
                    diagnostic,
                    projectAnalysisCommands.Scope.Project.Solution,
                    compilationWithAdditionalAnalyzers.AnalysisConfiguration,
                    token);

                var analysisIssue = diagnosticToAnalysisIssueConverter.Convert(diagnostic, quickFixes);
                var filePath = diagnostic.Location.GetMappedLineSpan().Path;
                if (!additionalIssuesByFile.TryGetValue(filePath, out var list))
                {
                    additionalIssuesByFile[filePath] = list = [];
                }
                // todo these diagnostics may also be duplicated for multi-target projects
                list.Add(analysisIssue);
            }
        }

        foreach (var kvp in additionalIssuesByFile)
        {
            additionalAnalysisIssueStorage.Store(kvp.Key, kvp.Value);
        }
    }
}
