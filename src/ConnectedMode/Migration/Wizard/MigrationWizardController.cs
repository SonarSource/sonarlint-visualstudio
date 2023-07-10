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
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core;

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
            var showTfvcHelpOp = GetTfVcHelpAction(); 

            var migrationWizardWindow = new MigrationWizardWindow(oldBinding, connectedModeMigration, OnShowHelp, showTfvcHelpOp, logger);

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

        private Action GetTfVcHelpAction()
        {
            Action helpOp;

            // We don't have an existing component that detects whether user is using Tfvc.
            // However, we can tell if they are using git, which we expect to be the majority
            // of users. So to reduce the noise, we won't show the Tfvc help for git users.
            var showTfvcWarning = gitWorkspaceService.GetRepoRoot() == null;

            if (showTfvcWarning)
            {
                logger.LogMigrationVerbose("Did not detect a git repo - displaying the Tfvc warning");
                helpOp = OnShowTfvcHelp;
            }
            else
            {
                logger.LogMigrationVerbose("Detected a git repo - not displaying the Tfvc warning");
                helpOp = null;
            }

            return helpOp;
        }

        private void OnShowHelp() => browserService.Navigate(MigrationStrings.Url_LearnMoreUrl);

        private void OnShowTfvcHelp() => browserService.Navigate(MigrationStrings.Url_TfvcHelp);
    }
}
