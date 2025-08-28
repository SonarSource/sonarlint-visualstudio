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
using OmniSharp.Roslyn.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

[Export(typeof(IRoslynAnalysisEngine))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SequentialRoslynAnalysisEngine(
    IRoslynWorkspaceWrapper workspace,
    IDiagnosticToRoslynIssueConverter issueConverter,
    IRoslynProjectCompilationProvider projectCompilationProvider,
    IAnalysisStopwatchService analysisStopwatchService,
    ILogger logger,
    [Import("PerformanceLogger")] ILogger performanceLogger) : IRoslynAnalysisEngine
{
    private class PerformanceMeasure
    {
        public double TotalTimeToGetQuickfixesMs { get; set; }
        public double TotalTimeToGetCodeActionsMs { get; set; }
        public double TotalTimeToGetTextChangesMs { get; set; }
    }

    private readonly ILogger logger = logger.ForContext("Roslyn Analysis", "Engine");

    public async Task<IEnumerable<RoslynIssue>> AnalyzeAsync(
        List<RoslynProjectAnalysisRequest> projectsAnalysis,
        IReadOnlyDictionary<Language, RoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
    {
        var performanceMeasure = new PerformanceMeasure();

        var uniqueDiagnostics = new HashSet<RoslynIssue>(DiagnosticDuplicatesComparer.Instance);
        foreach (var projectAnalysisCommands in projectsAnalysis)
        {
            var compilationWithAnalyzers = await projectCompilationProvider.GetProjectCompilationAsync(projectAnalysisCommands.Project, sonarRoslynAnalysisConfigurations, token);

            // todo SLVS-2467 issue streaming
            foreach (var analysisCommand in projectAnalysisCommands.AnalysisCommands)
            {
                var diagnostics = await analysisCommand.ExecuteAsync(compilationWithAnalyzers, token);
                foreach (var diagnostic in diagnostics)
                {
                    var quickFixes = await CalculateQuickFixes(sonarRoslynAnalysisConfigurations, token, diagnostic, projectAnalysisCommands, compilationWithAnalyzers, performanceMeasure);

                    var roslynIssue = issueConverter.ConvertToSonarDiagnostic(diagnostic, quickFixes, compilationWithAnalyzers.Language);

                    // todo SLVS-2468 improve issue merging
                    if (!uniqueDiagnostics.Add(roslynIssue))
                    {
                        logger.LogVerbose("Duplicate diagnostic discarded ID: {0}, File: {1}, Line: {2}", roslynIssue.RuleId, Path.GetFileName(roslynIssue.PrimaryLocation.FilePath),
                            roslynIssue.PrimaryLocation.TextRange.StartLine);
                    }
                }
            }
        }

        performanceLogger.WriteLine("Time took to get code actions for quickfixes {0}", performanceMeasure.TotalTimeToGetCodeActionsMs);
        performanceLogger.WriteLine("Time took to calculate text changes {0}", performanceMeasure.TotalTimeToGetTextChangesMs);
        performanceLogger.WriteLine("Time took to calculate quickfixes {0}", performanceMeasure.TotalTimeToGetQuickfixesMs);

        return uniqueDiagnostics;
    }

    private async Task<List<RoslynIssueQuickFix>> CalculateQuickFixes(
        IReadOnlyDictionary<Language, RoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token,
        Diagnostic diagnostic,
        RoslynProjectAnalysisRequest projectAnalysisCommands,
        IRoslynCompilationWithAnalyzersWrapper compilationWithAnalyzers,
        PerformanceMeasure performanceMeasure)
    {
        var t1 = analysisStopwatchService.Current.Item1.Elapsed;
        TimeSpan t2 = t1;
        var quickFixes = new List<RoslynIssueQuickFix>();

        if (diagnostic.Location.SourceTree?.FilePath is { } filePath && projectAnalysisCommands.Project.ContainsDocument(filePath, out var document) &&
            sonarRoslynAnalysisConfigurations[compilationWithAnalyzers.Language].CodeFixProvidersByRuleKey.TryGetValue(diagnostic.Id, out var codeFixProviders))
        {
            var actions = new List<CodeAction>();

            foreach (var codeFixProvider in codeFixProviders)
            {
                await codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (ca, di) => { actions.Add(ca); }, token)); // todo ???
            }

            t2 = analysisStopwatchService.Current.Item1.Elapsed;
            performanceMeasure.TotalTimeToGetCodeActionsMs += (t2 - t1).TotalMilliseconds;

            foreach (var codeAction in actions)
            {
                List<RoslynIssueQuickFixEdit> edits = [];
                quickFixes.Add(new(codeAction.Title, edits));
                var codeActionOperations = await codeAction.GetOperationsAsync(token);
                foreach (var applyChangesOperation in codeActionOperations.Cast<ApplyChangesOperation>())
                {
                    await foreach (var changedDocument in CodeActionCopyPaste.GetChangedDocuments(workspace.RoslynWorkspace, projectAnalysisCommands.Project.RoslynProject.Solution,
                                       applyChangesOperation.ChangedSolution, token))
                    {
                        foreach (var linePositionSpanTextChange in await TextChangesCopyPaste.GetAsync(changedDocument.newDoc, changedDocument.oldDoc, token))
                        {
                            edits.Add(linePositionSpanTextChange);
                        }
                    }
                }
            }
        }

        var t3 = analysisStopwatchService.Current.Item1.Elapsed;
        performanceMeasure.TotalTimeToGetTextChangesMs += (t3 - t2).TotalMilliseconds;
        performanceMeasure.TotalTimeToGetQuickfixesMs += (t3 - t1).TotalMilliseconds;

        return quickFixes;
    }
}
