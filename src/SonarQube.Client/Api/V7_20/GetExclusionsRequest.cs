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

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V7_20
{
    public class GetExclusionsRequest : RequestBase<ServerExclusions>, IGetExclusionsRequest
    {
        internal const string ExclusionsKey = "sonar.exclusions";
        internal const string GlobalExclusionsKey = "sonar.global.exclusions";
        internal const string InclusionsKey = "sonar.inclusions";

        [JsonProperty("component")]
        public virtual string ProjectKey { get; set; }

        [JsonProperty("keys")]
        public virtual string Keys { get; } =
            string.Join(",", ExclusionsKey, GlobalExclusionsKey, InclusionsKey);

        protected override string Path => "api/settings/values";

        protected override ServerExclusions ParseResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return new ServerExclusions();
            }

            var jsonParse = JObject.Parse(response);

            var settings = jsonParse["settings"]?.ToObject<Setting[]>();

            if (settings?.Any() != true)
            {
                return new ServerExclusions();
            }

            var exclusions = GetValues(settings, ExclusionsKey);
            var globalExclusions = GetValues(settings, GlobalExclusionsKey);
            var inclusions = GetValues(settings, InclusionsKey);

            return new ServerExclusions(
                exclusions: exclusions,
                globalExclusions: globalExclusions,
                inclusions: inclusions);
        }

        private static string[] GetValues(IEnumerable<Setting> settings, string key)
        {
            return settings.SingleOrDefault(x => x.Key == key)?.Values;
        }

        private sealed class Setting   
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("values")]
            public string[] Values { get; set; }
        }
    }
}
