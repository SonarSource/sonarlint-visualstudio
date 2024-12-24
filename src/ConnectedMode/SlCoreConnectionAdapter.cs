﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode;

public interface ISlCoreConnectionAdapter
{
    Task<AdapterResponse> ValidateConnectionAsync(ConnectionInfo connectionInfo, ICredentialsModel credentialsModel);
    Task<AdapterResponseWithData<List<OrganizationDisplay>>> GetOrganizationsAsync(ICredentialsModel credentialsModel);
    Task<AdapterResponseWithData<ServerProject>> GetServerProjectByKeyAsync(ServerConnection serverConnection, string serverProjectKey);
    Task<AdapterResponseWithData<List<ServerProject>>> GetAllProjectsAsync(ServerConnection serverConnection);
    Task<AdapterResponseWithData<List<ServerProject>>> FuzzySearchProjectsAsync(ServerConnection serverConnection, string searchTerm);
}

public class AdapterResponseWithData<T>(bool success, T responseData) : IResponseStatus
{
    public AdapterResponseWithData() : this(false, default) { }
    public bool Success { get; init; } = success;
    public T ResponseData { get; } = responseData;
}

public class AdapterResponse(bool success) : IResponseStatus
{
    public AdapterResponse(): this(false){}
    public bool Success { get; } = success;
}

[Export(typeof(ISlCoreConnectionAdapter))]
public class SlCoreConnectionAdapter : ISlCoreConnectionAdapter
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private static readonly AdapterResponseWithData<List<OrganizationDisplay>> FailedResponseWithData = new(false, []);
    private static readonly AdapterResponse FailedResponse = new(false);

    [ImportingConstructor]
    public SlCoreConnectionAdapter(ISLCoreServiceProvider serviceProvider, IThreadHandling threadHandling, ILogger logger)
    {
        this.serviceProvider = serviceProvider;
        this.threadHandling = threadHandling;
        this.logger = logger;
    }

    public async Task<AdapterResponse> ValidateConnectionAsync(ConnectionInfo connectionInfo, ICredentialsModel credentialsModel)
    {
        var credentials = credentialsModel.ToICredentials();
        
        var validateConnectionParams = new ValidateConnectionParams(GetTransientConnectionDto(connectionInfo, credentials));
        return await ValidateConnectionAsync(validateConnectionParams);
    }

    public Task<AdapterResponseWithData<List<OrganizationDisplay>>> GetOrganizationsAsync(ICredentialsModel credentialsModel)
    {
        return threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!TryGetConnectionConfigurationSlCoreService(out var connectionConfigurationSlCoreService))
            {
                return FailedResponseWithData;
            }

            try
            {
                var credentials = MapCredentials(credentialsModel?.ToICredentials());
                var response = await connectionConfigurationSlCoreService.ListUserOrganizationsAsync(new ListUserOrganizationsParams(credentials));
                var organizationDisplays = response.userOrganizations.Select(o => new OrganizationDisplay(o.key, o.name)).ToList();

                return new AdapterResponseWithData<List<OrganizationDisplay>>(true, organizationDisplays);
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"{Resources.ListUserOrganizations_Fails}: {ex.Message}");
                return FailedResponseWithData;
            }
        });
    }

    public Task<AdapterResponseWithData<ServerProject>> GetServerProjectByKeyAsync(ServerConnection serverConnection, string serverProjectKey)
    {
        var failedResponse = new AdapterResponseWithData<ServerProject>(false, null);
        
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
                
                return new AdapterResponseWithData<ServerProject>(true, new ServerProject(serverProjectKey, response.projectNamesByKey[serverProjectKey]));
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"{Resources.GetServerProjectByKey_Fails}: {ex.Message}");
                return failedResponse;
            }
        });
    }

    public async Task<AdapterResponseWithData<List<ServerProject>>> GetAllProjectsAsync(ServerConnection serverConnection)
    {
        var validateConnectionParams = new GetAllProjectsParams(GetTransientConnectionDto(serverConnection));
        return await GetAllProjectsAsync(validateConnectionParams);
    }

    public async Task<AdapterResponseWithData<List<ServerProject>>> FuzzySearchProjectsAsync(ServerConnection serverConnection, string searchTerm)
    {
        var failedResponse = new AdapterResponseWithData<List<ServerProject>>(false, []);
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
                return new AdapterResponseWithData<List<ServerProject>>(true, serverProjects);
            }
            catch (Exception ex)
            {
                logger.LogVerbose(Resources.FuzzySearchProjects_Fails, serverConnection.Id, searchTerm, ex.Message);
                return failedResponse;
            }
        });
    }

    private async Task<AdapterResponse> ValidateConnectionAsync(ValidateConnectionParams validateConnectionParams)
    {
        return await threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!TryGetConnectionConfigurationSlCoreService(out var connectionConfigurationSlCoreService))
            {
                return FailedResponse;
            }

            try
            {
                var slCoreResponse = await connectionConfigurationSlCoreService.ValidateConnectionAsync(validateConnectionParams);
                return new AdapterResponse(slCoreResponse.success);
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"{Resources.ValidateCredentials_Fails}: {ex.Message}");
                return FailedResponse;
            }
        });
    }

    private async Task<AdapterResponseWithData<List<ServerProject>>> GetAllProjectsAsync(GetAllProjectsParams getAllProjectsParams)
    {
        var failedResponse = new AdapterResponseWithData<List<ServerProject>>(false, []);
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
                return new AdapterResponseWithData<List<ServerProject>>(true, serverProjects);
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
            ConnectionServerType.SonarQube => Either<TransientSonarQubeConnectionDto, TransientSonarCloudConnectionDto>.CreateLeft(
                new TransientSonarQubeConnectionDto(connectionInfo.Id, credentialsDto)),
            ConnectionServerType.SonarCloud => Either<TransientSonarQubeConnectionDto, TransientSonarCloudConnectionDto>.CreateRight(
                new TransientSonarCloudConnectionDto(connectionInfo.Id, credentialsDto)),
            _ => throw new ArgumentException(Resources.UnexpectedConnectionType)
        };
    }

    private static Either<TransientSonarQubeConnectionDto, TransientSonarCloudConnectionDto> GetTransientConnectionDto(ServerConnection serverConnection)
    {
        var credentials = MapCredentials(serverConnection.Credentials);
        
        return serverConnection switch
        {
            ServerConnection.SonarQube sonarQubeConnection => Either<TransientSonarQubeConnectionDto, TransientSonarCloudConnectionDto>.CreateLeft(
                new TransientSonarQubeConnectionDto(sonarQubeConnection.Id, credentials)),
            ServerConnection.SonarCloud sonarCloudConnection => Either<TransientSonarQubeConnectionDto, TransientSonarCloudConnectionDto>.CreateRight(
                new TransientSonarCloudConnectionDto(sonarCloudConnection.OrganizationKey, credentials)),
            _ => throw new ArgumentException(Resources.UnexpectedConnectionType)
        };
    }

    private static Either<TokenDto, UsernamePasswordDto> GetEitherForToken(string token)
    {
        return Either<TokenDto, UsernamePasswordDto>.CreateLeft(new TokenDto(token));
    }

    private static Either<TokenDto, UsernamePasswordDto> GetEitherForUsernamePassword(string username, string password)
    {
        return Either<TokenDto, UsernamePasswordDto>.CreateRight(new UsernamePasswordDto(username, password));
    }
    
    private static Either<TokenDto, UsernamePasswordDto> MapCredentials(IConnectionCredentials credentials) =>
        credentials switch
        {
            UsernameAndPasswordCredentials basicAuthCredentials => GetEitherForUsernamePassword(basicAuthCredentials.UserName, basicAuthCredentials.Password.ToUnsecureString()),
            TokenAuthCredentials tokenAuthCredentials => GetEitherForToken(tokenAuthCredentials.Token.ToUnsecureString()),
            _ => throw new ArgumentException($"Unexpected {nameof(ICredentialsModel)} argument")
        };
}
