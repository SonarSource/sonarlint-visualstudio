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
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Binding;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation
{
    [Export(typeof(ISLCoreListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    public class ConnectedModeSuggestionListener(
        INoBindingSuggestionNotification noBindingSuggestionNotification,
        IConnectedModeUIManager connectedModeUiManager,
        ILogger logger,
        IIDEWindowService ideWindowService)
        : IConnectedModeSuggestionListener
    {
        private readonly ILogger logger = logger.ForContext("Connected Mode Suggestion");

        public async Task<AssistCreatingConnectionResponse> AssistCreatingConnectionAsync(AssistCreatingConnectionParams parameters)
        {
            var serverConnection = ConvertSeverConnection(parameters);
            var token = parameters.connectionParams.Right?.tokenValue ?? parameters.connectionParams.Left?.tokenValue;

            ideWindowService.BringToFront();
            var trustConnectionDialogResult = await connectedModeUiManager.ShowTrustConnectionDialogAsync(serverConnection, token).ConfigureAwait(false);
            if (trustConnectionDialogResult == true)
            {
                AssistCreatingConnectionResponse result = new(serverConnection.Id);
                logger.LogVerbose(SLCoreStrings.AssistConnectionSucceeds, result.newConnectionId);
                return result;
            }

            throw new OperationCanceledException(SLCoreStrings.AssistConnectionCancelled);
        }

        public Task<AssistBindingResponse> AssistBindingAsync(AssistBindingParams parameters) => throw new NotImplementedException();

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

            logger.LogVerbose(SLCoreStrings.AssistConnectionInvalidServerConnection, nameof(AssistCreatingConnectionParams));
            throw new ArgumentNullException(nameof(parameters));
        }
    }
}
