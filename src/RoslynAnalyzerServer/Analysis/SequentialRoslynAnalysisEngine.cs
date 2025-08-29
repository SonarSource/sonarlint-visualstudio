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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using Document = SonarLint.VisualStudio.Core.Document;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

[Export(typeof(IRoslynAnalysisEngine))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SequentialRoslynAnalysisEngine(
    IDiagnosticToRoslynIssueConverter issueConverter,
    IRoslynWorkspaceWrapper workspace,
    IRoslynQuickFixStorageWriter quickFixStorage,
    IRoslynProjectCompilationProvider projectCompilationProvider,
    ILogger logger) : IRoslynAnalysisEngine
{
    private readonly ILogger logger = logger.ForContext("Roslyn Analysis", "Engine");

    public async Task<IEnumerable<RoslynIssue>> AnalyzeAsync(
        List<RoslynProjectAnalysisRequest> projectsAnalysis,
        IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
    {
        var uniqueDiagnostics = new HashSet<RoslynIssue>(DiagnosticDuplicatesComparer.Instance);
        foreach (var projectAnalysisCommands in projectsAnalysis)
        {
            // todo clear old quickfixes
            var compilationWithAnalyzers = await projectCompilationProvider.GetProjectCompilationAsync(projectAnalysisCommands.Project, sonarRoslynAnalysisConfigurations, token);

            // todo SLVS-2467 issue streaming
            foreach (var analysisCommand in projectAnalysisCommands.AnalysisCommands)
            {
                var diagnostics = await analysisCommand.ExecuteAsync(compilationWithAnalyzers, token);

                foreach (var diagnostic in diagnostics)
                {
                    var quickFixes = await CreateQuickFixesAsync(diagnostic, projectAnalysisCommands.Project.Solution, compilationWithAnalyzers, token);

                    var roslynIssue = issueConverter.ConvertToSonarDiagnostic(diagnostic, quickFixes, compilationWithAnalyzers.Language);
                    // todo SLVS-2468 improve issue merging
                    if (!uniqueDiagnostics.Add(roslynIssue))
                    {
                        logger.LogVerbose("Duplicate diagnostic discarded ID: {0}, File: {1}, Line: {2}", roslynIssue.RuleId, Path.GetFileName(roslynIssue.PrimaryLocation.FilePath), roslynIssue.PrimaryLocation.TextRange.StartLine);
                    }
                }
            }
        }

        return uniqueDiagnostics;
    }

    private async Task<List<RoslynQuickFix>> CreateQuickFixesAsync(
        Diagnostic diagnostic,
        IRoslynSolutionWrapper solution,
        IRoslynCompilationWithAnalyzersWrapper compilationWithAnalyzers,
        CancellationToken token)
    {
        var codeActions = new List<CodeAction>();

        if (compilationWithAnalyzers.AnalysisConfiguration.CodeFixProvidersByRuleKey.TryGetValue(diagnostic.Id, out var availableCodeFixProviders)
            && solution.GetDocument(diagnostic.Location.SourceTree) is {} document)
        {
            foreach (var codeFixProvider in availableCodeFixProviders)
            {
                await codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (c, ds) => codeActions.Add(c), token));
            }
        }

        var quickFixes = new List<RoslynQuickFix>();
        foreach (var codeAction in codeActions)
        {
            var id = Guid.NewGuid();
            quickFixStorage.Add(id, new RoslynQuickFixApplicationImpl(workspace, solution, codeAction));
            quickFixes.Add(new RoslynQuickFix(id));
        }

        return quickFixes;
    }
}
