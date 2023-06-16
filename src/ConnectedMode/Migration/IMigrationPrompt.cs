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
using SonarLint.VisualStudio.ConnectedMode.Migration.Wizard;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    /// <summary>
    /// In charge of showing users a prompt to migrate to a new connected mode.
    /// </summary>
    internal interface IMigrationPrompt : IDisposable
    {
        Task ShowAsync();

        void Clear();
    }

    [Export(typeof(IMigrationPrompt))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class MigrationPrompt : IMigrationPrompt
    {
        private readonly INotificationService notificationService;
        private readonly IServiceProvider serviceProvider;
        private readonly IMigrationWizardController migrationWizardController;
        private readonly IBrowserService browserService;
        private readonly IThreadHandling threadHandling;

        private const string idPrefix = "ConnectedModeMigration_";

        [ImportingConstructor]
        internal MigrationPrompt([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            INotificationService notificationService,
            IMigrationWizardController migrationWizardController,
            IBrowserService browserService,
            IThreadHandling threadHandling)
        {
            this.notificationService = notificationService;
            this.serviceProvider = serviceProvider;
            this.migrationWizardController = migrationWizardController;
            this.browserService = browserService;
            this.threadHandling = threadHandling;

            migrationWizardController.MigrationWizardFinished += OnMigrationWizardFinished;
        }

        public async Task ShowAsync()
        {
           await threadHandling.RunOnUIThread(() =>
            {
                // The id contains the solution path so that each opened solution
                // per session has its own notification.
                var id = idPrefix + GetSolutionPath();

                var notification = new Notification(
                    id: id,
                    message: MigrationStrings.MigrationPrompt_Message,
                    actions: new INotificationAction[]
                    {
                    new NotificationAction(MigrationStrings.MigrationPrompt_MigrateButton, _ => OnMigrate(), false),
                    new NotificationAction(MigrationStrings.MigrationPrompt_LearnMoreButton, _ => OnLearnMore(), false),
                    });

                notificationService.ShowNotification(notification);
            });
        }

        private void OnMigrationWizardFinished(object sender, EventArgs e) => Clear();
        
        public void Clear()
        {
            notificationService.RemoveNotification();
        }

        private void OnMigrate()
        {
            migrationWizardController.StartMigrationWizard();
        }

        private void OnLearnMore()
        {
            browserService.Navigate(MigrationStrings.LearnMoreUrl);
        }

        private string GetSolutionPath()
        {
            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out var fullSolutionName);

            return fullSolutionName as string;
        }

        public void Dispose()
        {
            migrationWizardController.MigrationWizardFinished -= OnMigrationWizardFinished;
            Clear();
        }
    }
}
