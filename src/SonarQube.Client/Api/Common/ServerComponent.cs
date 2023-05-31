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

using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SonarQube.Client.Api.Common
{
    internal sealed class ServerComponent
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("qualifier")]
        public string Qualifier { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        public bool IsFile
        {
            get { return Qualifier == "FIL"; }
        }

        internal static ILookup<string, string> GetComponentKeyPathLookup(JObject root)
        {
            var components = root["components"] == null
                ? Array.Empty<ServerComponent>()
                : root["components"].ToObject<ServerComponent[]>();

            return components
                .Where(c => c.IsFile)
                .ToLookup(c => c.Key, c => c.Path); // Using a Lookup because it does not throw, unlike the Dictionary
        }
    }
}
