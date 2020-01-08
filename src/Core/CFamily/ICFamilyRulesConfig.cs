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
using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Core.CFamily
{
    public interface ICFamilyRulesConfig
    {
        string LanguageKey { get; }

        IEnumerable<string> AllPartialRuleKeys { get; }

        IEnumerable<string> ActivePartialRuleKeys { get; }

        IDictionary<string, IDictionary<string, string>> RulesParameters { get; }

        IDictionary<string, RuleMetadata> RulesMetadata { get; }
    }

    public class RuleMetadata
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("type")]
        public IssueType Type { get; set; }

        [JsonProperty("defaultSeverity")]
        public IssueSeverity DefaultSeverity { get; set; }

        [JsonProperty("compatibleLanguages")]
        public string[] CompatibleLanguages { get; set; }
    }

    public enum IssueType
    {
        CodeSmell = 0,
        Bug = 1,
        Vulnerability = 2,
    }
}
