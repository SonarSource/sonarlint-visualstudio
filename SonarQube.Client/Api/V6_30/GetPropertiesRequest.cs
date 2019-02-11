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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V6_30
{
    public class GetPropertiesRequest : RequestBase<SonarQubeProperty[]>, IGetPropertiesRequest
    {
        [JsonProperty("component")]
        public string ProjectKey { get; set; }

        protected override string Path => "api/settings/values";

        protected override SonarQubeProperty[] ParseResponse(string response) =>
            JObject.Parse(response)["settings"]
                .ToObject<PropertyResponse[]>()
                .SelectMany(ToProperties)
                .ToArray();

        public override async Task<SonarQubeProperty[]> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            var result = await InvokeUncheckedAsync(httpClient, token);

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                Logger.Info($"Project with key '{ProjectKey}' does not exist. Downloading the default properties.");

                ProjectKey = null;

                result = await InvokeUncheckedAsync(httpClient, token);
            }

            result.EnsureSuccess();

            return result.Value;
        }

        private IEnumerable<SonarQubeProperty> ToProperties(PropertyResponse arg)
        {
            if (arg.FieldValues != null)
            {
                for (int i = 0; i < arg.FieldValues.Length; i++)
                {
                    var fieldValue = arg.FieldValues[i];
                    foreach (var item in fieldValue)
                    {
                        yield return new SonarQubeProperty($"{arg.Key}.{i + 1}.{item.Key}", item.Value);
                    }
                }
            }
            else if (arg.Values != null)
            {
                yield return new SonarQubeProperty(arg.Key, string.Join(",", arg.Values));
            }
            else
            {
                yield return new SonarQubeProperty(arg.Key, arg.Value);
            }
        }

        private class PropertyResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("values")]
            public string[] Values { get; set; }

            [JsonProperty("fieldValues")]
            public Dictionary<string, string>[] FieldValues { get; set; }
        }
    }
}
