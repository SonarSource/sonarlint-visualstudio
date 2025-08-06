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
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;

namespace SonarLint.VisualStudio.Integration.CSharpVB;

public interface IRoslynConfigurationManager
{
    /// <summary>
    /// Gets the configuration for a specific language
    /// </summary>
    /// <param name="configurationScopeId">Current configuration scope ID</param>
    /// <param name="language">The language to get configuration for</param>
    /// <returns>The configuration tuple containing SonarLintConfiguration, diagnostic statuses, and analyzers</returns>
    Task<(SonarLintConfiguration sonarLintConfig, ImmutableDictionary<string, ReportDiagnostic> diagnosticStatuses, ImmutableArray<DiagnosticAnalyzer>? analyzers)>
        GetConfigurationAsync(string configurationScopeId, Language language);
    
    /// <summary>
    /// Combines SonarLint analyzer configuration with existing workspace analyzer options 
    /// </summary>
    /// <param name="workspaceAnalyzerOptions">The existing analyzer options from the workspace</param>
    /// <param name="sonarLintConfiguration">The SonarLint configuration to add</param>
    /// <returns>Combined analyzer options</returns>
    AnalyzerOptions GetWithSonarLintAdditionalFiles(AnalyzerOptions workspaceAnalyzerOptions, SonarLintConfiguration sonarLintConfiguration);
}

[Export(typeof(IRoslynConfigurationManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class RoslynConfigurationManager(
    IUserSettingsProvider userSettingsProvider,
    ISonarLintConfigGenerator roslynConfigGenerator,
    IEnterpriseRoslynAnalyzerProvider enterpriseAnalyzerProvider,
    IBasicRoslynAnalyzerProvider basicAnalyzerProvider,
    IAsyncLockFactory asyncLockFactory,
    ILogger logger) : IRoslynConfigurationManager
{
    private static readonly List<Language> Languages = [Language.CSharp, Language.VBNET];
    private readonly IAsyncLock asyncLock = asyncLockFactory.Create();
    private ImmutableArray<DiagnosticAnalyzer>? cachedAnalyzers;
    private ImmutableDictionary<string, ReportDiagnostic> cachedCsharpDiagnosticStatuses;
    private ImmutableDictionary<string, ReportDiagnostic> cachedVbnetDiagnosticStatuses;
    private SonarLintConfiguration cachedCsharpSonarLintConfig;
    private SonarLintConfiguration cachedVbnetSonarLintConfig;
    private string lastConfigScopeId;

    public async Task<(SonarLintConfiguration sonarLintConfig, ImmutableDictionary<string, ReportDiagnostic> diagnosticStatuses, ImmutableArray<DiagnosticAnalyzer>? analyzers)>
        GetConfigurationAsync(string configurationScopeId, Language language)
    {
        using (await asyncLock.AcquireAsync())
        {
            // If config scope hasn't changed, use cached configuration
            if (configurationScopeId == lastConfigScopeId && 
                cachedAnalyzers.HasValue && 
                (language == Language.CSharp ? cachedCsharpSonarLintConfig != null : cachedVbnetSonarLintConfig != null))
            {
                return language == Language.CSharp
                    ? (cachedCsharpSonarLintConfig, cachedCsharpDiagnosticStatuses, cachedAnalyzers)
                    : (cachedVbnetSonarLintConfig, cachedVbnetDiagnosticStatuses, cachedAnalyzers);
            }

            // Otherwise, regenerate the configuration
            var exclusions = ConvertExclusions(userSettingsProvider.UserSettings);
            var (ruleStatusesByLanguage, ruleParametersByLanguage) = ConvertRules(userSettingsProvider.UserSettings);

            // Cache diagnostic statuses for each language
            cachedCsharpDiagnosticStatuses = ruleStatusesByLanguage[Language.CSharp].ToImmutableDictionary(
                x => x.Key,
                ConvertSeverityToReportDiagnostic);
            cachedVbnetDiagnosticStatuses = ruleStatusesByLanguage[Language.VBNET].ToImmutableDictionary(
                x => x.Key,
                ConvertSeverityToReportDiagnostic);

            // Generate and cache SonarLint configuration for each language
            cachedCsharpSonarLintConfig = roslynConfigGenerator.Generate(
                ruleParametersByLanguage[Language.CSharp],
                userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties,
                exclusions,
                Language.CSharp);
            cachedVbnetSonarLintConfig = roslynConfigGenerator.Generate(
                ruleParametersByLanguage[Language.VBNET],
                userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties,
                exclusions,
                Language.VBNET);
                
            // Get and cache analyzers for the current configuration scope
            cachedAnalyzers = await GetAnalyzersAsync(configurationScopeId);
            lastConfigScopeId = configurationScopeId;

            return language == Language.CSharp
                ? (cachedCsharpSonarLintConfig, cachedCsharpDiagnosticStatuses, cachedAnalyzers)
                : (cachedVbnetSonarLintConfig, cachedVbnetDiagnosticStatuses, cachedAnalyzers);
        }
    }

    public AnalyzerOptions GetWithSonarLintAdditionalFiles(AnalyzerOptions workspaceAnalyzerOptions, SonarLintConfiguration sonarLintConfiguration)
    {
        var sonarLintAdditionalFile = ConfigurationSerializationService.Convert(sonarLintConfiguration);
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
}
