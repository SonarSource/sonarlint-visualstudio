using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Api;
using SonarQube.Client.Api.Requests;
using SonarQube.Client.Helpers;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;

namespace SonarQube.Client.Services
{
    public class SonarQubeService : ISonarQubeService
    {
        internal const int MaximumPageSize = 500;
        internal static readonly Version OrganizationsFeatureMinimalVersion = new Version(6, 2);

        private readonly HttpMessageHandler messageHandler;
        private readonly RequestFactory requestFactory;
        public readonly string userAgent;

        private HttpClient httpClient;

        private Version sonarQubeVersion = null;

        private bool connected;

        public bool HasOrganizationsFeature
        {
            get
            {
                EnsureIsConnected();

                return sonarQubeVersion >= OrganizationsFeatureMinimalVersion;
            }
        }

        public bool IsConnected => connected;

        public SonarQubeService(HttpMessageHandler messageHandler, RequestFactory requestFactory, string userAgent)
        {
            this.messageHandler = messageHandler;
            this.requestFactory = requestFactory;
            this.userAgent = userAgent;
        }

        private Task<TResponse> InvokeRequestAsync<TRequest, TResponse>(CancellationToken token)
            where TRequest : IRequest<TResponse>
        {
            return InvokeRequestAsync<TRequest, TResponse>(request => { }, token);
        }

        private async Task<TResponse> InvokeRequestAsync<TRequest, TResponse>(Action<TRequest> configure,
            CancellationToken token)
            where TRequest : IRequest<TResponse>
        {
            EnsureIsConnected();

            var request = CreateRequest<TRequest>();
            configure(request);

            var result = await request.InvokeAsync(httpClient, token);

            return result;
        }

        private TRequest CreateRequest<TRequest>()
            where TRequest : IRequest
        {
            return requestFactory.Create<TRequest>(sonarQubeVersion);
        }

        public async Task ConnectAsync(ConnectionInformation connection, CancellationToken token)
        {
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

            connected = true;

            var versionResponse = await InvokeRequestAsync<IGetVersionRequest, string>(token);
            sonarQubeVersion = Version.Parse(versionResponse);

            var credentialResponse = await InvokeRequestAsync<IValidateCredentialsRequest, bool>(token);
            if (!credentialResponse)
            {
                connected = false;
                throw new InvalidOperationException("Invalid credentials");
            }
        }

        public void Disconnect()
        {
            connected = false;
            httpClient.Dispose();
        }

        public Uri GetProjectDashboardUrl(string projectKey)
        {
            EnsureIsConnected();

            const string ProjectDashboardRelativeUrl = "dashboard/index/{0}";

            return new Uri(httpClient.BaseAddress, string.Format(ProjectDashboardRelativeUrl, projectKey));
        }

        public async Task<IList<SonarQubeOrganization>> GetAllOrganizationsAsync(CancellationToken token) =>
            await InvokeRequestAsync<IGetOrganizationsRequest, SonarQubeOrganization[]>(token);

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

            var qualityProfile = profilesWithGivenLanguage.Count > 1
                ? profilesWithGivenLanguage.Single(x => x.IsDefault)
                : profilesWithGivenLanguage.Single();

            var changeLog = await InvokeRequestAsync<IGetQualityProfileChangeLogRequest, DateTime[]>(
                request =>
                {
                    request.Page = 1;
                    request.PageSize = 1;
                    request.QualityProfileKey = qualityProfile.Key;
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

        public async Task<IList<SonarQubeIssue>> GetSuppressedIssuesAsync(string key, CancellationToken token)
        {
            var result = await InvokeRequestAsync<IGetIssuesRequest, SonarQubeIssue[]>(
                request =>
                {
                    request.ProjectKey = key;
                },
                token);

            return result
                .Where(x => x.ResolutionState != SonarQubeIssueResolutionState.Unresolved)
                .ToList();
        }

        public async Task<IList<SonarQubeNotification>> GetNotificationEventsAsync(string projectKey, DateTimeOffset eventsSince,
            CancellationToken token) =>
            await InvokeRequestAsync<IGetNotificationsRequest, SonarQubeNotification[]>(
                request =>
                {
                    request.ProjectKey = projectKey;
                    request.EventsSince = eventsSince;
                },
                token);

        private void EnsureIsConnected()
        {
            if (!connected)
            {
                throw new InvalidOperationException("This operation expects the service to be connected.");
            }
        }
    }
}
