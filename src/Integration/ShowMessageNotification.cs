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
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.SLCore.Listener.Promote;

namespace SonarLint.VisualStudio.Integration;

public interface IShowMessageNotification
{
    Task<string> ShowAsync(string parametersMessage, List<MessageActionItem> parametersActions);
}

[Export(typeof(IShowMessageNotification))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public sealed class ShowMessageNotification(INotificationService notificationService) : IShowMessageNotification
{
    public Task<string> ShowAsync(string parametersMessage, List<MessageActionItem> parametersActions)
    {
        var taskCompletionSource = new TaskCompletionSource<string>();
        var notificationActions = parametersActions
            .Select(action =>
                new NotificationAction(
                    action.displayText,
                    _ => taskCompletionSource.TrySetResult(action.key),
                    shouldDismissAfterAction: true))
            .ToList();
        var notification = new Notification(
            id: "ShowMessageNotification",
            message: parametersMessage,
            actions: notificationActions,
            showOncePerSession: false,
            closeOnSolutionClose: false,
            onClose: () => taskCompletionSource.TrySetResult(null));
        notificationService.ShowNotification(notification);
        return taskCompletionSource.Task;
    }
}
