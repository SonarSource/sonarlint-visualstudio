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
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

[Export(typeof(IRoslynAnalysisProfilesProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class RoslynAnalysisProfilesProvider : IRoslynAnalysisProfilesProvider
{
    public Dictionary<RoslynLanguage, RoslynAnalysisProfile> GetAnalysisProfilesByLanguage(
        ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents> analyzerAssemblyContents,
        List<ActiveRuleDto> activeRules,
        Dictionary<string, string>? analysisProperties)
    {
        var roslynAnalysisProfiles = InitializeProfilesForEachLanguage(analyzerAssemblyContents);
        AddRules(activeRules, analyzerAssemblyContents, roslynAnalysisProfiles);
        AddProperties(analysisProperties, roslynAnalysisProfiles);

        return roslynAnalysisProfiles;
    }

    private static Dictionary<RoslynLanguage, RoslynAnalysisProfile> InitializeProfilesForEachLanguage(ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents> analyzerAssemblyContents) =>
        analyzerAssemblyContents.ToDictionary(
            x => x.Key,
            x => new RoslynAnalysisProfile(x.Value.Analyzers, x.Value.CodeFixProvidersByRuleKey, [], []));

    private static void AddRules(
        List<ActiveRuleDto> activeRules,
        ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents> supportedRulesByLanguage,
        Dictionary<RoslynLanguage, RoslynAnalysisProfile> roslynAnalysisProfiles)
    {
        var activeRulesById = activeRules.ToDictionary(x => x.RuleId, y => y);

        foreach (var kvp in supportedRulesByLanguage)
        {
            var language = kvp.Key;

            if (!roslynAnalysisProfiles.TryGetValue(language, out var analysisProfile))
            {
                continue;
            }

            foreach (var ruleId in kvp.Value.SupportedRuleKeys.Select(ruleKey => new SonarCompositeRuleId(language.RepoKey, ruleKey)))
            {
                analysisProfile.Rules.Add(activeRulesById.TryGetValue(ruleId.Id, out var activeRule)
                    ? new RoslynRuleConfiguration(ruleId, true, activeRule.Parameters)
                    : new RoslynRuleConfiguration(ruleId, false, null));
            }
        }
    }

    private static void AddProperties(Dictionary<string, string>? analysisProperties, Dictionary<RoslynLanguage, RoslynAnalysisProfile> roslynAnalysisProfiles)
    {
        if (analysisProperties == null)
        {
            return;
        }

        foreach (var languageAndProfile in roslynAnalysisProfiles)
        {
            var prefix = $"sonar.{languageAndProfile.Key.ServerLanguageKey}.";

            foreach (var analysisProperty in analysisProperties.Where(analysisProperty => IsPropertyForLanguage(prefix, analysisProperty.Key)))
            {
                languageAndProfile.Value.AnalysisProperties.Add(analysisProperty.Key, analysisProperty.Value);
            }
        }
    }

    private static bool IsPropertyForLanguage(string prefix, string propertyKey) =>
        propertyKey.StartsWith(prefix) && propertyKey.Length > prefix.Length;
}
