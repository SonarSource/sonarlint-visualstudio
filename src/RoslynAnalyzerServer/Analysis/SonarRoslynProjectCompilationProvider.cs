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

[Export(typeof(ISonarRoslynProjectCompilationProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class SonarRoslynProjectCompilationProvider(ILogger logger) : ISonarRoslynProjectCompilationProvider
{
    private readonly ILogger logger = logger.ForContext("Roslyn Analysis", "Analyzer Exception");

    public async Task<ISonarRoslynCompilationWithAnalyzersWrapper> GetProjectCompilationAsync(
        ISonarRoslynProjectWrapper project,
        ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
    {
        var compilation = await project.GetCompilationAsync(token);

        var analysisConfigurationForLanguage = sonarRoslynAnalysisConfigurations[compilation.Language];

        return ApplyAnalyzersAndAdditionalFile(
            ApplyDiagnosticOptions(compilation, analysisConfigurationForLanguage),
            project,
            analysisConfigurationForLanguage);
    }

    private ISonarRoslynCompilationWithAnalyzersWrapper ApplyAnalyzersAndAdditionalFile(
        ISonarRoslynCompilationWrapper compilation,
        ISonarRoslynProjectWrapper project,
        SonarRoslynAnalysisConfiguration analysisConfigurationForLanguage)
    {
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
        return compilation.WithOptions(compilationOptions);
    }

    private void OnAnalyzerException(Exception arg1, DiagnosticAnalyzer arg2, Diagnostic arg3) =>
        logger.LogVerbose(
            new MessageLevelContext { VerboseContext = [arg2.GetType().Name, arg3.Id] },
            arg1.ToString());
}
