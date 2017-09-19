using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarQube.Client.Services
{
    public interface ISonarQubeService
    {
        bool HasOrganizationsFeature { get; }

        Task ConnectAsync(Connection connection, CancellationToken token);

        Task<IList<Organization>> GetAllOrganizationsAsync(CancellationToken token);

        Task<IList<Plugin>> GetAllPluginsAsync(CancellationToken token);

        Task<IList<Project>> GetAllProjectsAsync(string organizationKey, CancellationToken token);

        Task<IList<Property>> GetAllPropertiesAsync(CancellationToken token);

        Task<QualityProfile> GetQualityProfileAsync(string projectKey, ServerLanguage language, CancellationToken token);

        Uri GetProjectDashboardUrl(string projectKey);
    }
}
