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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

internal interface IOpenInIdeNotification
{
    void UnableToLocateIssue(string filePath, Guid toolWindowId);
    void UnableToOpenFile(string filePath, Guid toolWindowId);
    void InvalidRequest(string reason, Guid toolWindowId);
    void Clear();
}

[Export(typeof(IOpenInIdeNotification))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class OpenInIdeNotification : IOpenInIdeNotification, IDisposable
{
    private readonly IToolWindowService toolWindowService;
    private readonly INotificationService notificationService;
    private readonly IOutputWindowService outputWindowService;
    private readonly IBrowserService browserService;

    [ImportingConstructor]
    public OpenInIdeNotification(
        IToolWindowService toolWindowService,
        INotificationService notificationService,
        IOutputWindowService outputWindowService,
        IBrowserService browserService)
    {
        this.toolWindowService = toolWindowService;
        this.notificationService = notificationService;
        this.outputWindowService = outputWindowService;
        this.browserService = browserService;
    }

    public void UnableToLocateIssue(string filePath, Guid toolWindowId) =>
        Show(toolWindowId, string.Format(OpenInIdeResources.Notification_UnableToLocateIssue, filePath), true);

    public void UnableToOpenFile(string filePath, Guid toolWindowId) =>
        Show(toolWindowId, string.Format(OpenInIdeResources.Notification_UnableToOpenFile, filePath), true);

    public void InvalidRequest(string reason, Guid toolWindowId) =>
        Show(toolWindowId, string.Format(OpenInIdeResources.Notification_InvalidConfiguration, reason), false);

    public void Clear()
    {
        notificationService.CloseNotification();
    }

    public void Dispose()
    {
        Clear();
    }

    private void Show(Guid toolWindowId, string message, bool hasMoreInfo)
    {
        toolWindowService.Show(toolWindowId);

        var moreInfoAction = new NotificationAction(OpenInIdeResources.InfoBar_Button_MoreInfo, _ => browserService.Navigate(DocumentationLinks.OpenInIdeIssueLocation), false);
        var showLogsAction = new NotificationAction(OpenInIdeResources.InfoBar_Button_ShowLogs, _ => outputWindowService.Show(), false);
        List<INotificationAction> actions = hasMoreInfo
            ? [moreInfoAction, showLogsAction]
            : [showLogsAction];

        var notification = new Notification(
            id: "OpenInIdeNotification",
            showOncePerSession: false,
            message: message,
            actions: actions);
        notificationService.ShowNotification(notification, toolWindowId);
    }
}
