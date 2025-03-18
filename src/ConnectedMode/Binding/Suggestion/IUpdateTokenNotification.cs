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
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;

public interface IUpdateTokenNotification
{
    void Show(string connectionId);
}

[Export(typeof(IUpdateTokenNotification))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class UpdateTokenNotification(INotificationService notificationService, IConnectedModeUIManager connectedModeUiManager, IServerConnectionsRepository serverConnectionsRepository)
    : IUpdateTokenNotification
{
    internal const string IdTemplate = "update.token.for.{0}";

    public void Show(string connectionId)
    {
        if (!serverConnectionsRepository.TryGet(connectionId, out var serverConnection))
        {
            return;
        }

        var connection = serverConnection.ToConnection();
        var connectionInfo = connection.Info;

        var notification = new Notification(
            id: string.Format(IdTemplate, connectionInfo.Id),
            message: string.Format(BindingStrings.UpdateTokenNotificationText, connectionInfo.Id),
            actions:
            [
                new NotificationAction(BindingStrings.UpdateTokenNotificationEditCredentialsOptionText, _ => OnUpdateTokenHandler(connection), true),
                new NotificationAction(BindingStrings.UpdateTokenDismissOptionText, _ => OnDismissHandler(), true),
            ],
            showOncePerSession: true);
        notificationService.ShowNotification(notification);
    }

    private void OnUpdateTokenHandler(Connection connection) => connectedModeUiManager.ShowEditCredentialsDialog(connection);

    private void OnDismissHandler() => notificationService.CloseNotification();
}
