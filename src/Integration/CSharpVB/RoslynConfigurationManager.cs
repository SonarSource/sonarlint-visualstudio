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
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;

namespace SonarLint.VisualStudio.Integration.CSharpVB;

internal interface IRoslynConfigurationManager
{
    Task<RoslynAnalysisConfiguration> GetConfigurationAsync(Language language);
}

internal record RoslynAnalysisConfiguration(AdditionalText SonarLintXml, ImmutableDictionary<string, ReportDiagnostic> DiagnosticOptions, ImmutableArray<DiagnosticAnalyzer> Analyzers);

[Export(typeof(IRoslynConfigurationManager))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynConfigurationManager(
    IUserSettingsProvider userSettingsProvider,
    ISonarLintConfigGenerator roslynConfigGenerator,
    IEnterpriseRoslynAnalyzerProvider enterpriseAnalyzerProvider,
    IBasicRoslynAnalyzerProvider basicAnalyzerProvider,
    IAsyncLockFactory asyncLockFactory,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    ILogger logger) : IRoslynConfigurationManager
{
    private static readonly List<Language> Languages = [Language.CSharp, Language.VBNET];
    private readonly IAsyncLock asyncLock = asyncLockFactory.Create();
    private ImmutableArray<DiagnosticAnalyzer>? cachedAnalyzers;
    private ImmutableDictionary<string, ReportDiagnostic> cachedCsharpDiagnosticStatuses;
    private ImmutableDictionary<string, ReportDiagnostic> cachedVbnetDiagnosticStatuses;
    private AdditionalText cachedCsharpSonarLintAdditionalFile;
    private AdditionalText cachedVbnetSonarLintAdditionalFile;
    private string lastConfigScopeId;

    public async Task<RoslynAnalysisConfiguration> GetConfigurationAsync(Language language)
    {
        // this class is mostly needed for VS-based manual analysis to work. QP info and

        var configurationScopeId = activeConfigScopeTracker.Current?.Id;

        using (await asyncLock.AcquireAsync())
        {
            if (configurationScopeId != lastConfigScopeId || !cachedAnalyzers.HasValue)
            {
                var exclusions = ConvertExclusions(userSettingsProvider.UserSettings);
                var (ruleStatusesByLanguage, ruleParametersByLanguage) = ConvertRules(userSettingsProvider.UserSettings);

                cachedCsharpDiagnosticStatuses = ruleStatusesByLanguage[Language.CSharp].ToImmutableDictionary(
                    x => x.Key,
                    ConvertSeverityToReportDiagnostic);
                cachedVbnetDiagnosticStatuses = ruleStatusesByLanguage[Language.VBNET].ToImmutableDictionary(
                    x => x.Key,
                    ConvertSeverityToReportDiagnostic);

                cachedCsharpSonarLintAdditionalFile = ConfigurationSerializationService.Convert(ConfigurationSerializationService.Serialize(roslynConfigGenerator.Generate(
                    ruleParametersByLanguage[Language.CSharp],
                    userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties,
                    exclusions,
                    Language.CSharp)));
                cachedVbnetSonarLintAdditionalFile = ConfigurationSerializationService.Convert(ConfigurationSerializationService.Serialize(roslynConfigGenerator.Generate(
                    ruleParametersByLanguage[Language.VBNET],
                    userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties,
                    exclusions,
                    Language.VBNET)));

                cachedAnalyzers = await GetAnalyzersAsync(configurationScopeId);
                lastConfigScopeId = configurationScopeId;
            }

            var configurationAsync = language == Language.CSharp
                ? (cachedCsharpSonarLintAdditionalFile, cachedCsharpDiagnosticStatuses, cachedAnalyzers.Value)
                : (cachedVbnetSonarLintAdditionalFile, cachedVbnetDiagnosticStatuses, cachedAnalyzers.Value);
            return new RoslynAnalysisConfiguration(configurationAsync.Item1, configurationAsync.Item2, configurationAsync.Item3);
        }
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
