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

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V8_6
{
    public class GetHotspotRequest : RequestBase<SonarQubeHotspot>, IGetHotspotRequest
    {
        protected override string Path => "api/hotspots/show";

        [JsonProperty("hotspot")]
        public string HotspotKey { get; set; }

        protected override SonarQubeHotspot ParseResponse(string response)
        {
            var serverResponse = JObject.Parse(response).ToObject<GetHotspotResponse>();

            var rule = new SonarQubeHotspotRule(
                serverResponse.Rule.Key,
                serverResponse.Rule.Name,
                serverResponse.Rule.SecurityCategory,
                serverResponse.Rule.VulnerabilityProbability,
                serverResponse.Rule.RiskDescription,
                serverResponse.Rule.VulnerabilityDescription,
                serverResponse.Rule.FixRecommendations
            );

            var hotspot = new SonarQubeHotspot(
                serverResponse.Key,
                serverResponse.Message,
                serverResponse.Hash,
                serverResponse.Assignee,
                serverResponse.Status,
                serverResponse.Project.Organization,
                serverResponse.Project.Key,
                serverResponse.Project.Name,
                serverResponse.Component.Key,
                FilePathNormalizer.NormalizeSonarQubePath(serverResponse.Component.Path),
                serverResponse.CreationDate,
                serverResponse.UpdateDate,
                rule,
                ToIssueTextRange(serverResponse.TextRange),
                serverResponse.Resolution
            );

            return hotspot;
        }

        private static IssueTextRange ToIssueTextRange(ServerTextRange serverIssueTextRange) =>
            new IssueTextRange(serverIssueTextRange.StartLine, serverIssueTextRange.EndLine, serverIssueTextRange.StartOffset, serverIssueTextRange.EndOffset);

        private sealed class GetHotspotResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("hash")]
            public string Hash { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("assignee")]
            public string Assignee { get; set; }

            [JsonProperty("component")]
            public ServerComponent Component { get; set; }

            [JsonProperty("project")]
            public ServerProject Project { get; set; }

            [JsonProperty("creationDate")]
            public DateTimeOffset CreationDate { get; set; }

            [JsonProperty("updateDate")]
            public DateTimeOffset UpdateDate { get; set; }

            [JsonProperty("rule")]
            public ServerRule Rule { get; set; }

            [JsonProperty("textRange")]
            public ServerTextRange TextRange { get; set; }

            [JsonProperty("resolution")]
            public string Resolution { get; set; }
        }

        private sealed class ServerComponent
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }
        }

        private sealed class ServerProject
        {
            [JsonProperty("organization")]
            public string Organization { get; set; }

            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        private sealed class ServerRule
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("securityCategory")]
            public string SecurityCategory { get; set; }

            [JsonProperty("vulnerabilityProbability")]
            public string VulnerabilityProbability { get; set; }

            [JsonProperty("riskDescription")]
            public string RiskDescription { get; set; }

            [JsonProperty("vulnerabilityDescription")]
            public string VulnerabilityDescription { get; set; }

            [JsonProperty("fixRecommendations")]
            public string FixRecommendations { get; set; }
        }

        private sealed class ServerTextRange
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
