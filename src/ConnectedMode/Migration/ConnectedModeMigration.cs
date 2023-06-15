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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IConnectedModeMigration))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class ConnectedModeMigration : IConnectedModeMigration
    {
        // Private "alias" to simplify method arguments
        private sealed class ChangedFiles : List<FilePathAndContent<string>> { }

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
            // TODO - add cancellation
            // TODO - add progress messages

            logger.WriteLine(MigrationStrings.Starting);

            // TODO: cleanup - dummy implementation to provide feedback for the UI
            progress?.Report(new MigrationProgress(1, 2, "TODO 1", false));
            progress?.Report(new MigrationProgress(2, 2, "TODO 2", true));

            var legacySettings = GetLegacySettings();

            logger.WriteLine(MigrationStrings.GettingFiles);
            var files = await fileProvider.GetFilesAsync(token);
            logger.WriteLine(MigrationStrings.CountOfFilesToClean, files.Count());

            if (files.Any())
            {
                logger.WriteLine(MigrationStrings.CleaningFiles);
                var changedFiles = await CleanFilesAsync(files, legacySettings, token);

                // Note: no files have been changed yet. Now we are going to start making changes
                // to the user's projects and deleting files that might be under source control...

                logger.WriteLine(MigrationStrings.SavingFiles);
                await SaveChangedFilesAsync(changedFiles);
            }
            else
            {
                logger.WriteLine(MigrationStrings.SkippingCleaning);
            }

            // TODO - trigger unintrusive binding process (will need the binding arguments)
            logger.WriteLine(MigrationStrings.ProcessingNewBinding);

            // Note: SLVS will continue to detect the legacy binding mode until this step,
            // so if anything goes wrong during the migration and an exception occurs, the
            // user will see the migration gold bar next time they open the solution.
            logger.WriteLine(MigrationStrings.DeletingSonarLintFolder);
            await fileSystem.DeleteFolderAsync(legacySettings.LegacySonarLintFolderPath);

            logger.WriteLine(MigrationStrings.Finished);
        }

        private LegacySettings GetLegacySettings()
        {
            // TODO - calculate the partial paths to the ruleset and SonarLint.xml files
            // for both C# and VB.NET - #4362 and #4363
            return new LegacySettings("folder", "cs ruleset", "cs xml", "vb ruleset", "vb xml");
        }

        private Task<string> GetFileContentAsync(string filePath)
            => fileSystem.LoadAsTextAsync(filePath);

        private async Task<ChangedFiles> CleanFilesAsync(IEnumerable<string> filesToClean,
            LegacySettings legacySettings,
            CancellationToken token)
        {
            var changedFiles = new ChangedFiles();

            foreach (var file in filesToClean)
            {
                var content = await GetFileContentAsync(file);

                var newContent = await fileCleaner.CleanAsync(content, legacySettings, token);
                Debug.Assert(newContent == null || !newContent.Equals(content),
                            "New file content should be null or different from original content");

                if (newContent != MSBuildFileCleaner.Unchanged)
                {
                    changedFiles.Add(new FilePathAndContent<string>(file, newContent));
                }
            }

            return changedFiles;
        }

        private async Task SaveChangedFilesAsync(ChangedFiles changedFiles)
        {
            foreach(var file in changedFiles)
            {
                await fileSystem.SaveAsync(file.Path, file.Content);
            }
        }
    }
}
