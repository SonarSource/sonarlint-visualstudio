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

namespace SonarQube.Client.Api.V7_20
{
    public class GetIssuesRequest : PagedRequestBase<SonarQubeIssue>, IGetIssuesRequest
    {
        [JsonProperty("projects")]
        public virtual string ProjectKey { get; set; }

        protected override string Path => "api/issues/search";

        protected override SonarQubeIssue[] ParseResponse(string response) =>
            JObject.Parse(response)["issues"]
                .ToObject<ServerIssue[]>()
                .Select(ToSonarQubeIssue)
                .ToArray();

        private static SonarQubeIssue ToSonarQubeIssue(ServerIssue issue) =>
            new SonarQubeIssue(
                filePath: ComputePath(issue),
                hash: issue.Hash,
                line: issue.Line,
                message: issue.Message,
                moduleKey: issue.SubProject,
                resolutionState: ParseResolutionState(issue.Resolution),
                ruleId: GetRuleKey(issue.CompositeRuleKey));

        private static string ComputePath(ServerIssue issue) =>
            // Component is "{SubProject}:Path"
            issue.Component.Substring(issue.SubProject.Length + 1);

        private static string GetRuleKey(string compositeRuleKey) =>
            // ruleKey is "csharpsqid:S1234" or "vbnet:S1234" but we need S1234
            compositeRuleKey.Replace("vbnet:", string.Empty).Replace("csharpsquid:", string.Empty);

        private static SonarQubeIssueResolutionState ParseResolutionState(string resolution)
        {
            switch (resolution)
            {
                // Issues with Status=OPEN will not have a resolution so we're
                // expecting null, but we'll be defensive and handle empty too
                case null:
                case "": 
                    return SonarQubeIssueResolutionState.Unresolved;
                case "WONTFIX":
                    return SonarQubeIssueResolutionState.WontFix;
                case "FALSE-POSITIVE":
                    return SonarQubeIssueResolutionState.FalsePositive;
                case "FIXED":
                    return SonarQubeIssueResolutionState.Fixed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution));
            }
        }

        private class ServerIssue
        {
            [JsonProperty("key")]
            public string Key { get; set; }
            [JsonProperty("component")]
            public string Component { get; set; }
            [JsonProperty("hash")]
            public string Hash { get; set; }
            [JsonProperty("line")]
            public int Line { get; set; }
            [JsonProperty("message")]
            public string Message { get; set; }
            [JsonProperty("subProject")]
            public string SubProject { get; set; }
            [JsonProperty("resolution")]
            public string Resolution { get; set; }
            [JsonProperty("rule")]
            public string CompositeRuleKey { get; set; }
        }
    }
}
