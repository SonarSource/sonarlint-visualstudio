using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarQube.Client.Services
{
    public interface ISonarQubeClient
    {
        /// <summary>
        ///
        /// </summary>
        Task<Result<ComponentDTO[]>> GetComponentsSearchProjectsAsync(ConnectionDTO connection,
            ComponentRequest request, CancellationToken token);

        /// <summary>
        ///     Retrieves all organizations from the given SonarQube server.
        /// </summary>
        Task<Result<OrganizationDTO[]>> GetOrganizationsAsync(ConnectionDTO connection,
            OrganizationRequest request, CancellationToken token);

        /// <summary>
        ///     Retrieves all plugins installed on the given SonarQube server.
        /// </summary>
        /// <returns></returns>
        Task<Result<PluginDTO[]>> GetPluginsAsync(ConnectionDTO connection, CancellationToken token);

        /// <summary>
        ///     Retrieves all the projects from the given SonarQube server.
        /// </summary>
        Task<Result<ProjectDTO[]>> GetProjectsAsync(ConnectionDTO connection, CancellationToken token);

        /// <summary>
        ///     Retrieves all the properties for the given SonarQube server.
        /// </summary>
        Task<Result<PropertyDTO[]>> GetPropertiesAsync(ConnectionDTO connection, CancellationToken token);

        /// <summary>
        ///     Retrieves the change log for the given quality profile.
        /// </summary>
        Task<Result<QualityProfileChangeLogDTO>> GetQualityProfileChangeLogAsync(ConnectionDTO connection,
            QualityProfileChangeLogRequest request, CancellationToken token);

        /// <summary>
        ///     Retrieves the quality profile for the specified project and language.
        /// </summary>
        Task<Result<QualityProfileDTO[]>> GetQualityProfilesAsync(ConnectionDTO connection,
           QualityProfileRequest request, CancellationToken token);

        /// <summary>
        ///     Retrieves the server's Roslyn Quality Profile export for the specified profile and language
        /// </summary>
        /// <remarks>
        ///     The export contains everything required to configure the solution to match the SonarQube server
        ///     analysis, including: the Code Analysis rule set, analyzer NuGet packages, and any other additional
        ///     files for the analyzers.
        /// </remarks>
        Task<Result<RoslynExportProfile>> GetRoslynExportProfileAsync(ConnectionDTO connection,
            RoslynExportProfileRequest request, CancellationToken token);

        /// <summary>
        ///     Retrieves the version of the given SonarQube server.
        /// </summary>
        Task<Result<VersionDTO>> GetVersionAsync(ConnectionDTO connection, CancellationToken token);

        /// <summary>
        ///     Validates the given credentials on the given SonarQube server.
        /// </summary>
        Task<Result<CredentialsDTO>> ValidateCredentialsAsync(ConnectionDTO connection, CancellationToken token);
    }
}
