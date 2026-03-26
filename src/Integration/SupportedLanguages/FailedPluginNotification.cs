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
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Service.Plugin.Models;

namespace SonarLint.VisualStudio.Integration.SupportedLanguages;

public interface IFailedPluginNotification : IDisposable;

[Export(typeof(IFailedPluginNotification))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class FailedPluginNotification : IFailedPluginNotification
{
    private const string NotificationId = "PluginStatuses.FailedNotification";

    private readonly IPluginStatusesStore pluginStatusesStore;
    private readonly INotificationService notificationService;
    private readonly ISLCoreHandler slCoreHandler;
    private readonly ISupportedLanguagesWindowService supportedLanguagesWindowService;

    [ImportingConstructor]
    public FailedPluginNotification(
        IPluginStatusesStore pluginStatusesStore,
        INotificationService notificationService,
        ISLCoreHandler slCoreHandler,
        ISupportedLanguagesWindowService supportedLanguagesWindowService)
    {
        this.pluginStatusesStore = pluginStatusesStore;
        this.notificationService = notificationService;
        this.slCoreHandler = slCoreHandler;
        this.supportedLanguagesWindowService = supportedLanguagesWindowService;

        this.pluginStatusesStore.PluginStatusesChanged += OnPluginStatusesChanged;
    }

    private void OnPluginStatusesChanged(object sender, EventArgs e)
    {
        var failedPlugins = pluginStatusesStore.GetAll()
            .Where(p => p.State == PluginStateDto.FAILED)
            .ToList();

        if (failedPlugins.Count == 0)
        {
            notificationService.CloseNotification();
            return;
        }

        var message = $"{Strings.PluginStatusesFailedNotificationText}: {string.Join(", ", failedPlugins.Select(p => p.PluginName))}";

        var restartAction = new NotificationAction(
            SLCoreStrings.SloopRestartFailedNotificationService_Restart,
            _ => slCoreHandler.ForceRestartSloop(),
            shouldDismissAfterAction: true);

        var seeDetailsAction = new NotificationAction(
            Strings.PluginStatusesFailedNotificationSeeDetailsButton,
            _ => supportedLanguagesWindowService.Show(),
            shouldDismissAfterAction: true);

        var notification = new Notification(
            id: NotificationId,
            message: message,
            actions: [restartAction, seeDetailsAction],
            showOncePerSession: false);

        notificationService.ShowNotification(notification);
    }

    public void Dispose()
    {
        pluginStatusesStore.PluginStatusesChanged -= OnPluginStatusesChanged;
    }
}
