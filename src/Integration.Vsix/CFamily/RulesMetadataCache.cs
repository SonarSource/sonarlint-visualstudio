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
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.RulesLoader;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal interface IRulesConfiguration
    {
        IEnumerable<string> AllRuleKeys { get; }

        IEnumerable<string> ActiveRuleKeys { get; }

        IDictionary<string, IDictionary<string, string>> RulesParameters { get; }

        IDictionary<string, RuleMetadata> RulesMetadata { get; }
    }

    internal sealed class RulesMetadataCache : IRulesConfiguration
    {
        private static readonly Lazy<RulesMetadataCache> lazy = new Lazy<RulesMetadataCache>(() => new RulesMetadataCache());

        public static IRulesConfiguration Instance { get { return lazy.Value; } }

        public IEnumerable<string> AllRuleKeys { get; }
        public IEnumerable<string> ActiveRuleKeys { get; }

        public IDictionary<string, IDictionary<string, string>> RulesParameters { get; }

        public IDictionary<string, RuleMetadata> RulesMetadata { get; }

        private RulesMetadataCache()
        {
            AllRuleKeys = RulesLoader.ReadRulesList();
            ActiveRuleKeys = RulesLoader.ReadActiveRulesList();
            RulesParameters = AllRuleKeys
                .ToDictionary(key => key, key => RulesLoader.ReadRuleParams(key));
            RulesMetadata = AllRuleKeys
                .ToDictionary(key => key, key => RulesLoader.ReadRuleMetadata(key));
        }
    }
}
