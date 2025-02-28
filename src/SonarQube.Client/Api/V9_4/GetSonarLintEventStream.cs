/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Api.V9_4;

internal class GetSonarLintEventStream : RequestBase<Stream>, IGetSonarLintEventStream
{
    private static readonly string RoslynServerLanguageKeys = string.Join(",", LanguageProvider.Instance.RoslynLanguages.Select(x => x.ServerLanguageKey));

    protected override string Path => "api/push/sonarlint_events";

    protected override MediaTypeWithQualityHeaderValue[] AllowedMediaTypeHeaders => new[] { MediaTypeWithQualityHeaderValue.Parse("text/event-stream") };

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

    /// <summary>
    /// Supports only Roslyn languages as the SSE for non-Roslyn languages are handled by SLCore.
    /// </summary>
    [JsonProperty("languages")]
    public string Languages { get; set; } = RoslynServerLanguageKeys;

    [JsonProperty("projectKeys")]
    public string ProjectKey { get; set; }
}
