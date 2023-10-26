/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion
{
    internal interface ISuggestSharedBindingGoldBar
    {
        void Show(ServerType serverType, Action onConnectHandler);
    }

    [Export(typeof(ISuggestSharedBindingGoldBar))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SuggestSharedBindingGoldBar : ISuggestSharedBindingGoldBar
    {
        private readonly INotificationService notificationService;
        private readonly IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction;
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly IBrowserService browserService;

        internal /* for testing */ const string IdTemplate = "shared.binding.suggestion.for.{0}";

        [ImportingConstructor]
        public SuggestSharedBindingGoldBar(INotificationService notificationService, 
            IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction,
            ISolutionInfoProvider solutionInfoProvider,
            IBrowserService browserService)
        {
            this.notificationService = notificationService;
            this.doNotShowAgainNotificationAction = doNotShowAgainNotificationAction;
            this.solutionInfoProvider = solutionInfoProvider;
            this.browserService = browserService;
        }

        public void Show(ServerType serverType, Action onConnectHandler)
        {
            var notification = new Notification(
                id: string.Format(IdTemplate, solutionInfoProvider.GetSolutionName()),
                message: string.Format(BindingStrings.SharedBindingSuggestionMainText, serverType),
                actions: new INotificationAction[]
                {
                    new NotificationAction(BindingStrings.SharedBindingSuggestionConnectOptionText, _ => onConnectHandler(), true),
                    new NotificationAction(BindingStrings.SharedBindingSuggestionInfoOptionText, _ => OnLearnMore(), false),
                    doNotShowAgainNotificationAction
                });

            notificationService.ShowNotification(notification);
        }

        private void OnLearnMore()
        {
            browserService.Navigate(DocumentationLinks.SharedBinding);
        }
    }
}
