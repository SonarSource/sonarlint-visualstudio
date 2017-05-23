/*
 * SonarLint for Visual Studio
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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service.DataModel;

namespace SonarLint.VisualStudio.Integration.Service
{
    /// <summary>
    /// SonarQube service wrapper.
    /// The class is not thread safe.
    /// </summary>
    [Export(typeof(SonarQubeServiceWrapper)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class SonarQubeServiceWrapper : ISonarQubeServiceWrapper
    {
        public const string ProjectsAPI = "api/projects/index";                                 // Since 2.10
        public const string SearchProjectsAPI = "api/components/search_projects";               // Since 6.2; internal
        public const string ServerPluginsInstalledAPI = "api/updatecenter/installed_plugins";   // Since 2.10; internal
        public const string QualityProfileListAPI = "api/qualityprofiles/search";               // Since 5.2
        public const string QualityProfileExportAPI = "api/qualityprofiles/export";             // Since 5.2
        public const string PropertiesAPI = "api/properties/";                                  // Since 2.6
        public const string QualityProfileChangeLogAPI = "api/qualityprofiles/changelog";       // Since 5.2
        public const string OrganizationsAPI = "api/organizations/search";                      // Since 6.2; internal
        public const string ServerVersionAPI = "api/server/version";                            // Since 2.10; internal
        public const string ValidateCredentialsAPI = "api/authentication/validate";             // Since 3.3

        public const string ProjectDashboardRelativeUrl = "dashboard/index/{0}";

        public const string RoslynExporterFormat = "roslyn-{0}";

        private const int MaxAllowedPageSize = 500;

        private static readonly IReadOnlyDictionary<Language, string> languageKeys = new ReadOnlyDictionary<Language, string>(
            new Dictionary<Language, string>
            {
                { Language.CSharp, "cs" },
                { Language.VBNET, "vbnet" }
            });

        private readonly IServiceProvider serviceProvider;
        private readonly TimeSpan requestTimeout;

        private readonly Version OrganizationsSupportStartVersion = new Version(6, 2);

        #region Constructors

        [ImportingConstructor]
        public SonarQubeServiceWrapper([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : this(serviceProvider, TimeSpan.FromSeconds(100) /*The default value for HttpClient*/)
        {
        }

        protected SonarQubeServiceWrapper(IServiceProvider serviceProvider, TimeSpan timeout)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;

            // No need to validate, HttpClient will do it anyway.
            this.requestTimeout = timeout;
        }

        #endregion

        #region ISonarQubeServiceWrapper

        public bool AreCredentialsValid(ConnectionInformation connectionInformation, CancellationToken token)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            return this.SafeUseHttpClient<bool>(connectionInformation,
                client => this.ValidateCredentials(client, token));
        }

        public bool HasOrganizationsSupport(ConnectionInformation connectionInformation, CancellationToken token)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            var stringVersion = this.SafeUseHttpClient<string>(connectionInformation, client => this.GetServerVersion(client, token));
            if (stringVersion == null)
            {
                return false;
            }

            Version version;
            if (!Version.TryParse(stringVersion, out version))
            {
                return false;
            }

            return version >= OrganizationsSupportStartVersion;
        }

        public bool TryGetOrganizations(ConnectionInformation connectionInformation, CancellationToken token, out OrganizationInformation[] organizations)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            organizations = this.SafeUseHttpClient<OrganizationInformation[]>(connectionInformation,
                client => this.DownloadOrganizations(client, token));

            return organizations != null;
        }

        public bool TryGetProjects(ConnectionInformation connectionInformation, CancellationToken token, out ProjectInformation[] serverProjects)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            serverProjects = this.SafeUseHttpClient<ProjectInformation[]>(connectionInformation,
                client => this.DownloadProjects(client, token, connectionInformation.Organization?.Key));

            return serverProjects != null;
        }

        public bool TryGetProperties(ConnectionInformation connectionInformation, CancellationToken token, out ServerProperty[] properties)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            properties = this.SafeUseHttpClient<ServerProperty[]>(connectionInformation,
                client => GetServerProperties(client, token));

            return properties != null;
        }

        public bool TryGetExportProfile(ConnectionInformation connectionInformation, QualityProfile profile, Language language, CancellationToken token, out RoslynExportProfile export)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (!language.IsSupported)
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            export = this.SafeUseHttpClient<RoslynExportProfile>(connectionInformation,
                client => DownloadQualityProfileExport(client, profile, language, token));

            return export != null;
        }


        public bool TryGetQualityProfile(ConnectionInformation serverConnection, ProjectInformation project, Language language, CancellationToken token, out QualityProfile profile)
        {
            if (serverConnection == null)
            {
                throw new ArgumentNullException(nameof(serverConnection));
            }

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            if (!language.IsSupported)
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            profile = this.SafeUseHttpClient<QualityProfile>(serverConnection,
                async client =>
                {
                    QualityProfile qp = await DownloadQualityProfile(client, project, language, token);
                    if (qp == null)
                    {
                        return null;
                    }

                    QualityProfileChangeLog changeLog = await DownloadQualityProfileChangeLog(client, qp, token);
                    if (changeLog != null)
                    {
                        qp.QualityProfileTimestamp = changeLog.Events.SingleOrDefault()?.Date;
                    }

                    return qp;
                });

            return profile != null;
        }

        public bool TryGetPlugins(ConnectionInformation connectionInformation, CancellationToken token, out ServerPlugin[] plugins)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            plugins = this.SafeUseHttpClient<ServerPlugin[]>(connectionInformation,
                client => DownloadPluginInformation(client, token));

            return plugins != null;
        }

        public Uri CreateProjectDashboardUrl(ConnectionInformation connectionInformation, ProjectInformation project)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return new Uri(connectionInformation.ServerUri, string.Format(ProjectDashboardRelativeUrl, project.Key));
        }

        #endregion

        internal /*for testing purposes*/ protected virtual HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }

        private async Task<string> GetServerVersion(HttpClient configuredClient, CancellationToken token)
        {
            var response = await InvokeGetRequest(configuredClient, ServerVersionAPI, token);
            token.ThrowIfCancellationRequested();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private async Task<bool> ValidateCredentials(HttpClient configuredClient, CancellationToken token)
        {
            var httpResponse = await InvokeGetRequest(configuredClient, ValidateCredentialsAPI, token);
            var stringResponse = await ReadResponse(httpResponse, token);

            return (bool)JObject.Parse(stringResponse).SelectToken("valid");
        }

        #region Download projects

        private async Task<OrganizationInformation[]> DownloadOrganizations(HttpClient configuredClient, CancellationToken token)
        {
            int currentPage = 1;
            var allOrganizations = new List<OrganizationInformation>();
            OrganizationInformation[] currentPageOrganizations;

            do
            {
                var query = string.Format("{0}?ps={1}&p={2}", OrganizationsAPI, MaxAllowedPageSize, currentPage);
                var httpResponse = await InvokeGetRequest(configuredClient, query, token);
                var stringResponse = await ReadResponse(httpResponse, token);
                currentPageOrganizations = JObject.Parse(stringResponse)["organizations"].ToObject<OrganizationInformation[]>();
                if (currentPageOrganizations == null)
                {
                    return null;
                }
                else
                {
                    allOrganizations.AddRange(currentPageOrganizations);
                }

                currentPage++;
            } while (currentPageOrganizations.Length > 0);

            return allOrganizations.ToArray();
        }

        private async Task<ProjectInformation[]> DownloadProjects(HttpClient configuredClient, CancellationToken token,
            string organizationKey)
        {
            if (string.IsNullOrEmpty(organizationKey))
            {
                var httpResponse = await InvokeGetRequest(configuredClient, ProjectsAPI, token);
                var stringResponse = await ReadResponse(httpResponse, token);

                return ProcessJsonResponse<ProjectInformation[]>(stringResponse, token);
            }

            int currentPage = 1;
            bool hasOtherPages = false;
            var allProjects = new List<ComponentResult>();
            ComponentResult[] currentPageProjects;

            do
            {
                var query = string.Format("{0}?asc&organization={1}&ps={2}&p={3}", SearchProjectsAPI, organizationKey,
                    MaxAllowedPageSize, currentPage);
                var httpResponse = await InvokeGetRequest(configuredClient, query, token);
                var stringResponse = await ReadResponse(httpResponse, token);

                currentPageProjects = JObject.Parse(stringResponse)["components"].ToObject<ComponentResult[]>();
                allProjects.AddRange(currentPageProjects);

                var totalCount = JObject.Parse(stringResponse)["paging"]["total"].ToObject<int>();
                hasOtherPages = currentPage * MaxAllowedPageSize < totalCount;
                currentPage++;
            } while (hasOtherPages);

            return allProjects.Select(x => new ProjectInformation { Key = x.Key, Name = x.Name }).ToArray();
        }

        #endregion

        #region Server plugin info

        private static async Task<ServerPlugin[]> DownloadPluginInformation(HttpClient client, CancellationToken token)
        {
            string getPluginsUrl = ServerPluginsInstalledAPI;
            var httpResponse = await InvokeGetRequest(client, getPluginsUrl, token);
            var stringResponse = await ReadResponse(httpResponse, token);

            return ProcessJsonResponse<ServerPlugin[]>(stringResponse, token);
        }

        #endregion

        #region Quality profile

        internal /*for testing purposes*/ static string CreateQualityProfileUrl(Language language, ProjectInformation project = null)
        {
            string projectKey = project?.Key;

            return string.IsNullOrWhiteSpace(projectKey)
                ? AppendQueryString(QualityProfileListAPI, "?defaults=true")
                : AppendQueryString(QualityProfileListAPI, "?projectKey={0}", projectKey);
        }

        internal /*for testing purposes*/ static async Task<QualityProfile> DownloadQualityProfile(HttpClient client, ProjectInformation project, Language language, CancellationToken token)
        {
            string apiUrl = CreateQualityProfileUrl(language, project);
            HttpResponseMessage httpResponse = await InvokeGetRequest(client, apiUrl, token, ensureSuccess: false);
            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                // Special handling for the case when a project was not analyzed yet, in which case a 404 is returned
                // Request the profile without the project
                bool ensureSuccess = true;
                apiUrl = CreateQualityProfileUrl(language);
                httpResponse = await InvokeGetRequest(client, apiUrl, token, ensureSuccess);
            }

            httpResponse.EnsureSuccessStatusCode(); // Bubble up the rest of the errors


            var stringResponse = await ReadResponse(httpResponse, token);
            var profiles = ProcessJsonResponse<QualityProfiles>(stringResponse, token);
            var serverLanguage = GetServerLanguageKey(language);
            var profilesWithGivenLanguage = profiles.Profiles.Where(x => x.Language == serverLanguage).ToList();

            return profilesWithGivenLanguage.Count > 1
                ? profilesWithGivenLanguage.Single(x => x.IsDefault)
                : profilesWithGivenLanguage.Single();
        }

        #endregion

        #region Roslyn export profile

        internal /*for testing purposes*/  static string CreateRoslynExporterName(Language language)
        {
            return string.Format(CultureInfo.InvariantCulture, RoslynExporterFormat, languageKeys[language]);
        }

        internal /*for testing purposes*/ static string CreateQualityProfileExportUrl(QualityProfile profile, Language language, string exporter)
        {
            // TODO: why name and not key? why language is needed at all, profiles are per language
            return AppendQueryString(QualityProfileExportAPI, "?name={0}&language={1}&exporterKey={2}", profile.Name, GetServerLanguageKey(language), exporter);
        }

        private static async Task<RoslynExportProfile> DownloadQualityProfileExport(HttpClient client, QualityProfile profile, Language language, CancellationToken token)
        {
            var roslynExporterName = CreateRoslynExporterName(language);
            string api = CreateQualityProfileExportUrl(profile, language, roslynExporterName);
            HttpResponseMessage response = await InvokeGetRequest(client, api, token);

            using (Stream stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            {
                return RoslynExportProfile.Load(reader);
            }
        }

        #endregion

        #region Server properties

        private static async Task<ServerProperty[]> GetServerProperties(HttpClient client, CancellationToken token)
        {
            var httpResponse = await InvokeGetRequest(client, PropertiesAPI, token);
            var stringResponse = await ReadResponse(httpResponse, token);

            return ProcessJsonResponse<ServerProperty[]>(stringResponse, token);
        }

        #endregion

        #region Quality profile change log
        internal /*for testing purposes*/ static string CreateQualityProfileChangeLogUrl(QualityProfile profile)
        {
            // Results are in descending order, so setting the page size to 1 will improve performance
            return AppendQueryString(QualityProfileChangeLogAPI, "?profileKey={0}&ps=1", profile.Key);
        }

        private async Task<QualityProfileChangeLog> DownloadQualityProfileChangeLog(HttpClient client, QualityProfile profile, CancellationToken token)
        {
            string api = CreateQualityProfileChangeLogUrl(profile);
            HttpResponseMessage httpResponse = await InvokeGetRequest(client, api, token, ensureSuccess: false);
            // The service doesn't exist on older versions, and it's not absolutely mandatory since we can work
            // without the information provided, only with reduced functionality.
            if (httpResponse.IsSuccessStatusCode)
            {
                var stringResponse = await ReadResponse(httpResponse, token);

                return ProcessJsonResponse<QualityProfileChangeLog>(stringResponse, token);
            }
            else
            {
                VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, Strings.SonarQubeOptionalServiceFailed, QualityProfileChangeLogAPI, (int)httpResponse.StatusCode);
                return null;
            }
        }
        #endregion

        #region Helpers

        internal /*for testing purposes*/ static string GetServerLanguageKey(Language language)
        {
            Debug.Assert(languageKeys.ContainsKey(language), "Unsupported language; there is no corresponding server key");
            return languageKeys[language];
        }

        internal /*for testing purposes*/ static string AppendQueryString(string urlBase, string queryFormat, params string[] args)
        {
            return urlBase + string.Format(CultureInfo.InvariantCulture, queryFormat, args.Select(HttpUtility.UrlEncode).ToArray());
        }

        internal /*for testing purposes*/ static Uri CreateRequestUrl(HttpClient client, string apiUrl)
        {
            // We normalize these inputs to that the client base address always has a trailing slash,
            // and the API URL has no leading slash.
            // Failure to do so will cause an incorrect URL to be formed, with the apiUrl being relative
            // to the base address's hostname only.
            Debug.Assert(client.BaseAddress.ToString().EndsWith("/"), "HttpClient.BaseAddress should have a trailing slash");
            Debug.Assert(!apiUrl.StartsWith("/"), "API URLs should not begin with a slash as this forces the API to be relative to the base URI's hostname.");

            Uri normalBaseUri = client.BaseAddress.EnsureTrailingSlash();
            string normalApiUrl = apiUrl.TrimStart('/');

            return new Uri(normalBaseUri, normalApiUrl);
        }

        private static async Task<HttpResponseMessage> InvokeGetRequest(HttpClient client, string apiUrl, CancellationToken token, bool ensureSuccess = true)
        {
            var uri = CreateRequestUrl(client, apiUrl);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

            if (ensureSuccess)
            {
                response.EnsureSuccessStatusCode();
            }

            return response;
        }

        private static async Task<string> ReadResponse(HttpResponseMessage response, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static T ProcessJsonResponse<T>(string jsonResponse, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return JsonHelper.Deserialize<T>(jsonResponse);
        }

        private T SafeUseHttpClient<T>(ConnectionInformation connectionInformation, Func<HttpClient, Task<T>> sendRequestsAsync)
        {
            using (HttpClient client = this.CreateHttpClient())
            {
                client.BaseAddress = connectionInformation.ServerUri;
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderProvider.GetAuthenticationHeader(connectionInformation);
                client.Timeout = this.requestTimeout;
                T result = default(T);

                try
                {
                    result = sendRequestsAsync(client).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (HttpRequestException e)
                {
                    // For some errors we will get an inner exception which will have a more specific information
                    // that we would like to show i.e.when the host could not be resolved
                    System.Net.WebException innerException = e.InnerException as System.Net.WebException;
                    VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, Strings.SonarQubeRequestFailed, e.Message, innerException?.Message);
                }
                catch (TaskCanceledException)
                {
                    // Canceled or timeout
                    VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, Strings.SonarQubeRequestTimeoutOrCancelled);
                }
                catch (Exception ex)
                {
                    if (ErrorHandler.IsCriticalException(ex))
                    {
                        throw;
                    }

                    VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, Strings.SonarQubeRequestFailed, ex.Message, null);
                }

                return result;
            }
        }

        #endregion
    }
}
