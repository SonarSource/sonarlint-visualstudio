//-----------------------------------------------------------------------
// <copyright file="SourceControlledFileSystemTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

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
        public void SourceControlledFileSystem_IsFileExistOrPendingWrite()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file = @"Z:\Y\XXX \n.lll";

            // Case 1: file exists
            this.fileSystem.RegisterFile(file);

            // Act + Verify
            Assert.IsTrue(testSubject.IsFileExistOrPendingWrite(file.ToLowerInvariant()));

            // Case 2: file not exists, but pending
            this.fileSystem.ClearFiles();
            testSubject.PendFileWrite(file, () => true);

            // Act + Verify
            Assert.IsTrue(testSubject.IsFileExistOrPendingWrite(file.ToUpperInvariant()));

            // Case 3: file not exists and not pending
            testSubject.WritePendingFiles();

            // Act + Verify
            Assert.IsFalse(testSubject.IsFileExistOrPendingWrite(file));
        }

        [TestMethod]
        public void SourceControlledFileSystem_PendFileWrite_QueryNewFile()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file = @"Z:\Y\XXX \n.lll";
            bool pendExecuted = false;

            // Act
            testSubject.PendFileWrite(file, () => pendExecuted = true);
            Assert.IsTrue(testSubject.WritePendingFiles(), "Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file);
            this.queryEditAndSave.AssertEditRequested(new string[0]);
            Assert.IsTrue(pendExecuted, "Expected to be executed");
        }

        [TestMethod]
        public void SourceControlledFileSystem_PendFileWrite_QueryEditFile()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file = @"Z:\Y\XXX \n.lll";
            this.fileSystem.RegisterFile(file);
            bool pendExecuted = false;

            // Act
            testSubject.PendFileWrite(file, () => pendExecuted = true);
            Assert.IsTrue(testSubject.WritePendingFiles(), "Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(new string[0]);
            this.queryEditAndSave.AssertEditRequested(file);
            Assert.IsTrue(pendExecuted, "Expected to be executed");
        }

        [TestMethod]
        public void SourceControlledFileSystem_PendFileWrite_ExecutionOrder()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            string file2 = @"Z:\Y\XXX \3.lll";
            string file3 = @"Z:\Y\XXX \2.lll";
            this.fileSystem.RegisterFile(file1);
            List<string> executionOrder = new List<string>();

            // Act
            testSubject.PendFileWrite(file1, () => { executionOrder.Add(file1); return true; });
            testSubject.PendFileWrite(file2, () => { executionOrder.Add(file2); return true; });
            testSubject.PendFileWrite(file3, () => { executionOrder.Add(file3); return true; });
            Assert.IsTrue(testSubject.WritePendingFiles(), "Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file2, file3);
            this.queryEditAndSave.AssertEditRequested(file1);
            CollectionAssert.AreEqual(new[] { file1, file2, file3 }, executionOrder.ToArray(), "Unexpected execution order");
        }

        [TestMethod]
        public void SourceControlledFileSystem_WritePendingFiles_EditWhenDebugging()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.RegisterFile(file1);
            this.queryEditAndSave.VerifyQueryEditFlags |= (uint)VsQueryEditFlags.NoReload;
            KnownUIContextsAccessor.MonitorSelectionService.SetContext(VSConstants.UICONTEXT.Debugging_guid, true);
            testSubject.PendFileWrite(file1, () => true);

            // Act
            Assert.IsTrue(testSubject.WritePendingFiles(), "Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertEditRequested(file1);
        }

        [TestMethod]
        public void SourceControlledFileSystem_WritePendingFiles_EditWhenBuilding()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.RegisterFile(file1);
            this.queryEditAndSave.VerifyQueryEditFlags |= (uint)VsQueryEditFlags.NoReload;
            KnownUIContextsAccessor.MonitorSelectionService.SetContext(VSConstants.UICONTEXT.SolutionBuilding_guid, true);
            testSubject.PendFileWrite(file1, () => true);

            // Act
            Assert.IsTrue(testSubject.WritePendingFiles(), "Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertEditRequested(file1);
        }

        [TestMethod]
        public void SourceControlledFileSystem_WritePendingFiles_FailedToEdit()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.RegisterFile(file1);
            this.queryEditAndSave.QueryEditFilesVerdict = tagVSQueryEditResult.QER_EditNotOK;
            testSubject.PendFileWrite(file1, () => true);

            // Act
            Assert.IsFalse(testSubject.WritePendingFiles(), "Failed to checkout");

            // Verify
            this.queryEditAndSave.AssertEditRequested(file1);
        }

        [TestMethod]
        public void SourceControlledFileSystem_WritePendingFiles_NoisyPromptForCreateOperationIsRequired()
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
            testSubject.PendFileWrite(file1, () => true);

            // Act
            Assert.IsTrue(testSubject.WritePendingFiles(), "Failed to checkout");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file1, file1); // Twice silent and then noisy
        }

        [TestMethod]
        public void SourceControlledFileSystem_WritePendingFiles_ExecutionOrder()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            string file2 = @"Z:\Y\XXX \3.lll";
            string file3 = @"Z:\Y\XXX \2.lll";
            this.fileSystem.RegisterFile(file1);
            List<string> executionOrder = new List<string>();

            // Act
            testSubject.PendFileWrite(file1, () => { executionOrder.Add(file1); return true; });
            testSubject.PendFileWrite(file2, () => { executionOrder.Add(file2); return true; });
            testSubject.PendFileWrite(file3, () => { executionOrder.Add(file3); return true; });
            Assert.IsTrue(testSubject.WritePendingFiles(), "Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file2, file3);
            this.queryEditAndSave.AssertEditRequested(file1);
            CollectionAssert.AreEqual(new[] { file1, file2, file3 }, executionOrder.ToArray(), "Unexpected execution order");
        }

        [TestMethod]
        public void SourceControlledFileSystem_WritePendingFiles_Batching()
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
            testSubject.PendFileWrite(file1, () => { executionOrder.Add(file1); return false; });
            testSubject.PendFileWrite(file2, () => { executionOrder.Add(file2); return true; });
            testSubject.PendFileWrite(file3, () => { executionOrder.Add(file3); return true; });
            Assert.IsFalse(testSubject.WritePendingFiles(), "Expected to fail");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file2);
            this.queryEditAndSave.AssertEditRequested(file1, file3);
            this.queryEditAndSave.AssertAllBatchesCompleted(1);
            CollectionAssert.AreEqual(new[] { file1 }, executionOrder.ToArray(), "Only the first was expected to execute");

            // Act (write again)
            this.queryEditAndSave.Reset();
            executionOrder.Clear();

            // Verify (the test subject should have been cleared from previous state)
            Assert.IsTrue(testSubject.WritePendingFiles(), "Should succeed since there's nothing pending");
            this.queryEditAndSave.AssertAllBatchesCompleted(1);
            this.queryEditAndSave.AssertCreateRequested(new string[0]);
            this.queryEditAndSave.AssertEditRequested(new string[0]);
            Assert.AreEqual(0, executionOrder.Count, "Unexpected execution occurred");
        }

        [TestMethod]
        public void SourceControlledFileSystem_WritePendingFiles_Failure()
        {
            // Setup
            SourceControlledFileSystem testSubject = new SourceControlledFileSystem(this.serviceProvider, this.fileSystem);
            string file1 = @"Z:\Y\XXX \1.lll";
            string file2 = @"Z:\Y\XXX \3.lll";
            string file3 = @"Z:\Y\XXX \2.lll";
            this.fileSystem.RegisterFile(file1);
            this.fileSystem.RegisterFile(file3);

            // Act
            testSubject.PendFileWrite(file1, () => true);
            testSubject.PendFileWrite(file2, () => true);
            testSubject.PendFileWrite(file3, () => true);
            Assert.IsTrue(testSubject.WritePendingFiles(), "Not expecting any errors");

            // Verify
            this.queryEditAndSave.AssertCreateRequested(file2);
            this.queryEditAndSave.AssertEditRequested(file1, file3);
            this.queryEditAndSave.AssertAllBatchesCompleted(1);
        }
    }
}
