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
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Migration.Wizard
{
    /// <summary>
    /// Starts a wizard which guides a user through the migration progress.
    /// </summary>
    internal interface IMigrationWizardController
    {
        /// <summary>
        /// Raised when the wizard is closed after successfully finishing the migration process.
        /// </summary>
        event EventHandler MigrationWizardFinished;

        void StartMigrationWizard(BoundSonarQubeProject oldBinding);
    }

    [Export(typeof(IMigrationWizardController))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class MigrationWizardController : IMigrationWizardController
    {
        public event EventHandler MigrationWizardFinished;

        private readonly IConnectedModeMigration connectedModeMigration;
        private readonly IBrowserService browserService;
        private readonly IOutputWindowService outputWindowService;
        private readonly IGitWorkspaceService gitWorkspaceService;
        private readonly ILogger logger;

        [ImportingConstructor]
        public MigrationWizardController(IConnectedModeMigration connectedModeMigration,
            IBrowserService browserService,
            IOutputWindowService outputWindowService,
            IGitWorkspaceService gitWorkspaceService,
            ILogger logger)
        {
            this.connectedModeMigration = connectedModeMigration;
            this.browserService = browserService;
            this.outputWindowService = outputWindowService;
            this.gitWorkspaceService = gitWorkspaceService;
            this.logger = logger;
        }

        public void StartMigrationWizard(BoundSonarQubeProject oldBinding)
        {
            var migrationWizardWindow = new MigrationWizardWindow(oldBinding, connectedModeMigration, OnShowHelp, OnShowTfvcHelp, OnShowSharedBinding, IsUnderGit(), logger);

            var finishedSuccessfully = migrationWizardWindow.ShowModal();

            if (finishedSuccessfully != null && finishedSuccessfully.Value)
            {
                MigrationWizardFinished?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // Show the output window in event of an unsuccessful migration
                outputWindowService.Show();
            }
        }

        private bool IsUnderGit()
        {
            return gitWorkspaceService.GetRepoRoot() != null;
        }

        private void OnShowHelp() => browserService.Navigate(DocumentationLinks.MigrateToConnectedModeV7);

        private void OnShowTfvcHelp() => browserService.Navigate(DocumentationLinks.MigrateToConnectedModeV7_NotesForTfvcUsers);

        private void OnShowSharedBinding() => browserService.Navigate(DocumentationLinks.SetupSharedBinding);
    }
}
