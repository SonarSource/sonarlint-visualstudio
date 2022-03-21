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

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions
{
    /// <summary>
    /// Data class - contains all information that needs to be passed between the main VS
    /// process and the external Roslyn analysis process
    /// </summary>
    public class RoslynSettings
    {
        public static readonly RoslynSettings Empty = new RoslynSettings { Suppressions = Enumerable.Empty<SonarQubeIssue>() };

        /// <summary>
        /// The actual Sonar project key to which the settings relate
        /// </summary>
        /// <remarks>Note: this is not necessarily the same as the "settings key" - the settings key is a modified version of the 
        /// project key without any invalid file characters.</remarks>
        [JsonProperty("sonarProjectKey")]
        public string SonarProjectKey { get; set; }
        
        [JsonProperty("suppressions")]
        public IEnumerable<SonarQubeIssue> Suppressions { get; set; }
    }

    ///// <summary>
    ///// Describes a single C#/VB.NET issue that has been suppressed on the server
    ///// </summary>
    ///// <remarks>This class contains the subset of fields from <see cref="SonarQube.Client.Models.SonarQubeIssue"/>
    ///// needed to match suppressed issues against "live" Roslyn issues</remarks>
    //public class SuppressedIssue
    //{
    //    public SuppressedIssue(string filePath, string hash, string roslynLanguage, string roslynRuleId, IssueTextRange textRange)
    //    {
    //        FilePath = filePath;
    //        Hash = hash;
    //        RoslynLanguage = roslynLanguage;
    //        RoslynRuleId = roslynRuleId;
    //        TextRange = textRange;
    //    }

    //    /// <summary>
    //    /// Relative file path
    //    /// </summary>
    //    /// <remarks>
    //    /// The path is relative to the Sonar project root.
    //    /// The path is in Windows format i.e. the directory separators are backslashes
    //    /// </remarks>
    //    [JsonProperty("file")]
    //    public string FilePath { get; }
        
    //    [JsonProperty("hash")]
    //    public string Hash { get; }
        
    //    [JsonProperty("lang")] // TODO - Enum / Roslyn string?
    //    public string RoslynLanguage { get; }
        
    //    [JsonProperty("rule")]
    //    public string RoslynRuleId { get; }
        
    //    [JsonProperty("range")]  // TODO - Roslyn type?
    //    public IssueTextRange TextRange { get; }
    //}
}
