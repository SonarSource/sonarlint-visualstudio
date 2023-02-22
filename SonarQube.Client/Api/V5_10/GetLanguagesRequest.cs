﻿/*
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
using Newtonsoft.Json.Linq;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V5_10
{
    public class GetLanguagesRequest : RequestBase<SonarQubeLanguage[]>, IGetLanguagesRequest
    {
        protected override string Path => "api/languages/list";

        protected override SonarQubeLanguage[] ParseResponse(string response) =>
            JObject.Parse(response)["languages"]
                .ToObject<LanguageResponse[]>()
                .Select(ToLanguage)
                .ToArray();

        private SonarQubeLanguage ToLanguage(LanguageResponse language) =>
            new SonarQubeLanguage(language.Key, language.Name);

        private class LanguageResponse
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }
    }
}
