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
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V6_60
{
    public class GetProjectBranchesRequest : RequestBase<SonarQubeProjectBranch[]>, IGetProjectBranchesRequest
    {
        [JsonProperty("project")]
        public virtual string ProjectKey { get; set; }

        protected override string Path => "api/project_branches/list";

        protected override SonarQubeProjectBranch[] ParseResponse(string response) =>
            JObject.Parse(response)["branches"]
                .ToObject<ServerProjectBranch[]>()
                .Select(ToProjectBranch)
                .ToArray();

        private SonarQubeProjectBranch ToProjectBranch(ServerProjectBranch serverBranch) =>
            new SonarQubeProjectBranch(serverBranch.Name, serverBranch.IsMain, serverBranch.AnalysisDate, serverBranch.Type);

        private sealed class ServerProjectBranch
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("isMain")]
            public bool IsMain { get; set; }

            [JsonProperty("analysisDate")]
            public DateTimeOffset AnalysisDate { get; set; }
        }
    }
}
