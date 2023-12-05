/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarQube.Client.Api.Common;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V9_9
{
    internal class SearchFilesByNameRequest : PagedRequestBase<string>, ISearchFilesByNameRequest
    {
        protected override string Path => "api/components/tree";

        [JsonProperty("component")]
        public string ProjectKey { get; set; }

        [JsonProperty("branch")]
        public string BranchName { get; set; }

        [JsonProperty("q")]
        public string FileName { get; set; }

        //File and Test Files
        [JsonProperty("qualifiers")]
        public string Qualifiers => "FIL,UTS";

        protected override string[] ParseResponse(string response)
        {
            var root = JObject.Parse(response);

            return root.GetComponentPathList().ToArray();
        }
    }
}
