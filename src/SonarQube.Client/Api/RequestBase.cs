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
    /// <summary>
    /// Abstract implementation of IRequest<typeparamref name="TResponse"/> that automatically deserializes
    /// the returned string response and throws when non-200 response code.
    /// </summary>
    /// <typeparam name="TResponse">The type of the request result.</typeparam>
    public abstract class RequestBase<TResponse> : IRequest<TResponse>
    {
        /// <summary>
        /// Override this property to specify the relative path of this request.
        /// </summary>
        [JsonIgnore]
        protected abstract string Path { get; }

        /// <summary>
        /// Override this property to specify the HTTP method of this request.
        /// </summary>
        [JsonIgnore]
        protected virtual HttpMethod HttpMethod => HttpMethod.Get;

        [JsonIgnore]
        public ILogger Logger { get; set; }

        /// <summary>
        /// Override this method to deserialize the returned string response.
        /// </summary>
        /// <param name="response">The returned string response.</param>
        /// <returns>Instance of a class that represents the string response.</returns>
        protected abstract TResponse ParseResponse(string response);

        /// <summary>
        /// Invokes the request using the provided <paramref name="httpClient"/> and <paramref name="token"/>.
        /// </summary>
        /// <param name="httpClient">HttpClient instance to be used to invoke the request.</param>
        /// <param name="token">CancellationToken instance to be used to cancel the execution of the request.</param>
        /// <returns>Class instance that represents the request response.</returns>
        public virtual async Task<TResponse> InvokeAsync(HttpClient httpClient, CancellationToken token)
        {
            var result = await InvokeUncheckedAsync(httpClient, token);

            result.EnsureSuccess();

            return result.Value;
        }

        /// <summary>
        /// Invokes the request without checking for status code.
        /// </summary>
        /// <param name="httpClient">HttpClient instance to be used to invoke the request.</param>
        /// <param name="token">CancellationToken instance to be used to cancel the execution of the request.</param>
        /// <returns>Result instance that contains the HttpStatusCode and the deserialized request response.</returns>
        protected async Task<Result<TResponse>> InvokeUncheckedAsync(HttpClient httpClient, CancellationToken token)
        {
            Logger.Debug("Sending Http request:");

            string query = QueryStringSerializer.ToQueryString(this);

            var pathAndQuery = string.IsNullOrEmpty(query) ? Path : $"{Path}?{query}";

            var httpRequest = new HttpRequestMessage(HttpMethod, new Uri(pathAndQuery, UriKind.Relative));

            Logger.Debug(httpRequest.ToString());

            var httpResponse = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);

            Logger.Debug($"Response with HTTP status code '{httpResponse.StatusCode}' received.");

            return await ReadResponseAsync(httpResponse);
        }

        /// <summary>
        /// Override this method to change the way the response content is read and deserialized from the HttpResponseMessage
        /// created while invoking the request.
        /// </summary>
        /// <param name="httpResponse">HttpResponseMessage created when invoking the request.</param>
        /// <returns>Result instance that contains the HttpStatusCode and the deserialized request response.</returns>
        protected virtual async Task<Result<TResponse>> ReadResponseAsync(HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                return new Result<TResponse>(httpResponse, default(TResponse));
            }

            var responseString = await httpResponse.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            Logger.Debug(responseString);

            return new Result<TResponse>(httpResponse, ParseResponse(responseString));
        }
    }
}
