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

using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Integration.CSharpVB.StandaloneMode;

public interface IStandaloneRoslynSettingsUpdater
{
    void Update(UserSettings settings);
}

[Export(typeof(IStandaloneRoslynSettingsUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class StandaloneRoslynSettingsUpdater(
    IRoslynConfigGenerator generator,
    ILanguageProvider languageProvider,
    IThreadHandling threadHandling)
    : IStandaloneRoslynSettingsUpdater
{
    private readonly object lockObject = new();

    public void Update(UserSettings settings) =>
        threadHandling
            .RunOnBackgroundThread(() => UpdateInternal(settings))
            .Forget();

    private void UpdateInternal(UserSettings settings)
    {
        lock (lockObject)
        {
            var exclusions = ConvertExclusions(settings);
            var (ruleStatusesByLanguage, ruleParametersByLanguage) = ConvertRules(settings);

            foreach (var language in languageProvider.RoslynLanguages)
            {
                generator.GenerateAndSaveConfiguration(
                    language,
                    settings.BaseDirectory,
                    settings.AnalysisSettings.AnalysisProperties,
                    exclusions,
                    ruleStatusesByLanguage[language],
                    ruleParametersByLanguage[language]);
            }
        }
    }

    private static StandaloneRoslynFileExclusions ConvertExclusions(UserSettings settings)
    {
        var exclusions = new StandaloneRoslynFileExclusions(settings.AnalysisSettings);
        return exclusions;
    }

    private (Dictionary<Language, List<IRoslynRuleStatus>>, Dictionary<Language, List<IRuleParameters>>) ConvertRules(UserSettings settings)
    {
        var ruleStatusesByLanguage = InitializeForAllRoslynLanguages<IRoslynRuleStatus>();
        var ruleParametersByLanguage = InitializeForAllRoslynLanguages<IRuleParameters>();
        foreach (var analysisSettingsRule in settings.AnalysisSettings.Rules)
        {
            if (!SonarCompositeRuleId.TryParse(analysisSettingsRule.Key, out var ruleId)
                || !languageProvider.RoslynLanguages.Contains(ruleId.Language))
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

    private Dictionary<Language, List<T>> InitializeForAllRoslynLanguages<T>()
    {
        var dictionary = new Dictionary<Language, List<T>>();

        foreach (var language in languageProvider.RoslynLanguages)
        {
            dictionary[language] = [];
        }

        return dictionary;
    }
}
