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

namespace SonarLint.VisualStudio.Integration.CSharpVB.Analysis;

public interface ISonarRoslynAnalysisEngine
{
    Task<IEnumerable<SonarDiagnostic>> AnalyzeAsync(
        string[] filePaths,
        ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token);
}

[Export(typeof(ISonarRoslynAnalysisEngine))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SonarLintRoslynAnalyzer(
    IRoslynConfigurationManager configurationManager,
    IRoslynDocumentFinder documentFinder,
    IRoslynDiagnosticsConverter diagnosticsConverter,
    ILogger logger,
    IThreadHandling threadHandling) : ISonarRoslynAnalysisEngine
{
    public async Task<IEnumerable<SonarDiagnostic>> AnalyzeAsync(
        string[] filePaths,
        ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
    {
        threadHandling.ThrowIfOnUIThread();
        var uniqueDiagnostics = new HashSet<SonarDiagnostic>(DiagnosticDuplicatesComparer.Instance);

        var analysisPathsByProject = filePaths
            .SelectMany(documentFinder.FindProjectsWithDocument)
            .GroupBy(x => x.project, x => x.analysisFilePath);

        foreach (var projectAndPaths in analysisPathsByProject)
        {
            var project = projectAndPaths.Key;
            var compilationWithAnalyzers = await GetProjectCompilationAsync(token, project, sonarRoslynAnalysisConfigurations);

            foreach (var analysisFilePath in projectAndPaths)
            {
                var projectDiagnostics = await AnalyzeInProjectAsync(compilationWithAnalyzers, analysisFilePath, project.Name, token);

                if (projectDiagnostics == null)
                {
                    continue;
                }

                foreach (var diagnostic in projectDiagnostics)
                {
                    if (!uniqueDiagnostics.Add(diagnostic))
                    {
                        // todo log issue merged
                    }
                    else
                    {
                        // todo issue streaming?
                    }
                }
            }
        }

        return uniqueDiagnostics;
    }

    private async Task<CompilationWithAnalyzers> GetProjectCompilationAsync(
        CancellationToken token,
        Project project,
        ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations)
    {
        var compilation = await project.GetCompilationAsync(token);
        if (compilation == null)
        {
            logger.WriteLine($"Failed to get compilation for project: {project.Name}");
            return null;
        }

        var compilationWithAnalyzers = GetCompilationWithAnalyzers(compilation, project, sonarRoslynAnalysisConfigurations);
        return compilationWithAnalyzers;
    }

    private async Task<IEnumerable<SonarDiagnostic>> AnalyzeInProjectAsync(
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

        // todo issue streaming, syntactic diagnostics should appear first
        var analyzerSyntacticDiagnosticsAsync = compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(semanticModel.SyntaxTree, token);
        var analyzerSemanticDiagnosticsAsync = compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(semanticModel, null, token);

        var syntaxDiagnostics = await analyzerSyntacticDiagnosticsAsync;
        var semanticDiagnostics = await analyzerSemanticDiagnosticsAsync;

        return diagnosticsConverter.ConvertToDiagnostics(syntaxDiagnostics, semanticDiagnostics);
    }

    private CompilationWithAnalyzers GetCompilationWithAnalyzers(
        Compilation compilation,
        Project project,
        ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations)
    {
        var language = compilation.Language switch
        {
            "C#" => Language.CSharp,
            "Visual Basic" => Language.VBNET,
            _ => throw new NotImplementedException(),
        };

        // todo cleanup globalconfig from AnalyzerConfigDocuments, but check if that is not breaking/discarding the compilation
        // todo cleanup SonarLint.xml from AdditionalFiles, but check if that is not breaking/discarding the compilation

        var analysisConfigurationForLanguage = sonarRoslynAnalysisConfigurations[language];

        var compilationWithAnalyzers = compilation
            .WithOptions(compilation.Options.WithSpecificDiagnosticOptions(analysisConfigurationForLanguage.DiagnosticOptions))
            .WithAnalyzers(
                analysisConfigurationForLanguage.Analyzers,
                new CompilationWithAnalyzersOptions(
                    project.AnalyzerOptions.WithAdditionalFiles(project.AnalyzerOptions.AdditionalFiles.Concat([analysisConfigurationForLanguage.SonarLintXml]).ToImmutableArray()),
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
