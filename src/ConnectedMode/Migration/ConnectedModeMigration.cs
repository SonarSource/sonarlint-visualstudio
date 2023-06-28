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
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarQube.Client;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IConnectedModeMigration))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class ConnectedModeMigration : IConnectedModeMigration
    {
        // Private "alias" to simplify method arguments
        private sealed class ChangedFiles : List<FilePathAndContent<string>> { }

        private readonly IMigrationSettingsProvider settingsProvider;
        private readonly IFileProvider fileProvider;
        private readonly IFileCleaner fileCleaner;
        private readonly IVsAwareFileSystem fileSystem;
        private readonly ISonarQubeService sonarQubeService;
        private readonly IUnintrusiveBindingController unintrusiveBindingController;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;

        // The user can have both the legacy and new connected mode files. In that case, we expect the SonarQubeService to already be connected.
        private bool isAlreadyConnectedToServer;

        [ImportingConstructor]
        public ConnectedModeMigration(IMigrationSettingsProvider settingsProvider,
            IFileProvider fileProvider,
            IFileCleaner fileCleaner,
            IVsAwareFileSystem fileSystem,
            ISonarQubeService sonarQubeService,
            IUnintrusiveBindingController unintrusiveBindingController,
            ILogger logger,
            IThreadHandling threadHandling)
        {
            this.settingsProvider = settingsProvider;
            this.fileProvider = fileProvider;
            this.fileCleaner = fileCleaner;
            this.fileSystem = fileSystem;
            this.sonarQubeService = sonarQubeService;
            this.unintrusiveBindingController = unintrusiveBindingController;
            this.logger = logger;
            this.threadHandling = threadHandling;
        }

        public async Task MigrateAsync(BoundSonarQubeProject oldBinding, IProgress<MigrationProgress> progress, CancellationToken token)
        {
            isAlreadyConnectedToServer = sonarQubeService.IsConnected;

            await threadHandling.SwitchToBackgroundThread();

            try
            {
                if (!isAlreadyConnectedToServer)
                {
                    await sonarQubeService.ConnectAsync(oldBinding.CreateConnectionInformation(), token);
                }

                await MigrateImplAsync(oldBinding, progress, token);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // If we establish the server connection during migration, disconnect it if the migration failed.
                if (!isAlreadyConnectedToServer)
                {
                    sonarQubeService.Disconnect();
                }

                throw;
            }
        }

        private async Task MigrateImplAsync(BoundSonarQubeProject oldBinding, IProgress<MigrationProgress> progress, CancellationToken token) 
        {
            // TODO - add cancellation
            // TODO - add progress messages

            logger.WriteLine(MigrationStrings.Process_Starting);

            var legacySettings = await settingsProvider.GetAsync(oldBinding.ProjectKey);

            // TODO: add proper progress messages.
            progress?.Report(new MigrationProgress(0, 1, "Finding files to clean ...", false));

            logger.WriteLine(MigrationStrings.Process_GettingFiles);
            var files = await fileProvider.GetFilesAsync(token);
            logger.WriteLine(MigrationStrings.Process_CountOfFilesToCheck, files.Count());

            ChangedFiles changedFiles = null;
            if (files.Any())
            {
                progress?.Report(new MigrationProgress(0, 1, "Cleaning files ...", false));
                logger.WriteLine(MigrationStrings.Process_CheckingFiles);
                changedFiles = await CleanFilesAsync(files, legacySettings, token);
            }
            else
            {
                progress?.Report(new MigrationProgress(0, 1, "Skipping cleaning as no dirty files were found...", false));
                logger.WriteLine(MigrationStrings.Process_SkippingChecking);
            }

            progress?.Report(new MigrationProgress(0, 1, "Creating new binding files ...", false));
            logger.WriteLine(MigrationStrings.Process_ProcessingNewBinding);

            var progressAdapter = new FixedStepsProgressToMigrationProgressAdapter(progress);
            await unintrusiveBindingController.BindAsync(oldBinding, progressAdapter, token);

            // Now make all of the files changes required to remove the legacy settings
            // i.e. update project files and delete .sonarlint folder
            await MakeLegacyFileChangesAsync(legacySettings, changedFiles, progress);

            progress?.Report(new MigrationProgress(0, 1, "Migration finished successfully!", false));
            logger.WriteLine(MigrationStrings.Process_Finished);
        }

        private System.Threading.Tasks.Task<string> GetFileContentAsync(string filePath)
            => fileSystem.LoadAsTextAsync(filePath);

        private async System.Threading.Tasks.Task<ChangedFiles> CleanFilesAsync(IEnumerable<string> filesToClean,
            LegacySettings legacySettings,
            CancellationToken token)
        {
            var changedFiles = new ChangedFiles();

            var fileCount = filesToClean.Count();
            Debug.Assert(fileCount > 0, "Expecting to have at least one file to check");
            var currentFileNumber = 0;

            foreach (var file in filesToClean)
            {
                currentFileNumber++;
                logger.WriteLine(MigrationStrings.Process_CheckingFile, currentFileNumber, fileCount, file);
                var content = await GetFileContentAsync(file);

                var newContent = fileCleaner.Clean(content, legacySettings, token);
                Debug.Assert(newContent == null || !newContent.Equals(content),
                            "New file content should be null or different from original content");

                if (newContent == XmlFileCleaner.Unchanged)
                {
                    logger.WriteLine(MigrationStrings.Process_CheckedFile_Unchanged);
                }
                else
                {
                    logger.WriteLine(MigrationStrings.Process_CheckedFile_Changed);
                    changedFiles.Add(new FilePathAndContent<string>(file, newContent));
                }
            }

            logger.WriteLine(MigrationStrings.Process_NumberOfChangedFiles, changedFiles.Count);
            foreach (var file in changedFiles)
            {
                logger.WriteLine(MigrationStrings.Process_ListChangedFile, file.Path);
            }

            return changedFiles;
        }

        private async Task MakeLegacyFileChangesAsync(LegacySettings legacySettings, ChangedFiles changedFiles,
            IProgress<MigrationProgress> progress)
        {
            try
            {
                await fileSystem.BeginChangeBatchAsync();

                // Note: no files have been changed yet. Now we are going to start making changes
                // to the user's projects and deleting files that might be under source control...
                await SaveChangedFilesAsync(changedFiles, progress);

                await DeleteSonarLintFolderAsync(legacySettings, progress);
            }
            finally
            {
                await fileSystem.EndChangeBatchAsync();
            }
        }

        private async Task DeleteSonarLintFolderAsync(LegacySettings legacySettings,
            IProgress<MigrationProgress> progress)
        {
            // Note: SLVS will continue to detect the legacy binding mode until this step,
            // so if anything goes wrong during the migration and an exception occurs, the
            // user will see the migration gold bar next time they open the solution.
            progress?.Report(new MigrationProgress(0, 1, "Deleting old binding folder ...", false));
            logger.WriteLine(MigrationStrings.Process_DeletingSonarLintFolder);
            await fileSystem.DeleteFolderAsync(legacySettings.LegacySonarLintFolderPath);
        }

        private async Task SaveChangedFilesAsync(ChangedFiles changedFiles, IProgress<MigrationProgress> progress)
        {
            if (changedFiles == null)
            {
                return; // nothing to do
            }

            progress?.Report(new MigrationProgress(0, 1, "Saving modified files ...", false));
            logger.WriteLine(MigrationStrings.Process_SavingFiles);

            foreach (var file in changedFiles)
            {
                await fileSystem.SaveAsync(file.Path, file.Content);
            }
        }
    }
}
