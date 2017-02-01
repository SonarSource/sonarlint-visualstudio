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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SourceControlledFileSystemTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsQueryEditQuerySave2 queryEditAndSave;
        private ConfigurableFileSystem fileSystem;

        [TestInitialize]
        public void TestInitialize()
        {
            KnownUIContextsAccessor.Reset();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.queryEditAndSave = new ConfigurableVsQueryEditQuerySave2();
            this.serviceProvider.RegisterService(typeof(SVsQueryEditQuerySave), this.queryEditAndSave);
            this.fileSystem = new ConfigurableFileSystem();
        }

        [TestMethod]
        public void SourceControlledFileSystem_ArgCheck()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SourceControlledFileSystem(null));
        }

        [TestMethod]
        public void SourceControlledFileSystem_FileExistOrQueuedToBeWritten()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file = @"Z:\Y\XXX \n.lll";

            // Case 1: file exists
            this.fileSystem.RegisterFile(file);

            // Act + Verify
            testSubject.FileExistOrQueuedToBeWritten(file.ToLowerInvariant()).Should().BeTrue();

            // Case 2: file not exists, but pending
            this.fileSystem.ClearFiles();
            testSubject.QueueFileWrite(file, () => true);

            // Act + Verify
            testSubject.FileExistOrQueuedToBeWritten(file.ToUpperInvariant()).Should().BeTrue();

            // Case 3: file not exists and not pending
            testSubject.WriteQueuedFiles();

            // Act + Verify
            testSubject.FileExistOrQueuedToBeWritten(file).Should().BeFalse();
        }

        [TestMethod]
        public void SourceControlledFileSystem_QueueFileWrite_QueryNewFile()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file = @"Z:\Y\XXX \n.lll";
            bool pendExecuted = false;

            // Act
            testSubject.QueueFileWrite(file, () => pendExecuted = true);
            testSubject.WriteQueuedFiles().Should().BeTrue("Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file);
            this.queryEditAndSave.AssertNoEditRequested();
            pendExecuted.Should().BeTrue("Expected to be executed");
        }

        [TestMethod]
        public void SourceControlledFileSystem_QueueFileWrite_QueryEditFile()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file = @"Z:\Y\XXX \n.lll";
            this.fileSystem.RegisterFile(file);
            bool pendExecuted = false;

            // Act
            testSubject.QueueFileWrite(file, () => pendExecuted = true);
            testSubject.WriteQueuedFiles().Should().BeTrue("Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertNoCreateRequested();
            this.queryEditAndSave.AssertEditRequested(file);
            pendExecuted.Should().BeTrue("Expected to be executed");
        }

        [TestMethod]
        public void SourceControlledFileSystem_QueueFileWrite_WriteQueuedFiles_ExecutionOrder()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            string file2 = @"Z:\Y\XXX \3.lll";
            string file3 = @"Z:\Y\XXX \2.lll";
            this.fileSystem.RegisterFile(file1);
            List<string> executionOrder = new List<string>();

            // Act
            testSubject.QueueFileWrite(file1, () => { executionOrder.Add(file1); return true; });
            testSubject.QueueFileWrite(file2, () => { executionOrder.Add(file2); return true; });
            testSubject.QueueFileWrite(file3, () => { executionOrder.Add(file3); return true; });
            testSubject.WriteQueuedFiles().Should().BeTrue("Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file2, file3);
            this.queryEditAndSave.AssertEditRequested(file1);
            CollectionAssert.AreEqual(new[] { file1, file2, file3 }, executionOrder.ToArray(), "Unexpected execution order");
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_CheckoutFileWhenWhenDebugging()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.RegisterFile(file1);
            this.queryEditAndSave.VerifyQueryEditFlags |= (uint)VsQueryEditFlags.NoReload;
            KnownUIContextsAccessor.MonitorSelectionService.SetContext(VSConstants.UICONTEXT.Debugging_guid, true);
            testSubject.QueueFileWrite(file1, () => true);

            // Act
            testSubject.WriteQueuedFiles().Should().BeTrue("Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertEditRequested(file1);
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_CheckoutFileWhenBuilding()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.RegisterFile(file1);
            this.queryEditAndSave.VerifyQueryEditFlags |= (uint)VsQueryEditFlags.NoReload;
            KnownUIContextsAccessor.MonitorSelectionService.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);
            testSubject.QueueFileWrite(file1, () => true);

            // Act
            testSubject.WriteQueuedFiles().Should().BeTrue("Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertEditRequested(file1);
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_QueryEditFilesFailed()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.RegisterFile(file1);
            this.queryEditAndSave.QueryEditFilesVerdict = tagVSQueryEditResult.QER_EditNotOK;
            testSubject.QueueFileWrite(file1, () => true);

            // Act
            testSubject.WriteQueuedFiles().Should().BeFalse("Failed to checkout");

            // Verify
            this.queryEditAndSave.AssertEditRequested(file1);
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_NoisyPromptForCreateOperationIsRequired()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            this.queryEditAndSave.QuerySaveFilesVerification = flags =>
            {
                if (flags == VsQuerySaveFlags.SilentMode)
                {
                     return tagVSQuerySaveResult.QSR_NoSave_NoisyPromptRequired;
                }
                if (flags == VsQuerySaveFlags.DefaultOperation)
                {
                    return tagVSQuerySaveResult.QSR_SaveOK;
                }

                return tagVSQuerySaveResult.QSR_ForceSaveAs;
            };
            testSubject.QueueFileWrite(file1, () => true);

            // Act
            testSubject.WriteQueuedFiles().Should().BeTrue("Failed to checkout");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file1, file1); // Twice silent and then noisy
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_Batching()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            string file2 = @"Z:\Y\XXX \3.lll";
            string file3 = @"Z:\Y\XXX \2.lll";
            this.fileSystem.RegisterFile(file1);
            this.fileSystem.RegisterFile(file3);
            List<string> executionOrder = new List<string>();

            // Act
            testSubject.QueueFileWrite(file1, () => { executionOrder.Add(file1); return false; });
            testSubject.QueueFileWrite(file2, () => { executionOrder.Add(file2); return true; });
            testSubject.QueueFileWrite(file3, () => { executionOrder.Add(file3); return true; });
            testSubject.WriteQueuedFiles().Should().BeFalse("Expected to fail");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file2);
            this.queryEditAndSave.AssertEditRequested(file1, file3);
            this.queryEditAndSave.AssertAllBatchesCompleted(1);
            CollectionAssert.AreEqual(new[] { file1 }, executionOrder.ToArray(), "Only the first was expected to execute");

            // Act (write again)
            this.queryEditAndSave.Reset();
            executionOrder.Clear();

            // Verify (the test subject should have been cleared from previous state)
            testSubject.WriteQueuedFiles().Should().BeTrue("Should succeed since there's nothing pending");
            this.queryEditAndSave.AssertAllBatchesCompleted(1);
            this.queryEditAndSave.AssertNoCreateRequested();
            this.queryEditAndSave.AssertNoEditRequested();
            executionOrder.Should().HaveCount(0, "Unexpected execution occurred");
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_FailureInWriteOperation()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            string file2 = @"Z:\Y\XXX \3.lll";

            // Act
            testSubject.QueueFileWrite(file1, () => false);
            testSubject.QueueFileWrite(file2, () => true);
            testSubject.WriteQueuedFiles().Should().BeFalse("Expecting a failure");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file1, file2);
            this.queryEditAndSave.AssertNoEditRequested();
            this.queryEditAndSave.AssertAllBatchesCompleted(1);
        }
    }
}
