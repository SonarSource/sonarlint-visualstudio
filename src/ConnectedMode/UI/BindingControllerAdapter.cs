/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UI;

internal interface IBindingControllerAdapter
{
    Task<BindingResult> ValidateAndBindAsync(BindingRequest request, IConnectedModeUIManager uiManager, CancellationToken token);
    bool UnbindCurrentSolution();
    bool Unbind(string bindingKey);
}

[Export(typeof(IBindingControllerAdapter))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class BindingControllerAdapter(
    IBindingController bindingController,
    ISolutionInfoProvider solutionInfoProvider,
    IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter,
    ILogger logger)
    : IBindingControllerAdapter
{
    private readonly ILogger logger = logger.ForContext(ConnectedMode.Resources.ConnectedModeLogContext, ConnectedMode.Resources.ConnectedModeBindingLogContext);

    public async Task<BindingResult> ValidateAndBindAsync(BindingRequest request, IConnectedModeUIManager uiManager, CancellationToken token)
    {
        var logContext = new MessageLevelContext { Context = [request.TypeName], VerboseContext = [request.ToString()] };

        var connection = GetExistingServerConnectionOrNull(request.ConnectionId) ?? await GetNewServerConnectionOrNullAsync(request, uiManager);
        var validationResult = ValidateRequest(request, connection, logContext);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var localBindingKey = await solutionInfoProvider.GetSolutionNameAsync();
            var boundServerProject = new BoundServerProject(localBindingKey, request.ProjectKey, connection);
            bindingController.Bind(boundServerProject);
            return BindingResult.Success;
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(logContext, ConnectedMode.Resources.Binding_Fails, ex.Message);
            return BindingResult.Failed;
        }
    }

    public bool UnbindCurrentSolution() => solutionInfoProvider.GetSolutionName() is { } solutionName && Unbind(solutionName);

    public bool Unbind(string bindingKey) => bindingController.Unbind(bindingKey);

    private BindingResult.ValidationFailure ValidateRequest(
        BindingRequest request,
        ServerConnection connection,
        MessageLevelContext logContext)
    {
        if (string.IsNullOrEmpty(request.ProjectKey))
        {
            logger.WriteLine(logContext, ConnectedMode.Resources.Binding_ProjectKeyNotFound);
            return BindingResult.ProjectKeyNotFound;
        }

        if (connection == null)
        {
            logger.WriteLine(logContext, ConnectedMode.Resources.Binding_ConnectionNotFound);
            return BindingResult.ConnectionNotFound;
        }

        if (connection.Credentials == null)
        {
            logger.WriteLine(logContext, ConnectedMode.Resources.Binding_CredentiasNotFound, connection.Id);
            return BindingResult.CredentialsNotFound;
        }

        return null;
    }

    private ServerConnection GetExistingServerConnectionOrNull(string connectionId) => serverConnectionsRepositoryAdapter.TryGet(connectionId, out var connection) ? connection : null;

    private async Task<ServerConnection> GetNewServerConnectionOrNullAsync(BindingRequest request, IConnectedModeUIManager uiManager) =>
        request is BindingRequest.Shared shared
        && await uiManager.ShowTrustConnectionDialogAsync(shared.Model.CreateConnectionInfo().GetServerConnectionFromConnectionInfo(), null) is true
            ? GetExistingServerConnectionOrNull(shared.ConnectionId)
            : null;
}
