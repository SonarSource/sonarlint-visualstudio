/*
 * SonarQube Client
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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
            this.httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderFactory.Create(connection);
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
            var apiPath = BuildRelativeUrl("api/components/search_projects",
                new Dictionary<string, string>
                {
                    ["organization"] = request.OrganizationKey,
                    ["p"] = request.Page.ToString(),
                    ["ps"] = request.PageSize.ToString(),
                    ["asc"] = "true",
                });

            return InvokeSonarQubeApi(apiPath, token,
                stringResponse => JObject.Parse(stringResponse)["components"].ToObject<ComponentResponse[]>());
        }

        public Task<Result<ServerIssue[]>> GetIssuesAsync(string key, CancellationToken token)
        {
            // Since 5.1; internal
            var apiPath = BuildRelativeUrl("batch/issues",
                new Dictionary<string, string>
                {
                    ["key"] = key
                });

            return InvokeSonarQubeApi(apiPath, token,
                async (HttpResponseMessage response) =>
                {
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
            var apiPath = BuildRelativeUrl("api/organizations/search",
                new Dictionary<string, string>
                {
                    ["p"] = request.Page.ToString(),
                    ["ps"] = request.PageSize.ToString()
                });

            return InvokeSonarQubeApi(apiPath, token,
                stringResponse => JObject.Parse(stringResponse)["organizations"].ToObject<OrganizationResponse[]>());
        }

        public Task<Result<PluginResponse[]>> GetPluginsAsync(CancellationToken token)
        {
            // Since 2.10; internal
            var serverPluginsInstalledApiUrl = new Uri("api/updatecenter/installed_plugins", UriKind.Relative);

            return InvokeSonarQubeApi(serverPluginsInstalledApiUrl, token,
                stringResponse => ProcessJsonResponse<PluginResponse[]>(stringResponse, token));
        }

        public Task<Result<ProjectResponse[]>> GetProjectsAsync(CancellationToken token)
        {
            // Since 2.10
            var projectsApiUrl = new Uri("api/projects/index", UriKind.Relative);

            return InvokeSonarQubeApi(projectsApiUrl, token,
                stringResponse => ProcessJsonResponse<ProjectResponse[]>(stringResponse, token));
        }

        public Task<Result<PropertyResponse[]>> GetPropertiesAsync(CancellationToken token)
        {
            // Since 2.6
            var propertiesApiUrl = new Uri("api/properties/", UriKind.Relative);

            return InvokeSonarQubeApi(propertiesApiUrl, token,
                stringResponse => ProcessJsonResponse<PropertyResponse[]>(stringResponse, token));
        }

        public Task<Result<QualityProfileChangeLogResponse>> GetQualityProfileChangeLogAsync(
            QualityProfileChangeLogRequest request, CancellationToken token)
        {
            // Since 5.2
            var apiPath = BuildRelativeUrl("api/qualityprofiles/changelog",
                new Dictionary<string, string>
                {
                    ["profileKey"] = request.QualityProfileKey,
                    ["ps"] = request.PageSize.ToString()
                });

            return InvokeSonarQubeApi(apiPath, token,
                stringResponse => ProcessJsonResponse<QualityProfileChangeLogResponse>(stringResponse, token));
        }

        public Task<Result<QualityProfileResponse[]>> GetQualityProfilesAsync(QualityProfileRequest request,
            CancellationToken token)
        {
            var queryParams = new Dictionary<string, string>();
            if (request.ProjectKey == null)
            {
                queryParams["defaults"] = "true";
            }
            else
            {
                queryParams["projectKey"] = request.ProjectKey;
            }

            // Since 5.2
            var apiPath = BuildRelativeUrl("api/qualityprofiles/search", queryParams);

            return InvokeSonarQubeApi(apiPath, token,
                stringResponse => JObject.Parse(stringResponse)["profiles"].ToObject<QualityProfileResponse[]>());
        }

        public Task<Result<RoslynExportProfileResponse>> GetRoslynExportProfileAsync(RoslynExportProfileRequest request,
            CancellationToken token)
        {
            // Since 5.2
            var apiPath = BuildRelativeUrl("api/qualityprofiles/export",
                new Dictionary<string, string>
                {
                    ["name"] = request.QualityProfileName,
                    ["language"] = request.LanguageKey,
                    ["exporterKey"] = string.Format(CultureInfo.InvariantCulture, "roslyn-{0}", request.LanguageKey)
                });

            return InvokeSonarQubeApi(apiPath, token,
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
            var serverVersionAPI = new Uri("api/server/version", UriKind.Relative);

            return InvokeSonarQubeApi(serverVersionAPI, token,
                stringResponse => new VersionResponse { Version = stringResponse });
        }

        public Task<Result<CredentialResponse>> ValidateCredentialsAsync(CancellationToken token)
        {
            // Since 3.3
            var validateCredentialsApiUrl = new Uri("api/authentication/validate", UriKind.Relative);

            return InvokeSonarQubeApi(validateCredentialsApiUrl, token,
                stringResponse => new CredentialResponse { IsValid = (bool)JObject.Parse(stringResponse).SelectToken("valid") });
        }

        public Task<Result<NotificationsResponse[]>> GetNotificationEventsAsync(NotificationsRequest request,
            CancellationToken token)
        {
            // Since 6.6; internal
            var apiPath = BuildRelativeUrl("api/developers/search_events",
                new Dictionary<string, string>
                {
                    ["projects"] = request.ProjectKey,
                    ["from"] = ToJavaTimeFormat(request.EventsSince)
                });

            return InvokeSonarQubeApi(apiPath, token,
                    async (HttpResponseMessage httpResponse) =>
                    {
                        if (httpResponse.StatusCode != HttpStatusCode.OK)
                        {
                            return new NotificationsResponse[0];
                        }

                        var stringResponse = await GetStringResultAsync(httpResponse, token);
                        return JObject.Parse(stringResponse)["events"].ToObject<NotificationsResponse[]>()
                            ?? new NotificationsResponse[0];
                    }
                );
        }

        private static string ToJavaTimeFormat(DateTimeOffset date)
        {
            // This is the only format the notifications API accepts. ISO 8601 formats don't work.
            var dateTime = date.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            var timezone = date.ToString("zzz", CultureInfo.InvariantCulture).Replace(":", "");

            // The Java format is "yyyy-MM-dd'T'HH:mm:ssZ"
            // For example 2013-05-01T13:00:00+0100
            return dateTime + timezone;
        }

        private Uri BuildRelativeUrl(string relativePath, Dictionary<string, string> queryParameters)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            foreach (var kvp in queryParameters)
            {
                query[kvp.Key] = kvp.Value;
            }

            return new Uri(string.Format("{0}?{1}", relativePath, query.ToString()),
                UriKind.Relative);
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

        private static async Task<HttpResponseMessage> InvokeGetRequest(HttpClient client, Uri apiUrl,
            CancellationToken token)
        {
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

        private async Task<Result<T>> InvokeSonarQubeApi<T>(Uri apiUrl, CancellationToken token,
                    Func<string, T> parseStringResult)
        {
            var httpResponse = await InvokeGetRequest(this.httpClient, apiUrl, token);
            var stringResponse = await GetStringResultAsync(httpResponse, token);

            return new Result<T>(httpResponse, parseStringResult(stringResponse));
        }

        private async Task<Result<T>> InvokeSonarQubeApi<T>(Uri apiUrl, CancellationToken token,
            Func<HttpResponseMessage, Task<T>> parseResponse)
        {
            var httpResponse = await InvokeGetRequest(this.httpClient, apiUrl, token);
            token.ThrowIfCancellationRequested();
            var response = await parseResponse(httpResponse);

            return new Result<T>(httpResponse, response);
        }
    }
}