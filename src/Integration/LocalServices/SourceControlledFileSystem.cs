/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.IO;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// A general helper class that deals with all the various source control systems integrated with VS (without using any SCC specific APIs).
    /// The idea is to checkout files (and notify that about to create new files) by using the <see cref="IVsQueryEditQuerySave2"/> service
    /// which will in turn notify the source control system and will delegate the rest of the work to it (i.e. checking it out).
    /// </summary>
    internal class SourceControlledFileSystem : ISourceControlledFileSystem
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IFileSystem fileSystemWrapper;
        private readonly HashSet<string> filesEdit = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> filesCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<Func<bool>> fileWriteOperations = new Queue<Func<bool>>();
        private IVsQueryEditQuerySave2 queryFileOperation;

        public SourceControlledFileSystem(IServiceProvider serviceProvider)
            : this(serviceProvider, null)
        {
        }

        internal /*for testing purposes*/ SourceControlledFileSystem(IServiceProvider serviceProvider, IFileSystem fileSystem)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;
            this.fileSystemWrapper = fileSystem ?? this;
        }

        protected IVsQueryEditQuerySave2 QueryFileOperation
        {
            get
            {
                if (this.queryFileOperation == null)
                {
                    this.queryFileOperation = this.serviceProvider.GetService<SVsQueryEditQuerySave, IVsQueryEditQuerySave2>();
                }

                return this.queryFileOperation;
            }
        }

        #region ISourceControlledFileSystem
        public void QueueFileWrite(string filePath, Func<bool> fileWriteOperation)
        {
            if (this.fileSystemWrapper.FileExist(filePath))
            {
                this.filesEdit.Add(filePath);
            }
            else
            {
                this.filesCreate.Add(filePath);
            }

            this.fileWriteOperations.Enqueue(fileWriteOperation);
        }

        public bool FileExistOrQueuedToBeWritten(string filePath)
        {
            return this.filesCreate.Contains(filePath) || this.fileSystemWrapper.FileExist(filePath);
        }

        public bool WriteQueuedFiles()
        {
            bool success = true;
            this.QueryFileOperation.BeginQuerySaveBatch();
            try
            {
                if (this.filesEdit.Count > 0)
                {
                    success = success && this.CheckoutForEdit(this.filesEdit.ToArray());
                }

                if (success && this.filesCreate.Count > 0)
                {
                    success = success && this.CheckoutForSave(this.filesCreate.ToArray());
                }

                while (success && this.fileWriteOperations.Count > 0)
                {
                    success = success && this.fileWriteOperations.Dequeue().Invoke();
                }
            }
            finally
            {
                this.filesEdit.Clear();
                this.filesCreate.Clear();
                this.fileWriteOperations.Clear();

                this.QueryFileOperation.EndQuerySaveBatch(); // If we would not be silent this would show a UI for the batch.
            }

            return success;
        }
        #endregion

        #region IFileSystem
        bool IFileSystem.DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        bool IFileSystem.FileExist(string filePath)
        {
            return File.Exists(filePath);
        }

        void IFileSystem.CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        #endregion

        #region Helpers
        private bool CheckoutForEdit(params string[] fileNames)
        {
            VsQueryEditFlags flags = VsQueryEditFlags.SilentMode | VsQueryEditFlags.DetectAnyChangedFile;

            if (KnownUIContexts.DebuggingContext.IsActive || KnownUIContexts.SolutionBuildingContext.IsActive)
            {
                // Don't reload files while debugging or building
                flags |= VsQueryEditFlags.NoReload;
            }

            uint verdict;
            uint moreInfo;
            ErrorHandler.ThrowOnFailure(this.QueryFileOperation.QueryEditFiles((uint)flags, fileNames.Length, fileNames, null, null, out verdict, out moreInfo));

            return tagVSQueryEditResult.QER_EditOK == (tagVSQueryEditResult)verdict;
        }

        private bool CheckoutForSave(params string[] fileNames)
        {
            uint result;
            uint[] rgrgf = new uint[fileNames.Length];

            ErrorHandler.ThrowOnFailure(this.QueryFileOperation.QuerySaveFiles((uint)VsQuerySaveFlags.SilentMode, fileNames.Length, fileNames, rgrgf, null, out result));
            tagVSQuerySaveResult saveResult = (tagVSQuerySaveResult)result;

            if (saveResult == tagVSQuerySaveResult.QSR_NoSave_NoisyPromptRequired)
            {
                ErrorHandler.ThrowOnFailure(this.QueryFileOperation.QuerySaveFiles((uint)VsQuerySaveFlags.DefaultOperation, fileNames.Length, fileNames, rgrgf, null, out result));
                saveResult = (tagVSQuerySaveResult)result;
            }

            return tagVSQuerySaveResult.QSR_SaveOK == saveResult;
        }
        #endregion
    }
}
