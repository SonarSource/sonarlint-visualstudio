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

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarQube.Client.Helpers;
using SonarQube.Client.Services;

namespace SonarQube.Client
{
    public abstract class RequestBase<TResponse> : IRequest<TResponse>
    {
        [JsonIgnore]
        protected abstract string Path { get; }

        [JsonIgnore]
        protected virtual HttpMethod HttpMethod => HttpMethod.Get;

        protected abstract TResponse ParseResponse(string response);

        public virtual async Task<TResponse> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            var result = await InvokeImplAsync(httpClient, token);

            result.EnsureSuccess();

            return result.Value;
        }

        protected async Task<Result<TResponse>> InvokeImplAsync(HttpClient httpClient, CancellationToken token)
        {
            string query = QueryStringSerializer.ToQueryString(this);

            var pathAndQuery = string.IsNullOrEmpty(query) ? Path : $"{Path}?{query}";

            var httpRequest = new HttpRequestMessage(HttpMethod, new Uri(pathAndQuery, UriKind.Relative));

            var httpResponse = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);

            return await ReadResponseAsync(httpResponse);
        }

        protected virtual async Task<Result<TResponse>> ReadResponseAsync(HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                return new Result<TResponse>(httpResponse, default(TResponse));
            }

            var responseString = await httpResponse.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            return new Result<TResponse>(httpResponse, ParseResponse(responseString));
        }
    }
}
