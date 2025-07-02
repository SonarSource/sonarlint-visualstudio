/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarQube.Client.Helpers;
using IConnectionCredentials = SonarLint.VisualStudio.Core.Binding.IConnectionCredentials;

namespace SonarLint.VisualStudio.ConnectedMode;

public interface ISlCoreConnectionAdapter
{
    Task<ResponseStatus> ValidateConnectionAsync(ConnectionInfo connectionInfo, ICredentialsModel credentialsModel);

    Task<ResponseStatusWithData<List<OrganizationDisplay>>> GetOrganizationsAsync(ICredentialsModel credentialsModel, CloudServerRegion cloudServerRegion);


    Task<ResponseStatusWithData<ServerProject>> GetServerProjectByKeyAsync(ServerConnection serverConnection, string serverProjectKey);

    Task<ResponseStatusWithData<List<ServerProject>>> GetAllProjectsAsync(ServerConnection serverConnection);

    Task<ResponseStatusWithData<List<ServerProject>>> FuzzySearchProjectsAsync(ServerConnection serverConnection, string searchTerm);

    Task<ResponseStatusWithData<string>> GenerateTokenAsync(ConnectionInfo connectionInfo, CancellationToken cancellationToken);
}

[Export(typeof(ISlCoreConnectionAdapter))]
[method: ImportingConstructor]
public class SlCoreConnectionAdapter(ISLCoreServiceProvider serviceProvider, IThreadHandling threadHandling, ILogger logger) : ISlCoreConnectionAdapter
{
    private static readonly ResponseStatusWithData<List<OrganizationDisplay>> FailedResponseWithData = new(false, []);
    private static readonly ResponseStatus FailedResponse = new(false);
    private readonly ILogger logger = logger.ForVerboseContext(nameof(SlCoreConnectionAdapter));

    public async Task<ResponseStatus> ValidateConnectionAsync(ConnectionInfo connectionInfo, ICredentialsModel credentialsModel)
    {
        var credentials = credentialsModel.ToICredentials();

        var validateConnectionParams = new ValidateConnectionParams(GetTransientConnectionDto(connectionInfo, credentials));
        return await ValidateConnectionAsync(validateConnectionParams);
    }

    public Task<ResponseStatusWithData<List<OrganizationDisplay>>> GetOrganizationsAsync(ICredentialsModel credentialsModel, CloudServerRegion cloudServerRegion) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!TryGetConnectionConfigurationSlCoreService(out var connectionConfigurationSlCoreService))
            {
                return FailedResponseWithData;
            }

            try
            {
                var credentials = MapCredentials(credentialsModel?.ToICredentials());
                var response = await connectionConfigurationSlCoreService.ListUserOrganizationsAsync(new ListUserOrganizationsParams(credentials, cloudServerRegion.ToSlCoreRegion()));
                var organizationDisplays = response.userOrganizations.Select(o => new OrganizationDisplay(o.key, o.name)).ToList();

                return new ResponseStatusWithData<List<OrganizationDisplay>>(true, organizationDisplays);
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"{Resources.ListUserOrganizations_Fails}: {ex.Message}");
                return FailedResponseWithData;
            }
        });

    public Task<ResponseStatusWithData<ServerProject>> GetServerProjectByKeyAsync(ServerConnection serverConnection, string serverProjectKey)
    {
        var failedResponse = new ResponseStatusWithData<ServerProject>(false, null);

        return threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!TryGetConnectionConfigurationSlCoreService(out var connectionConfigurationSlCoreService))
            {
                return failedResponse;
            }

            try
            {
                var transientConnection = GetTransientConnectionDto(serverConnection);
                var response = await connectionConfigurationSlCoreService.GetProjectNamesByKeyAsync(new GetProjectNamesByKeyParams(transientConnection, [serverProjectKey]));

                if (response.projectNamesByKey.TryGetValue(serverProjectKey, out var projectName) && projectName == null)
                {
                    logger.LogVerbose(Resources.GetServerProjectByKey_ProjectNotFound, serverProjectKey);
                    return failedResponse;
                }

                return new ResponseStatusWithData<ServerProject>(true, new ServerProject(serverProjectKey, response.projectNamesByKey[serverProjectKey]));
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"{Resources.GetServerProjectByKey_Fails}: {ex.Message}");
                return failedResponse;
            }
        });
    }

    public async Task<ResponseStatusWithData<List<ServerProject>>> GetAllProjectsAsync(ServerConnection serverConnection)
    {
        var validateConnectionParams = new GetAllProjectsParams(GetTransientConnectionDto(serverConnection));
        return await GetAllProjectsAsync(validateConnectionParams);
    }

    public async Task<ResponseStatusWithData<List<ServerProject>>> FuzzySearchProjectsAsync(ServerConnection serverConnection, string searchTerm)
    {
        var failedResponse = new ResponseStatusWithData<List<ServerProject>>(false, []);
        return await threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!TryGetConnectionConfigurationSlCoreService(out var connectionConfigurationSlCoreService))
            {
                return failedResponse;
            }

            try
            {
                var fuzzySearchParams = new FuzzySearchProjectsParams(serverConnection.Id, searchTerm);
                var slCoreResponse = await connectionConfigurationSlCoreService.FuzzySearchProjectsAsync(fuzzySearchParams);
                var serverProjects = slCoreResponse.topResults.Select(proj => new ServerProject(proj.key, proj.name)).ToList();
                return new ResponseStatusWithData<List<ServerProject>>(true, serverProjects);
            }
            catch (Exception ex)
            {
                logger.LogVerbose(Resources.FuzzySearchProjects_Fails, serverConnection.Id, searchTerm, ex.Message);
                return failedResponse;
            }
        });
    }

    public async Task<ResponseStatusWithData<string>> GenerateTokenAsync(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
    {
        var failedResponse = new ResponseStatusWithData<string>(false, null);
        return await threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!TryGetConnectionConfigurationSlCoreService(out var connectionConfigurationSlCoreService))
            {
                return failedResponse;
            }

            var serverUri = connectionInfo.ServerType == ConnectionServerType.SonarCloud
                ? connectionInfo.CloudServerRegion.Url.ToString()
                : connectionInfo.Id;

            try
            {
                var utmContent = connectionInfo.ServerType == ConnectionServerType.SonarCloud ? "create-edit-sqc-connection" : "create-edit-sqs-connection";
                var utm = new Utm(utmContent, "generate-token");
                var slCoreResponse = await connectionConfigurationSlCoreService.HelpGenerateUserTokenAsync(new HelpGenerateUserTokenParams(serverUri, utm), cancellationToken);
                return new ResponseStatusWithData<string>(true, slCoreResponse.token);
            }
            catch (Exception ex)
            {
                logger.LogVerbose(Resources.GenerateToken_Fails, serverUri, ex.Message);
                return failedResponse;
            }
        });
    }

    private async Task<ResponseStatus> ValidateConnectionAsync(ValidateConnectionParams validateConnectionParams) =>
        await threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!TryGetConnectionConfigurationSlCoreService(out var connectionConfigurationSlCoreService))
            {
                return FailedResponse;
            }

            try
            {
                var slCoreResponse = await connectionConfigurationSlCoreService.ValidateConnectionAsync(validateConnectionParams);
                return new ResponseStatus(slCoreResponse.success);
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"{Resources.ValidateCredentials_Fails}: {ex.Message}");
                return FailedResponse;
            }
        });

    private async Task<ResponseStatusWithData<List<ServerProject>>> GetAllProjectsAsync(GetAllProjectsParams getAllProjectsParams)
    {
        var failedResponse = new ResponseStatusWithData<List<ServerProject>>(false, []);
        return await threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!TryGetConnectionConfigurationSlCoreService(out var connectionConfigurationSlCoreService))
            {
                return failedResponse;
            }

            try
            {
                var slCoreResponse = await connectionConfigurationSlCoreService.GetAllProjectsAsync(getAllProjectsParams);
                var serverProjects = slCoreResponse.sonarProjects.Select(proj => new ServerProject(proj.key, proj.name)).ToList();
                return new ResponseStatusWithData<List<ServerProject>>(true, serverProjects);
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"{Resources.GetAllProjects_Fails}: {ex.Message}");
                return failedResponse;
            }
        });
    }

    private bool TryGetConnectionConfigurationSlCoreService(out IConnectionConfigurationSLCoreService connectionConfigurationSlCoreService)
    {
        if (serviceProvider.TryGetTransientService(out IConnectionConfigurationSLCoreService slCoreService))
        {
            connectionConfigurationSlCoreService = slCoreService;
            return true;
        }

        connectionConfigurationSlCoreService = null;
        logger.LogVerbose($"[{nameof(IConnectionConfigurationSLCoreService)}] {SLCoreStrings.ServiceProviderNotInitialized}");
        return false;
    }

    private static Either<TransientSonarQubeConnectionDto, TransientSonarCloudConnectionDto> GetTransientConnectionDto(ConnectionInfo connectionInfo, IConnectionCredentials credentials)
    {
        var credentialsDto = MapCredentials(credentials);

        return connectionInfo.ServerType switch
        {
            ConnectionServerType.SonarQube => new TransientSonarQubeConnectionDto(connectionInfo.Id, credentialsDto),
            ConnectionServerType.SonarCloud => new TransientSonarCloudConnectionDto(connectionInfo.Id, credentialsDto, connectionInfo.CloudServerRegion.ToSlCoreRegion()),
            _ => throw new ArgumentException(Resources.UnexpectedConnectionType)
        };
    }

    private static Either<TransientSonarQubeConnectionDto, TransientSonarCloudConnectionDto> GetTransientConnectionDto(ServerConnection serverConnection)
    {
        var credentials = MapCredentials(serverConnection.Credentials);

        return serverConnection switch
        {
            ServerConnection.SonarQube sonarQubeConnection => new TransientSonarQubeConnectionDto(sonarQubeConnection.Id, credentials),
            ServerConnection.SonarCloud sonarCloudConnection => new TransientSonarCloudConnectionDto(sonarCloudConnection.OrganizationKey, credentials, sonarCloudConnection.Region.ToSlCoreRegion()),
            _ => throw new ArgumentException(Resources.UnexpectedConnectionType)
        };
    }

    private static Either<TokenDto, UsernamePasswordDto> MapCredentials(IConnectionCredentials credentials) =>
        credentials switch
        {
            UsernameAndPasswordCredentials basicAuthCredentials => new UsernamePasswordDto(basicAuthCredentials.UserName, basicAuthCredentials.Password.ToUnsecureString()),
            TokenAuthCredentials tokenAuthCredentials => new TokenDto(tokenAuthCredentials.Token.ToUnsecureString()),
            _ => throw new ArgumentException($"Unexpected {nameof(ICredentialsModel)} argument")
        };
}
