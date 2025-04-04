﻿/*
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
using SonarLint.VisualStudio.ConnectedMode.Migration.Wizard;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Notifications;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    /// <summary>
    /// In charge of showing users a prompt to migrate to a new connected mode.
    /// </summary>
    /// <remarks>The caller can assume that the component follows VS threading rules
    /// i.e. the implementing class is responsible for switching to the UI thread if necessary.
    /// The caller doesn't need to worry about it.
    /// </remarks>
    internal interface IMigrationPrompt : IDisposable
    {
        /// <summary>
        /// Shows a prompt async with a different message depending on the value of hasNewBindingFiles.
        /// </summary>
        Task ShowAsync(BoundSonarQubeProject oldBinding, bool hasNewBindingFiles);

        void Clear();
    }

    [Export(typeof(IMigrationPrompt))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal sealed class MigrationPrompt : IMigrationPrompt
    {
        private readonly INotificationService notificationService;
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly IMigrationWizardController migrationWizardController;
        private readonly IBrowserService browserService;

        private BoundSonarQubeProject oldBinding;

        private const string idPrefix = "ConnectedModeMigration_";

        [ImportingConstructor]
        internal MigrationPrompt(ISolutionInfoProvider solutionInfoProvider,
            INotificationService notificationService,
            IMigrationWizardController migrationWizardController,
            IBrowserService browserService)
        {
            this.notificationService = notificationService;
            this.solutionInfoProvider = solutionInfoProvider;
            this.migrationWizardController = migrationWizardController;
            this.browserService = browserService;

            migrationWizardController.MigrationWizardFinished += OnMigrationWizardFinished;
        }

        public async Task ShowAsync(BoundSonarQubeProject oldBinding, bool hasNewBindingFiles)
        {
            this.oldBinding = oldBinding;

            var solutionFileName = await solutionInfoProvider.GetFullSolutionFilePathAsync();

            // The id contains the solution path so that each opened solution
            // per session has its own notification.
            var id = idPrefix + solutionFileName;

            var message = hasNewBindingFiles ? MigrationStrings.MigrationPrompt_AlreadyConnected_Message : MigrationStrings.MigrationPrompt_Message;

            var notification = new Notification(
                id: id,
                message: message,
                actions: new INotificationAction[]
                {
                    new NotificationAction(MigrationStrings.MigrationPrompt_MigrateButton, _ => OnMigrate(), false),
                    new NotificationAction(MigrationStrings.MigrationPrompt_LearnMoreButton, _ => OnLearnMore(), false),
                });

            notificationService.ShowNotification(notification);
        }

        private void OnMigrationWizardFinished(object sender, EventArgs e) => Clear();

        public void Clear()
        {
            notificationService.CloseNotification();
        }

        private void OnMigrate()
        {
            migrationWizardController.StartMigrationWizard(oldBinding);
        }

        private void OnLearnMore()
        {
            browserService.Navigate(DocumentationLinks.MigrateToConnectedModeV7);
        }

        public void Dispose()
        {
            migrationWizardController.MigrationWizardFinished -= OnMigrationWizardFinished;
            Clear();
        }
    }
}
