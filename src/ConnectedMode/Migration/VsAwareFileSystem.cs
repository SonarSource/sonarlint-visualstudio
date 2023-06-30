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
using System.Diagnostics;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IVsAwareFileSystem))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class VsAwareFileSystem : IVsAwareFileSystem
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;
        private readonly IThreadHandling threadHandling;
        private IVsQueryEditQuerySave2 sccService;

        private bool batchStarted = false;

        [ImportingConstructor]
        public VsAwareFileSystem([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ILogger logger,
            IThreadHandling threadHandling)
            : this(serviceProvider, logger, threadHandling, new FileSystem())
        { }

        internal /* for testing */ VsAwareFileSystem(IServiceProvider serviceProvider,
            ILogger logger,
            IThreadHandling threadHandling,
            IFileSystem fileSystem)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.fileSystem = fileSystem;
            this.threadHandling = threadHandling;
        }

        public async Task DeleteFolderAsync(string folderPath)
        {
            Debug.Assert(batchStarted, "Expecting the changes to happen in a batch");   

            logger.LogMigrationVerbose($"Deleting directory: {folderPath}");

            await CheckOutFilesToDeleteAsync(folderPath);

            fileSystem.Directory.Delete(folderPath, true);
        }

        public Task<string> LoadAsTextAsync(string filePath)
        {
            logger.LogMigrationVerbose($"Reading file: {filePath}");
            var content = fileSystem.File.ReadAllText(filePath);
            return Task.FromResult(content);
        }

        public async Task SaveAsync(string filePath, string text)
        {
            Debug.Assert(batchStarted, "Expecting the changes to happen in a batch");

            logger.LogMigrationVerbose($"Saving file: {filePath}");

            bool success = await CheckoutForEditAsync(filePath);

            if (!success)
            {
                var message = string.Format(MigrationStrings.VSFileSystem_Error_FailedToCheckOutFile, filePath);
                throw new InvalidOperationException(message);
            }

            fileSystem.File.WriteAllText(filePath, text);
        }

        public async Task BeginChangeBatchAsync()
        {
            Debug.Assert(!batchStarted, "Not expecting to already be in a batch");

            logger.LogMigrationVerbose("Beginning batch of file changes...");

            sccService = await GetSccServiceAsync();
            sccService.BeginQuerySaveBatch();
            batchStarted = true;
        }

        public Task EndChangeBatchAsync()
        {
            logger.LogMigrationVerbose("Batch file changes completed");
            sccService.EndQuerySaveBatch();
            batchStarted = false;
            return Task.CompletedTask;
        }

        private async Task<IVsQueryEditQuerySave2> GetSccServiceAsync()
        {
            IVsQueryEditQuerySave2 sccService = null;
            await threadHandling.RunOnUIThread(() =>
            {
                sccService = serviceProvider.GetService(typeof(SVsQueryEditQuerySave)) as IVsQueryEditQuerySave2;
            });

            return sccService;
        }

        private async Task CheckOutFilesToDeleteAsync(string folderPath)
        {
            // Can't check out the directory for saving - if you do, VS will pop up a dialogue,
            // even if the "silent" flag is set.
            bool success = await CheckoutForEditAsync(folderPath);

            if (success)
            {
                var files = fileSystem.Directory.GetFiles(folderPath, "*.*", System.IO.SearchOption.AllDirectories);
                success = await CheckoutForSaveAsync(files);
            }

            if (!success)
            {
                var message = string.Format(MigrationStrings.VsFileSystem_Error_FailedToCheckOutFolderForDeletion, folderPath);
                throw new InvalidOperationException(message);
            }
        }

        private async Task<bool> CheckoutForEditAsync(params string[] fileNames)
        {
            Debug.Assert(sccService != null, "sccService should not be null");
            logger.LogMigrationVerbose("Checking out file(s) for edit: " +
                string.Join(", ", fileNames));

            VsQueryEditFlags flags = VsQueryEditFlags.SilentMode | VsQueryEditFlags.DetectAnyChangedFile |
                // Force no prompting here to fix SLVS#801 (otherwise TFS would prompt about the slconfig
                // and generated ruleset files in new connected mode that are not in the solution)
                VsQueryEditFlags.ForceEdit_NoPrompting;

            uint verdict = 0;
            uint moreInfo = 0;

            await threadHandling.RunOnUIThread(() =>
            {
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(sccService.QueryEditFiles((uint)flags, fileNames.Length, fileNames, null, null, out verdict, out moreInfo));
            });

            var success = (tagVSQueryEditResult.QER_EditOK == (tagVSQueryEditResult)verdict);
            if (!success)
            {
                this.logger.WriteLine(MigrationStrings.VSFileSystem_FailedToCheckOutFilesForEditing, (tagVSQueryEditResultFlags)moreInfo);
            }
            return success;
        }

        private async Task<bool> CheckoutForSaveAsync(params string[] fileNames)
        {
            bool result = false;

            await threadHandling.RunOnUIThread(() =>
                result = DoCheckoutForSave(fileNames));

            return result;
        }

        private bool DoCheckoutForSave(params string[] fileNames)
        {
            uint result;
            uint[] rgrgf = new uint[fileNames.Length];

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(sccService.QuerySaveFiles((uint)VsQuerySaveFlags.SilentMode, fileNames.Length, fileNames, rgrgf, null, out result));
            tagVSQuerySaveResult saveResult = (tagVSQuerySaveResult)result;

            if (saveResult == tagVSQuerySaveResult.QSR_NoSave_NoisyPromptRequired)
            {
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(sccService.QuerySaveFiles((uint)VsQuerySaveFlags.DefaultOperation, fileNames.Length, fileNames, rgrgf, null, out result));
                saveResult = (tagVSQuerySaveResult)result;
            }

            var success = (tagVSQuerySaveResult.QSR_SaveOK == saveResult);
            if (!success)
            {
                this.logger.WriteLine(MigrationStrings.VSFileSystem_FailedToCheckOutFilesForSave, saveResult);
            }
            return success;
        }

    }
}
