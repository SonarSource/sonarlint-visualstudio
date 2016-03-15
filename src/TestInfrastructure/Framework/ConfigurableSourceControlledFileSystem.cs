//-----------------------------------------------------------------------
// <copyright file="ConfigurableSourceControlledFileSystem.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSourceControlledFileSystem : ConfigurableFileSystem, ISourceControlledFileSystem
    {
        private readonly Dictionary<string, Func<bool>> fileWriteOperations = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase);

        #region ISourceControlledFileSystem
        bool ISourceControlledFileSystem.IsFileExistOrPendingWrite(string filePath)
        {
            return this.fileWriteOperations.ContainsKey(filePath) || ((IFileSystem)this).IsFileExist(filePath);
        }

        void ISourceControlledFileSystem.PendFileWrite(string filePath, Func<bool> fileWriteOperation)
        {
            Assert.IsFalse(this.fileWriteOperations.ContainsKey(filePath), "Not expected to modify the same file during execution");
            Assert.IsNotNull(fileWriteOperation, "Not expecting the operation to be null");

            fileWriteOperations[filePath] = fileWriteOperation;
        }

        bool ISourceControlledFileSystem.WritePendingFiles()
        {
            if (this.WritePending)
            {
                foreach (var op in this.fileWriteOperations.Values)
                {
                    if (!op())
                    {
                        return false;
                    }
                }

            }

            this.fileWriteOperations.Clear();

            return true;
        }
        #endregion

        #region Test helpers

        public bool WritePending { get; set; } = true;

        public void WritePendingNoErrorsExpected()
        {
            Assert.IsTrue(((ISourceControlledFileSystem)this).WritePendingFiles(), "Failed to write all the pending files");
        }

        public void WritePendingErrorsExpected()
        {
            Assert.IsFalse(((ISourceControlledFileSystem)this).WritePendingFiles(), "Expected to fail writing the pending files");
        }

        public void ClearPending()
        {
            this.fileWriteOperations.Clear();
        }
        #endregion
    }
}
