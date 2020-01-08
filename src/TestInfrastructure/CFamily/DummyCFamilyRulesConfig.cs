/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Collections.Generic;
using System.Linq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    public class DummyCFamilyRulesConfig : ICFamilyRulesConfig
    {
        private readonly IDictionary<string, bool> ruleKeyToActiveMap;

        public DummyCFamilyRulesConfig(string languageKey)
        {
            LanguageKey = languageKey;
            ruleKeyToActiveMap = new Dictionary<string, bool>();
        }
        public DummyCFamilyRulesConfig AddRule(string partialRuleKey, IssueSeverity issueSeverity, bool isActive)
        {
            return AddRule(partialRuleKey, issueSeverity, isActive, null);
        }

        public DummyCFamilyRulesConfig AddRule(string partialRuleKey, bool isActive)
        {
            ruleKeyToActiveMap[partialRuleKey] = isActive;
            RulesMetadata[partialRuleKey] = new RuleMetadata();
            return this;
        }

        public DummyCFamilyRulesConfig AddRule(string partialRuleKey, bool isActive, Dictionary<string, string> parameters)
        {
            return AddRule(partialRuleKey, (IssueSeverity)0 /* default enum value */, isActive, parameters);
        }

        public DummyCFamilyRulesConfig AddRule(string partialRuleKey, IssueSeverity issueSeverity, bool isActive, Dictionary<string, string> parameters)
        {
            ruleKeyToActiveMap[partialRuleKey] = isActive;
            RulesMetadata[partialRuleKey] = new RuleMetadata { DefaultSeverity = issueSeverity };

            if (parameters != null)
            {
                RulesParameters[partialRuleKey] = parameters;
            }
            return this;
        }

        #region IRulesConfiguration interface

        public string LanguageKey { get; set; }

        public IEnumerable<string> AllPartialRuleKeys => ruleKeyToActiveMap.Keys;

        public IEnumerable<string> ActivePartialRuleKeys => ruleKeyToActiveMap.Where(kvp => kvp.Value)
                                                                        .Select(kvp => kvp.Key)
                                                                        .ToList();
        public IDictionary<string, IDictionary<string, string>> RulesParameters { get; set; }
                = new Dictionary<string, IDictionary<string, string>>();

        public IDictionary<string, RuleMetadata> RulesMetadata { get; set; }
                = new Dictionary<string, RuleMetadata>();

        #endregion
    }
}
