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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

[Export(typeof(IRoslynAnalysisConfigurationProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynAnalysisConfigurationProvider(
    ISonarLintXmlProvider sonarLintXmlProvider,
    IRoslynAnalyzerProvider roslynAnalyzerProvider,
    IRoslynAnalysisProfilesProvider analyzerProfilesProvider,
    IAsyncLockFactory asyncLockFactory,
    IThreadHandling threadHandling,
    ILogger logger) : IRoslynAnalysisConfigurationProvider
{
    private readonly IAsyncLock asyncLock = asyncLockFactory.Create();
    private readonly ILogger logger = logger.ForContext(Resources.RoslynLogContext, Resources.RoslynAnalysisLogContext, Resources.RoslynAnalysisConfigurationLogContext);
    private AnalysisConfigurationCache? cache;

    public Task<IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration>> GetConfigurationAsync(
        List<ActiveRuleDto> activeRules,
        Dictionary<string, string> analysisProperties,
        AnalyzerInfoDto analyzerInfo) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            using (await asyncLock.AcquireAsync())
            {
                if (!cache.HasValue || cache.Value.Parameters.ShouldInvalidateCache(activeRules, analysisProperties, analyzerInfo))
                {
                    BuildConfigurations(activeRules, analysisProperties, analyzerInfo);
                }
                return cache!.Value.Configurations;
            }
        });

    private void BuildConfigurations(
        List<ActiveRuleDto> activeRules,
        Dictionary<string, string> analysisProperties,
        AnalyzerInfoDto analyzerInfo)
    {
        var analyzerAssemblyContents = roslynAnalyzerProvider.LoadAndProcessAnalyzerAssemblies(analyzerInfo);
        var analysisProfilesByLanguage = analyzerProfilesProvider.GetAnalysisProfilesByLanguage(analyzerAssemblyContents, activeRules, analysisProperties);
        var activeRulesMap = activeRules.ToDictionary(r => r.RuleId, r => r);
        cache = new AnalysisConfigurationCache(
            new AnalysisConfigurationParametersCache(activeRulesMap, analysisProperties, analyzerInfo),
            BuildConfigurations(analysisProfilesByLanguage));
    }

    private IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> BuildConfigurations(Dictionary<RoslynLanguage, RoslynAnalysisProfile> analysisProfilesByLanguage)
    {
        var configurations = new Dictionary<RoslynLanguage, RoslynAnalysisConfiguration>();
        foreach (var analyzerAndLanguage in analysisProfilesByLanguage)
        {
            var language = analyzerAndLanguage.Key;
            var analysisProfile = analyzerAndLanguage.Value;

            var languageLogContext = new MessageLevelContext { VerboseContext = [language.Id] };

            if (analysisProfile is not { Analyzers.Length: > 0 })
            {
                logger.LogVerbose(languageLogContext, Resources.RoslynAnalysisConfigurationNoAnalyzers, language.Name);
                continue;
            }

            if (!analysisProfile.Rules.Any(r => r.IsActive))
            {
                logger.LogVerbose(languageLogContext, Resources.RoslynAnalysisConfigurationNoActiveRules, language.Name);
                continue;
            }

            configurations.Add(
                language,
                new RoslynAnalysisConfiguration(
                    sonarLintXmlProvider.Create(analysisProfile),
                    analysisProfile.Rules.ToImmutableDictionary(x => x.RuleId.RuleKey, y => y.ReportDiagnostic),
                    analysisProfile.Analyzers,
                    analysisProfile.CodeFixProvidersByRuleKey));
        }
        return configurations;
    }

    private record struct AnalysisConfigurationCache(AnalysisConfigurationParametersCache Parameters, IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration> Configurations);
}
