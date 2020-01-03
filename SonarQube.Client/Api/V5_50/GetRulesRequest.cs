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

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        // Update this property if more fields are needed in the response. Have in mind
        // that the field names here do not always correspond to the actual field names
        // in the response! For example 'internalKey' in the request corresponds to 'key'
        // in the response. The server error message (400) returns all supported fields.
        [JsonProperty("f")]
        public string ResponseFields => "repo,internalKey,params,actives";

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
                parameters = activeQP.Parameters.ToDictionary(p => p.Key, p => p.Value);
            }
            else
            {
                severity = SonarQubeIssueSeverity.Unknown;
                parameters = new Dictionary<string, string>();
            }

            return new SonarQubeRule(GetRuleKey(response.Key), response.RepositoryKey, isActive, severity, parameters);
        }

        private static string GetRuleKey(string compositeKey) =>
            compositeKey.Substring(compositeKey.IndexOf(':') + 1);

        private class RuleResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("repo")]
            public string RepositoryKey { get; set; }
        }

        private class QualityProfileResponse
        {
            [JsonProperty("qProfile")]
            public string Key { get; set; }

            [JsonProperty("severity")]
            public string Severity { get; set; }

            [JsonProperty("params")]
            public ParameterResponse[] Parameters { get; set; }
        }

        private class ParameterResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }
    }
}
