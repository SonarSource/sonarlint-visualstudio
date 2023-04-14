/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract
{
    internal class Issue
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("column")]
        public int Column { get; set; }

        [JsonProperty("endLine")]
        public int EndLine { get; set; }

        [JsonProperty("endColumn")]
        public int EndColumn { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("ruleId")]
        public string RuleId { get; set; }

        [JsonProperty("secondaryLocations")]
        public IssueLocation[] SecondaryLocations { get; set; }

        [JsonProperty("cost")]
        public int? Cost { get; set; }

        [JsonProperty("quickFixes")]
        public QuickFix[] QuickFixes { get; set; }
    }

    internal class IssueLocation
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("column")]
        public int Column { get; set; }

        [JsonProperty("endLine")]
        public int EndLine { get; set; }

        [JsonProperty("endColumn")]
        public int EndColumn { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    internal class QuickFix
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("edits")]
        public Edit[] Edits { get; set; }
    }

    internal class Edit
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("loc")]
        public TextRange TextRange { get; set; }
    }

    internal class TextRange
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("column")]
        public int Column { get; set; }

        [JsonProperty("endLine")]
        public int EndLine { get; set; }

        [JsonProperty("endColumn")]
        public int EndColumn { get; set; }
    }
}
