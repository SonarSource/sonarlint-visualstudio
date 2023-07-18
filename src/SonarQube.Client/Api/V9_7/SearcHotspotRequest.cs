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

using System;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Api.Common;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V9_7
{
    public class SearchHotspotRequest : PagedRequestBase<SonarQubeHotspotSearch>, ISearchHotspotRequest
    {
        [JsonProperty("projectKey")]
        public string ProjectKey { get; set; }

        // Notes:
        // 1) Branch support is not available in SQ Community edition. SQ will just ignore it.
        // 2) SonarQube has supported the parameter since v6.6. However, the LTS at the point
        // we added added branch-awareness to SLVS was v8.9.10. To minimise the amount of
        // work on the SLVS side, we'll add branch support from SQ v7.2.
        [JsonProperty("branch", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue("")]
        public string BranchKey { get; set; }

        protected override string Path => "api/hotspots/search";

        /// <summary>
        /// Lookup component key -> path for files. Each response contains normalized data, containing
        /// issues and components, where each issue's "component" property points to a component with
        /// the same "key". We obtain the FilePath of each issue from its corresponding component.
        /// </summary>
        private ILookup<string, string> componentKeyPathLookup;

        protected override SonarQubeHotspotSearch[] ParseResponse(string response)
        {
            var root = JObject.Parse(response);

            // This is a paged request so ParseResponse will be called once for each "page"
            // of the response. However, we expect each page to be self-contained, so we want
            // to rebuild the lookup each time.
            componentKeyPathLookup = root.GetComponentKeyPathLookup();

            return root["hotspots"]
                .ToObject<ServerHotspotSearch[]>()
                .Select(ToSonarQubeHotspotSearch)
                .ToArray();
        }

        private SonarQubeHotspotSearch ToSonarQubeHotspotSearch(ServerHotspotSearch serverHotspotSearch)
        {
            return new SonarQubeHotspotSearch(serverHotspotSearch.HotspotKey,
                serverHotspotSearch.ComponentKey,
                FilePathNormalizer.NormalizeSonarQubePath(componentKeyPathLookup[serverHotspotSearch.ComponentKey].FirstOrDefault()),
                serverHotspotSearch.ProjectKey,
                serverHotspotSearch.Status,
                serverHotspotSearch.Resolution,
                serverHotspotSearch.TextRange.ToIssueTextRange(),
                serverHotspotSearch.RuleKey,
                serverHotspotSearch.Message,
                serverHotspotSearch.VulnerabilityProbability,
                serverHotspotSearch.CreationDate,
                serverHotspotSearch.UpdateDate);
        }

        private sealed class ServerHotspotSearch
        {
            [JsonProperty("key")]
            public string HotspotKey { get; set; }

            [JsonProperty("component")]
            public string ComponentKey { get; set; }

            [JsonProperty("project")]
            public string ProjectKey { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("resolution")]
            public string Resolution { get; set; }

            [JsonProperty("textRange")]
            public ServerIssueTextRange TextRange { get; set; }

            [JsonProperty("ruleKey")]
            public string RuleKey { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("vulnerabilityProbability")]
            public string VulnerabilityProbability { get; set; }

            [JsonProperty("creationDate")]
            public DateTimeOffset CreationDate { get; set; }

            [JsonProperty("updateDate")]
            public DateTimeOffset UpdateDate { get; set; }
        }
    }
}
