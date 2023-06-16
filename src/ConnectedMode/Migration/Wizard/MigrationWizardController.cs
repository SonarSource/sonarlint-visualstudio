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

        void StartMigrationWizard();
    }

    [Export(typeof(IMigrationWizardController))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class MigrationWizardController : IMigrationWizardController
    {
        public event EventHandler MigrationWizardFinished;

        private readonly IConnectedModeMigration connectedModeMigration;
        private readonly ILogger logger;

        [ImportingConstructor]
        public MigrationWizardController(IConnectedModeMigration connectedModeMigration, ILogger logger)
        {
            this.connectedModeMigration = connectedModeMigration;
            this.logger = logger;
        }

        public void StartMigrationWizard()
        {
            var migrationWizardWindow = new MigrationWizardWindow(connectedModeMigration, logger);

            var finishedSuccessfully = migrationWizardWindow.ShowModal();

            if (finishedSuccessfully != null && finishedSuccessfully.Value)
            {
               MigrationWizardFinished?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
