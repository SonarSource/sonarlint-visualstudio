//-----------------------------------------------------------------------
// <copyright file="ISourceControlledFileSystem.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Provides abstraction over the SCC file management. The files are queued up using <see cref="PendFileWrite(string, Func{bool})"/>
    /// until <see cref="WritePendingFiles"/> which will call the SCC to check out the file and will run the queued up write operations.
    /// </summary>
    internal interface ISourceControlledFileSystem : IFileSystem
    {
        /// <summary>
        /// Register write operation for a file path. New or edit is determined internally at the time of this method execution.
        /// </summary>
        /// <param name="filePath">File path that want to write to</param>
        /// <param name="fileWriteOperation">The actual write operation, return false if failed</param>
        void PendFileWrite(string filePath, Func<bool> fileWriteOperation);

        /// <summary>
        /// Returns whether the file exists on disk or has a pend write to it
        /// </summary>
        bool IsFileExistOrPendingWrite(string filePath);

        /// <summary>
        /// Checks out the file if the solution is under source control, and then executes all the file writes.
        /// </summary>
        /// <returns>Whether was able to checkout (if applicable) and write to all the files</returns>
        bool WritePendingFiles();
    }
}
