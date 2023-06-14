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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IConnectedModeMigration))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class ConnectedModeMigration : IConnectedModeMigration
    {
        private readonly IRoslynProjectWalker projectWalker;
        private readonly IProjectCleaner[] projectCleaners;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;

        internal /* for testing */ IEnumerable<IProjectCleaner> ProjectCleaners => projectCleaners?.ToList() ?? Enumerable.Empty<IProjectCleaner>();

        [ImportingConstructor]
        public ConnectedModeMigration(IRoslynProjectWalker projectWalker, [ImportMany]IProjectCleaner[] projectCleaners,
            ILogger logger, IThreadHandling threadHandling)
        {
            this.projectWalker = projectWalker;
            this.projectCleaners = projectCleaners;
            this.logger = logger;
            this.threadHandling = threadHandling;
        }

        public async Task MigrateAsync(IProgress<MigrationProgress> progress, CancellationToken token)
        {
            await threadHandling.SwitchToBackgroundThread();
            logger.WriteLine(MigrationStrings.Starting);

            // TODO: cleanup - dummy implementation to provide feedback for the UI
            progress?.Report(new MigrationProgress(1, 2, "TODO 1", false));
            progress?.Report(new MigrationProgress(2, 2, "TODO 2", true));

            foreach (var project in GetProjects())
            {
                foreach (var cleaner in projectCleaners)
                {
                    await cleaner.CleanAsync(null, progress, token);
                }
            }

            logger.WriteLine(MigrationStrings.Finished);
        }

        private IEnumerable<IVsHierarchy> GetProjects() => projectWalker.Walk();
    }
}
