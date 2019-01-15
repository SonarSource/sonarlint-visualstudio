/*
 * SonarQube Client
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
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests.V7_20
{
    public class GetIssuesRequest : PagedRequestBase<SonarQubeIssue>, IGetIssuesRequest
    {
        [JsonProperty("projects")]
        public virtual string ProjectKey { get; set; }

        [JsonProperty("statuses")]
        public string Statuses { get; set; }

        // This property is not present in the IGetIssuesRequest interface, it is meant to be
        // set by the GetIssuesRequestWrapper to add additional parameters to the API calls.
        [JsonProperty("types")]
        public string Types { get; set; }

        protected override string Path => "api/issues/search";

        protected override SonarQubeIssue[] ParseResponse(string response)
        {
            var root = JObject.Parse(response);

            // Lookup component key -> path for files. Each response contains normalized data, containing
            // issues and components, where each issue's "component" property points to a component with
            // the same "key". We obtain the FilePath of each issue from its corresponding component.
            var componentKeyPathLookup = GetComponentKeyPathLookup(root);

            return root["issues"]
                .ToObject<ServerIssue[]>()
                .Select(issue => ToSonarQubeIssue(issue, componentKeyPathLookup))
                .ToArray();
        }

        private static ILookup<string, string> GetComponentKeyPathLookup(JObject root)
        {
            var components = root["components"] == null
                ? Array.Empty<ServerComponent>()
                : root["components"].ToObject<ServerComponent[]>();

            return components
                .Where(c => c.IsFile)
                .ToLookup(c => c.Key, c => c.Path); // Using a Lookup because it does not throw, unlike the Dictionary
        }

        private static SonarQubeIssue ToSonarQubeIssue(ServerIssue issue, ILookup<string, string> componentKeyPathLookup) =>
            new SonarQubeIssue(ComputePath(issue, componentKeyPathLookup), issue.Hash, issue.Line, issue.Message, ComputeModuleKey(issue),
                GetRuleKey(issue.CompositeRuleKey), issue.Status == "RESOLVED");

        private static string ComputePath(ServerIssue issue, ILookup<string, string> componentKeyPathLookup) =>
            componentKeyPathLookup[issue.Component].FirstOrDefault() ?? string.Empty;

        private static string ComputeModuleKey(ServerIssue issue) =>
            issue.SubProject ?? issue.Component;

        private static string GetRuleKey(string compositeRuleKey) =>
            // ruleKey is "csharpsqid:S1234" or "vbnet:S1234" but we need S1234
            compositeRuleKey.Replace("vbnet:", string.Empty).Replace("csharpsquid:", string.Empty);

        private class ServerIssue
        {
            [JsonProperty("rule")]
            public string CompositeRuleKey { get; set; }
            [JsonProperty("component")]
            public string Component { get; set; }
            [JsonProperty("subProject")]
            public string SubProject { get; set; }
            [JsonProperty("hash")]
            public string Hash { get; set; }
            [JsonProperty("line")]
            public int? Line { get; set; }
            [JsonProperty("message")]
            public string Message { get; set; }
            [JsonProperty("status")]
            public string Status { get; set; }
        }

        private class ServerComponent
        {
            [JsonProperty("key")]
            public string Key { get; set; }
            [JsonProperty("qualifier")]
            public string Qualifier { get; set; }
            [JsonProperty("path")]
            public string Path { get; set; }

            public bool IsFile
            {
                get { return Qualifier == "FIL"; }
            }
        }
    }
}
