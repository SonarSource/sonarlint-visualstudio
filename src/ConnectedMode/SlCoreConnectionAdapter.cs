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
}

[Export(typeof(ISlCoreConnectionAdapter))]
public class SlCoreConnectionAdapter : ISlCoreConnectionAdapter
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;

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

    private async Task<ValidateConnectionResponse> ValidateConnectionAsync(ValidateConnectionParams validateConnectionParams)
    {
        await threadHandling.SwitchToBackgroundThread();

        if (!serviceProvider.TryGetTransientService(out IConnectionConfigurationSLCoreService connectionConfigurationSlCoreService))
        {
            logger.LogVerbose($"[{nameof(IConnectionConfigurationSLCoreService)}] {SLCoreStrings.ServiceProviderNotInitialized}");
            return new ValidateConnectionResponse(false, Resources.ValidateCredentials_Fails);
        }

        try
        {
            return await connectionConfigurationSlCoreService.ValidateConnectionAsync(validateConnectionParams);
        }
        catch (Exception)
        {
            return new ValidateConnectionResponse(false, Resources.ValidateCredentials_Fails);
        }
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
}
