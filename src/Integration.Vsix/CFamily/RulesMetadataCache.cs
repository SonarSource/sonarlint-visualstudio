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
using SonarLint.VisualStudio.Core;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.RulesLoader;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal interface IRulesConfiguration
    {
        string LanguageKey { get; }

        IEnumerable<string> AllPartialRuleKeys { get; }

        IEnumerable<string> ActivePartialRuleKeys { get; }

        IDictionary<string, IDictionary<string, string>> RulesParameters { get; }

        IDictionary<string, RuleMetadata> RulesMetadata { get; }
    }

    internal sealed class RulesMetadataCache
    {
        private static IEnumerable<string> AllLanguagesAllRuleKeys { get; }
        private static IEnumerable<string> AllLanguagesActiveRuleKeys { get; }
        private static IDictionary<string, RuleMetadata> AllLanguagesRulesMetadata { get; }
        private static IDictionary<string, IDictionary<string, string>> AllLanguagesRulesParameters { get; }

        private static IDictionary<string, IRulesConfiguration> RulesByLanguage { get; }

        public static IRulesConfiguration GetSettings(string cFamilyLanguage)
        {
            RulesByLanguage.TryGetValue(cFamilyLanguage, out var rulesConfiguration);
            return rulesConfiguration;
        }

        static RulesMetadataCache()
        {
            // Read all rules/metadata, irrespective of language. Stored in
            // statics so we don't re-read the files for each language
            AllLanguagesAllRuleKeys = RulesLoader.ReadRulesList();
            AllLanguagesActiveRuleKeys = RulesLoader.ReadActiveRulesList();
            AllLanguagesRulesParameters = AllLanguagesAllRuleKeys
                .ToDictionary(key => key, key => RulesLoader.ReadRuleParams(key));
            AllLanguagesRulesMetadata = AllLanguagesAllRuleKeys
                .ToDictionary(key => key, key => RulesLoader.ReadRuleMetadata(key));

            RulesByLanguage = new Dictionary<string, IRulesConfiguration>
            {
                { SonarLanguageKeys.CPlusPlus,  new SingleLanguageRulesConfiguration(SonarLanguageKeys.CPlusPlus)},
                { SonarLanguageKeys.C, new SingleLanguageRulesConfiguration(SonarLanguageKeys.C)}
            };
        }

        private class SingleLanguageRulesConfiguration : IRulesConfiguration
        {
            private static StringComparer RuleKeyComparer = StringComparer.OrdinalIgnoreCase;

            public SingleLanguageRulesConfiguration(string cFamilyLanguage)
            {
                LanguageKey = cFamilyLanguage;

                var ruleKeysForLanguage = AllLanguagesRulesMetadata
                    .Where(kvp => kvp.Value.CompatibleLanguages.Contains(cFamilyLanguage, RuleKeyComparer))
                    .Select(kvp => kvp.Key)
                    .ToArray();

                AllPartialRuleKeys = ruleKeysForLanguage;
                ActivePartialRuleKeys = AllLanguagesActiveRuleKeys
                    .Intersect(ruleKeysForLanguage, RuleKeyComparer)
                    .ToArray();

                RulesParameters = ruleKeysForLanguage
                    .ToDictionary(key => key, key => AllLanguagesRulesParameters[key]);
                RulesMetadata = ruleKeysForLanguage
                    .ToDictionary(key => key, key => AllLanguagesRulesMetadata[key]);
            }

            public string LanguageKey { get; }

            public IEnumerable<string> AllPartialRuleKeys { get; }

            public IEnumerable<string> ActivePartialRuleKeys { get; }

            public IDictionary<string, IDictionary<string, string>> RulesParameters { get; }

            public IDictionary<string, RuleMetadata> RulesMetadata { get; }
        }
    }
}
