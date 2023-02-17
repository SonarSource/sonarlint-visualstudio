﻿/*
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
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.TestInfrastructure
{
    internal class ConfigurableSourceControlledFileSystem : ISourceControlledFileSystem
    {
        private readonly IFileSystem fileSystem;

        public ConfigurableSourceControlledFileSystem(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        private readonly Dictionary<string, Func<bool>> fileWriteOperations = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase);

        #region ISourceControlledFileSystem

        public bool FilesExistOrQueuedToBeWritten(IEnumerable<string> filePaths)
        {
            return filePaths.All(filePath => fileWriteOperations.ContainsKey(filePath) || fileSystem.File.Exists(filePath));
        }

        public void QueueFileWrites(IEnumerable<string> filePaths, Func<bool> fileWriteOperation)
        {
            foreach (var filePath in filePaths)
            {
                fileWriteOperations.Should().NotContainKey(filePath, "Not expected to modify the same file during execution");
                fileWriteOperation.Should().NotBeNull("Not expecting the operation to be null");

                fileWriteOperations[filePath] = fileWriteOperation;
            }
        }

        bool ISourceControlledFileSystem.WriteQueuedFiles()
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

        #endregion ISourceControlledFileSystem

        #region Test helpers

        public bool WritePending { get; set; } = true;

        public void WritePendingNoErrorsExpected()
        {
            ((ISourceControlledFileSystem)this).WriteQueuedFiles().Should().BeTrue("Failed to write all the pending files");
        }

        public void WritePendingErrorsExpected()
        {
            ((ISourceControlledFileSystem)this).WriteQueuedFiles().Should().BeFalse("Expected to fail writing the pending files");
        }

        public void WritePendingFiles()
        {
            ((ISourceControlledFileSystem)this).WriteQueuedFiles();
        }

        public void ClearPending()
        {
            this.fileWriteOperations.Clear();
        }

        public void AssertQueuedOperationCount(int expected)
        {
            this.fileWriteOperations.Count.Should().Be(expected);
        }

        #endregion Test helpers
    }
}
