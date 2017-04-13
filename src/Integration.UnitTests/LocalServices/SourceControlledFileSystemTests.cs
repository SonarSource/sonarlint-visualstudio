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
            // Arrange
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file = @"Z:\Y\XXX \n.lll";

            // Case 1: file exists
            this.fileSystem.RegisterFile(file);

            // Act + Assert
            testSubject.FileExistOrQueuedToBeWritten(file.ToLowerInvariant()).Should().BeTrue();

            // Case 2: file not exists, but pending
            this.fileSystem.ClearFiles();
            testSubject.QueueFileWrite(file, () => true);

            // Act + Assert
            testSubject.FileExistOrQueuedToBeWritten(file.ToUpperInvariant()).Should().BeTrue();

            // Case 3: file not exists and not pending
            testSubject.WriteQueuedFiles();

            // Act + Assert
            testSubject.FileExistOrQueuedToBeWritten(file).Should().BeFalse();
        }

        [TestMethod]
        public void SourceControlledFileSystem_QueueFileWrite_QueryNewFile()
        {
            // Arrange
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file = @"Z:\Y\XXX \n.lll";
            bool pendExecuted = false;

            // Act
            testSubject.QueueFileWrite(file, () => pendExecuted = true);
            testSubject.WriteQueuedFiles().Should().BeTrue("Not expecting any errors");

            // Assert
            this.queryEditAndSave.AssertCreateRequested(file);
            this.queryEditAndSave.AssertNoEditRequested();
            pendExecuted.Should().BeTrue("Expected to be executed");
        }

        [TestMethod]
        public void SourceControlledFileSystem_QueueFileWrite_QueryEditFile()
        {
            // Arrange
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file = @"Z:\Y\XXX \n.lll";
            this.fileSystem.RegisterFile(file);
            bool pendExecuted = false;

            // Act
            testSubject.QueueFileWrite(file, () => pendExecuted = true);
            testSubject.WriteQueuedFiles().Should().BeTrue("Not expecting any errors");

            // Assert
            this.queryEditAndSave.AssertNoCreateRequested();
            this.queryEditAndSave.AssertEditRequested(file);
            pendExecuted.Should().BeTrue("Expected to be executed");
        }

        [TestMethod]
        public void SourceControlledFileSystem_QueueFileWrite_WriteQueuedFiles_ExecutionOrder()
        {
            // Arrange
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

            // Assert
            this.queryEditAndSave.AssertCreateRequested(file2, file3);
            this.queryEditAndSave.AssertEditRequested(file1);
            CollectionAssert.AreEqual(new[] { file1, file2, file3 }, executionOrder.ToArray(), "Unexpected execution order");
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_CheckoutFileWhenWhenDebugging()
        {
            // Arrange
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.RegisterFile(file1);
            this.queryEditAndSave.VerifyQueryEditFlags |= (uint)VsQueryEditFlags.NoReload;
            KnownUIContextsAccessor.MonitorSelectionService.SetContext(VSConstants.UICONTEXT.Debugging_guid, true);
            testSubject.QueueFileWrite(file1, () => true);

            // Act
            testSubject.WriteQueuedFiles().Should().BeTrue("Not expecting any errors");

            // Assert
            this.queryEditAndSave.AssertEditRequested(file1);
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_CheckoutFileWhenBuilding()
        {
            // Arrange
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.RegisterFile(file1);
            this.queryEditAndSave.VerifyQueryEditFlags |= (uint)VsQueryEditFlags.NoReload;
            KnownUIContextsAccessor.MonitorSelectionService.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);
            testSubject.QueueFileWrite(file1, () => true);

            // Act
            testSubject.WriteQueuedFiles().Should().BeTrue("Not expecting any errors");

            // Assert
            this.queryEditAndSave.AssertEditRequested(file1);
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_QueryEditFilesFailed()
        {
            // Arrange
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.RegisterFile(file1);
            this.queryEditAndSave.QueryEditFilesVerdict = tagVSQueryEditResult.QER_EditNotOK;
            testSubject.QueueFileWrite(file1, () => true);

            // Act
            testSubject.WriteQueuedFiles().Should().BeFalse("Failed to checkout");

            // Assert
            this.queryEditAndSave.AssertEditRequested(file1);
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_NoisyPromptForCreateOperationIsRequired()
        {
            // Arrange
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

            // Assert
            this.queryEditAndSave.AssertCreateRequested(file1, file1); // Twice silent and then noisy
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_Batching()
        {
            // Arrange
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

            // Assert
            this.queryEditAndSave.AssertCreateRequested(file2);
            this.queryEditAndSave.AssertEditRequested(file1, file3);
            this.queryEditAndSave.AssertAllBatchesCompleted(1);
            CollectionAssert.AreEqual(new[] { file1 }, executionOrder.ToArray(), "Only the first was expected to execute");

            // Act (write again)
            this.queryEditAndSave.Reset();
            executionOrder.Clear();

            // Assert (the test subject should have been cleared from previous state)
            testSubject.WriteQueuedFiles().Should().BeTrue("Should succeed since there's nothing pending");
            this.queryEditAndSave.AssertAllBatchesCompleted(1);
            this.queryEditAndSave.AssertNoCreateRequested();
            this.queryEditAndSave.AssertNoEditRequested();
            executionOrder.Should().BeEmpty("Unexpected execution occurred");
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_FailureInWriteOperation()
        {
            // Arrange
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            string file2 = @"Z:\Y\XXX \3.lll";

            // Act
            testSubject.QueueFileWrite(file1, () => false);
            testSubject.QueueFileWrite(file2, () => true);
            testSubject.WriteQueuedFiles().Should().BeFalse("Expecting a failure");

            // Assert
            this.queryEditAndSave.AssertCreateRequested(file1, file2);
            this.queryEditAndSave.AssertNoEditRequested();
            this.queryEditAndSave.AssertAllBatchesCompleted(1);
        }
    }
}