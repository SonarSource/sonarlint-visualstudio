/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.CFamily.Rules;

namespace SonarLint.VisualStudio.CFamily.CompilationDatabase
{
    public interface IRulesConfigProtocolFormatter
    {
        RuleConfigProtocolFormat Format(ICFamilyRulesConfig rulesConfig);
    }

    public class RuleConfigProtocolFormat
    {
        /// <summary>
        /// Comma-separated list of active rule ids
        /// </summary>
        public string QualityProfile { get; }

        /// <summary>
        /// The key for each individual setting is in the form {ruleId}.{configname}
        /// </summary>
        public Dictionary<string, string> RuleParameters { get; }

        public RuleConfigProtocolFormat(string qualityProfile, Dictionary<string, string> ruleParameters)
        {
            QualityProfile = qualityProfile;
            RuleParameters = ruleParameters;
        }
    }

    public class RulesConfigProtocolFormatter : IRulesConfigProtocolFormatter
    {
        public RuleConfigProtocolFormat Format(ICFamilyRulesConfig rulesConfig)
        {
            if (rulesConfig == null)
            {
                throw new ArgumentNullException(nameof(rulesConfig));
            }

            var qualityProfile = string.Join(",", rulesConfig.ActivePartialRuleKeys);
            var ruleParameters = GetRuleParameters(rulesConfig);

            return new RuleConfigProtocolFormat(qualityProfile, ruleParameters);
        }

        private static Dictionary<string, string> GetRuleParameters(ICFamilyRulesConfig rulesConfiguration)
        {
            var ruleParameters = new Dictionary<string, string>();

            foreach (var ruleKey in rulesConfiguration.ActivePartialRuleKeys)
            {
                if (rulesConfiguration.RulesParameters.TryGetValue(ruleKey, out var ruleParams))
                {
                    foreach (var param in ruleParams)
                    {
                        var optionKey = ruleKey + "." + param.Key;
                        ruleParameters.Add(optionKey, param.Value);
                    }
                }
            }

            return ruleParameters;
        }
    }
}
