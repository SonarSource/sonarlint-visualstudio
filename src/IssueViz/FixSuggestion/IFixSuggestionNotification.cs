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
using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion;

public interface IFixSuggestionNotification
{
    void UnableToOpenFile(string filePath);
    void InvalidRequest(string reason);
    void UnableToLocateIssue(string filePath);
    void Clear();
}

[Export(typeof(IFixSuggestionNotification))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class FixSuggestionNotification : IFixSuggestionNotification, IDisposable
{
    private readonly INotificationService notificationService;
    private readonly IOutputWindowService outputWindowService;
    private readonly IBrowserService browserService;

    [ImportingConstructor]
    public FixSuggestionNotification(INotificationService notificationService,
        IOutputWindowService outputWindowService,
        IBrowserService browserService)
    {
        this.notificationService = notificationService;
        this.outputWindowService = outputWindowService;
        this.browserService = browserService;
    }

    public void UnableToOpenFile(string filePath)
    {
        var unableToOpenFileMsg = string.Format(FixSuggestionResources.InfoBarUnableToOpenFile, filePath);
        Show(unableToOpenFileMsg);
    }

    public void InvalidRequest(string reason)
    {
        var unableToOpenFileMsg = string.Format(FixSuggestionResources.InfoBarInvalidRequest, reason);
        Show(unableToOpenFileMsg);
    }

    public void UnableToLocateIssue(string filePath)
    {
        var unableToOpenFileMsg = string.Format(FixSuggestionResources.InfoBarUnableToLocateFixSuggestion, filePath);
        Show(unableToOpenFileMsg);
    }

    private void Show(string text)
    {
        var notification = new Notification(
            id: "FixSuggestionNotification",
            showOncePerSession: false,
            message: text,
            actions:
            [
                new NotificationAction(FixSuggestionResources.InfoBarButtonMoreInfo, _ => browserService.Navigate(DocumentationLinks.OpenInIdeIssueLocation), false),
                new NotificationAction(FixSuggestionResources.InfoBarButtonShowLogs, _ => outputWindowService.Show(), false)
            ]);
        notificationService.ShowNotification(notification);
    }

    public void Clear()
    {
        notificationService.RemoveNotification();
    }

    public void Dispose()
    {
        Clear();
    }
}
