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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;

internal sealed record AnalysisConfigurationParametersCache(
    Dictionary<string, ActiveRuleDto> ActiveRuleDtos,
    Dictionary<string, string> AnalysisProperties,
    AnalyzerInfoDto AnalyzerInfo);

internal static class AnalysisConfigurationParametersCacheExtensions
{
    public static bool ShouldInvalidateCache(
        this AnalysisConfigurationParametersCache? cache,
        List<ActiveRuleDto> newActiveRuleDtos,
        Dictionary<string, string> newAnalysisProperties,
        AnalyzerInfoDto analyzerInfo) =>
        cache == null
        || cache.AnalyzerInfo != analyzerInfo
        || !AreSameActiveRuleDtos(newActiveRuleDtos, cache.ActiveRuleDtos)
        || !AreDictionariesEqual(newAnalysisProperties, cache.AnalysisProperties);

    private static bool AreSameActiveRuleDtos(List<ActiveRuleDto> newActiveRuleDtos, Dictionary<string, ActiveRuleDto> oldActiveRuleDtos)
    {
        if (oldActiveRuleDtos.Count != newActiveRuleDtos.Count)
        {
            return false;
        }

        foreach (var newRule in newActiveRuleDtos)
        {
            if (!oldActiveRuleDtos.TryGetValue(newRule.RuleId, out var cachedActiveRuleDto) ||
                !AreDictionariesEqual(newRule.Parameters, cachedActiveRuleDto.Parameters))
            {
                return false;
            }
        }
        return true;
    }

    private static bool AreDictionariesEqual(Dictionary<string, string> newDictionary, Dictionary<string, string> oldDictionary)
    {
        if (newDictionary.Count != oldDictionary.Count)
        {
            return false;
        }

        foreach (var newKvp in newDictionary)
        {
            if (!oldDictionary.TryGetValue(newKvp.Key, out var oldValue) || oldValue != newKvp.Value)
            {
                return false;
            }
        }
        return true;
    }
}
