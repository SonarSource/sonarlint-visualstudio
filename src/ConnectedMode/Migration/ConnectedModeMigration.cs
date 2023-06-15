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
    [Export(typeof(IConnectedModeMigration))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class ConnectedModeMigration : IConnectedModeMigration
    {
        private readonly ILogger logger;
        private readonly IFileProvider fileProvider;
        private readonly IFileCleaner fileCleaner;
        private readonly IVsAwareFileSystem fileSystem;

        [ImportingConstructor]
        public ConnectedModeMigration(IFileProvider fileProvider, IFileCleaner fileCleaner, IVsAwareFileSystem fileSystem, ILogger logger)
        {
            this.logger = logger;
            this.fileProvider = fileProvider;
            this.fileCleaner = fileCleaner;
            this.fileSystem = fileSystem;
        }

        public async Task MigrateAsync(IProgress<MigrationProgress> progress, CancellationToken token)
        {
            logger.WriteLine(MigrationStrings.Starting);

            // TODO: cleanup - dummy implementation to provide feedback for the UI
            progress?.Report(new MigrationProgress(1, 2, "TODO 1", false));
            progress?.Report(new MigrationProgress(2, 2, "TODO 2", true));

            var files = await fileProvider.GetFilesAsync(token);
            var legacySettings = GetLegacySettitngs();

            foreach (var file in files)
            {
                var content = GetFileContent(file);
                await fileCleaner.CleanAsync(content, legacySettings, token);
            }

            logger.WriteLine(MigrationStrings.Finished);
        }

        private LegacySettings GetLegacySettitngs()
        {
            // TODO - calculate the partial paths to the ruleset and SonarLint.xml files
            return null;
        }

        private string GetFileContent(string filePath)
        {
            // TODO - fetch the content from disc/memory
            return "<Project />"; // minimal valid Project
        }
    }
}
