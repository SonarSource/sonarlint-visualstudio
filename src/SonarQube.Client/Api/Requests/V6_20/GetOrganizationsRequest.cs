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

using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;

namespace SonarQube.Client.Api.Requests.V6_20
{
    public class GetOrganizationsRequest : PagedRequestBase<SonarQubeOrganization>, IGetOrganizationsRequest
    {
        private const bool OnlyUserOrganizationsDefault = false;

        /// <summary>
        /// This property does not exist before SonarQube 7.0 and is deliberately implemented to always return
        /// the default value in order to prevent it from serialization.
        /// </summary>
        [JsonProperty("member", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(OnlyUserOrganizationsDefault)]
        public virtual bool OnlyUserOrganizations
        {
            get { return OnlyUserOrganizationsDefault; }
            set { /* not supported in this implementation */ }
        }

        protected override string Path => "api/organizations/search";

        protected override SonarQubeOrganization[] ParseResponse(string response) =>
            JObject.Parse(response)["organizations"]
                .ToObject<OrganizationResponse[]>()
                .Select(ToOrganization)
                .ToArray();

        private SonarQubeOrganization ToOrganization(OrganizationResponse response) =>
            new SonarQubeOrganization(response.Key, response.Name);

        private class OrganizationResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }
    }
}
