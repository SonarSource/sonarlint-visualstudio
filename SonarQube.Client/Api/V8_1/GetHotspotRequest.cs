/*
 * SonarQube Client
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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V8_1
{
    public class GetHotspotRequest : RequestBase<SonarQubeHotspot>, IGetHotspotRequest
    {
        //TODO - this is an internal API - change to use a public API when available

        protected override string Path => "api/hotspots/show";

        [JsonProperty("hotspot")]
        public string HotspotKey { get; set; }

        protected override SonarQubeHotspot ParseResponse(string response)
        {
            var serverResponse = JObject.Parse(response).ToObject<GetHotspotResponse>();
            var hotspot = new SonarQubeHotspot(
                serverResponse.Key,
                serverResponse.Message,
                serverResponse.Assignee,
                serverResponse.Status,
                serverResponse.Project.Organization,
                serverResponse.Project.Key,
                serverResponse.Project.Name,
                serverResponse.Component.Key,
                FilePathNormalizer.NormalizeSonarQubePath(serverResponse.Component.Path),
                serverResponse.Rule.Key,
                serverResponse.Rule.Name,
                serverResponse.Rule.SecurityCategory,
                serverResponse.Rule.VulnerabilityProbability,
                ToIssueTextRange(serverResponse.TextRange)
            );

            return hotspot;
        }

        private static IssueTextRange ToIssueTextRange(ServerTextRange serverIssueTextRange) =>
            new IssueTextRange(serverIssueTextRange.StartLine, serverIssueTextRange.EndLine, serverIssueTextRange.StartOffset, serverIssueTextRange.EndOffset);

        private class GetHotspotResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("line")]
            public int Line { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("assignee")]
            public string Assignee { get; set; }

            [JsonProperty("component")]
            public ServerComponent Component { get; set; }

            [JsonProperty("project")]
            public ServerProject Project { get; set; }

            [JsonProperty("rule")]
            public ServerRule Rule { get; set; }

            [JsonProperty("textRange")]
            public ServerTextRange TextRange { get; set; }
        }

        private class ServerComponent
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }
        }

        private class ServerProject
        {
            [JsonProperty("organization")]
            public string Organization { get; set; }

            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        private class ServerRule
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("securityCategory")]
            public string SecurityCategory { get; set; }

            [JsonProperty("vulnerabilityProbability")]
            public string VulnerabilityProbability { get; set; }
        }

        private class ServerTextRange
        {
            [JsonProperty("startLine")]
            public int StartLine { get; set; }
            [JsonProperty("endLine")]
            public int EndLine { get; set; }
            [JsonProperty("startOffset")]
            public int StartOffset { get; set; }
            [JsonProperty("endOffset")]
            public int EndOffset { get; set; }
        }
    }
}
