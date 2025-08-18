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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

[Export(typeof(IRoslynAnalysisProfilesProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class RoslynAnalysisProfilesProvider : IRoslynAnalysisProfilesProvider
{
    public Dictionary<Language, RoslynAnalysisProfile> GetAnalysisProfilesByLanguage(
        ImmutableDictionary<Language, AnalyzersAndSupportedRules> supportedDiagnosticsByLanguage,
        List<ActiveRuleDto> activeRules,
        Dictionary<string, string>? analysisProperties)
    {
        var roslynAnalysisProfiles = InitializeProfilesForEachLanguage(supportedDiagnosticsByLanguage);
        AddRules(activeRules, supportedDiagnosticsByLanguage, roslynAnalysisProfiles);
        AddProperties(analysisProperties, roslynAnalysisProfiles);

        return roslynAnalysisProfiles;
    }

    private static Dictionary<Language, RoslynAnalysisProfile> InitializeProfilesForEachLanguage(ImmutableDictionary<Language, AnalyzersAndSupportedRules> supportedDiagnosticsByLanguage)
    {
        var roslynAnalysisProfiles = supportedDiagnosticsByLanguage.Keys.ToDictionary(x => x, _ => new RoslynAnalysisProfile([], []));
        return roslynAnalysisProfiles;
    }

    private static void AddProperties(Dictionary<string, string>? analysisProperties, Dictionary<Language, RoslynAnalysisProfile> roslynAnalysisProfiles)
    {
        if (analysisProperties == null)
        {
            return;
        }

        foreach (var languageAndProfile in roslynAnalysisProfiles)
        {
            var prefix = $"sonar.{languageAndProfile.Key.ServerLanguageKey}.";

            foreach (var analysisProperty in analysisProperties.Where(analysisProperty => IsSettingForLanguage(prefix, analysisProperty.Key)))
            {
                languageAndProfile.Value.AnalysisProperties.Add(analysisProperty.Key, analysisProperty.Value);
            }
        }
    }

    private static bool IsSettingForLanguage(string prefix, string propertyKey) =>
        propertyKey.StartsWith(prefix) && propertyKey.Length > prefix.Length;

    private static void AddRules(
        List<ActiveRuleDto> activeRules,
        ImmutableDictionary<Language, AnalyzersAndSupportedRules> supportedDiagnosticsByLanguage,
        Dictionary<Language, RoslynAnalysisProfile> roslynAnalysisProfiles)
    {
        foreach (var activeRule in activeRules)
        {
            if (!SonarCompositeRuleId.TryParse(activeRule.RuleKey, out var ruleId))
            {
                continue;
            }

            if (!roslynAnalysisProfiles.TryGetValue(ruleId.Language, out var analysisProfile))
            {
                continue;
            }

            analysisProfile.Rules.Add(
                new RoslynRuleConfiguration(
                    ruleId,
                    supportedDiagnosticsByLanguage[ruleId.Language].SupportedRuleKeys.Contains(ruleId.RuleKey),
                    activeRule.RuleParameters));
        }
    }
}
