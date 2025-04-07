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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;

// Logically equivalent to the SonarScanner for MSBuild class "RoslynSonarLint"
// See https://github.com/SonarSource/sonar-scanner-msbuild/blob/9ccfdb648a0411014b29c7aee8e347aeab87ea71/src/SonarScanner.MSBuild.PreProcessor/Roslyn/RoslynSonarLint.cs#L29

namespace SonarLint.VisualStudio.Integration.CSharpVB;

public interface ISonarLintConfigGenerator
{
    /// <summary>
    /// Generates the data for a SonarLint.xml file for the specified language
    /// </summary>
    SonarLintConfiguration Generate(IEnumerable<IRuleParameters> rules,
        IDictionary<string, string> sonarProperties,
        IFileExclusions fileExclusions,
        Language language);
}

[Export(typeof(ISonarLintConfigGenerator))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class SonarLintConfigGenerator(ILanguageProvider languageProvider) : ISonarLintConfigGenerator
{
    private const string SecuredPropertySuffix = ".secured";

    public SonarLintConfiguration Generate(
        IEnumerable<IRuleParameters> rules,
        IDictionary<string, string> sonarProperties,
        IFileExclusions fileExclusions,
        Language language)
    {
        if (rules == null) { throw new ArgumentNullException(nameof(rules)); }
        if (sonarProperties == null) { throw new ArgumentNullException(nameof(sonarProperties)); }
        if (fileExclusions == null) { throw new ArgumentNullException(nameof(fileExclusions)); }
        if (language == null) { throw new ArgumentNullException(nameof(language)); }

        var slvsSettings = GetSettingsForLanguage(language, sonarProperties);
        slvsSettings.AddRange(GetInclusionsExclusions(fileExclusions));

        // We don't expect third-party rules to look for their settings in SonarLint.xml so we
        // only fetch parameters for SonarC#/VB rules.
        var sonarRepoKey = GetSonarRepoKey(language);
        var slvsRules = GetRulesForRepo(sonarRepoKey, rules);

        return new SonarLintConfiguration { Settings = slvsSettings, Rules = slvsRules };
    }

    private static IEnumerable<SonarLintKeyValuePair> GetInclusionsExclusions(IFileExclusions exclusions) =>
        exclusions
            .ToDictionary()
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .Select(x =>
                new SonarLintKeyValuePair { Key = x.Key, Value = x.Value });

    private string GetSonarRepoKey(Language language)
    {
        if (languageProvider.RoslynLanguages.Contains(language))
        {
            return language.RepoInfo.Key;
        }

        throw new ArgumentOutOfRangeException(nameof(language));
    }

    private static List<SonarLintKeyValuePair> GetSettingsForLanguage(Language language, IDictionary<string, string> sonarProperties) =>
        sonarProperties.Where(kvp => IsSettingForLanguage(language.ServerLanguageKey, kvp.Key) && !IsSecuredServerProperty(kvp.Key))
            .Select(ToSonarLintKeyValue)
            .OrderBy(s => s.Key)
            .ToList();

    private static bool IsSettingForLanguage(string language, string propertyKey)
    {
        var prefix = $"sonar.{language}.";

        return propertyKey.StartsWith(prefix) &&
               propertyKey.Length > prefix.Length;
    }

    private static bool IsSecuredServerProperty(string s) => s.EndsWith(SecuredPropertySuffix, StringComparison.OrdinalIgnoreCase);

    private static List<SonarLintRule> GetRulesForRepo(string sonarRepoKey, IEnumerable<IRuleParameters> sqRules) =>
        sqRules.Where(ar => sonarRepoKey.Equals(ar.RepositoryKey) && HasParameters(ar))
            .Select(ToSonarLintRule)
            .OrderBy(slr => slr.Key)
            .ToList();

    private static bool HasParameters(IRuleParameters sqRule) => sqRule.Parameters is {Count: > 0};

    private static SonarLintRule ToSonarLintRule(IRuleParameters sqRule)
    {
        List<SonarLintKeyValuePair> slvsParameters = null;
        if (sqRule.Parameters != null && sqRule.Parameters.Count > 0)
        {
            slvsParameters = sqRule.Parameters
                .Select(ToSonarLintKeyValue)
                .OrderBy(p => p.Key)
                .ToList();
        }

        return new SonarLintRule() { Key = sqRule.Key, Parameters = slvsParameters };
    }

    private static SonarLintKeyValuePair ToSonarLintKeyValue(KeyValuePair<string, string> setting) => new() { Key = setting.Key, Value = setting.Value };
}
