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
using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.Binding
{
    [Export(typeof(IBindingSuggestionHandler))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class BindingSuggestionHandler : IBindingSuggestionHandler
    {
        private readonly INotificationService notificationService;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IIDEWindowService ideWindowService;
        private readonly ITeamExplorerController teamExplorerController;

        [ImportingConstructor]
        public BindingSuggestionHandler(INotificationService notificationService, IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IIDEWindowService ideWindowService, ITeamExplorerController teamExplorerController)
        {
            this.notificationService = notificationService;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.ideWindowService = ideWindowService;
            this.teamExplorerController = teamExplorerController;
        }

        public void Notify()
        {
            const string id = "ConnectedModeSuggestionId";

            var isStandaloneMode = activeSolutionBoundTracker.CurrentConfiguration.Mode == SonarLintMode.Standalone;

            var message = isStandaloneMode
                ? BindingStrings.BindingSuggestionProjectNotBound
                : BindingStrings.BindingSuggetsionBindingConflict;

            var notification = new Notification(
                id: id,
                message: message,
                showOncePerSession: false,
                actions: isStandaloneMode
                    ? [new NotificationAction(BindingStrings.BindingSuggestionConnect, _ => teamExplorerController.ShowSonarQubePage(), true)]
                    : []);

            notificationService.ShowNotification(notification);

            ideWindowService.BringToFront();
        }
    }
}
