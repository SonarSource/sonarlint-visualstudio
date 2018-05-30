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

using System.Linq;
using Newtonsoft.Json;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests.V2_60
{
    public class GetPropertiesRequest : RequestBase<SonarQubeProperty[]>, IGetPropertiesRequest
    {
        protected override string Path => "api/properties";

        protected override SonarQubeProperty[] ParseResponse(string response) =>
            JsonHelper.Deserialize<PropertyResponse[]>(response)
                .Select(ToProperty)
                .ToArray();

        private SonarQubeProperty ToProperty(PropertyResponse arg) =>
            new SonarQubeProperty(arg.Key, arg.Value);

        private class PropertyResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }
    }
}
