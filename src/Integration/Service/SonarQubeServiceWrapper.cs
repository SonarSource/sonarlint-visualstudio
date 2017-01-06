/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service.DataModel;
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
        public const string ServerPluginsInstalledAPI = "api/updatecenter/installed_plugins";   // Since 2.10; internal
        public const string QualityProfileListAPI = "api/qualityprofiles/search";               // Since 5.2
        public const string QualityProfileExportAPI = "api/qualityprofiles/export";             // Since 5.2
        public const string PropertiesAPI = "api/properties/";                                  // Since 2.6
        public const string QualityProfileChangeLogAPI = "api/qualityprofiles/changelog";       // Since 5.2

        public const string ProjectDashboardRelativeUrl = "dashboard/index/{0}";

        public const string RoslynExporterFormat = "roslyn-{0}";

        private static readonly IReadOnlyDictionary<Language, string> languageKeys = new ReadOnlyDictionary<Language, string>(
            new Dictionary<Language, string>
            {
                { Language.CSharp, "cs" },
                { Language.VBNET, "vbnet" }
            });

        private readonly IServiceProvider serviceProvider;
        private readonly TimeSpan requestTimeout;

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

        public bool TryGetProjects(ConnectionInformation connectionInformation, CancellationToken token, out ProjectInformation[] serverProjects)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            serverProjects = this.SafeUseHttpClient<ProjectInformation[]>(connectionInformation,
                client => this.DownloadProjects(client, token));

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

        #region Download projects

        private async Task<ProjectInformation[]> DownloadProjects(HttpClient configuredClient, CancellationToken token)
        {
            HttpResponseMessage downloadProjectsResponse = await InvokeGetRequest(configuredClient, ProjectsAPI, token);
            return await ProcessJsonResponse<ProjectInformation[]>(downloadProjectsResponse, token);
        }

        #endregion

        #region Server plugin info

        private static async Task<ServerPlugin[]> DownloadPluginInformation(HttpClient client, CancellationToken token)
        {
            string getPluginsUrl = ServerPluginsInstalledAPI;
            HttpResponseMessage response = await InvokeGetRequest(client, getPluginsUrl, token);

            return await ProcessJsonResponse<ServerPlugin[]>(response, token);
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
            HttpResponseMessage response = await InvokeGetRequest(client, apiUrl, token, ensureSuccess: false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Special handling for the case when a project was not analyzed yet, in which case a 404 is returned
                // Request the profile without the project
                bool ensureSuccess = true;
                apiUrl = CreateQualityProfileUrl(language);
                response = await InvokeGetRequest(client, apiUrl, token, ensureSuccess);
            }

            response.EnsureSuccessStatusCode(); // Bubble up the rest of the errors

            var profiles = await ProcessJsonResponse<QualityProfiles>(response, token);
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
            HttpResponseMessage response = await InvokeGetRequest(client, PropertiesAPI, token);

            return await ProcessJsonResponse<ServerProperty[]>(response, token);
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
            HttpResponseMessage response = await InvokeGetRequest(client, api, token, ensureSuccess: false);
            // The service doesn't exist on older versions, and it's not absolutely mandatory since we can work
            // without the information provided, only with reduced functionality.
            if (response.IsSuccessStatusCode)
            {
                return await ProcessJsonResponse<QualityProfileChangeLog>(response, token);
            }
            else
            {
                VsShellUtils.WriteToSonarLintOutputPane(this.serviceProvider, Strings.SonarQubeOptionalServiceFailed, QualityProfileChangeLogAPI, (int)response.StatusCode);
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

        private static async Task<T> ProcessJsonResponse<T>(HttpResponseMessage response, CancellationToken token)
        {
            Debug.Assert(response != null && response.IsSuccessStatusCode, "Invalid response");

            token.ThrowIfCancellationRequested();
            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            token.ThrowIfCancellationRequested();
            return JsonHelper.Deserialize<T>(content);
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
