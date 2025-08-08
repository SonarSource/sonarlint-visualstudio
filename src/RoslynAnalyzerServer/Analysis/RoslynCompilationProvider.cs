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
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

internal interface ISonarRoslynProjectCompilationProvider
{
    Task<CompilationWithAnalyzers> GetProjectCompilationAsync(
        ISonarRoslynProjectWrapper project,
        ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token);
}

[Export(typeof(ISonarRoslynProjectCompilationProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SonarRoslynProjectCompilationProvider(ILogger logger) : ISonarRoslynProjectCompilationProvider
{
    public async Task<CompilationWithAnalyzers> GetProjectCompilationAsync(
        ISonarRoslynProjectWrapper project,
        ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
    {
        var compilation = await project.GetCompilationAsync(token);
        if (compilation == null)
        {
            logger.WriteLine($"Failed to get compilation for project: {project.Name}");
            return null;
        }

        var analysisConfigurationForLanguage = sonarRoslynAnalysisConfigurations[compilation.Language];


        return ApplyAnalyzersAndAdditionalFile(
            ApplyDiagnosticOptions(compilation, analysisConfigurationForLanguage),
            project,
            analysisConfigurationForLanguage);
    }

    private CompilationWithAnalyzers ApplyAnalyzersAndAdditionalFile(
        ISonarRoslynCompilationWrapper compilation,
        ISonarRoslynProjectWrapper project,
        SonarRoslynAnalysisConfiguration analysisConfigurationForLanguage)
    {
        // todo IF NEEDED cleanup globalconfig from AnalyzerConfigDocuments, but check if that is not breaking/discarding the compilation
        var additionalFiles = project.RoslynAnalyzerOptions.AdditionalFiles;
        var sonarLintXmlName = Path.GetFileName(analysisConfigurationForLanguage.SonarLintXml.Path);
        var analyzerOptions = project.RoslynAnalyzerOptions.WithAdditionalFiles(additionalFiles
            .Where(x => Path.GetFileName(x.Path) != sonarLintXmlName)
            .Concat([analysisConfigurationForLanguage.SonarLintXml])
            .ToImmutableArray());

        var compilationWithAnalyzersOptions = new CompilationWithAnalyzersOptions(
            analyzerOptions,
            OnAnalyzerException,
            true,
            false,
            false);

        return compilation
            .WithAnalyzers(analysisConfigurationForLanguage.Analyzers, compilationWithAnalyzersOptions);
    }

    private static ISonarRoslynCompilationWrapper ApplyDiagnosticOptions(
        ISonarRoslynCompilationWrapper compilation,
        SonarRoslynAnalysisConfiguration analysisConfigurationForLanguage)
    {
        var compilationOptions = compilation.RoslynCompilationOptions.WithSpecificDiagnosticOptions(analysisConfigurationForLanguage.DiagnosticOptions);
        compilation = compilation
            .WithOptions(compilationOptions);
        return compilation;
    }

    private void OnAnalyzerException(Exception arg1, DiagnosticAnalyzer arg2, Diagnostic arg3) =>
        logger.WriteLine(
            new MessageLevelContext { Context = ["Roslyn Analyzer", arg2.GetType().Name, arg3.Id] },
            arg1.ToString());
}
