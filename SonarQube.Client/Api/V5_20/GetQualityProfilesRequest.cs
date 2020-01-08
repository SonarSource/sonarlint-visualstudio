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

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V5_20
{
    public class GetQualityProfilesRequest : RequestBase<SonarQubeQualityProfile[]>, IGetQualityProfilesRequest
    {
        [JsonProperty("projectKey")]
        public virtual string ProjectKey { get; set; }

        [JsonProperty("organization")]
        public virtual string OrganizationKey { get; set; }

        [JsonProperty("defaults")]
        public virtual bool? Defaults => string.IsNullOrWhiteSpace(ProjectKey) ? (bool?)true : null;

        protected override string Path => "api/qualityprofiles/search";

        public override async Task<SonarQubeQualityProfile[]> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            var result = await InvokeUncheckedAsync(httpClient, token);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                Logger.Info("This project has no quality profile. Downloading the default quality profile.");

                // The project has not been scanned yet, get default quality profile
                ProjectKey = null;

                result = await InvokeUncheckedAsync(httpClient, token);
            }

            result.EnsureSuccess();

            return result.Value;
        }

        protected override SonarQubeQualityProfile[] ParseResponse(string response) =>
            JObject.Parse(response)["profiles"]
                .ToObject<QualityProfileResponse[]>()
                .Select(FromResponse)
                .ToArray();

        private static SonarQubeQualityProfile FromResponse(QualityProfileResponse response) =>
            new SonarQubeQualityProfile(
                response.Key, response.Name, response.Language, response.IsDefault, response.LastRuleChange);

        private class QualityProfileResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("language")]
            public string Language { get; set; }

            [JsonProperty("isDefault")]
            public bool IsDefault { get; set; }

            [JsonProperty("rulesUpdatedAt")]
            public DateTime LastRuleChange { get; set; }
        }
    }
}
