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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    /// <summary>
    /// Handle the non-UI part of migrating to the new unintrusive Connected Mode
    /// file settings
    /// </summary>
    internal interface IConnectedModeMigration
    {
        // TODO: decide if the method should return a list of specific manual cleanup
        // steps that the user still needs to perform
        Task MigrateAsync(IProgress<MigrationProgress> progress, CancellationToken token);
    }

    [Export(typeof(IConnectedModeMigration))]
    internal class ConnectedModeMigration : IConnectedModeMigration
    {
        private readonly ILogger logger;

        [ImportingConstructor]
        public ConnectedModeMigration(ILogger logger)
        {
            this.logger = logger;
        }

        public Task MigrateAsync(IProgress<MigrationProgress> progress, CancellationToken token)
        {
            logger.WriteLine(MigrationStrings.Starting);

            // TODO: implement migration
            progress.Report(new MigrationProgress(1, 2, "TODO 1", false));
            progress.Report(new MigrationProgress(2, 2, "TODO 2", true));

            logger.WriteLine(MigrationStrings.Finished);
            return Task.CompletedTask;
        }
    }

}
