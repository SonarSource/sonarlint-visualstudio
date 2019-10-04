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

using System.Collections.Generic;
using System.Linq;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    internal class DummyRulesConfiguration : IRulesConfiguration
    {
        public static readonly IRulesConfiguration ValidRulesConfig = new DummyRulesConfiguration
            {   
                RuleKeyToActiveMap = new Dictionary<string, bool>
                {
                    { "rule1", true },
                    { "rule2", false }
                }
        };

        public IDictionary<string, bool> RuleKeyToActiveMap { get; set; } = new Dictionary<string, bool>();

        #region IRulesConfiguration interface

        public IEnumerable<string> AllRuleKeys => RuleKeyToActiveMap.Keys;

        public IEnumerable<string> ActiveRuleKeys => RuleKeyToActiveMap.Where(kvp => kvp.Value)
                                                                        .Select(kvp => kvp.Key)
                                                                        .ToList();
        public IDictionary<string, IDictionary<string, string>> RulesParameters { get; set; }
                = new Dictionary<string, IDictionary<string, string>>();

        public IDictionary<string, RulesLoader.RuleMetadata> RulesMetadata { get; set; }
                = new Dictionary<string, RulesLoader.RuleMetadata>();

        #endregion
    }
}
