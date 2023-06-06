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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    /// <summary>
    /// In charge of showing users a prompt to migrate to a new connected mode.
    /// </summary>
    public interface IMigrationPrompt
    {
        Task ShowAsync();

        void Clear();
    }

    [Export(typeof(IMigrationPrompt))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MigrationPrompt : IMigrationPrompt
    {
        private readonly INotificationService notificationService;

        private readonly IServiceProvider serviceProvider;

        private readonly IThreadHandling threadHandling;

        private const string idPrefix = "migration_";

        [ImportingConstructor]
        public MigrationPrompt([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, INotificationService notificationService, IThreadHandling threadHandling)
        {
            this.notificationService = notificationService;
            this.serviceProvider = serviceProvider;
            this.threadHandling = threadHandling;
        }

        public async Task ShowAsync()
        {
           await threadHandling.RunOnUIThread(() =>
            {
                var id = idPrefix + GetSolutionPath();

                var notification = new Notification(
                    id: id,
                    message: Resources.Migration_MigrationPrompt_Message,
                    actions: new INotificationAction[]
                    {
                    new NotificationAction(Resources.Migration_MigrationPrompt_MigrateButton, _ => OnMigrate(), false),
                    new NotificationAction(Resources.Migration_MigrationPrompt_LearnMoreButton, _ => OnLearnMore(), false),
                    });

                notificationService.ShowNotification(notification);
            });
        }

        public void Clear()
        {
            notificationService.RemoveNotification();
        }

        private void OnMigrate()
        {
            // TODO: Show migration wizard
        }

        private void OnLearnMore()
        {
            // TODO: Show relevant documentation in browser
        }

        private string GetSolutionPath()
        {
            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out var fullSolutionName);

            return fullSolutionName as string;
        }
    }
}
