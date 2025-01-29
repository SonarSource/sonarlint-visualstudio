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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Core.Telemetry;

namespace SonarLint.VisualStudio.ConnectedMode.Promote;

public interface IPromoteGoldBar
{
    void PromoteConnectedMode(string languagesToPromote);
}

[Export(typeof(IPromoteGoldBar))]
[PartCreationPolicy(CreationPolicy.Shared)]
public sealed class PromoteGoldBar : IPromoteGoldBar, IDisposable
{
    private readonly INotificationService notificationService;
    private readonly IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction;
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly IBrowserService browserService;
    private readonly ITelemetryManager telemetryManager;
    private readonly IConnectedModeUIManager connectedModeUiManager;

    [ImportingConstructor]
    public PromoteGoldBar(
        INotificationService notificationService,
        IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction,
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IBrowserService browserService,
        ITelemetryManager telemetryManager,
        IConnectedModeUIManager connectedModeUiManager)
    {
        this.notificationService = notificationService;
        this.doNotShowAgainNotificationAction = doNotShowAgainNotificationAction;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.browserService = browserService;
        this.telemetryManager = telemetryManager;
        this.connectedModeUiManager = connectedModeUiManager;

        this.activeSolutionBoundTracker.SolutionBindingChanged += OnActiveSolutionBindingChanged;
    }

    public void PromoteConnectedMode(string languagesToPromote)
    {
        var notification = new Notification(
            id: "PromoteNotification",
            message: string.Format(Resources.PromoteConnectedModeLanguagesMessage, languagesToPromote),
            actions:
            [
                new NotificationAction(Resources.PromoteBind, _ => OnBind(), false),
                new NotificationAction(Resources.PromoteSonarQubeCloud, _ => OnTrySonarQubeCloud(), false),
                new NotificationAction(Resources.PromoteLearnMore, _ => OnLearnMore(), false),
                doNotShowAgainNotificationAction
            ]);

        notificationService.ShowNotification(notification);
    }

    public void Dispose() => activeSolutionBoundTracker.SolutionBindingChanged -= OnActiveSolutionBindingChanged;

    private void OnActiveSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
    {
        if (e.Configuration.Mode == SonarLintMode.Connected)
        {
            notificationService.CloseNotification();
        }
    }

    private void OnBind() => connectedModeUiManager.ShowManageBindingDialog();

    private void OnTrySonarQubeCloud()
    {
        browserService.Navigate(TelemetryLinks.LinkIdToUrls[TelemetryLinks.SonarQubeCloudFreeSignUpId]);
        telemetryManager.LinkClicked(TelemetryLinks.SonarQubeCloudFreeSignUpId);
    }

    private void OnLearnMore() => browserService.Navigate(DocumentationLinks.ConnectedModeBenefits);
}
