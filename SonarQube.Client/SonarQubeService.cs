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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Helpers;
using SonarQube.Client.Logging;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;
using SonarQube.Client.Requests;
using SonarQube.Client.Api;

namespace SonarQube.Client
{
    public class SonarQubeService : ISonarQubeService, IDisposable
    {
        internal const int MaximumPageSize = 500;
        internal static readonly Version OrganizationsFeatureMinimalVersion = new Version(6, 2);

        private readonly HttpMessageHandler messageHandler;
        private readonly RequestFactory requestFactory;
        private readonly string userAgent;
        private readonly ILogger logger;

        private HttpClient httpClient;
        private Version sonarQubeVersion;

        public bool HasOrganizationsFeature
        {
            get
            {
                EnsureIsConnected();

                return sonarQubeVersion >= OrganizationsFeatureMinimalVersion;
            }
        }

        public bool IsConnected { get; private set; }

        public SonarQubeService(HttpMessageHandler messageHandler, string userAgent, ILogger logger)
            : this(messageHandler, DefaultConfiguration.Configure(new RequestFactory(logger)), userAgent, logger)
        {
        }

        public SonarQubeService(HttpMessageHandler messageHandler, RequestFactory requestFactory, string userAgent, ILogger logger)
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

            var request = requestFactory.Create<TRequest>(sonarQubeVersion);
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

            logger.Debug($"Getting the version of SonarQube...");

            var versionResponse = await InvokeRequestAsync<IGetVersionRequest, string>(token);
            sonarQubeVersion = Version.Parse(versionResponse);

            logger.Info($"Connected to SonarQube '{sonarQubeVersion}'.");

            logger.Debug($"Validating the credentials...");

            var credentialResponse = await InvokeRequestAsync<IValidateCredentialsRequest, bool>(token);
            if (!credentialResponse)
            {
                IsConnected = false;
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

        public async Task<IList<SonarQubePlugin>> GetAllPluginsAsync(CancellationToken token) =>
            await InvokeRequestAsync<IGetPluginsRequest, SonarQubePlugin[]>(token);

        public async Task<IList<SonarQubeProject>> GetAllProjectsAsync(string organizationKey, CancellationToken token) =>
            await InvokeRequestAsync<IGetProjectsRequest, SonarQubeProject[]>(
                request =>
                {
                    request.OrganizationKey = organizationKey;
                },
                token);

        public async Task<IList<SonarQubeProperty>> GetAllPropertiesAsync(CancellationToken token) =>
            await InvokeRequestAsync<IGetPropertiesRequest, SonarQubeProperty[]>(token);

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
                    request.OrganizationKey = organizationKey;
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
                    request.QualityProfileKey = qualityProfile.Key;
                    request.QualityProfileName = qualityProfile.Name;
                    request.LanguageName = language.Key;
                    request.OrganizationKey = organizationKey;
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
                    request.OrganizationKey = organizationKey;
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

    }
}
