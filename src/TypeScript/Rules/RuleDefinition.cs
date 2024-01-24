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

using Newtonsoft.Json;

// Data classes used to deserialize rule metadata from the json file embedded the SonarJS 

namespace SonarLint.VisualStudio.TypeScript.Rules
{
    internal class RuleDefinition
    {
        // Unused fields:
        // * Status: READY, DEPRECATED?
        // * htmlDescription

        [JsonProperty("ruleKey")]
        public string RuleKey { get; set; }

        [JsonProperty("type")]
        public RuleType Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("severity")]
        public RuleSeverity Severity { get; set; }

        [JsonProperty("tags")]
        public string[] Tags { get; set; }

        // TODO: #2284: Support user-configurable rules in JS/TS
        [JsonProperty("params")]
        public object[] Params { get; set; }

        [JsonProperty("defaultParams")]
        public object[] DefaultParams { get; set; }

        [JsonProperty("scope")]
        public RuleScope Scope { get; set; }

        [JsonProperty("eslintKey")]
        public string EslintKey { get; set; }

        [JsonProperty("stylelintKey")]
        public string StylelintKey { get; set; }

        [JsonProperty("activatedByDefault")]
        public bool ActivatedByDefault { get; set; }
    }

    internal enum RuleType
    {
        CODE_SMELL,
        BUG,
        SECURITY_HOTSPOT,
        VULNERABILITY
    }

    internal enum RuleSeverity
    {
        BLOCKER,
        CRITICAL,
        MAJOR,
        MINOR,
        INFO
    }

    internal enum RuleScope
    {
        MAIN,
        TEST,
        ALL
    }
}
