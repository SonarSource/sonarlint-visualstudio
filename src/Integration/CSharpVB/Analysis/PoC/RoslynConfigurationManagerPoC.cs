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
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;
using SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;

namespace SonarLint.VisualStudio.Integration.CSharpVB.Analysis.PoC;

internal interface IRoslynConfigurationManagerPoC
{
    Task<ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration>> GetConfigurationAsync();
}

[Export(typeof(IRoslynConfigurationManagerPoC))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynConfigurationManagerPoC(
    IUserSettingsProvider userSettingsProvider,
    ISonarLintConfigGenerator roslynConfigGenerator,
    IEnterpriseRoslynAnalyzerProvider enterpriseAnalyzerProvider,
    IBasicRoslynAnalyzerProvider basicAnalyzerProvider,
    IAsyncLockFactory asyncLockFactory,
    IActiveConfigScopeTracker activeConfigScopeTracker,
    ILogger logger) : IRoslynConfigurationManagerPoC
{
    private static readonly List<Language> Languages = [Language.CSharp, Language.VBNET];
    private readonly IAsyncLock asyncLock = asyncLockFactory.Create();
    private ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> cache;
    private string lastConfigScopeId;

    public async Task<ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration>> GetConfigurationAsync()
    {
        // this class is mostly needed for VS-based manual analysis to work. QP info and

        var configurationScopeId = activeConfigScopeTracker.Current?.Id;

        using (await asyncLock.AcquireAsync())
        {
            if (configurationScopeId != lastConfigScopeId || cache == null)
            {
                var exclusions = ConvertExclusions(userSettingsProvider.UserSettings);
                var (ruleStatusesByLanguage, ruleParametersByLanguage) = ConvertRules(userSettingsProvider.UserSettings);

                var csharpDiagnosticStatuses = ruleStatusesByLanguage[Language.CSharp].ToImmutableDictionary(
                    x => x.Key,
                    ConvertSeverityToReportDiagnostic);
                var vbnetDiagnosticStatuses = ruleStatusesByLanguage[Language.VBNET].ToImmutableDictionary(
                    x => x.Key,
                    ConvertSeverityToReportDiagnostic);

                var csharpSonarLintAdditionalFile = ConfigurationSerializationService.Convert(ConfigurationSerializationService.Serialize(roslynConfigGenerator.Generate(
                    ruleParametersByLanguage[Language.CSharp],
                    userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties,
                    exclusions,
                    Language.CSharp)));
                var vbnetSonarLintAdditionalFile = ConfigurationSerializationService.Convert(ConfigurationSerializationService.Serialize(roslynConfigGenerator.Generate(
                    ruleParametersByLanguage[Language.VBNET],
                    userSettingsProvider.UserSettings.AnalysisSettings.AnalysisProperties,
                    exclusions,
                    Language.VBNET)));

                // analyzer caching may be improved
                var analyzers = await GetAnalyzersAsync(configurationScopeId);
                lastConfigScopeId = configurationScopeId;

                var builder = ImmutableDictionary.CreateBuilder<Language, SonarRoslynAnalysisConfiguration>();
                builder.Add(Language.CSharp, new SonarRoslynAnalysisConfiguration(csharpSonarLintAdditionalFile, csharpDiagnosticStatuses, analyzers));
                builder.Add(Language.VBNET, new SonarRoslynAnalysisConfiguration(vbnetSonarLintAdditionalFile, vbnetDiagnosticStatuses, analyzers));
                cache = builder.ToImmutable();
            }

            return cache;
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
