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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarQube.Client.Services
{
    public class SonarQubeClient : ISonarQubeClient
    {
        private readonly HttpMessageHandler messageHandler;
        private readonly TimeSpan requestTimeout;

        public SonarQubeClient(HttpMessageHandler messageHandler, TimeSpan requestTimeout)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (requestTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Doesn't expect a zero or negative timeout.", nameof(requestTimeout));
            }

            this.messageHandler = messageHandler;
            this.requestTimeout = requestTimeout;
        }

        public Task<Result<ComponentDTO[]>> GetComponentsSearchProjectsAsync(ConnectionDTO connection,
                                            ComponentRequest request, CancellationToken token)
        {
            const string SearchProjectsAPI = "api/components/search_projects"; // Since 6.2; internal

            var query = AppendQueryString(SearchProjectsAPI, "?organization={0}&p={1}&ps={2}&asc=true",
                        request.OrganizationKey, request.Page, request.PageSize); // TODO: should handle optional params

            return InvokeSonarQubeApi(connection, query, token,
                stringResponse => JObject.Parse(stringResponse)["components"].ToObject<ComponentDTO[]>());
        }

        public Task<Result<OrganizationDTO[]>> GetOrganizationsAsync(ConnectionDTO connection,
            OrganizationRequest request, CancellationToken token)
        {
            const string OrganizationsAPI = "api/organizations/search"; // Since 6.2; internal

            var query = AppendQueryString(OrganizationsAPI, "?p={0}&ps={1}", request.Page, request.PageSize); // TODO: should handle optional params

            return InvokeSonarQubeApi(connection, query, token,
                stringResponse => JObject.Parse(stringResponse)["organizations"].ToObject<OrganizationDTO[]>());
        }

        public Task<Result<PluginDTO[]>> GetPluginsAsync(ConnectionDTO connection, CancellationToken token)
        {
            const string ServerPluginsInstalledAPI = "api/updatecenter/installed_plugins"; // Since 2.10; internal

            return InvokeSonarQubeApi(connection, ServerPluginsInstalledAPI, token,
                stringResponse => ProcessJsonResponse<PluginDTO[]>(stringResponse, token));
        }

        public Task<Result<ProjectDTO[]>> GetProjectsAsync(ConnectionDTO connection, CancellationToken token)
        {
            const string ProjectsAPI = "api/projects/index"; // Since 2.10

            return InvokeSonarQubeApi(connection, ProjectsAPI, token,
                stringResponse => ProcessJsonResponse<ProjectDTO[]>(stringResponse, token));
        }

        public Task<Result<PropertyDTO[]>> GetPropertiesAsync(ConnectionDTO connection, CancellationToken token)
        {
            const string PropertiesAPI = "api/properties/"; // Since 2.6

            return InvokeSonarQubeApi(connection, PropertiesAPI, token,
                stringResponse => ProcessJsonResponse<PropertyDTO[]>(stringResponse, token));
        }

        public Task<Result<QualityProfileChangeLogDTO>> GetQualityProfileChangeLogAsync(ConnectionDTO connection,
            QualityProfileChangeLogRequest request, CancellationToken token)
        {
            const string QualityProfileChangeLogAPI = "api/qualityprofiles/changelog"; // Since 5.2

            var query = AppendQueryString(QualityProfileChangeLogAPI, "?profileKey={0}&ps={1}",
                        request.QualityProfileKey, request.PageSize); // TODO: should handle optional params

            return InvokeSonarQubeApi(connection, query, token,
                stringResponse => ProcessJsonResponse<QualityProfileChangeLogDTO>(stringResponse, token));
        }

        public Task<Result<QualityProfileDTO[]>> GetQualityProfilesAsync(ConnectionDTO connection,
           QualityProfileRequest request, CancellationToken token)
        {
            const string QualityProfileListAPI = "api/qualityprofiles/search"; // Since 5.2

            var query = request.ProjectKey == null
                ? AppendQueryString(QualityProfileListAPI, "?defaults=true")
                : AppendQueryString(QualityProfileListAPI, "?projectKey={0}", request.ProjectKey);

            return InvokeSonarQubeApi(connection, query, token,
                stringResponse => JObject.Parse(stringResponse)["profiles"].ToObject<QualityProfileDTO[]>());
        }

        public Task<Result<RoslynExportProfile>> GetRoslynExportProfileAsync(ConnectionDTO connection,
            RoslynExportProfileRequest request, CancellationToken token)
        {
            const string QualityProfileExportAPI = "api/qualityprofiles/export"; // Since 5.2

            var roslynExporterName = string.Format(CultureInfo.InvariantCulture, "roslyn-{0}",
                        request.Language.Key);
            var query = AppendQueryString(QualityProfileExportAPI, "?name={0}&language={1}&exporterKey={2}",
                request.QualityProfileName, request.Language.Key, roslynExporterName); // TODO: should handle optional params

            return InvokeSonarQubeApi(connection, query, token,
                stringResponse =>
                {
                    using (var reader = new StringReader(stringResponse))
                    {
                        return RoslynExportProfile.Load(reader);
                    }
                });
        }

        public Task<Result<VersionDTO>> GetVersionAsync(ConnectionDTO connection, CancellationToken token)
        {
            const string ServerVersionAPI = "api/server/version"; // Since 2.10; internal

            return InvokeSonarQubeApi(connection, ServerVersionAPI, token,
                stringResponse => new VersionDTO { Version = stringResponse });
        }

        public Task<Result<CredentialsDTO>> ValidateCredentialsAsync(ConnectionDTO connection,
            CancellationToken token)
        {
            const string ValidateCredentialsAPI = "api/authentication/validate"; // Since 3.3

            return InvokeSonarQubeApi(connection, ValidateCredentialsAPI, token,
                stringResponse => new CredentialsDTO { AreValid = (bool)JObject.Parse(stringResponse).SelectToken("valid") });
        }

        private static string AppendQueryString(string urlBase, string queryFormat, params object[] args)
        {
            return urlBase +
                string.Format(CultureInfo.InvariantCulture, queryFormat,
                    args.Select(x => HttpUtility.UrlEncode(x.ToString())).ToArray());
        }

        private static Uri CreateRequestUrl(HttpClient client, string apiUrl)
        {
            // We normalize these inputs to that the client base address always has a trailing slash,
            // and the API URL has no leading slash.
            // Failure to do so will cause an incorrect URL to be formed, with the apiUrl being relative
            // to the base address's hostname only.
            Debug.Assert(client.BaseAddress.ToString().EndsWith("/"),
                "HttpClient.BaseAddress should have a trailing slash");
            Debug.Assert(!apiUrl.StartsWith("/"),
                "API URLs should not begin with a slash as this forces the API to be relative to the base URI's host.");

            Uri normalBaseUri = client.BaseAddress.EnsureTrailingSlash();
            string normalApiUrl = apiUrl.TrimStart('/');

            return new Uri(normalBaseUri, normalApiUrl);
        }

        private static async Task<string> GetStringResultAsync(HttpResponseMessage response,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> InvokeGetRequest(HttpClient client, string apiUrl,
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

        private HttpClient CreateHttpClient(ConnectionDTO connection)
        {
            var httpClient = new HttpClient(this.messageHandler)
            {
                BaseAddress = connection.ServerUri,
                Timeout = this.requestTimeout
            };
            httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderFactory.Create(connection);

            return httpClient;
        }

        private async Task<Result<T>> InvokeSonarQubeApi<T>(ConnectionDTO connection, string query, CancellationToken token,
            Func<string, T> parseStringResult)
        {
            using (var client = CreateHttpClient(connection))
            {
                var httpResponse = await InvokeGetRequest(client, query, token);
                var stringResponse = await GetStringResultAsync(httpResponse, token);

                return new Result<T>(httpResponse, parseStringResult(stringResponse));
            }
        }
    }
}
