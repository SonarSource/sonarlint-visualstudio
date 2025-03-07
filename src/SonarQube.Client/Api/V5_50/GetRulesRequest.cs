﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarQube.Client.Api.Common;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V5_50
{
    public class GetRulesRequest : PagedRequestBase<SonarQubeRule>, IGetRulesRequest
    {
        protected override string Path => "api/rules/search";

        [JsonProperty("activation")]
        public bool? IsActive { get; set; }

        [JsonProperty("qprofile")]
        public string QualityProfileKey { get; set; }

        [JsonProperty("rule_key")]
        public string RuleKey { get; set; }

        // Update this property if more fields are needed in the response. Have in mind
        // that the field names here do not always correspond to the actual field names
        // in the response! For example 'internalKey' in the request corresponds to 'key'
        // in the response. The server error message (400) returns all supported fields.
        // Also Make sure the field is supported in this version of the API.
        // If not add a new request for API version that supports. e.g. "GetRulesWithDescriptionSectionsRequest"

        internal static readonly IList<string> ResponseList = new List<string> { "repo", "internalKey", "params", "actives" };

        [JsonIgnore]
        internal IList<string> ResponseListField { get; set; } = ResponseList;

        [JsonProperty("f")]
        public string ResponseFields => string.Join(",", ResponseListField);

        protected override SonarQubeRule[] ParseResponse(string response)
        {
            var responseJson = JObject.Parse(response);

            var activeQualityProfiles = ((JObject)responseJson["actives"]).Properties()
                // Flatten the RuleKey-QualityProfile[] dictionary to make the lookup creation easier
                .SelectMany(
                    p => p.Value
                        .ToObject<QualityProfileResponse[]>()
                        .Select(q => new { Key = p.Name, QualityProfile = q }))
                .ToLookup(
                    x => x.Key,
                    x => x.QualityProfile);

            return responseJson["rules"]
                .ToObject<RuleResponse[]>()
                .Select(rule => ToSonarQubeRule(rule, activeQualityProfiles[rule.Key]))
                .ToArray();
        }

        private SonarQubeRule ToSonarQubeRule(RuleResponse response,
            IEnumerable<QualityProfileResponse> activeQualityProfiles)
        {
            var isActive = activeQualityProfiles.Any();

            SonarQubeIssueSeverity severity;
            Dictionary<string, string> parameters;
            if (isActive)
            {
                var activeQP = activeQualityProfiles.First();
                severity = SonarQubeIssueSeverityConverter.Convert(activeQP.Severity);

                // Optimisation: avoid creating objects if there are no parameters
                parameters = activeQP.Parameters.Length > 0 ?
                    activeQP.Parameters.ToDictionary(p => p.Key, p => p.Value) : null;
            }
            else
            {
                severity = SonarQubeIssueSeverity.Unknown;
                parameters = null;
            }

            var issueType = SonarQubeIssueTypeConverter.Convert(response.Type);

            return new SonarQubeRule(GetRuleKey(response.Key),
                response.RepositoryKey,
                isActive,
                severity,
                CleanCodeTaxonomyHelpers.ToSonarQubeCleanCodeAttribute(response.CleanCodeAttribute),
                CleanCodeTaxonomyHelpers.ToDefaultImpacts(response.Impacts),
                parameters,
                issueType);
        }

        private static string GetRuleKey(string compositeKey) =>
            compositeKey.Substring(compositeKey.IndexOf(':') + 1);

        private sealed class RuleResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("repo")]
            public string RepositoryKey { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("cleanCodeAttribute")]
            public string CleanCodeAttribute { get; set; }

            [JsonProperty("impacts")]
            public ServerImpact[] Impacts { get; set; }
        }

        private sealed class QualityProfileResponse
        {
            [JsonProperty("qProfile")]
            public string Key { get; set; }

            [JsonProperty("severity")]
            public string Severity { get; set; }

            [JsonProperty("params")]
            public ParameterResponse[] Parameters { get; set; }
        }

        private sealed class ParameterResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }
    }
}
