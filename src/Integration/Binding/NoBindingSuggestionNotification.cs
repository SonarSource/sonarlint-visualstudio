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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.Integration.Binding
{
    [Export(typeof(INoBindingSuggestionNotification))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class NoBindingSuggestionNotification : INoBindingSuggestionNotification, IDisposable
    {
        private const string NotificationId = "ConnectedModeSuggestionId";
        private readonly INotificationService notificationService;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IIDEWindowService ideWindowService;
        private readonly IConnectedModeUIManager connectedModeUiManager;
        private readonly IBrowserService browserService;

        [ImportingConstructor]
        public NoBindingSuggestionNotification(
            INotificationService notificationService,
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IIDEWindowService ideWindowService,
            IConnectedModeUIManager connectedModeUiManager,
            IBrowserService browserService)
        {
            this.notificationService = notificationService;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.ideWindowService = ideWindowService;
            this.connectedModeUiManager = connectedModeUiManager;
            this.browserService = browserService;

            this.activeSolutionBoundTracker.SolutionBindingChanged += OnActiveSolutionBindingChanged;
        }

        public void Show(string projectKey, bool isSonarCloud)
        {
            var connectAction = new NotificationAction(BindingStrings.NoBindingSuggestionNotification_ConfigureBindingAction, _ => connectedModeUiManager.ShowManageBindingDialogAsync().Forget(),
                true);
            var learnMoreAction = new NotificationAction(BindingStrings.NoBindingSuggestionNotification_LearnMoreAction, _ => browserService.Navigate(DocumentationLinks.OpenInIdeBindingSetup), false);

            var notification = new Notification(
                id: NotificationId,
                message: FormatNotificationMessage(projectKey, isSonarCloud),
                showOncePerSession: false,
                actions: [connectAction, learnMoreAction]);

            notificationService.ShowNotification(notification);
            ideWindowService.BringToFront();
        }

        public void Dispose() => activeSolutionBoundTracker.SolutionBindingChanged -= OnActiveSolutionBindingChanged;

        private void OnActiveSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            if (e.Configuration.Mode == SonarLintMode.Connected)
            {
                notificationService.CloseNotification();
            }
        }

        private static string FormatNotificationMessage(string projectKey, bool isSonarCloud)
        {
            var serverName = isSonarCloud ? CoreStrings.SonarQubeCloudProductName : CoreStrings.SonarQubeServerProductName;
            return string.Format(BindingStrings.NoBindingSuggestionNotification_Message, serverName, projectKey);
        }
    }
}
