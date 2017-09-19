using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarQube.Client
{
    public class SonarQubeService
    {
        internal const int MaximumPageSize = 500;
        internal readonly Version OrganizationsFeatureMinimalVersion = new Version(6, 2);

        private readonly ISonarQubeClient sonarqubeClient;

        private ConnectionDTO connection;
        private bool isConnected;
        private Version serverVersion;

        public SonarQubeService(ISonarQubeClient sonarqubeClient)
        {
            this.sonarqubeClient = sonarqubeClient;
        }

        public bool HasOrganizationsFeature
        {
            get
            {
                if (!this.isConnected)
                {
                    throw new InvalidOperationException("This operation expects the service to be connected.");
                }

                return this.serverVersion >= OrganizationsFeatureMinimalVersion;
            }
        }

        public async Task ConnectAsync(Connection connection, CancellationToken token)
        {
            if (this.isConnected)
            {
                throw new InvalidOperationException("This operation expects the service to be connected.");
            }

            var connectionDto = new ConnectionDTO
            {
                Authentication = connection.Authentication,
                ServerUri = connection.ServerUri,
                Login = connection.Login,
                Password = connection.Password
            };

            var credentialsValidationResult = await this.sonarqubeClient.ValidateCredentialsAsync(connectionDto, token);
            if (credentialsValidationResult.IsFailure)
            {

                // TODO: something went wrong
            }
            if (!credentialsValidationResult.Value.AreValid)
            {
                // TODO: credentials are not valid
            }

            var versionResult = await this.sonarqubeClient.GetVersionAsync(connectionDto, token);
            if (versionResult.IsFailure)
            {
                // TODO: something went wrong
            }

            Version version;
            if (!Version.TryParse(versionResult.Value.Version, out version))
            {
                // TODO: version is not valid
            }

            this.connection = connectionDto;
            this.serverVersion = version;
            this.isConnected = true;
        }
        public async Task<IList<Organization>> GetAllOrganizationsAsync(CancellationToken token)
        {
            if (!this.isConnected)
            {
                throw new InvalidOperationException("This operation expects the service to be connected.");
            }

            int currentPage = 1;
            var allOrganizations = new List<OrganizationDTO>();
            Result<OrganizationDTO[]> organizationsResult;

            do
            {
                var request = new OrganizationRequest { Page = currentPage, PageSize = MaximumPageSize };
                organizationsResult = await this.sonarqubeClient.GetOrganizationsAsync(this.connection, request, token);

                if (organizationsResult.IsFailure)
                {
                    // TODO: something went wrong
                }
                else
                {
                    allOrganizations.AddRange(organizationsResult.Value);
                }

                currentPage++;
            }
            while (organizationsResult.Value.Length > 0);

            return allOrganizations.Select(Organization.FromDto).ToList();
        }

        public async Task<IList<Plugin>> GetAllPluginsAsync(CancellationToken token)
        {
            if (!this.isConnected)
            {
                throw new InvalidOperationException("This operation expects the service to be connected.");
            }

            var pluginsResult = await this.sonarqubeClient.GetPluginsAsync(this.connection, token);
            if (pluginsResult.IsFailure)
            {
                // TODO: something went wrong
            }

            return pluginsResult.Value.Select(Plugin.FromDto).ToList();
        }

        public async Task<IList<Project>> GetAllProjects(string organizationKey, CancellationToken token)
        {
            if (!this.isConnected)
            {
                throw new InvalidOperationException("This operation expects the service to be connected.");
            }

            if (organizationKey == null) // Orgs are not supported or not enabled
            {
                var projectsResult = await this.sonarqubeClient.GetProjectsAsync(this.connection, token);
                if (projectsResult.IsFailure)
                {
                    // TODO: something went wrong
                }

                return projectsResult.Value.Select(Project.FromDto).ToList();
            }

            int currentPage = 1;
            var allProjects = new List<ComponentDTO>();
            Result<ComponentDTO[]> componentsResult;

            do
            {
                var request = new ComponentRequest { Page = currentPage, PageSize = MaximumPageSize };
                componentsResult = await this.sonarqubeClient.GetComponentsSearchProjectsAsync(this.connection,
                    request, token);

                if (componentsResult.IsFailure)
                {
                    // TODO: something went wrong
                }
                else
                {
                    allProjects.AddRange(componentsResult.Value);
                }

                currentPage++;
            }
            while (componentsResult.Value.Length > 0);

            return allProjects.Select(Project.FromDto).ToList();
        }

        public async Task<IList<Property>> GetAllPropertiesAsync(CancellationToken token)
        {
            if (!this.isConnected)
            {
                throw new InvalidOperationException("This operation expects the service to be connected.");
            }

            var propertiesResult = await this.sonarqubeClient.GetPropertiesAsync(this.connection, token);
            if (propertiesResult.IsFailure)
            {
                // TODO: something went wrong
            }

            return propertiesResult.Value.Select(Property.FromDto).ToList();
        }

        public async Task<QualityProfile> GetQualityProfileAsync(string projectKey, ServerLanguage language,
            CancellationToken token)
        {
            if (!this.isConnected)
            {
                throw new InvalidOperationException("This operation expects the service to be connected.");
            }

            var qualityProfileRequest = new QualityProfileRequest { ProjectKey = projectKey };
            var qualityProfilesResult = await this.sonarqubeClient.GetQualityProfilesAsync(this.connection,
                qualityProfileRequest, token);
            if (qualityProfilesResult.IsFailure)
            {
                // Special handling for the case when a project was not analyzed yet, in which case a 404 is returned
                // TODO: Request the profile without the project

                // TODO: something went wrong
            }

            var profilesWithGivenLanguage = qualityProfilesResult.Value.Where(x => x.Language == language.Key).ToList();

            var qualityProfile = profilesWithGivenLanguage.Count > 1
                ? profilesWithGivenLanguage.Single(x => x.IsDefault)
                : profilesWithGivenLanguage.Single();

            var qualityProfileChangeLogRequest = new QualityProfileChangeLogRequest { PageSize = 1, QualityProfileKey = qualityProfile.Key };
            var changeLog = await this.sonarqubeClient.GetQualityProfileChangeLogAsync(this.connection, qualityProfileChangeLogRequest, token);
            if (changeLog.IsFailure)
            {
                // TODO: something went wrong
            }

            return QualityProfile.FromDto(qualityProfile, changeLog.Value.Events.Single().Date);
        }

        public Uri GetProjectDashboardUrl(string projectKey)
        {
            if (!this.isConnected)
            {
                throw new InvalidOperationException("This operation expects the service to be connected.");
            }

            const string ProjectDashboardRelativeUrl = "dashboard/index/{0}";
            return new Uri(this.connection.ServerUri, string.Format(ProjectDashboardRelativeUrl, projectKey));
        }

        private void HandleFailures<T>(Result<T> result, CancellationToken token)
        {
            if (result.Exception is TaskCanceledException)
            {
                if (token.IsCancellationRequested)
                {
                    // TODO: user cancellation
                }
                else
                {
                    // TODO: timeout
                }
            }

            // TODO: others
        }
    }
}
