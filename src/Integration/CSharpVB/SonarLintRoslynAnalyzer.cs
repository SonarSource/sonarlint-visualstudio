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
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;
using Document = Microsoft.CodeAnalysis.Document;

namespace SonarLint.VisualStudio.Integration.CSharpVB;

public interface ISonarLintRoslynAnalyzer
{
    Task<ImmutableList<IAnalysisIssue>> AnalyzeAsync(string filePath, CancellationToken token);
}

[Export(typeof(ISonarLintRoslynAnalyzer))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class SonarLintRoslynAnalyzer(
    IUserSettingsProvider userSettingsProvider,
    ISonarLintConfigGenerator roslynConfigGenerator,
    IEnterpriseRoslynAnalyzerProvider enterpriseAnalyzerProvider,
    IBasicRoslynAnalyzerProvider basicAnalyzerProvider,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    IRoslynWorkspaceWrapper workspaceWrapper,
    IAsyncLockFactory asyncLockFactory,
    ILogger logger,
    IThreadHandling threadHandling)
    : ISonarLintRoslynAnalyzer
{
    /// <summary>
    ///     The file will never be written to disk so the path is irrelevant.
    ///     It only needs to be named 'SonarLint.Xml' so the sonar-dotnet analyzers could load it.
    /// </summary>
    private static readonly string DummySonarLintXmlFilePath = Path.Combine(Path.GetTempPath(), "SonarLint.xml");

    private static readonly List<Language> Languages = [Language.CSharp, Language.VBNET];
    private readonly IAsyncLock asyncLock = asyncLockFactory.Create();
    private ImmutableArray<DiagnosticAnalyzer>? cachedAnalyzers;
    private ImmutableDictionary<string, ReportDiagnostic> cachedCsharpDiagnosticStatuses;
    private ImmutableDictionary<string, ReportDiagnostic> cachedVbnetDiagnosticStatuses;
    private SonarLintConfiguration cachedCsharpSonarLintConfig;
    private SonarLintConfiguration cacheVbnetSonarLintConfig;
    private string lastConfigScopeId;

    public async Task<ImmutableList<IAnalysisIssue>> AnalyzeAsync(string filePath, CancellationToken token)
    {
        threadHandling.ThrowIfOnUIThread();

        var project = FindDocumentAndProject(filePath, out var analysisFilePath);
        if (project == null)
        {
            return null; // todo
        }

        var compilation = await project.GetCompilationAsync(token);
        if (compilation == null)
        {
            return null; // todo
        }

        var compilationWithAnalyzers = await GetCompilationWithAnalyzersAsync(compilation, project);

        var syntaxTree2 = compilationWithAnalyzers.Compilation.SyntaxTrees.SingleOrDefault(x => analysisFilePath.Equals(x.FilePath));
        if (syntaxTree2 == null)
        {
            return null; // todo
        }
        var semanticModel2 = compilationWithAnalyzers.Compilation.GetSemanticModel(syntaxTree2);

        var analyzerSyntacticDiagnosticsAsync = compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(semanticModel2.SyntaxTree, token);
        var analyzerSemanticDiagnosticsAsync = compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(semanticModel2, null, token);

        var issues = ConvertToAnalysisIssues(await analyzerSyntacticDiagnosticsAsync, await analyzerSemanticDiagnosticsAsync);

        return issues;
    }

    private static ImmutableList<IAnalysisIssue> ConvertToAnalysisIssues(ImmutableArray<Diagnostic> syntax, ImmutableArray<Diagnostic> analyzerSemanticDiagnosticsAsync)
    {
        var issues = analyzerSemanticDiagnosticsAsync.Concat(syntax)
            .Select(diagnostic =>
            {
                var fileLinePositionSpan = diagnostic.Location.GetMappedLineSpan();
                var isWarning = diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning;
                return new AnalysisIssue(
                    null,
                    diagnostic.Id + ":" + "GG",
                    null,
                    diagnostic.IsSuppressed,
                    isWarning ? AnalysisIssueSeverity.Critical : AnalysisIssueSeverity.Minor,
                    AnalysisIssueType.CodeSmell,
                    new Impact(SoftwareQuality.Maintainability, isWarning ? SoftwareQualitySeverity.High : SoftwareQualitySeverity.Low),
                    new AnalysisIssueLocation(
                        diagnostic.GetMessage(),
                        diagnostic.Location.SourceTree.FilePath,
                        new TextRange(
                            fileLinePositionSpan.StartLinePosition.Line + 1,
                            fileLinePositionSpan.EndLinePosition.Line + 1,
                            fileLinePositionSpan.StartLinePosition.Character,
                            fileLinePositionSpan.EndLinePosition.Character,
                            null)), [
                    ]);
            }).Cast<IAnalysisIssue>()
            .ToImmutableList();
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

        var (sonarLintConfiguration, diagnosticStatuses, diagnosticAnalyzers) = await GetConfigurationAsync(currentScopeId, language);

        var withSonarLintAdditionalFiles = GetWithSonarLintAdditionalFiles(project.AnalyzerOptions, sonarLintConfiguration);

        var compilationWithAnalyzers = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(diagnosticStatuses)).WithAnalyzers(
            diagnosticAnalyzers!.Value,
            new CompilationWithAnalyzersOptions(
                withSonarLintAdditionalFiles,
                OnAnalyzerException,
                true,
                false,
                false));
        return compilationWithAnalyzers;
    }

    private async Task<(SonarLintConfiguration cachedSonarLintConfig, ImmutableDictionary<string, ReportDiagnostic> cachedDiagnosticStatuses, ImmutableArray<DiagnosticAnalyzer>? cachedAnalyzers)>
        GetConfigurationAsync(string currentScopeId, Language language)
    {
        // this method is trash and needs to be rewritten

        using (await asyncLock.AcquireAsync())
        {
            var exclusions = ConvertExclusions(userSettingsProvider.UserSettings);
            var (ruleStatusesByLanguage, ruleParametersByLanguage) = ConvertRules(userSettingsProvider.UserSettings);

            cachedCsharpDiagnosticStatuses = ruleStatusesByLanguage[Language.CSharp].ToImmutableDictionary(
                x => x.Key,
                ConvertSeverityToReportDiagnostic);
            cachedVbnetDiagnosticStatuses = ruleStatusesByLanguage[Language.VBNET].ToImmutableDictionary(
                x => x.Key,
                ConvertSeverityToReportDiagnostic);

            cachedCsharpSonarLintConfig = roslynConfigGenerator.Generate(
                ruleParametersByLanguage[Language.CSharp],
                userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties,
                exclusions,
                Language.CSharp);
            cacheVbnetSonarLintConfig = roslynConfigGenerator.Generate(
                ruleParametersByLanguage[Language.VBNET],
                userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties,
                exclusions,
                Language.VBNET);
            cachedAnalyzers = await GetAnalyzersAsync(currentScopeId);
            lastConfigScopeId = currentScopeId;

            if (language == Language.CSharp)
            {
                return (cachedCsharpSonarLintConfig, cachedCsharpDiagnosticStatuses, cachedAnalyzers);
            }
            else
            {
                return (cacheVbnetSonarLintConfig, cachedVbnetDiagnosticStatuses, cachedAnalyzers);
            }
        }
    }

    private static ReportDiagnostic ConvertSeverityToReportDiagnostic(IRoslynRuleStatus y) =>
        y.GetSeverity() switch
        {
            RuleAction.None => ReportDiagnostic.Suppress,
            RuleAction.Hidden => ReportDiagnostic.Hidden,
            RuleAction.Info => ReportDiagnostic.Info,
            RuleAction.Warning => ReportDiagnostic.Warn,
            RuleAction.Error => ReportDiagnostic.Error,
            _ => throw new ArgumentOutOfRangeException()
        };

    private async Task<ImmutableArray<DiagnosticAnalyzer>> GetAnalyzersAsync(string configurationScopeId)
    {
        var analyzerFileReferences =
            configurationScopeId == null || await enterpriseAnalyzerProvider.GetEnterpriseOrNullAsync(configurationScopeId) is not { } enterpriseAnalyzerReferences
                ? await basicAnalyzerProvider.GetBasicAsync()
                : enterpriseAnalyzerReferences;

        var analyzerTypes = LoadAnalyzerTypes(analyzerFileReferences);

        var analyzers = analyzerTypes.Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)).ToImmutableArray();
        return analyzers;
    }

    private static IEnumerable<Type> LoadAnalyzerTypes(ImmutableArray<AnalyzerFileReference> analyzerFileReferences)
    {
        var analyzerTypes = analyzerFileReferences.SelectMany(x => x.AssemblyLoader.LoadFromPath(x.FullPath).GetTypes())
            .Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract);
        return analyzerTypes;
    }

    private void OnAnalyzerException(Exception arg1, DiagnosticAnalyzer arg2, Diagnostic arg3) =>
        logger.WriteLine(new MessageLevelContext { Context = ["Roslyn Analyzer", arg2.GetType().Name, arg3.Id] }, arg1.ToString());

    public static AdditionalText Convert(SonarLintConfiguration sonarLintConfiguration)
    {
        var sonarLintXmlFileContent = SonarLintConfigurationSerializer.Serialize(sonarLintConfiguration);
        var sonarLintXmlAdditionalText = new AdditionalTextImpl(DummySonarLintXmlFilePath, sonarLintXmlFileContent);

        return sonarLintXmlAdditionalText;
    }

    /// <summary>
    ///     Add sonar-dotnet analyzer additional files.
    ///     Override any existing sonar-dotnet analyzer additional files that were already in the project.
    /// </summary>
    private static AnalyzerOptions GetWithSonarLintAdditionalFiles(AnalyzerOptions workspaceAnalyzerOptions, SonarLintConfiguration sonarLintConfiguration)
    {
        var sonarLintAdditionalFile = Convert(sonarLintConfiguration);
        var sonarLintAdditionalFileName = Path.GetFileName(sonarLintAdditionalFile.Path);

        var additionalFiles = workspaceAnalyzerOptions.AdditionalFiles;
        var builder = ImmutableArray.CreateBuilder<AdditionalText>();
        builder.AddRange(additionalFiles.Where(x => !IsSonarLintAdditionalFile(x)));
        builder.Add(sonarLintAdditionalFile);

        var modifiedAdditionalFiles = builder.ToImmutable();
        var finalAnalyzerOptions = new AnalyzerOptions(modifiedAdditionalFiles, workspaceAnalyzerOptions.AnalyzerConfigOptionsProvider);

        return finalAnalyzerOptions;

        bool IsSonarLintAdditionalFile(AdditionalText existingAdditionalFile) =>
            Path.GetFileName(existingAdditionalFile.Path).Equals(
                sonarLintAdditionalFileName,
                StringComparison.OrdinalIgnoreCase);
    }

    private static StandaloneRoslynFileExclusions ConvertExclusions(UserSettings settings)
    {
        var exclusions = new StandaloneRoslynFileExclusions(settings.AnalysisSettings);
        return exclusions;
    }

    private static (Dictionary<Language, List<IRoslynRuleStatus>>, Dictionary<Language, List<IRuleParameters>>) ConvertRules(UserSettings settings)
    {
        var ruleStatusesByLanguage = InitializeForAllRoslynLanguages<IRoslynRuleStatus>();
        var ruleParametersByLanguage = InitializeForAllRoslynLanguages<IRuleParameters>();
        foreach (var analysisSettingsRule in settings.AnalysisSettings.Rules)
        {
            if (!SonarCompositeRuleId.TryParse(analysisSettingsRule.Key, out var ruleId)
                || !Languages.Contains(ruleId.Language))
            {
                continue;
            }

            ruleStatusesByLanguage[ruleId.Language]
                .Add(new StandaloneRoslynRuleStatus(ruleId, analysisSettingsRule.Value.Level is RuleLevel.On));
            ruleParametersByLanguage[ruleId.Language]
                .Add(new StandaloneRoslynRuleParameters(ruleId, analysisSettingsRule.Value.Parameters));
        }
        return (ruleStatusesByLanguage, ruleParametersByLanguage);
    }

    private static Dictionary<Language, List<T>> InitializeForAllRoslynLanguages<T>()
    {
        var dictionary = new Dictionary<Language, List<T>>();

        foreach (var language in Languages)
        {
            dictionary[language] = [];
        }

        return dictionary;
    }

    private Project FindDocumentAndProject(string filePath, out string analysisFilePath)
    {
        analysisFilePath = null;
        var currentSolutionRoslynSolution = workspaceWrapper.CurrentSolution.RoslynSolution;

        foreach (var roslynSolutionProject in currentSolutionRoslynSolution.Projects)
        {
            foreach (var document in roslynSolutionProject.Documents)
            {
                if (CompareFilePath(filePath, document, out analysisFilePath))
                {
                    return roslynSolutionProject;
                }
            }
        }
        return default;
    }

    private static bool CompareFilePath(
        string filePath,
        Document document,
        out string analysisFilePath)
    {
        analysisFilePath = null;
        if (document.FilePath is null)
        {
            return false;
        }

        if (!document.FilePath.Equals(filePath)
            && (!document.FilePath.StartsWith(filePath) || !document.FilePath.EndsWith(".g.cs")))
        {
            return false;
        }

        analysisFilePath = document.FilePath;
        return true;
    }

    // There isn't a public implementation of source text so we need to create one
    internal class AdditionalTextImpl : AdditionalText
    {
        private readonly SourceText sourceText;

        public override string Path { get; }

        public AdditionalTextImpl(string path, string content)
        {
            Path = path;
            sourceText = SourceText.From(content);
        }

        public override SourceText GetText(CancellationToken cancellationToken = default) => sourceText;
    }

    internal static class SonarLintConfigurationSerializer
    {
        public static string Serialize(SonarLintConfiguration sonarLintConfiguration)
        {
            var settings = new XmlWriterSettings
            {
                CloseOutput = true,
                ConformanceLevel = ConformanceLevel.Document,
                Indent = true,
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
                OmitXmlDeclaration = false,
                Encoding = new UTF8Encoding(false) // to avoid generating unicode BOM
            };

            using (var stream = new MemoryStream { Position = 0 })
            using (var xmlWriter = XmlWriter.Create(stream, settings))
            {
                var serializer = new XmlSerializer(typeof(SonarLintConfiguration));
                serializer.Serialize(xmlWriter, sonarLintConfiguration);
                xmlWriter.Flush();

                var data = stream.ToArray();
                var sonarLintXmlFileContent = Encoding.UTF8.GetString(data);

                return sonarLintXmlFileContent;
            }
        }
    }
}
