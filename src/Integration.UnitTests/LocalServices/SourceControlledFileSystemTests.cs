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
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SourceControlledFileSystemTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsQueryEditQuerySave2 queryEditAndSave;
        private MockFileSystem fileSystem;
        private Mock<IKnownUIContexts> knownUIContexts;
        private TestLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            knownUIContexts = new Mock<IKnownUIContexts>();
            SetKnownUIContexts(false, false);
            serviceProvider = new ConfigurableServiceProvider();
            queryEditAndSave = new ConfigurableVsQueryEditQuerySave2();
            serviceProvider.RegisterService(typeof(SVsQueryEditQuerySave), this.queryEditAndSave);
            fileSystem = new MockFileSystem();
            logger = new TestLogger();
        }

        [TestMethod]
        public void SourceControlledFileSystem_ArgCheck()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SourceControlledFileSystem(null, logger));
            Exceptions.Expect<ArgumentNullException>(() => new SourceControlledFileSystem(this.serviceProvider, null));
        }

        [TestMethod]
        public void SourceControlledFileSystem_FileExistOrQueuedToBeWritten()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            string file = @"Z:\Y\XXX \n.lll";

            // Case 1: file exists
            this.fileSystem.AddFile(file, new MockFileData(""));

            // Act + Assert
            testSubject.FileExistOrQueuedToBeWritten(file.ToLowerInvariant()).Should().BeTrue();

            // Case 2: file not exists, but pending
            foreach (var filePath in fileSystem.AllFiles)
            {
                fileSystem.RemoveFile(filePath);
            }
            testSubject.QueueFileWrite(file, () => true);

            // Act + Assert
            testSubject.FileExistOrQueuedToBeWritten(file.ToUpperInvariant()).Should().BeTrue();

            // Case 3: file not exists and not pending
            testSubject.WriteQueuedFiles();

            // Act + Assert
            testSubject.FileExistOrQueuedToBeWritten(file).Should().BeFalse();
        }

        [TestMethod]
        [DataRow(true, true, true)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(false, false, false)]
        public void SourceControlledFileSystem_FilesExistOrQueuedToBeWritten_ReturnsIfAllFilesAreQueuedOrWritten(bool firstFileExists, bool secondFileExists, bool expectedResult)
        {
            var testSubject = CreateTestSubject();
            var files = new List<string> { @"Z:\Y\XXX\first.txt", @"Z:\Y\XXX\second.txt" };

            if (firstFileExists)
            {
                fileSystem.AddFile(files.First(), new MockFileData(""));
            }
            if (secondFileExists)
            {
                fileSystem.AddFile(files.Last(), new MockFileData(""));
            }

            var result = testSubject.FilesExistOrQueuedToBeWritten(files);
            result.Should().Be(expectedResult);
        }

        [TestMethod]
        public void SourceControlledFileSystem_QueueFileWrites_OneCallbackForAllFiles()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var files = new List<string> { @"Z:\Y\XXX\first.txt", @"Z:\Y\XXX\second.txt" };
            var callback = new Mock<Func<bool>>();

            // Act
            testSubject.QueueFileWrites(files, callback.Object);
            testSubject.WriteQueuedFiles();

            // Assert
            callback.Verify(x => x(), Times.Once);
        }

        [TestMethod]
        public void SourceControlledFileSystem_QueueFileWrite_QueryNewFile()
        {
            // Arrange
            var testSubject = CreateTestSubject();
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
            var testSubject = CreateTestSubject();
            string file = @"Z:\Y\XXX \n.lll";
            this.fileSystem.AddFile(file, new MockFileData(""));
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
            var testSubject = CreateTestSubject();
            string file1 = @"Z:\Y\XXX \1.lll";
            string file2 = @"Z:\Y\XXX \3.lll";
            string file3 = @"Z:\Y\XXX \2.lll";
            this.fileSystem.AddFile(file1, new MockFileData(""));
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
            var testSubject = CreateTestSubject();
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.AddFile(file1, new MockFileData(""));
            this.queryEditAndSave.VerifyQueryEditFlags |= (uint)VsQueryEditFlags.NoReload;

            SetKnownUIContexts(false, true);
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
            var testSubject = CreateTestSubject();
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.AddFile(file1, new MockFileData(""));
            this.queryEditAndSave.VerifyQueryEditFlags |= (uint)VsQueryEditFlags.NoReload;
            SetKnownUIContexts(true, false);

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
            var testSubject = CreateTestSubject();
            string file1 = @"Z:\Y\XXX \1.lll";
            this.fileSystem.AddFile(file1, new MockFileData(""));
            this.queryEditAndSave.QueryEditFilesVerdict = tagVSQueryEditResult.QER_EditNotOK;
            this.queryEditAndSave.QueryEditFilesMoreInfo = tagVSQueryEditResultFlags.QER_EditNotPossible | tagVSQueryEditResultFlags.QER_NoisyPromptRequired;
            testSubject.QueueFileWrite(file1, () => true);

            // Act
            testSubject.WriteQueuedFiles().Should().BeFalse("Failed to checkout");

            // Assert
            this.queryEditAndSave.AssertEditRequested(file1);
            this.logger.AssertPartialOutputStrings("QER_NoisyPromptRequired, QER_EditNotPossible"); // expecting a logged message containing the "more info" code
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_NoisyPromptForCreateOperationIsRequired()
        {
            // Arrange
            var testSubject = CreateTestSubject();
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
        public void SourceControlledFileSystem_WriteQueuedFiles_Failed()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            string file1 = @"Z:\Y\XXX \1.lll";
            this.queryEditAndSave.QuerySaveFilesVerification = flags =>
            {
                return tagVSQuerySaveResult.QSR_NoSave_UserCanceled;
            };
            testSubject.QueueFileWrite(file1, () => true);

            // Act
            testSubject.WriteQueuedFiles().Should().BeFalse("Should not have attempted to write the files");

            // Assert
            this.queryEditAndSave.AssertCreateRequested(file1);
            this.logger.AssertPartialOutputStrings(tagVSQuerySaveResult.QSR_NoSave_UserCanceled.ToString()); // expecting the result code in the output message
        }

        [TestMethod]
        public void SourceControlledFileSystem_WriteQueuedFiles_Batching()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            string file1 = @"Z:\Y\XXX \1.lll";
            string file2 = @"Z:\Y\XXX \3.lll";
            string file3 = @"Z:\Y\XXX \2.lll";
            this.fileSystem.AddFile(file1, new MockFileData(""));
            this.fileSystem.AddFile(file3, new MockFileData(""));
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
            var testSubject = CreateTestSubject();
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

        private SourceControlledFileSystem CreateTestSubject() =>
            new SourceControlledFileSystem(serviceProvider, logger, fileSystem, knownUIContexts.Object);

        private void SetKnownUIContexts(bool isSolutionBuilding, bool isDebugging)
        {
            knownUIContexts.Reset();
            knownUIContexts.SetupGet(x => x.SolutionBuildingContext).Returns(CreateContext(isSolutionBuilding));
            knownUIContexts.SetupGet(x => x.DebuggingContext).Returns(CreateContext(isDebugging));
        }

        private static IUIContext CreateContext(bool isActive)
        {
            var context = new Mock<IUIContext>();
            context.Setup(x => x.IsActive).Returns(isActive);
            return context.Object;
        }
    }
}
