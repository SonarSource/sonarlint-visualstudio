/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSourceControlledFileSystem : ConfigurableFileSystem, ISourceControlledFileSystem
    {
        private readonly Dictionary<string, Func<bool>> fileWriteOperations = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase);

        #region ISourceControlledFileSystem

        bool ISourceControlledFileSystem.FileExistOrQueuedToBeWritten(string filePath)
        {
            return this.fileWriteOperations.ContainsKey(filePath) || ((IFileSystem)this).FileExist(filePath);
        }

        void ISourceControlledFileSystem.QueueFileWrite(string filePath, Func<bool> fileWriteOperation)
        {
            this.fileWriteOperations.Should().NotContainKey(filePath, "Not expected to modify the same file during execution");
            fileWriteOperation.Should().NotBeNull("Not expecting the operation to be null");

            fileWriteOperations[filePath] = fileWriteOperation;
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

        public void ClearPending()
        {
            this.fileWriteOperations.Clear();
        }

        #endregion Test helpers
    }
}