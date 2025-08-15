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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

internal record struct ActiveRoslynRule(string RuleId, Dictionary<string, string>? Parameters);

internal class RoslynAnalysisConfigurationProvider
{
    private IRuleStatusProvider ruleStatusProvider;
    private ISonarLintXmlProvider sonarLintXmlProvider;
    private IAnalyzerProvider analyzerProvider;
    private ILanguageProvider languageProvider;

    Dictionary<Language, RoslynAnalysisConfiguration> GetConfiguration(List<ActiveRuleDto> activeRules, Dictionary<string, string>? analysisProperties)
    {
        var analyzersByLanguage = analyzerProvider.GetAnalyzersByLanguage();
        var ruleConfiguration = GetRuleConfiguration(activeRules);

        return languageProvider.RoslynLanguages.ToDictionary(x => x, y =>
        {
            var analyzersCollection = analyzersByLanguage[y];
            var activeRulesConfiguration = ruleConfiguration[y];

            return new RoslynAnalysisConfiguration(
                sonarLintXmlProvider.Create(activeRulesConfiguration.Values, analysisProperties),
                ruleStatusProvider.GetDiagnosticOptions(analyzersCollection.SupportedDiagnosticIds, activeRulesConfiguration),
                analyzersCollection.Analyzers);
        });
    }

    private IReadOnlyDictionary<Language, Dictionary<string, ActiveRoslynRule>> GetRuleConfiguration(List<ActiveRuleDto> activeRules)
    {
        var dictionary = languageProvider.RoslynLanguages.ToDictionary(x => x, y => new Dictionary<string, ActiveRoslynRule>());

        foreach (var activeRule in activeRules)
        {
            if (!SonarCompositeRuleId.TryParse(activeRule.RuleKey, out var ruleId))
            {
                continue;
            }

            if (!dictionary.TryGetValue(ruleId.Language, out var value))
            {
                continue;
            }

            value.Add(ruleId.RuleKey, new ActiveRoslynRule(ruleId.RuleKey, activeRule.RuleParameters));
        }

        return dictionary;
    }
}
