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
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V6_30
{
    public class GetPluginsRequest : RequestBase<SonarQubePlugin[]>, IGetPluginsRequest
    {
        protected override string Path => "api/plugins/installed";

        protected override SonarQubePlugin[] ParseResponse(string response) =>
            JObject.Parse(response)["plugins"]
                .ToObject<PluginResponse[]>()
                .Select(ToPlugin)
                .ToArray();

        private SonarQubePlugin ToPlugin(PluginResponse response) =>
            new SonarQubePlugin(response.Key, FormatForSonarLint(response.Version));

        /// <summary>
        /// Converts versions from the "api/plugins/installed" format to what SonarLint expects:
        /// "1 (Build 3456)" -> "1.0.0"
        /// "1.2" -> "1.2.0"
        /// "1.2.3 (Build 456)" -> "1.2.3"
        /// </summary>
        /// <param name="version">Version field formatted by SonarQube</param>
        /// <returns>Version field formatted for SonarLint</returns>
        public static string FormatForSonarLint(string version)
        {
            const string sonarQubeVersionPattern =
                @"(?<major>\d+)[\.]?(?<minor>\d+)?[\.]?(?<patch>\d+)?.*";

            var match = Regex.Match(version, sonarQubeVersionPattern);
            if (!match.Success)
            {
                return version;
            }
            return $"{GetPart(match, "major")}.{GetPart(match, "minor")}.{GetPart(match, "patch")}";

        }
        private static string GetPart(Match match, string name)
        {
            var group = match.Groups[name];
            return group.Success ? group.Value : "0";
        }

        private class PluginResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("version")]
            public string Version { get; set; }
        }
    }
}
