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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;
using Document = Microsoft.CodeAnalysis.Document;

namespace SonarLint.VisualStudio.Integration.CSharpVB;

public interface ISonarLintRoslynAnalyzer
{
    Task<ImmutableList<IAnalysisIssue>> AnalyzeAsync(string[] filePaths, CancellationToken token);
}

[Export(typeof(ISonarLintRoslynAnalyzer))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SonarLintRoslynAnalyzer(
    IActiveConfigScopeTracker activeConfigScopeTracker,
    IRoslynConfigurationManager configurationManager,
    IRoslynWorkspaceWrapper roslynWorkspaceWrapper,
    IRoslynDocumentFinder documentFinder,
    IDiagnosticsConverter diagnosticsConverter,
    ILogger logger,
    IThreadHandling threadHandling) : ISonarLintRoslynAnalyzer
{
    public async Task<ImmutableList<IAnalysisIssue>> AnalyzeAsync(string[] filePaths, CancellationToken token)
    {
        threadHandling.ThrowIfOnUIThread();
        var uniqueIssues = new List<IAnalysisIssue>();

        var solution = roslynWorkspaceWrapper.CurrentSolution;

        var analysisPathsByProject = filePaths
            .SelectMany(x => documentFinder.FindProjectsWithDocument(x, solution.RoslynSolution))
            .GroupBy(x => x.project, x => x.analysisFilePath);

        foreach (var projectAndPaths in analysisPathsByProject)
        {
            var project = projectAndPaths.Key;
            var compilationWithAnalyzers = await GetProjectCompilationAsync(token, project);

            foreach (var analysisFilePath in projectAndPaths)
            {
                var projectIssues = await AnalyzeInProjectAsync(compilationWithAnalyzers, analysisFilePath, project.Name, token);

                if (projectIssues == null)
                {
                    continue;
                }

                foreach (var issue in projectIssues)
                {
                    if (!diagnosticsConverter.IsDuplicateIssue(uniqueIssues, issue))
                    {
                        uniqueIssues.Add(issue);
                    }
                }
            }
        }

        return uniqueIssues.ToImmutableList();
    }

    private async Task<CompilationWithAnalyzers> GetProjectCompilationAsync(CancellationToken token, Project project)
    {
        var compilation = await project.GetCompilationAsync(token);
        if (compilation == null)
        {
            logger.WriteLine($"Failed to get compilation for project: {project.Name}");
            return null;
        }

        var compilationWithAnalyzers = await GetCompilationWithAnalyzersAsync(compilation, project);
        return compilationWithAnalyzers;
    }

    private async Task<IEnumerable<IAnalysisIssue>> AnalyzeInProjectAsync(
        CompilationWithAnalyzers compilationWithAnalyzers,
        string analysisFilePath,
        string projectName,
        CancellationToken token)
    {
        var syntaxTree = compilationWithAnalyzers.Compilation.SyntaxTrees.SingleOrDefault(x => analysisFilePath.Equals(x.FilePath));
        if (syntaxTree == null)
        {
            logger.WriteLine($"Failed to get syntax tree for file: {analysisFilePath} in project {projectName}");
            return [];
        }

        var semanticModel = compilationWithAnalyzers.Compilation.GetSemanticModel(syntaxTree);

        var analyzerSyntacticDiagnosticsAsync = compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(semanticModel.SyntaxTree, token);
        var analyzerSemanticDiagnosticsAsync = compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(semanticModel, null, token);

        var issues = diagnosticsConverter.Convert(
            await analyzerSyntacticDiagnosticsAsync,
            await analyzerSemanticDiagnosticsAsync);

        return issues;
    }

    private async Task<CompilationWithAnalyzers> GetCompilationWithAnalyzersAsync(Compilation compilation, Project project)
    {
        var currentScopeId = activeConfigScopeTracker.Current?.Id;
        var language = compilation.Language switch
        {
            "C#" => Language.CSharp,
            "Visual Basic" => Language.VBNET,
            _ => throw new NotImplementedException(),
        };

        var (sonarLintConfiguration, diagnosticStatuses, diagnosticAnalyzers) =
            await configurationManager.GetConfigurationAsync(currentScopeId, language);

        var withSonarLintAdditionalFiles =
            configurationManager.GetWithSonarLintAdditionalFiles(project.AnalyzerOptions, sonarLintConfiguration);

        var compilationWithAnalyzers = compilation
            .WithOptions(compilation.Options.WithSpecificDiagnosticOptions(diagnosticStatuses))
            .WithAnalyzers(
                diagnosticAnalyzers!.Value,
                new CompilationWithAnalyzersOptions(
                    withSonarLintAdditionalFiles,
                    OnAnalyzerException,
                    true,
                    false,
                    false));

        return compilationWithAnalyzers;
    }

    private void OnAnalyzerException(Exception arg1, DiagnosticAnalyzer arg2, Diagnostic arg3) =>
        logger.WriteLine(
            new MessageLevelContext { Context = ["Roslyn Analyzer", arg2.GetType().Name, arg3.Id] },
            arg1.ToString());
}
