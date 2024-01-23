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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V9_4
{
    internal class GetSonarLintEventStream : RequestBase<Stream>, IGetSonarLintEventStream
    {
        private static readonly string AllKnownLanguages = string.Join(",", SonarQubeLanguage.AllLanguages.Select(x => x.Key));

        protected override string Path => "api/push/sonarlint_events";

        protected override MediaTypeWithQualityHeaderValue[] AllowedMediaTypeHeaders =>
            new[]
            {
                MediaTypeWithQualityHeaderValue.Parse("text/event-stream")
            };

        protected override async Task<Result<Stream>> ReadResponseAsync(HttpResponseMessage httpResponse)
        {
            var stream = await httpResponse.Content.ReadAsStreamAsync();

            return new Result<Stream>(httpResponse, stream);
        }

        protected override Stream ParseResponse(string response)
        {
            // should not be called
            throw new InvalidOperationException();
        }

        [JsonProperty("languages")] 
        public string Languages { get; set; } = AllKnownLanguages;

        [JsonProperty("projectKeys")]
        public string ProjectKey { get; set; }
    }
}
