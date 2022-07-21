/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.Core;

namespace CFamilyJarPreProcessor.FileGenerator
{
    /// <summary>
    /// Converts rule metadata from the ICFamilyRulesConfig format to the common
    /// RuleConfig used in SLVS
    /// </summary>
    internal class RuleConfigGenerator
    {
        private readonly ILogger logger;
        public RuleConfigGenerator(ILogger logger)
        {
            this.logger = logger;
        }

        public RuleConfig CreateRuleConfig(string ruleKey, ICFamilyRulesConfig config)
        {
            logger.LogMessage($"  Processing rule: {ruleKey}");
            var level = config.ActivePartialRuleKeys.Contains(ruleKey) ? RuleLevel.On : RuleLevel.Off;
            
            if (!config.RulesMetadata.TryGetValue(ruleKey, out var inputRuleMetadata))
            {
                throw new InvalidOperationException($"Invalid input data: could not find rule metadata for {ruleKey}");
            }

            var parameters = GetParameters(ruleKey, config);
            return new RuleConfig { Level = level, Severity = inputRuleMetadata.DefaultSeverity, Parameters = parameters};
        }

        private Dictionary<string, string> GetParameters(string ruleKey, ICFamilyRulesConfig config)
        {
            if (!config.RulesParameters.TryGetValue(ruleKey, out var inputParameters))
            {
                return null;
            }

            var parameters = new Dictionary<string, string>(inputParameters);
            return parameters;
        }
    }
}
