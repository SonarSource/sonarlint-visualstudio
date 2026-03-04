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
        RoslynProjectAnalysisRequest projectAnalysisRequest,
        IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> sonarRoslynAnalysisConfigurations,
        CancellationToken token)
    {
        var project = projectAnalysisRequest.Project;
        var compilation = await project.GetCompilationAsync(token);
        var configuration = sonarRoslynAnalysisConfigurations[compilation.Language];

        return BuildCompilationWithAnalyzers(project, compilation, configuration, projectAnalysisRequest.TargetFilePaths);
    }

    public async Task<(IRoslynCompilationWithAnalyzersWrapper mainCompilation, IRoslynCompilationWithAnalyzersWrapper? additionalCompilation)> GetProjectCompilationsAsync(
        RoslynProjectAnalysisRequest projectAnalysisRequest,
        IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> mainConfigurations,
        IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> additionalConfigurations,
        CancellationToken token)
    {
        var project = projectAnalysisRequest.Project;
        var compilation = await project.GetCompilationAsync(token);

        var mainConfiguration = mainConfigurations[compilation.Language];
        var additionalConfiguration = GetAdditionalConfiguration(additionalConfigurations, compilation);

        return (BuildCompilationWithAnalyzers(project, compilation, mainConfiguration, projectAnalysisRequest.TargetFilePaths),
            additionalConfiguration is not null
                ? BuildCompilationWithAnalyzers(project, compilation, additionalConfiguration.Value, projectAnalysisRequest.TargetFilePaths)
                : null);
    }

    private static RoslynAnalysisConfiguration? GetAdditionalConfiguration(
        IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> additionalConfigurations,
        IRoslynCompilationWrapper compilation)
    {
        if (additionalConfigurations.TryGetValue(compilation.Language, out var configuration))
        {
            return configuration;
        }

        return null;
    }

    private IRoslynCompilationWithAnalyzersWrapper BuildCompilationWithAnalyzers(
        IRoslynProjectWrapper project,
        IRoslynCompilationWrapper compilation,
        RoslynAnalysisConfiguration configuration,
        ImmutableHashSet<string> targetFilePaths) =>
        ApplyAnalyzersAndAdditionalFile(
            ApplyDiagnosticOptions(project, compilation, configuration, targetFilePaths),
            project,
            configuration);

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
            true);

        return compilation
            .WithAnalyzers(analysisConfigurationForLanguage.Analyzers, compilationWithAnalyzersOptions, analysisConfigurationForLanguage);
    }

    private static IRoslynCompilationWrapper ApplyDiagnosticOptions(
        IRoslynProjectWrapper project,
        IRoslynCompilationWrapper compilation,
        RoslynAnalysisConfiguration analysisConfigurationForLanguage,
        ImmutableHashSet<string> targetFilePaths)
    {
        var mergedDiagnosticOptions = OverrideQualityProfileWithProjectSettings(project, analysisConfigurationForLanguage.DiagnosticOptions);
        var compilationOptions = compilation.RoslynCompilationOptions
            .WithSpecificDiagnosticOptions(mergedDiagnosticOptions)
            .WithSyntaxTreeOptionsProvider(new TreeOptionsProvider(mergedDiagnosticOptions, targetFilePaths));
        return compilation.WithOptions(compilationOptions);
    }

    private static ImmutableDictionary<string, ReportDiagnostic> OverrideQualityProfileWithProjectSettings(
        IRoslynProjectWrapper project,
        ImmutableDictionary<string, ReportDiagnostic> analysisConfigurationForLanguage)
    {
        if (project.SpecificDiagnosticOptions is null)
        {
            return analysisConfigurationForLanguage;
        }

        var result = analysisConfigurationForLanguage;
        foreach (var option in project.SpecificDiagnosticOptions)
        {
            result = result.SetItem(option.Key, option.Value);
        }
        return result;
    }

    private void OnAnalyzerException(Exception arg1, DiagnosticAnalyzer arg2, Diagnostic arg3) =>
        analyzerExceptionLogger.LogVerbose(
            new MessageLevelContext { VerboseContext = [arg2.GetType().Name, arg3.Id] },
            arg1.ToString());
}
