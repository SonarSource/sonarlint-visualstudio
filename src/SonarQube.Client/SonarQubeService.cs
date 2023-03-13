/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarQube.Client.Api;
using SonarQube.Client.Helpers;
using SonarQube.Client.Logging;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Requests;

namespace SonarQube.Client
{
    public class SonarQubeService : ISonarQubeService, IDisposable
    {
        private readonly HttpMessageHandler messageHandler;
        private IRequestFactory requestFactory;
        private readonly string userAgent;
        private readonly ILogger logger;
        private readonly IRequestFactorySelector requestFactorySelector;
        private readonly ISecondaryIssueHashUpdater secondaryIssueHashUpdater;
        private readonly ISSEStreamReaderFactory sseStreamReaderFactory;

        private HttpClient httpClient;
        private ServerInfo currentServerInfo;

        public async Task<bool> HasOrganizations(CancellationToken token)
        {
            EnsureIsConnected();

            var hasOrganisations = httpClient.BaseAddress.Host.Equals("sonarcloud.io", StringComparison.OrdinalIgnoreCase);

            return await Task.FromResult<bool>(hasOrganisations);
        }

        public bool IsConnected => GetServerInfo() != null;

        public ServerInfo GetServerInfo() => currentServerInfo;

        public SonarQubeService(HttpMessageHandler messageHandler, string userAgent, ILogger logger)
            : this(messageHandler, userAgent, logger, new RequestFactorySelector(), new SecondaryLocationHashUpdater(), new SSEStreamReaderFactory(logger))
        {
        }

        internal /* for testing */ SonarQubeService(HttpMessageHandler messageHandler, string userAgent, ILogger logger,
            IRequestFactorySelector requestFactorySelector,
            ISecondaryIssueHashUpdater secondaryIssueHashUpdater,
            ISSEStreamReaderFactory sseStreamReaderFactory)
        {
            if (messageHandler == null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
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
            this.userAgent = userAgent;
            this.logger = logger;

            this.requestFactorySelector = requestFactorySelector;
            this.secondaryIssueHashUpdater = secondaryIssueHashUpdater;
            this.sseStreamReaderFactory = sseStreamReaderFactory;
        }

        /// <summary>
        /// Convenience overload for requests that do not need configuration.
        /// </summary>
        private Task<TResponse> InvokeCheckedRequestAsync<TRequest, TResponse>(CancellationToken token)
            where TRequest : IRequest<TResponse>
        {
            return InvokeCheckedRequestAsync<TRequest, TResponse>(request => { }, token);
        }

        /// <summary>
        /// Creates a new instance of the specified TRequest request, configures and invokes it and returns its response.
        /// </summary>
        /// <typeparam name="TRequest">The request interface to invoke.</typeparam>
        /// <typeparam name="TResponse">The type of the request response result.</typeparam>
        /// <param name="configure">Action that configures a type instance that implements TRequest.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns the result of the request invocation.</returns>
        private Task<TResponse> InvokeCheckedRequestAsync<TRequest, TResponse>(Action<TRequest> configure,
            CancellationToken token)
            where TRequest : IRequest<TResponse>
        {
            EnsureIsConnected();

            return InvokeUncheckedRequestAsync<TRequest, TResponse>(configure, token);
        }

        /// <summary>
        /// Executes the call without checking whether the connection to the server has been established. This should only normally be used directly while connecting.
        /// Other uses should call <see cref="InvokeCheckedRequestAsync{TRequest,TResponse}(System.Threading.CancellationToken)"/>.
        /// </summary>
        protected virtual async Task<TResponse> InvokeUncheckedRequestAsync<TRequest, TResponse>(Action<TRequest> configure, CancellationToken token)
            where TRequest : IRequest<TResponse>
        {
            var request = requestFactory.Create<TRequest>(currentServerInfo);
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

            requestFactory = requestFactorySelector.Select(connection.IsSonarCloud, logger);

            try
            {
                var serverTypeDescription = connection.IsSonarCloud ? "SonarCloud" : "SonarQube";

                logger.Debug($"Getting the version of {serverTypeDescription}...");

                var versionResponse = await InvokeUncheckedRequestAsync<IGetVersionRequest, string>(request => { }, token);
                var serverInfo = new ServerInfo(Version.Parse(versionResponse), connection.IsSonarCloud ? ServerType.SonarCloud : ServerType.SonarQube);

                logger.Info($"Connected to {serverTypeDescription} '{serverInfo.Version}'.");

                logger.Debug($"Validating the credentials...");

                var credentialResponse = await InvokeUncheckedRequestAsync<IValidateCredentialsRequest, bool>(request => { }, token);
                if (!credentialResponse)
                {
                    throw new InvalidOperationException("Invalid credentials");
                }

                logger.Debug($"Credentials accepted.");

                currentServerInfo = serverInfo;
            }
            catch
            {
                currentServerInfo = null;
                throw;
            }
        }

        public void Disconnect()
        {
            logger.Info($"Disconnecting...");
            logger.Debug($"Current state before disconnecting is '{(IsConnected ? "Connected" : "Disconnected")}'.");

            // Don't dispose the HttpClient when disconnecting. We'll need it if
            // the caller connects to another server.
            currentServerInfo = null;
            requestFactory = null;
        }

        protected virtual ServerInfo EnsureIsConnected()
        {
            var serverInfo = GetServerInfo();

            if (serverInfo == null)
            {
                logger.Error("The service is expected to be connected.");
                throw new InvalidOperationException("This operation expects the service to be connected.");
            }

            return serverInfo;
        }

        public async Task<IList<SonarQubeOrganization>> GetAllOrganizationsAsync(CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetOrganizationsRequest, SonarQubeOrganization[]>(
                request =>
                {
                    request.OnlyUserOrganizations = true;
                },
                token);

        public async Task<IList<SonarQubeLanguage>> GetAllLanguagesAsync(CancellationToken token) =>
           await InvokeCheckedRequestAsync<IGetLanguagesRequest, SonarQubeLanguage[]>(token);

        public async Task<Stream> DownloadStaticFileAsync(string pluginKey, string fileName, CancellationToken token) =>
            await InvokeCheckedRequestAsync<IDownloadStaticFile, Stream>(
                request =>
                {
                    request.PluginKey = pluginKey;
                    request.FileName = fileName;
                },
                token);

        public async Task<IList<SonarQubePlugin>> GetAllPluginsAsync(CancellationToken token) =>
           await InvokeCheckedRequestAsync<IGetPluginsRequest, SonarQubePlugin[]>(token);

        public async Task<IList<SonarQubeProject>> GetAllProjectsAsync(string organizationKey, CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetProjectsRequest, SonarQubeProject[]>(
                request =>
                {
                    request.OrganizationKey = GetOrganizationKeyForWebApiCalls(organizationKey, logger);
                },
                token);

        public async Task<IList<SonarQubeProperty>> GetAllPropertiesAsync(string projectKey, CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetPropertiesRequest, SonarQubeProperty[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                },
                token);

        public virtual Uri GetProjectDashboardUrl(string projectKey)
        {
            var serverInfo = EnsureIsConnected();

            const string SonarQube_ProjectDashboardRelativeUrl = "dashboard/index/{0}";
            const string SonarCloud_ProjectDashboardRelativeUrl = "project/overview?id={0}";

            var urlFormat = serverInfo.ServerType == ServerType.SonarCloud ? SonarCloud_ProjectDashboardRelativeUrl : SonarQube_ProjectDashboardRelativeUrl;

            return new Uri(httpClient.BaseAddress, string.Format(urlFormat, projectKey));
        }

        public async Task<SonarQubeQualityProfile> GetQualityProfileAsync(string projectKey, string organizationKey, SonarQubeLanguage language, CancellationToken token)
        {
            var qualityProfiles = await InvokeCheckedRequestAsync<IGetQualityProfilesRequest, SonarQubeQualityProfile[]>(
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

            var changeLog = await InvokeCheckedRequestAsync<IGetQualityProfileChangeLogRequest, DateTime[]>(
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
            await InvokeCheckedRequestAsync<IGetRoslynExportProfileRequest, RoslynExportProfileResponse>(
                request =>
                {
                    request.QualityProfileName = qualityProfileName;
                    request.LanguageKey = language.Key;
                    request.OrganizationKey = GetOrganizationKeyForWebApiCalls(organizationKey, logger);
                },
                token);

        public async Task<IList<SonarQubeIssue>> GetSuppressedIssuesAsync(string projectKey, string branch,
            string[] issueKeys, CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetIssuesRequest, SonarQubeIssue[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                    request.Branch = branch;
                    request.IssueKeys = issueKeys;
                    request.Statuses = "RESOLVED"; // Resolved issues will be hidden in SLVS
                },
                token);

        public async Task<IList<SonarQubeNotification>> GetNotificationEventsAsync(string projectKey, DateTimeOffset eventsSince,
            CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetNotificationsRequest, SonarQubeNotification[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                    request.EventsSince = eventsSince;
                },
                token);

        public async Task<IList<SonarQubeModule>> GetAllModulesAsync(string projectKey, CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetModulesRequest, SonarQubeModule[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                    request.Qualifiers = "BRC"; // Returns root module (TRK) + sub modules
                },
                token);

        public async Task<IList<SonarQubeRule>> GetRulesAsync(bool isActive, string qualityProfileKey, CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetRulesRequest, SonarQubeRule[]>(
                request =>
                {
                    request.IsActive = isActive;
                    request.QualityProfileKey = qualityProfileKey;
                },
        token);

        public async Task<SonarQubeRule> GetRuleByKeyAsync(string ruleKey, CancellationToken token)
        {
            var rules = await InvokeCheckedRequestAsync<IGetRulesRequest, SonarQubeRule[]>(request => { request.RuleKey = ruleKey; }, token);
            Debug.Assert(rules.Length <= 1);

            return rules.FirstOrDefault();
        }

        public async Task<SonarQubeHotspot> GetHotspotAsync(string hotspotKey, CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetHotspotRequest, SonarQubeHotspot>(
                request =>
                {
                    request.HotspotKey = hotspotKey;
                },
                token);

        public async Task<IList<SonarQubeIssue>> GetTaintVulnerabilitiesAsync(string projectKey, string branch, CancellationToken token)
        {
            var issues = await InvokeCheckedRequestAsync<IGetTaintVulnerabilitiesRequest, SonarQubeIssue[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                    request.Branch = branch;
                }, token);

            await secondaryIssueHashUpdater.UpdateHashesAsync(issues, this, token);
            return issues;
        }

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
            var serverInfo = EnsureIsConnected();

            // The URL should be in the same form as the permalink generated by SQ/SC e.g
            // SonarQube : http://localhost:9000/security_hotspots?id=hotspots1&hotspots=AXYuYJMY6B0Z0V1QHhTB
            // SonarCloud: https://sonarcloud.io/project/security_hotspots?id=SonarSource_infra-buddy-works&hotspots=AXHKPyG0kVC5aeEv8COM
            // Versioning: the other hotspot APIs our SLVS features depend on where introduced in v8.6

            const string SonarQube_ViewHotspotRelativeUrl = "security_hotspots?id={0}&hotspots={1}";
            const string SonarCloud_ViewHotspotRelativeUrl = "project/security_hotspots?id={0}&hotspots={1}";

            var urlFormat = serverInfo.ServerType == ServerType.SonarCloud ? SonarCloud_ViewHotspotRelativeUrl : SonarQube_ViewHotspotRelativeUrl;

            return new Uri(httpClient.BaseAddress, string.Format(urlFormat, projectKey, hotspotKey));
        }

        public async Task<string> GetSourceCodeAsync(string fileKey, CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetSourceCodeRequest, string>(
                request =>
                {
                    request.FileKey = fileKey;
                },
                token);

        public async Task<IList<SonarQubeProjectBranch>> GetProjectBranchesAsync(string projectKey, CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetProjectBranchesRequest, SonarQubeProjectBranch[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                }, token);

        public async Task<ServerExclusions> GetServerExclusions(string projectKey, CancellationToken token) =>
            await InvokeCheckedRequestAsync<IGetExclusionsRequest, ServerExclusions>(
                request =>
                {
                    request.ProjectKey = projectKey;
                }, token);

        public async Task<ISSEStreamReader> CreateSSEStreamReader(string projectKey, CancellationToken token)
        {
            var networkStream = await InvokeCheckedRequestAsync<IGetSonarLintEventStream, Stream>(
                request =>
                {
                    request.ProjectKey = projectKey;
                },
                token);

            return sseStreamReaderFactory.Create(networkStream, token);
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
                    currentServerInfo = null;
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
