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
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SonarLint.VisualStudio.Roslyn.Suppressions
{
    /// <summary>
    /// Data class - contains all information that needs to be passed between the main VS
    /// process and the external Roslyn analysis process
    /// </summary>
    internal class RoslynSettings
    {
        public static readonly RoslynSettings Empty = new RoslynSettings { Suppressions = Enumerable.Empty<SuppressedIssue>() };

        /// <summary>
        /// The actual Sonar project key to which the settings relate
        /// </summary>
        /// <remarks>Note: this is not necessarily the same as the "settings key" - the settings key is a modified version of the 
        /// project key without any invalid file characters.</remarks>
        [JsonProperty("sonarProjectKey")]
        public string SonarProjectKey { get; set; }
        
        [JsonProperty("suppressions")]
        public IEnumerable<SuppressedIssue> Suppressions { get; set; }
    }

    // Used as a parameter in data-driven tests, so needs to be public
    public enum RoslynLanguage
    {
        Unknown = 0,
        CSharp = 1,
        VB = 2
    }

    /// <summary>
    /// Describes a single C#/VB.NET issue that has been suppressed on the server
    /// </summary>
    /// <remarks>This class contains the subset of fields from <see cref="SonarQube.Client.Models.SonarQubeIssue"/>
    /// needed to match suppressed issues against "live" Roslyn issues</remarks>
    internal class SuppressedIssue
    {
        /// <summary>
        /// Relative file path
        /// </summary>
        /// <remarks>
        /// The path is relative to the Sonar project root.
        /// The path is in Windows format i.e. the directory separators are backslashes
        /// </remarks>
        [JsonProperty("file")]
        public string FilePath { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("lang")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RoslynLanguage RoslynLanguage { get; set; }

        /// <summary>
        /// The rule ID reported by the Roslyn analyzer e.g. S123
        /// </summary>
        /// <remarks>This Sonar rule key without repository key.</remarks>
        [JsonProperty("rule")]
        public string RoslynRuleId { get; set; }

        /// <summary>
        /// The 0-based line for the issue. Will be null for file-level issues
        /// </summary>
        /// <remarks>Roslyn issues are 0-based - Sonar issues are 1-based.</remarks>
        [JsonProperty("line")]
        public int? RoslynIssueLine { get; set; }
    }
}
