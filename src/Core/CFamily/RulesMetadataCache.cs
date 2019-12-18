/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Core.CFamily
{
    public sealed class RulesMetadataCache
    {
        private IEnumerable<string> AllLanguagesAllRuleKeys { get; }
        private IEnumerable<string> AllLanguagesActiveRuleKeys { get; }
        private IDictionary<string, RuleMetadata> AllLanguagesRulesMetadata { get; }
        private IDictionary<string, IDictionary<string, string>> AllLanguagesRulesParameters { get; }

        private IDictionary<string, IRulesConfiguration> RulesByLanguage { get; }

        public IRulesConfiguration GetSettings(string cFamilyLanguage)
        {
            RulesByLanguage.TryGetValue(cFamilyLanguage, out var rulesConfiguration);
            return rulesConfiguration;
        }

        public RulesMetadataCache(string rulesDirectoryPath)
        {
            var rulesLoader = new RulesLoader(rulesDirectoryPath);

            // Read all rules/metadata, irrespective of language. Stored in
            // statics so we don't re-read the files for each language
            AllLanguagesAllRuleKeys = rulesLoader.ReadRulesList();
            AllLanguagesActiveRuleKeys = rulesLoader.ReadActiveRulesList();
            AllLanguagesRulesParameters = AllLanguagesAllRuleKeys
                .ToDictionary(key => key, key => rulesLoader.ReadRuleParams(key));
            AllLanguagesRulesMetadata = AllLanguagesAllRuleKeys
                .ToDictionary(key => key, key => rulesLoader.ReadRuleMetadata(key));

            RulesByLanguage = new Dictionary<string, IRulesConfiguration>
            {
                { SonarLanguageKeys.CPlusPlus,  new SingleLanguageRulesConfiguration(this, SonarLanguageKeys.CPlusPlus)},
                { SonarLanguageKeys.C, new SingleLanguageRulesConfiguration(this, SonarLanguageKeys.C)}
            };
        }

        private class SingleLanguageRulesConfiguration : IRulesConfiguration
        {
            private static StringComparer RuleKeyComparer = StringComparer.OrdinalIgnoreCase;

            public SingleLanguageRulesConfiguration(RulesMetadataCache cache, string cFamilyLanguage)
            {
                LanguageKey = cFamilyLanguage;

                var ruleKeysForLanguage = cache.AllLanguagesRulesMetadata
                    .Where(kvp => kvp.Value.CompatibleLanguages.Contains(cFamilyLanguage, RuleKeyComparer))
                    .Select(kvp => kvp.Key)
                    .ToArray();

                AllPartialRuleKeys = ruleKeysForLanguage;
                ActivePartialRuleKeys = cache.AllLanguagesActiveRuleKeys
                    .Intersect(ruleKeysForLanguage, RuleKeyComparer)
                    .ToArray();

                RulesParameters = ruleKeysForLanguage
                    .ToDictionary(key => key, key => cache.AllLanguagesRulesParameters[key]);
                RulesMetadata = ruleKeysForLanguage
                    .ToDictionary(key => key, key => cache.AllLanguagesRulesMetadata[key]);
            }

            public string LanguageKey { get; }

            public IEnumerable<string> AllPartialRuleKeys { get; }

            public IEnumerable<string> ActivePartialRuleKeys { get; }

            public IDictionary<string, IDictionary<string, string>> RulesParameters { get; }

            public IDictionary<string, RuleMetadata> RulesMetadata { get; }
        }
    }
}
