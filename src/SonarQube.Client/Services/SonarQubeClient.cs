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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Helpers;
using SonarQube.Client.Messages;

namespace SonarQube.Client.Services
{
    public sealed class SonarQubeClient : ISonarQubeClient, IDisposable
    {
        private readonly HttpClient httpClient;
        private bool isDisposed;

        public SonarQubeClient(ConnectionRequest connection, HttpMessageHandler messageHandler, TimeSpan requestTimeout)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }
            if (requestTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Doesn't expect a zero or negative timeout.", nameof(requestTimeout));
            }

            this.httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = connection.ServerUri,
                Timeout = requestTimeout
            };
            this.httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderFactory.Create(
                connection.Login, connection.Password, connection.Authentication);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            this.httpClient.Dispose();
            this.isDisposed = true;
        }

        public Task<Result<ComponentResponse[]>> GetComponentsSearchProjectsAsync(ComponentRequest request,
            CancellationToken token)
        {
            // Since 6.2; internal
            return InvokeSonarQubeApi("api/components/search_projects", request, token,
                stringResponse => JObject.Parse(stringResponse)["components"].ToObject<ComponentResponse[]>());
        }

        public Task<Result<ServerIssue[]>> GetIssuesAsync(string key, CancellationToken token)
        {
            // Since 5.1; internal
            return InvokeSonarQubeApi("batch/issues", new { key = key }, token,
                async (HttpResponseMessage response) =>
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return null;
                    }

                    var byteArray = await response.Content.ReadAsByteArrayAsync();
                    // Protobuf for C# throws when trying to read outside of the buffer and ReadAsStreamAsync returns a non
                    // seekable stream so we can't determine when to stop. The hack is to use an intermediate MemoryStream
                    // so we can control when to stop reading.
                    // Note we might want to use FileStream instead to avoid intensive memory usage.
                    using (var stream = new MemoryStream(byteArray))
                    {
                        return ReadFromProtobufStream(stream, ServerIssue.Parser).ToArray();
                    }
                });
        }

        public Task<Result<OrganizationResponse[]>> GetOrganizationsAsync(OrganizationRequest request, CancellationToken token)
        {
            // Since 6.2; internal
            return InvokeSonarQubeApi("api/organizations/search", request, token,
                stringResponse => JObject.Parse(stringResponse)["organizations"].ToObject<OrganizationResponse[]>());
        }

        public Task<Result<PluginResponse[]>> GetPluginsAsync(CancellationToken token)
        {
            // Since 2.10; internal
            return InvokeSonarQubeApi("api/updatecenter/installed_plugins", null, token,
                stringResponse => ProcessJsonResponse<PluginResponse[]>(stringResponse, token));
        }

        public Task<Result<ProjectResponse[]>> GetProjectsAsync(CancellationToken token)
        {
            // Since 2.10
            return InvokeSonarQubeApi("api/projects/index", null, token,
                stringResponse => ProcessJsonResponse<ProjectResponse[]>(stringResponse, token));
        }

        public Task<Result<PropertyResponse[]>> GetPropertiesAsync(CancellationToken token)
        {
            // Since 2.6
            return InvokeSonarQubeApi("api/properties/", null, token,
                stringResponse => ProcessJsonResponse<PropertyResponse[]>(stringResponse, token));
        }

        public Task<Result<QualityProfileChangeLogResponse>> GetQualityProfileChangeLogAsync(
            QualityProfileChangeLogRequest request, CancellationToken token)
        {
            // Since 5.2
            return InvokeSonarQubeApi("api/qualityprofiles/changelog", request, token,
                stringResponse => ProcessJsonResponse<QualityProfileChangeLogResponse>(stringResponse, token));
        }

        public Task<Result<QualityProfileResponse[]>> GetQualityProfilesAsync(QualityProfileRequest request,
            CancellationToken token)
        {
            if (request.ProjectKey == null)
            {
                request.Defaults = true;
            }

            // Since 5.2
            return InvokeSonarQubeApi("api/qualityprofiles/search", request, token,
                stringResponse => JObject.Parse(stringResponse)["profiles"].ToObject<QualityProfileResponse[]>());
        }

        public Task<Result<RoslynExportProfileResponse>> GetRoslynExportProfileAsync(RoslynExportProfileRequest request,
            CancellationToken token)
        {
            // Since 5.2
            return InvokeSonarQubeApi("api/qualityprofiles/export", request, token,
                stringResponse =>
                {
                    using (var reader = new StringReader(stringResponse))
                    {
                        return RoslynExportProfileResponse.Load(reader);
                    }
                });
        }

        public Task<Result<VersionResponse>> GetVersionAsync(CancellationToken token)
        {
            // Since 2.10; internal
            return InvokeSonarQubeApi("api/server/version", null, token,
                stringResponse => new VersionResponse { Version = stringResponse });
        }

        public Task<Result<CredentialResponse>> ValidateCredentialsAsync(CancellationToken token)
        {
            // Since 3.3
            return InvokeSonarQubeApi("api/authentication/validate", null, token,
                stringResponse => new CredentialResponse { IsValid = (bool)JObject.Parse(stringResponse).SelectToken("valid") });
        }

        public Task<Result<NotificationsResponse[]>> GetNotificationEventsAsync(NotificationsRequest request,
            CancellationToken token)
        {
            // Since 6.6; internal
            return InvokeSonarQubeApi("api/developers/search_events", request, token,
                stringResponse => JObject.Parse(stringResponse)["events"].ToObject<NotificationsResponse[]>());
        }

        private Uri BuildRelativeUrl(string relativePath, object request = null)
        {
            var queryString = QueryStringSerializer.ToQueryString(request);
            var url = string.IsNullOrEmpty(queryString)
                ? relativePath
                : $"{relativePath}?{queryString}";

            return new Uri(url, UriKind.Relative);
        }

        private static Uri CreateRequestUrl(HttpClient client, Uri apiUrl)
        {
            // We normalize these inputs to that the client base address always has a trailing slash,
            // and the API URL has no leading slash.
            // Failure to do so will cause an incorrect URL to be formed, with the apiUrl being relative
            // to the base address's hostname only.
            Debug.Assert(client.BaseAddress.ToString().EndsWith("/"),
                "HttpClient.BaseAddress should have a trailing slash");
            Debug.Assert(!apiUrl.IsAbsoluteUri,
                "API URLs should not begin with a slash as this forces the API to be relative to the base URI's host.");

            Uri normalBaseUri = client.BaseAddress.EnsureTrailingSlash();
            string normalApiUrl = apiUrl.ToString().TrimStart('/');

            return new Uri(normalBaseUri, normalApiUrl);
        }

        private static async Task<string> GetStringResultAsync(HttpResponseMessage response,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> InvokeGetRequest(HttpClient client, string apiPath, object request,
            CancellationToken token)
        {
            var apiUrl = BuildRelativeUrl(apiPath, request);
            var uri = CreateRequestUrl(client, apiUrl);
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            var httpResponse = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);

            return httpResponse;
        }

        private static T ProcessJsonResponse<T>(string jsonResponse, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return JsonHelper.Deserialize<T>(jsonResponse);
        }

        private static IEnumerable<T> ReadFromProtobufStream<T>(Stream stream, MessageParser<T> parser)
            where T : IMessage<T>
        {
            while (stream.Position < stream.Length)
            {
                yield return parser.ParseDelimitedFrom(stream);
            }
        }

        private async Task<Result<T>> InvokeSonarQubeApi<T>(string apiPath, object request, CancellationToken token,
            Func<string, T> parseStringResult)
        {
            var httpResponse = await InvokeGetRequest(this.httpClient, apiPath, request, token);
            if (!httpResponse.IsSuccessStatusCode)
            {
                return new Result<T>(httpResponse, default(T));
            }

            var stringResponse = await GetStringResultAsync(httpResponse, token);

            return new Result<T>(httpResponse, parseStringResult(stringResponse));
        }

        private async Task<Result<T>> InvokeSonarQubeApi<T>(string apiPath, object request, CancellationToken token,
            Func<HttpResponseMessage, Task<T>> parseResponse)
        {
            var httpResponse = await InvokeGetRequest(this.httpClient, apiPath, request, token);
            token.ThrowIfCancellationRequested();
            var response = await parseResponse(httpResponse);

            return new Result<T>(httpResponse, response);
        }
    }
}
