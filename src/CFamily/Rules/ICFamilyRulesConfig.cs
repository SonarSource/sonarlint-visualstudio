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

using System.Collections.Generic;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.CFamily.Rules
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

        [JsonProperty("code")]
        public Code Code { get; set; }
    }

    public class Code
    {
        [JsonProperty("impacts")]
        public Dictionary<SoftwareQuality, SoftwareQualitySeverity> Impacts { get; set; } = new Dictionary<SoftwareQuality, SoftwareQualitySeverity>();
    }

    public enum IssueType
    {
        CodeSmell = 0,
        Bug = 1,
        Vulnerability = 2,
        SecurityHotspot = 3,
    }
}
