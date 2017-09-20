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

        Task ConnectAsync(ConnectionInformation connection, CancellationToken token);

        Task<IList<Organization>> GetAllOrganizationsAsync(CancellationToken token);

        Task<IList<SonarQubePlugin>> GetAllPluginsAsync(CancellationToken token);

        Task<IList<SonarQubeProject>> GetAllProjectsAsync(string organizationKey, CancellationToken token);

        Task<IList<SonarQubeProperty>> GetAllPropertiesAsync(CancellationToken token);

        Uri GetProjectDashboardUrl(string projectKey);

        Task<QualityProfile> GetQualityProfileAsync(string projectKey, ServerLanguage language, CancellationToken token);

        Task<RoslynExportProfile> GetRoslynExportProfileAsync(string qualityProfileName, ServerLanguage language,
            CancellationToken token);
    }
}
