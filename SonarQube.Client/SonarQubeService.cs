/*
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Api;
using SonarQube.Client.Helpers;
using SonarQube.Client.Logging;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;

namespace SonarQube.Client
{
    public class SonarQubeService : ISonarQubeService, IDisposable
    {
        private readonly HttpMessageHandler messageHandler;
        private readonly IRequestFactory requestFactory;
        private readonly string userAgent;
        private readonly ILogger logger;

        private HttpClient httpClient;

        public async Task<bool> HasOrganizations(CancellationToken token)
        {
            EnsureIsConnected();

            var hasOrganisations = this.httpClient.BaseAddress.Host.Equals("sonarcloud.io", StringComparison.OrdinalIgnoreCase);

            return await Task.FromResult<bool>(hasOrganisations);
        }

        public bool IsConnected { get; private set; }

        public ServerInfo ServerInfo { get; private set; }

        public SonarQubeService(HttpMessageHandler messageHandler, string userAgent, ILogger logger)
            : this(messageHandler, DefaultConfiguration.Configure(new RequestFactory(logger)), userAgent, logger)
        {
        }

        internal /* for testing */ SonarQubeService(HttpMessageHandler messageHandler, IRequestFactory requestFactory, string userAgent, ILogger logger)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }
            if (requestFactory == null)
            {
                throw new ArgumentNullException(nameof(requestFactory));
            }
            if (userAgent == null)
            {
                throw new ArgumentNullException(nameof(userAgent));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            this.messageHandler = messageHandler;
            this.requestFactory = requestFactory;
            this.userAgent = userAgent;
            this.logger = logger;
        }

        /// <summary>
        /// Convenience overload for requests that do not need configuration.
        /// </summary>
        private Task<TResponse> InvokeRequestAsync<TRequest, TResponse>(CancellationToken token)
            where TRequest : IRequest<TResponse>
        {
            return InvokeRequestAsync<TRequest, TResponse>(request => { }, token);
        }

        /// <summary>
        /// Creates a new instance of the specified TRequest request, configures and invokes it and returns its response.
        /// </summary>
        /// <typeparam name="TRequest">The request interface to invoke.</typeparam>
        /// <typeparam name="TResponse">The type of the request response result.</typeparam>
        /// <param name="configure">Action that configures a type instance that implements TRequest.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns the result of the request invocation.</returns>
        private async Task<TResponse> InvokeRequestAsync<TRequest, TResponse>(Action<TRequest> configure,
            CancellationToken token)
            where TRequest : IRequest<TResponse>
        {
            EnsureIsConnected();

            var request = requestFactory.Create<TRequest>(ServerInfo);
            configure(request);

            var result = await request.InvokeAsync(httpClient, token);

            return result;
        }

        public async Task ConnectAsync(ConnectionInformation connection, CancellationToken token)
        {
            logger.Info($"Connecting to '{connection.ServerUri}'.");
            logger.Debug($"IsConnected is {IsConnected}.");

            httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = connection.ServerUri,
                DefaultRequestHeaders =
                {
                    Authorization = AuthenticationHeaderFactory.Create(
                        connection.UserName, connection.Password, connection.Authentication),
                },
            };

            httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

            IsConnected = true;
            var serverTypeDescription = connection.IsSonarCloud ? "SonarCloud" : "SonarQube";

            logger.Debug($"Getting the version of {serverTypeDescription}...");

            var versionResponse = await InvokeRequestAsync<IGetVersionRequest, string>(token);
            ServerInfo = new ServerInfo(Version.Parse(versionResponse),
                connection.IsSonarCloud ? ServerType.SonarCloud : ServerType.SonarQube);

            logger.Info($"Connected to {serverTypeDescription} '{ServerInfo.Version}'.");

            logger.Debug($"Validating the credentials...");

            var credentialResponse = await InvokeRequestAsync<IValidateCredentialsRequest, bool>(token);
            if (!credentialResponse)
            {
                IsConnected = false;
                ServerInfo = null;
                throw new InvalidOperationException("Invalid credentials");
            }

            logger.Debug($"Credentials accepted.");
        }

        public void Disconnect()
        {
            logger.Info($"Disconnecting...");
            logger.Debug($"Current state before disconnecting is '{(IsConnected ? "Connected" : "Disconnected")}'.");

            // Don't dispose the HttpClient when disconnecting. We'll need it if
            // the caller connects to another server.
            IsConnected = false;
            ServerInfo = null;
        }

        private void EnsureIsConnected()
        {
            if (!IsConnected)
            {
                logger.Error("The service is expected to be connected.");
                throw new InvalidOperationException("This operation expects the service to be connected.");
            }
        }

        public async Task<IList<SonarQubeOrganization>> GetAllOrganizationsAsync(CancellationToken token) =>
            await InvokeRequestAsync<IGetOrganizationsRequest, SonarQubeOrganization[]>(
                request =>
                {
                    request.OnlyUserOrganizations = true;
                },
                token);

        public async Task<IList<SonarQubeLanguage>> GetAllLanguagesAsync(CancellationToken token) =>
           await InvokeRequestAsync<IGetLanguagesRequest, SonarQubeLanguage[]>(token);

        public async Task<Stream> DownloadStaticFileAsync(string pluginKey, string fileName, CancellationToken token) =>
            await InvokeRequestAsync<IDownloadStaticFile, Stream>(
                request =>
                {
                    request.PluginKey = pluginKey;
                    request.FileName = fileName;
                },
                token);

        public async Task<IList<SonarQubePlugin>> GetAllPluginsAsync(CancellationToken token) =>
           await InvokeRequestAsync<IGetPluginsRequest, SonarQubePlugin[]>(token);

        public async Task<IList<SonarQubeProject>> GetAllProjectsAsync(string organizationKey, CancellationToken token) =>
            await InvokeRequestAsync<IGetProjectsRequest, SonarQubeProject[]>(
                request =>
                {
                    request.OrganizationKey = GetOrganizationKeyForWebApiCalls(organizationKey, logger);
                },
                token);

        public async Task<IList<SonarQubeProperty>> GetAllPropertiesAsync(string projectKey, CancellationToken token) =>
            await InvokeRequestAsync<IGetPropertiesRequest, SonarQubeProperty[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                },
                token);

        public Uri GetProjectDashboardUrl(string projectKey)
        {
            EnsureIsConnected();

            const string ProjectDashboardRelativeUrl = "dashboard/index/{0}";

            return new Uri(httpClient.BaseAddress, string.Format(ProjectDashboardRelativeUrl, projectKey));
        }

        public async Task<SonarQubeQualityProfile> GetQualityProfileAsync(string projectKey, string organizationKey, SonarQubeLanguage language, CancellationToken token)
        {
            var qualityProfiles = await InvokeRequestAsync<IGetQualityProfilesRequest, SonarQubeQualityProfile[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                    request.OrganizationKey = GetOrganizationKeyForWebApiCalls(organizationKey, logger);
                },
                token);

            // Consider adding the language filter to the request configuration above and removing this line
            var profilesWithGivenLanguage = qualityProfiles.Where(x => x.Language == language.Key).ToList();

            if (profilesWithGivenLanguage.Count == 0)
            {
                throw new InvalidOperationException($"The {language.PluginName} plugin is not installed on the connected SonarQube.");
            }

            var qualityProfile = profilesWithGivenLanguage.Count > 1
                ? profilesWithGivenLanguage.Single(x => x.IsDefault)
                : profilesWithGivenLanguage.Single();

            var changeLog = await InvokeRequestAsync<IGetQualityProfileChangeLogRequest, DateTime[]>(
                request =>
                {
                    request.Page = 1;
                    request.PageSize = 1;
                    request.ItemsLimit = 1;
                    request.QualityProfileKey = qualityProfile.Key;
                    request.QualityProfileName = qualityProfile.Name;
                    request.LanguageName = language.Key;
                    request.OrganizationKey = GetOrganizationKeyForWebApiCalls(organizationKey, logger);
                },
                token);

            var updatedDate = changeLog.Any() ? changeLog.Single() : qualityProfile.TimeStamp;

            return new SonarQubeQualityProfile(qualityProfile.Key, qualityProfile.Name, qualityProfile.Language,
                qualityProfile.IsDefault, updatedDate);
        }

        public async Task<RoslynExportProfileResponse> GetRoslynExportProfileAsync(string qualityProfileName,
            string organizationKey, SonarQubeLanguage language, CancellationToken token) =>
            await InvokeRequestAsync<IGetRoslynExportProfileRequest, RoslynExportProfileResponse>(
                request =>
                {
                    request.QualityProfileName = qualityProfileName;
                    request.LanguageKey = language.Key;
                    request.OrganizationKey = GetOrganizationKeyForWebApiCalls(organizationKey, logger);
                },
                token);

        public async Task<IList<SonarQubeIssue>> GetSuppressedIssuesAsync(string projectKey, CancellationToken token) =>
            await InvokeRequestAsync<IGetIssuesRequest, SonarQubeIssue[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                    request.Statuses = "RESOLVED"; // Resolved issues will be hidden in SLVS
                },
                token);

        public async Task<IList<SonarQubeNotification>> GetNotificationEventsAsync(string projectKey, DateTimeOffset eventsSince,
            CancellationToken token) =>
            await InvokeRequestAsync<IGetNotificationsRequest, SonarQubeNotification[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                    request.EventsSince = eventsSince;
                },
                token);

        public async Task<IList<SonarQubeModule>> GetAllModulesAsync(string projectKey, CancellationToken token) =>
            await InvokeRequestAsync<IGetModulesRequest, SonarQubeModule[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                    request.Qualifiers = "BRC"; // Returns root module (TRK) + sub modules
                },
                token);

        public async Task<IList<SonarQubeRule>> GetRulesAsync(bool isActive, string qualityProfileKey, CancellationToken token) =>
            await InvokeRequestAsync<IGetRulesRequest, SonarQubeRule[]>(
                request =>
                {
                    request.IsActive = isActive;
                    request.QualityProfileKey = qualityProfileKey;
                },
                token);

        public async Task<SonarQubeHotspot> GetHotspotAsync(string hotspotKey, CancellationToken token) =>
            await InvokeRequestAsync<IGetHotspotRequest, SonarQubeHotspot>(
                request =>
                {
                    request.HotspotKey = hotspotKey;
                },
                token);

        public async Task<IList<SonarQubeIssue>> GetTaintVulnerabilitiesAsync(string projectKey, CancellationToken token) =>
            await InvokeRequestAsync<IGetTaintVulnerabilitiesRequest, SonarQubeIssue[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                }, token);

        public Uri GetViewIssueUrl(string projectKey, string issueKey)
        {
            EnsureIsConnected();

            // The URL should be in the same form as the permalink generated by SQ/SC e.g
            // SonarQube : http://localhost:9000/project/issues?id=security1&issues=AXZRhxr-9W_phHQ8Bzgn&open=AXZRhxr-9W_phHQ8Bzgn
            // SonarCloud: https://sonarcloud.io/project/issues?id=sonarlint-visualstudio&issues=AW-EuNQbXwT7-YPcXpll&open=AW-EuNQbXwT7-YPcXpll
            // Versioning: so far the format of the URL is the same across all versions from at least v6.7
            const string ViewIssueRelativeUrl = "project/issues?id={0}&issues={1}&open={1}";

            return new Uri(httpClient.BaseAddress, string.Format(ViewIssueRelativeUrl, projectKey, issueKey));
        }

        public Uri GetViewHotspotUrl(string projectKey, string hotspotKey)
        {
            EnsureIsConnected();

            // The URL should be in the same form as the permalink generated by SQ/SC e.g
            // SonarQube : http://localhost:9000/security_hotspots?id=hotspots1&hotspots=AXYuYJMY6B0Z0V1QHhTB
            // SonarCloud: https://sonarcloud.io/project/security_hotspots?id=SonarSource_infra-buddy-works&hotspots=AXHKPyG0kVC5aeEv8COM
            // Versioning: the other hotspot APIs our SLVS features depend on where introduced in v8.6

            const string SonarQube_ViewHotspotRelativeUrl = "security_hotspots?id={0}&hotspots={1}";
            const string SonarCloud_ViewHotspotRelativeUrl = "project/security_hotspots?id={0}&hotspots={1}";

            var urlFormat = ServerInfo.ServerType == ServerType.SonarCloud ? SonarCloud_ViewHotspotRelativeUrl : SonarQube_ViewHotspotRelativeUrl;

            return new Uri(httpClient.BaseAddress, string.Format(urlFormat, projectKey, hotspotKey));
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            logger.Debug("Disposing SonarQubeService...");
            if (!disposedValue)
            {
                logger.Debug("SonarQubeService was not disposed, continuing with dispose...");
                if (disposing)
                {
                    IsConnected = false;
                    ServerInfo = null;
                    messageHandler.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion // IDisposable Support

        internal /* for testing */ static string GetOrganizationKeyForWebApiCalls(string organizationKey, ILogger logger)
        {
            // Special fake internal key for testing binding to a large number of organizations.
            // If the special key is used we'll pass null for the organization so no filtering will
            // be done.
            const string FakeInternalTestingOrgKey = "sonar.internal.testing.no.org";

            if (FakeInternalTestingOrgKey.Equals(organizationKey, System.StringComparison.OrdinalIgnoreCase))
            {
                logger.Debug($"DEBUG: org key is {FakeInternalTestingOrgKey}. Setting it to null.");
                return null;
            }
            return organizationKey;
        }

    }
}
