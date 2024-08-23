/*
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
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.ConnectedMode;

public interface ISlCoreConnectionAdapter
{
    Task<ValidateConnectionResponse> ValidateConnectionAsync(ConnectionInfo connectionInfo, string token);
    Task<ValidateConnectionResponse> ValidateConnectionAsync(ConnectionInfo connectionInfo, string username, string password);
    Task<AdapterResponseWithData<List<OrganizationDisplay>>> GetOrganizationsAsync(ICredentialsModel credentialsModel);
}

public class AdapterResponseWithData<T>(bool success, T responseData) : IResponseStatus
{
    public bool Success { get; init; } = success;
    public T ResponseData { get; } = responseData;
} 

[Export(typeof(ISlCoreConnectionAdapter))]
public class SlCoreConnectionAdapter : ISlCoreConnectionAdapter
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private static readonly AdapterResponseWithData<List<OrganizationDisplay>> FailedResponseWithData = new(false, []);

    [ImportingConstructor]
    public SlCoreConnectionAdapter(ISLCoreServiceProvider serviceProvider, IThreadHandling threadHandling, ILogger logger)
    {
        this.serviceProvider = serviceProvider;
        this.threadHandling = threadHandling;
        this.logger = logger;
    }

    public async Task<ValidateConnectionResponse> ValidateConnectionAsync(ConnectionInfo connectionInfo, string token)
    {
        var validateConnectionParams = GetValidateConnectionParams(connectionInfo, GetEitherForToken(token));
        return await ValidateConnectionAsync(validateConnectionParams);
    }

    public async Task<ValidateConnectionResponse> ValidateConnectionAsync(ConnectionInfo connectionInfo, string username, string password)
    {
        var validateConnectionParams = GetValidateConnectionParams(connectionInfo, GetEitherForUsernamePassword(username, password));
        return await ValidateConnectionAsync(validateConnectionParams);
    }

    public async Task<AdapterResponseWithData<List<OrganizationDisplay>>> GetOrganizationsAsync(ICredentialsModel credentialsModel)
    {
        return await threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!TryGetConnectionConfigurationSlCoreService(out var connectionConfigurationSlCoreService))
            {
                return FailedResponseWithData;
            }

            try
            {
                var credentials = GetCredentialsDto(credentialsModel);
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

    private async Task<ValidateConnectionResponse> ValidateConnectionAsync(ValidateConnectionParams validateConnectionParams)
    {
        return await threadHandling.RunOnBackgroundThread(async () =>
        {
            if (!TryGetConnectionConfigurationSlCoreService(out var connectionConfigurationSlCoreService))
            {
                return new ValidateConnectionResponse(false, UiResources.ValidatingConnectionFailedText);
            }

            try
            {
                return await connectionConfigurationSlCoreService.ValidateConnectionAsync(validateConnectionParams);
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"{Resources.ValidateCredentials_Fails}: {ex.Message}");
                return new ValidateConnectionResponse(false, ex.Message);
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

    private static ValidateConnectionParams GetValidateConnectionParams(ConnectionInfo connectionInfo, Either<TokenDto, UsernamePasswordDto> credentials)
    {
        return new ValidateConnectionParams(GetTransientConnectionDto(connectionInfo, credentials));
    }

    private static Either<TransientSonarQubeConnectionDto, TransientSonarCloudConnectionDto> GetTransientConnectionDto(ConnectionInfo connectionInfo, Either<TokenDto, UsernamePasswordDto> credentials)
    {
        return connectionInfo.ServerType == ConnectionServerType.SonarQube
            ? Either<TransientSonarQubeConnectionDto, TransientSonarCloudConnectionDto>.CreateLeft(new TransientSonarQubeConnectionDto(connectionInfo.Id, credentials))
            : Either<TransientSonarQubeConnectionDto, TransientSonarCloudConnectionDto>.CreateRight(new TransientSonarCloudConnectionDto(connectionInfo.Id, credentials));
    }

    private static Either<TokenDto, UsernamePasswordDto> GetEitherForToken(string token)
    {
        return Either<TokenDto, UsernamePasswordDto>.CreateLeft(new TokenDto(token));
    }

    private static Either<TokenDto, UsernamePasswordDto> GetEitherForUsernamePassword(string username, string password)
    {
        return Either<TokenDto, UsernamePasswordDto>.CreateRight(new UsernamePasswordDto(username, password));
    }

    private static Either<TokenDto, UsernamePasswordDto> GetCredentialsDto(ICredentialsModel credentialsModel)
    {
        return credentialsModel switch
        {
            TokenCredentialsModel tokenCredentialsModel => GetEitherForToken(tokenCredentialsModel.Token),
            UsernamePasswordModel usernamePasswordModel => GetEitherForUsernamePassword(usernamePasswordModel.Username, usernamePasswordModel.Password),
            _ => throw new ArgumentException($"Unexpected {nameof(ICredentialsModel)} argument")
        };
    }
}
