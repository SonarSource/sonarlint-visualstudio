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
using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Binding;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation
{
    [Export(typeof(ISLCoreListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    public class ConnectedModeSuggestionListener(
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IConnectedModeUIManager connectedModeUiManager,
        INoBindingSuggestionNotification noBindingSuggestionNotification,
        IIDEWindowService ideWindowService,
        ILogger logger)
        : IConnectedModeSuggestionListener
    {
        private readonly ILogger logger = logger.ForContext(SLCoreStrings.SLCoreName, SLCoreStrings.ConnectedMode_LogContext, SLCoreStrings.ConnectedModeSuggestion_LogContext);

        public async Task<AssistCreatingConnectionResponse> AssistCreatingConnectionAsync(AssistCreatingConnectionParams parameters)
        {
            ideWindowService.BringToFront();
            var serverConnection = ConvertSeverConnection(parameters);
            var token = parameters.connectionParams.Right?.tokenValue ?? parameters.connectionParams.Left?.tokenValue;

            var trustConnectionDialogResult = await connectedModeUiManager.ShowTrustConnectionDialogAsync(serverConnection, token).ConfigureAwait(false);
            if (trustConnectionDialogResult == true)
            {
                AssistCreatingConnectionResponse result = new(serverConnection.Id);
                logger.WriteLine(SLCoreStrings.AssistConnectionSucceeds, result.newConnectionId);
                return result;
            }

            logger.WriteLine(SLCoreStrings.AssistConnectionFailed);
            throw new OperationCanceledException(SLCoreStrings.AssistConnectionFailed);
        }

        public async Task<AssistBindingResponse> AssistBindingAsync(AssistBindingParams parameters)
        {
            ideWindowService.BringToFront();
            return new AssistBindingResponse(await BindToConfigScopeAsync(parameters));
        }

        private async Task<string> BindToConfigScopeAsync(AssistBindingParams parameters)
        {
            if (activeConfigScopeTracker.Current?.Id != parameters.configScopeId)
            {
                logger.WriteLine(SLCoreStrings.ConfigurationScopeMismatch);
                return null;
            }

            var boundConfigScope = await connectedModeUiManager.ShowManageBindingDialogAsync(new AutomaticBindingRequest.Assisted(parameters.connectionId, parameters.projectKey, parameters.isFromSharedConfiguration)).ConfigureAwait(false)
                ? parameters.configScopeId
                : null;

            if (boundConfigScope == null)
            {
                logger.WriteLine(SLCoreStrings.AssistBindingFailed);
            }
            else
            {
                logger.WriteLine(SLCoreStrings.AssistBindingSucceeded, boundConfigScope);
            }
            return boundConfigScope;
        }

        public void NoBindingSuggestionFound(NoBindingSuggestionFoundParams parameters) => noBindingSuggestionNotification.Show(parameters.projectKey, parameters.isSonarCloud);

        private ServerConnection ConvertSeverConnection(AssistCreatingConnectionParams parameters)
        {
            if (parameters.connectionParams?.Right is { } sonarCloudConnectionParams)
            {
                return new ServerConnection.SonarCloud(sonarCloudConnectionParams.organizationKey, region: sonarCloudConnectionParams.sonarCloudRegion.ToCloudServerRegion());
            }
            if (parameters.connectionParams?.Left is { } sonarQubeConnectionParams)
            {
                return new ServerConnection.SonarQube(sonarQubeConnectionParams.serverUrl);
            }

            logger.WriteLine(SLCoreStrings.AssistConnectionInvalidServerConnection, parameters.ToString());
            throw new ArgumentNullException(nameof(parameters));
        }
    }
}
