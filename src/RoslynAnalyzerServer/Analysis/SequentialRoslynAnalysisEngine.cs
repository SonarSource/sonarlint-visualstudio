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
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Core;
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
    IAdditionalAnalysisIssueStorageWriter additionalAnalysisIssueStorage,
    IPragmaSuppressionAnalysisConfigurationFactory pragmaSuppressionAnalysisConfigurationFactory,
    ILogger logger) : IRoslynAnalysisEngine
{
    private readonly ILogger logger = logger.ForContext(Resources.RoslynLogContext, Resources.RoslynAnalysisLogContext, Resources.RoslynAnalysisEngineLogContext);

    public async Task<IEnumerable<RoslynIssue>> AnalyzeAsync(
        List<RoslynProjectAnalysisRequest> projectsAnalysis,
        IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
    {
        var uniqueDiagnostics = new HashSet<RoslynIssue>(DiagnosticDuplicatesComparer.Instance);
        var uniqueAdditionalDiagnostics = new HashSet<RoslynIssue>(DiagnosticDuplicatesComparer.Instance);
        var knownIssuesStore = new CurrentAnalysisIssuesStore();
        var additionalConfigurations = pragmaSuppressionAnalysisConfigurationFactory.Create(knownIssuesStore, sonarRoslynAnalysisConfigurations);

        foreach (var projectAnalysisRequest in projectsAnalysis)
        {
            var (compilationWithAnalyzers, compilationWithAdditionalAnalyzers) = await projectCompilationProvider.GetProjectCompilationsAsync(
                projectAnalysisRequest.Scope,
                sonarRoslynAnalysisConfigurations,
                additionalConfigurations,
                token);

            // todo SLVS-2467 issue streaming
            await foreach (var (diagnostic, issue) in AnalyzeAsync(token, projectAnalysisRequest.Scope, projectAnalysisRequest.AnalysisCommands, compilationWithAnalyzers))
            {
                knownIssuesStore.Add(diagnostic);
                if (issue is not null && !uniqueDiagnostics.Add(issue))
                {
                    // todo SLVS-2468 improve issue merging
                    logger.LogVerbose(Resources.AnalysisEngine_DuplicateDiagnostic, issue.RuleId, issue.PrimaryLocation.FileUri.LocalPath,
                        issue.PrimaryLocation.TextRange.StartLine);
                }
            }

            if (compilationWithAdditionalAnalyzers is not null)
            {
                await foreach (var (_, issue) in AnalyzeAsync(token, projectAnalysisRequest.Scope, projectAnalysisRequest.AdditionalCommands, compilationWithAdditionalAnalyzers))
                {
                    if (issue is null)
                    {
                        continue;
                    }
                    uniqueAdditionalDiagnostics.Add(issue);
                }
            }
        }

        additionalAnalysisIssueStorage.Add(uniqueAdditionalDiagnostics);
        return uniqueDiagnostics;
    }

    private async IAsyncEnumerable<(Diagnostic, RoslynIssue?)> AnalyzeAsync(
        [EnumeratorCancellation] CancellationToken token, // todo check async enumerable
        ProjectAnalysisRequestScope scope,
        IEnumerable<IRoslynAnalysisCommand> analysisCommands,
        IRoslynCompilationWithAnalyzersWrapper compilationWithAdditionalAnalyzers)
    {
        foreach (var analysisCommand in analysisCommands)
        {
            var diagnostics = await analysisCommand.ExecuteAsync(compilationWithAdditionalAnalyzers, token);

            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.IsSuppressed)
                {
                    yield return (diagnostic, null); // suppressed issues are not reported as sonar issues
                    continue;
                }

                var quickFixes = await quickFixFactory.CreateQuickFixesAsync(
                    diagnostic,
                    scope.Project.Solution,
                    compilationWithAdditionalAnalyzers.AnalysisConfiguration,
                    token);

                var roslynIssue = issueConverter.ConvertToSonarDiagnostic(diagnostic, quickFixes, compilationWithAdditionalAnalyzers.Language);
                yield return (diagnostic, roslynIssue);
            }
        }
    }
}
