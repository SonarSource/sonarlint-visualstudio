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
    internal sealed class RulesMetadataCache
    {
        private static readonly Lazy<RulesMetadataCache> lazy = new Lazy<RulesMetadataCache>(() => new RulesMetadataCache());

        public static RulesMetadataCache Instance { get { return lazy.Value; } }

        public List<string> AllRulesList { get; }
        public List<string> ActiveRulesList { get; }

        public Dictionary<string, Dictionary<string, string>> RulesParameters { get; }

        public Dictionary<string, RuleMetadata> RulesMetadata { get; }

        private RulesMetadataCache()
        {
            AllRulesList = RulesLoader.ReadRulesList();
            ActiveRulesList = RulesLoader.ReadActiveRulesList();
            RulesParameters = AllRulesList
                .ToDictionary(key => key, key => RulesLoader.ReadRuleParams(key));
            RulesMetadata = AllRulesList
                .ToDictionary(key => key, key => RulesLoader.ReadRuleMetadata(key));
        }
    }
}
