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

using System.Linq;
using Newtonsoft.Json;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V2_10
{
    public class GetProjectsRequest : RequestBase<SonarQubeProject[]>, IGetProjectsRequest
    {
        [JsonIgnore]
        public virtual bool Ascending { get; set; }

        [JsonIgnore]
        public virtual string OrganizationKey { get; set; }

        [JsonIgnore]
        public virtual int Page { get; set; }

        [JsonIgnore]
        public virtual int PageSize { get; set; }

        [JsonIgnore]
        public virtual int ItemsLimit { get; set; }

        protected override string Path => "api/projects/index";

        protected override SonarQubeProject[] ParseResponse(string response) =>
            JsonHelper.Deserialize<ProjectResponse[]>(response)
                .Select(ToProject)
                .ToArray();

        private SonarQubeProject ToProject(ProjectResponse projectResponse) =>
            new SonarQubeProject(projectResponse.Key, projectResponse.Name);

        private class ProjectResponse
        {
            [JsonProperty("k")]
            public string Key { get; set; }

            [JsonProperty("nm")]
            public string Name { get; set; }
        }
    }
}
