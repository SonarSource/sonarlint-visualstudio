/*
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
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IConnectedModeMigration))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class ConnectedModeMigration : IConnectedModeMigration
    {
        // Private "alias" to simplify method arguments
        private sealed class ChangedFiles : List<FilePathAndContent<string>>
        {
        }

        private readonly IMigrationSettingsProvider settingsProvider;
        private readonly IFileProvider fileProvider;
        private readonly IFileCleaner fileCleaner;
        private readonly IVsAwareFileSystem fileSystem;
        private readonly ISonarQubeService sonarQubeService;
        private readonly IUnintrusiveBindingController unintrusiveBindingController;
        private readonly IRoslynSuppressionUpdater roslynSuppressionUpdater;
        private readonly ISharedBindingConfigProvider sharedBindingConfigProvider;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly IServerConnectionsRepository serverConnectionsRepository;
        private readonly IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider;

        // The user can have both the legacy and new connected mode files. In that case, we expect the SonarQubeService to already be connected.
        private bool isAlreadyConnectedToServer;

        [ImportingConstructor]
        public ConnectedModeMigration(
            IMigrationSettingsProvider settingsProvider,
            IFileProvider fileProvider,
            IFileCleaner fileCleaner,
            IVsAwareFileSystem fileSystem,
            ISonarQubeService sonarQubeService,
            IUnintrusiveBindingController unintrusiveBindingController,
            IRoslynSuppressionUpdater roslynSuppressionUpdater,
            ISharedBindingConfigProvider sharedBindingConfigProvider,
            ILogger logger,
            IThreadHandling threadHandling,
            ISolutionInfoProvider solutionInfoProvider,
            IServerConnectionsRepository serverConnectionsRepository,
            IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider)
        {
            this.settingsProvider = settingsProvider;
            this.fileProvider = fileProvider;
            this.fileCleaner = fileCleaner;
            this.fileSystem = fileSystem;
            this.sonarQubeService = sonarQubeService;
            this.unintrusiveBindingController = unintrusiveBindingController;
            this.roslynSuppressionUpdater = roslynSuppressionUpdater;
            this.sharedBindingConfigProvider = sharedBindingConfigProvider;

            this.logger = logger;
            this.threadHandling = threadHandling;
            this.solutionInfoProvider = solutionInfoProvider;
            this.serverConnectionsRepository = serverConnectionsRepository;
            this.unintrusiveBindingPathProvider = unintrusiveBindingPathProvider;
        }

        public async Task MigrateAsync(
            BoundSonarQubeProject oldBinding,
            IProgress<MigrationProgress> progress,
            bool shareBinding,
            CancellationToken token)
        {
            isAlreadyConnectedToServer = sonarQubeService.IsConnected;

            await threadHandling.SwitchToBackgroundThread();

            try
            {
                if (!isAlreadyConnectedToServer)
                {
                    await sonarQubeService.ConnectAsync(oldBinding.CreateConnectionInformation(), token);
                }

                await MigrateImplAsync(oldBinding, progress, shareBinding, token);
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

        private async Task MigrateImplAsync(
            BoundSonarQubeProject oldBinding,
            IProgress<MigrationProgress> progress,
            bool shareBinding,
            CancellationToken token)
        {
            logger.WriteLine(MigrationStrings.Process_Starting);

            var legacySettings = await settingsProvider.GetAsync(oldBinding.ProjectKey);

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
                progress?.Report(new MigrationProgress(0, 1, "... skipping cleaning as no candidate files were found", false));
                logger.WriteLine(MigrationStrings.Process_SkippingChecking);
            }

            progress?.Report(new MigrationProgress(0, 1, "Creating new binding files ...", false));
            logger.WriteLine(MigrationStrings.Process_ProcessingNewBinding);

            var progressAdapter = new FixedStepsProgressToMigrationProgressAdapter(progress);
            var serverConnection = GetServerConnectionWithMigration(oldBinding);
            await unintrusiveBindingController.BindAsync(BoundServerProject.FromBoundSonarQubeProject(oldBinding, await solutionInfoProvider.GetSolutionNameAsync(), serverConnection), progressAdapter,
                token);

            // Now make all of the files changes required to remove the legacy settings
            // i.e. update project files and delete .sonarlint folder
            await MakeLegacyFileChangesAsync(legacySettings, changedFiles, progress, token);

            // Trigger a re-fetch of suppressions so the Roslyn settings are updated.
            await roslynSuppressionUpdater.UpdateAllServerSuppressionsAsync();

            if (shareBinding)
            {
                progress?.Report(new MigrationProgress(0, 1, "Saving Shared Binding Config...", false));
                var saveSuccess = SaveSharedBinding(oldBinding);
                if (saveSuccess == false)
                {
                    progress?.Report(new MigrationProgress(0, 1, "... Failed to save Binding Config. Skippig the step", false));
                }
            }

            progress?.Report(new MigrationProgress(0, 1, "Migration finished successfully!", false));
            logger.WriteLine(MigrationStrings.Process_Finished);
        }

        private bool SaveSharedBinding(BoundSonarQubeProject binding)
        {
            var sharedBindingConfigModel = new SharedBindingConfigModel { Organization = binding.Organization?.Key, ProjectKey = binding.ProjectKey, Uri = binding.ServerUri };
            return sharedBindingConfigProvider.SaveSharedBinding(sharedBindingConfigModel) != null;
        }

        private Task<string> GetFileContentAsync(string filePath) => fileSystem.LoadAsTextAsync(filePath);

        private async Task<ChangedFiles> CleanFilesAsync(
            IEnumerable<string> filesToClean,
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

        private async Task MakeLegacyFileChangesAsync(
            LegacySettings legacySettings,
            ChangedFiles changedFiles,
            IProgress<MigrationProgress> progress,
            CancellationToken token)
        {
            // Last check before making changes to files on disc...
            token.ThrowIfCancellationRequested();

            try
            {
                await fileSystem.BeginChangeBatchAsync();

                // Note: no files have been changed yet. Now we are going to start making changes
                // to the user's projects and deleting files that might be under source control...
                await SaveChangedFilesAsync(changedFiles, progress, token);

                await DeleteSonarLintFolderAsync(legacySettings, progress, token);
            }
            finally
            {
                await fileSystem.EndChangeBatchAsync();
            }
        }

        private async Task DeleteSonarLintFolderAsync(
            LegacySettings legacySettings,
            IProgress<MigrationProgress> progress,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // Note: SLVS will continue to detect the legacy binding mode until this step,
            // so if anything goes wrong during the migration and an exception occurs, the
            // user will see the migration gold bar next time they open the solution.
            progress?.Report(new MigrationProgress(0, 1, "Deleting old binding folder ...", false));
            logger.WriteLine(MigrationStrings.Process_DeletingSonarLintFolder);
            await fileSystem.DeleteFolderAsync(legacySettings.LegacySonarLintFolderPath);
        }

        private async Task SaveChangedFilesAsync(
            ChangedFiles changedFiles,
            IProgress<MigrationProgress> progress,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (changedFiles == null)
            {
                return; // nothing to do
            }

            progress?.Report(new MigrationProgress(0, 1, "Saving modified files ...", false));
            logger.WriteLine(MigrationStrings.Process_SavingFiles);

            foreach (var file in changedFiles)
            {
                token.ThrowIfCancellationRequested();
                await fileSystem.SaveAsync(file.Path, file.Content);
            }
        }

        private ServerConnection GetServerConnectionWithMigration(BoundSonarQubeProject project)
        {
            if (ServerConnection.FromBoundSonarQubeProject(project) is not { } proposedConnection)
            {
                throw new InvalidOperationException(BindingStrings.UnintrusiveController_InvalidConnection);
            }

            // at this point we expect that the connections file exist if there are bindings in new format, meaning that all the existing bindings are already migrated to the newer format
            // if the file doesn't exist, creating it now will prevent the migration of all existing bindings.
            // The order being important, we throw an exception. For more info see IBindingToConnectionMigration
            // But if there are no bindings in the new format, then creating the connections file is safe, because the new migration did not do anything in this case
            if (!serverConnectionsRepository.ConnectionsFileExists() && unintrusiveBindingPathProvider.GetBindingPaths().Any())
            {
                logger.WriteLine(MigrationStrings.ConnectionsJson_DoesNotExist);
                return proposedConnection;
            }

            if (!serverConnectionsRepository.TryGet(proposedConnection.Id, out _))
            {
                serverConnectionsRepository.TryAdd(proposedConnection);
            }

            return proposedConnection;
        }
    }
}
