//-----------------------------------------------------------------------
// <copyright file="SourceControlledFileSystem.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    internal class SourceControlledFileSystem : ISourceControlledFileSystem
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IFileSystem fileSystemWrapper;
        private readonly HashSet<string> filesEdit = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> filesCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<Func<bool>> fileWriteOperations = new Queue<Func<bool>>();
        private IVsQueryEditQuerySave2 queryFileOperation;

        public SourceControlledFileSystem(IServiceProvider serviceProvider)
            :this(serviceProvider, null)
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
                    success = success & this.fileWriteOperations.Dequeue().Invoke();
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

            if (KnownUIContexts.DebuggingContext.IsActive  || KnownUIContexts.SolutionBuildingContext.IsActive)
            {   
                // Don't reload files while debugging or building
                flags |= VsQueryEditFlags.NoReload;
            }

            uint verdict;
            uint moreInfo;
            ErrorHandler.ThrowOnFailure(this.QueryFileOperation.QueryEditFiles((uint)flags , fileNames.Length, fileNames, null, null, out verdict, out moreInfo));

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
