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
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

[Export(typeof(IRoslynProjectCompilationProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynProjectCompilationProvider(ILogger logger) : IRoslynProjectCompilationProvider
{
    private readonly ILogger analyzerExceptionLogger = logger.ForContext(Resources.RoslynLogContext, Resources.RoslynAnalysisLogContext, Resources.RoslynAnalysisAnalyzerExceptionLogContext);

    public async Task<IRoslynCompilationWithAnalyzersWrapper> GetProjectCompilationAsync(
        IRoslynProjectWrapper project,
        IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
    {
        var compilation = await project.GetCompilationAsync(token);

        var analysisConfigurationForLanguage = sonarRoslynAnalysisConfigurations[compilation.Language];

        return ApplyAnalyzersAndAdditionalFile(
            ApplyDiagnosticOptions(compilation, analysisConfigurationForLanguage),
            project,
            analysisConfigurationForLanguage);
    }

    private IRoslynCompilationWithAnalyzersWrapper ApplyAnalyzersAndAdditionalFile(
        IRoslynCompilationWrapper compilation,
        IRoslynProjectWrapper project,
        RoslynAnalysisConfiguration analysisConfigurationForLanguage)
    {
        var additionalFiles = project.RoslynAnalyzerOptions.AdditionalFiles;
        var analyzerOptions = project.RoslynAnalyzerOptions.WithAdditionalFiles(additionalFiles
            .Where(x => Path.GetFileName(x.Path) != analysisConfigurationForLanguage.SonarLintXml.FileName)
            .Concat([analysisConfigurationForLanguage.SonarLintXml])
            .ToImmutableArray());

        var compilationWithAnalyzersOptions = new CompilationWithAnalyzersOptions(
            analyzerOptions,
            OnAnalyzerException,
            true,
            false,
            false);

        return compilation
            .WithAnalyzers(analysisConfigurationForLanguage.Analyzers, compilationWithAnalyzersOptions, analysisConfigurationForLanguage);
    }

    private static IRoslynCompilationWrapper ApplyDiagnosticOptions(
        IRoslynCompilationWrapper compilation,
        RoslynAnalysisConfiguration analysisConfigurationForLanguage)
    {
        var compilationOptions = compilation.RoslynCompilationOptions.WithSpecificDiagnosticOptions(analysisConfigurationForLanguage.DiagnosticOptions);
        return compilation.WithOptions(compilationOptions);
    }

    private void OnAnalyzerException(Exception arg1, DiagnosticAnalyzer arg2, Diagnostic arg3) =>
        analyzerExceptionLogger.LogVerbose(
            new MessageLevelContext { VerboseContext = [arg2.GetType().Name, arg3.Id] },
            arg1.ToString());
}
