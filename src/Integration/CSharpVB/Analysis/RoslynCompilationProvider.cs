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

internal interface ISonarRoslynProjectCompilationProvider
{
    Task<CompilationWithAnalyzers> GetProjectCompilationAsync(
        Project project,
        ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token);
}

[Export(typeof(ISonarRoslynProjectCompilationProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SonarRoslynProjectCompilationProvider(ILogger logger) : ISonarRoslynProjectCompilationProvider
{
    public async Task<CompilationWithAnalyzers> GetProjectCompilationAsync(
        Project project,
        ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
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
