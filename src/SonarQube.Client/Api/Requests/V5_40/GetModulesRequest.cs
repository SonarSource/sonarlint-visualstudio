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
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests.V5_40
{
    public class GetModulesRequest : PagedRequestBase<SonarQubeModule>, IGetModulesRequest
    {
        protected override string Path => "api/components/tree";

        [JsonProperty("qualifiers")]
        public string Qualifiers
        {
            get { return "BRC"; } // Sub-projects
            set { /* not supported in this implementation */ }
        }

        [JsonProperty("component")]
        public string ProjectKey { get; set; }

        protected override SonarQubeModule[] ParseResponse(string response)
        {
            var jsonParse = JObject.Parse(response);

            return new[] { jsonParse["baseComponent"].ToObject<ModuleResponse>() }
                .Concat(jsonParse["components"].ToObject<ModuleResponse[]>())
                .Select(ToSonarQubeModule)
                .ToArray();
        }

        private SonarQubeModule ToSonarQubeModule(ModuleResponse response) =>
            new SonarQubeModule(response.Key, response.Name, response.Path);

        private class ModuleResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }
        }
    }
}
