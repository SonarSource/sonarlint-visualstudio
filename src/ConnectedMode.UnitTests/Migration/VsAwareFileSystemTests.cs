/*
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration.VsAwareFileSystemTestSpecificExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class VsAwareFileSystemTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VsAwareFileSystem, IVsAwareFileSystem>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<VsAwareFileSystem>();

        [TestMethod]
        public async Task LoadAsText_CallsFileSystem()
        {
            const string fileName = "X:\\dir\\myproj.csproj";
            const string fileContents = "some content";

            var testSubject = CreateTestSubject(out var fileSystem, out var sccService);
            fileSystem.SetupFile(fileName, fileContents);

            var actual = await testSubject.LoadAsTextAsync(fileName);

            actual.Should().Be(fileContents);
            sccService.CheckBeginBatchIsNotCalled();
            sccService.CheckEndBatchIsNotCalled();
        }

        [TestMethod]
        public async Task BeginAndEndBatch_CallsQueryEditSaveInterface()
        {
            var testSubject = CreateTestSubject(out var _, out var sccService);

            // Sanity check - nothing called to start with
            sccService.CheckBeginBatchIsNotCalled();
            sccService.CheckEndBatchIsNotCalled();

            // Begin batch
            await testSubject.BeginChangeBatchAsync();
            sccService.CheckBeginBatchCalled();
            sccService.CheckEndBatchIsNotCalled();

            // End batch
            await testSubject.EndChangeBatchAsync();
            sccService.CheckEndBatchCalled();
        }

        [TestMethod]
        public async Task Save_CallsFileSystem()
        {
            const string fileName = "c:\\aaa\\foo.txt";
            const string fileContents = "some data";

            var testSubject = CreateTestSubject(out var fileSystem, out var _);

            await testSubject.BeginChangeBatchAsync(); // all modifications are expected to be in a batch
            await testSubject.SaveAsync(fileName, fileContents);

            fileSystem.CheckFileIsWritten(fileName, fileContents);
        }

        [TestMethod]
        public async Task Save_SccServiceIsCalledOnUIThread()
        {
            var calls = new List<string>();

            var threadHandling = CreateThreadHandlingWithRunOnUICallback(() => calls.Add("RunOnUIThread"));
            var testSubject = CreateTestSubject(out var _, out var sccService, threadHandling.Object);

            sccService.SetupQueryEditResponse(tagVSQueryEditResult.QER_EditOK, new[] { "any" }, () => calls.Add("QueryEdit"));
            await testSubject.BeginChangeBatchAsync();
            calls.Clear();

            // Act
            await testSubject.SaveAsync("any", "any");

            calls.Should().ContainInOrder("RunOnUIThread", "QueryEdit");
        }

        [TestMethod]
        [DataRow(tagVSQueryEditResult.QER_NoEdit_UserCanceled, true)]
        [DataRow(tagVSQueryEditResult.QER_EditNotOK, true)]
        [DataRow(tagVSQueryEditResult.QER_EditOK, false)]
        public async Task Save_CannotCheckOut_ExceptionIsThrown(tagVSQueryEditResult verdict, bool shouldThrow)
        {
            var testSubject = CreateTestSubject(out var fileSystem, out var sccService);

            fileSystem.SetupFile("file1");
            sccService.SetupQueryEditResponse(verdict, new[] { "file1" });

            await testSubject.BeginChangeBatchAsync(); // all modifications are expected to be in a batch
            Func<Task> act = () => testSubject.SaveAsync("file1", "c1");

            if (shouldThrow)
            {
                await act.Should().ThrowAsync<InvalidOperationException>();
                fileSystem.CheckFileIsNotWritten("file1");
            }
            else
            {
                await act.Should().NotThrowAsync<InvalidOperationException>();
                fileSystem.CheckFileIsWritten("file1", "c1");
            }
            sccService.CheckQueryEditsCalled("file1");
        }

        [TestMethod]
        public async Task DeleteDirectory_CallsFileSystem()
        {
            const string dirName = "c:\\aaa\\bbb";

            var testSubject = CreateTestSubject(out var fileSystem, out var _);

            await testSubject.BeginChangeBatchAsync(); // all modifications are expected to be in a batch
            await testSubject.DeleteFolderAsync(dirName);

            fileSystem.Verify(x => x.Directory.Delete(dirName, true), Times.Once);
        }

        [TestMethod]
        public async Task DeleteDirectory_ContainedFilesAreCheckedOutForEdit()
        {
            // ... and the directory is checked out for saving
            const string dirName = "c:\\aaa\\bbb";

            var testSubject = CreateTestSubject(out var fileSystem, out var sccService);
            fileSystem.SetupGetFilesInDir(dirName, "f1", "f2", "f3");

            sccService.SetupQueryEditResponse(tagVSQueryEditResult.QER_EditOK, new[] { dirName });
            sccService.SetupQuerySavesResponse(tagVSQuerySaveResult.QSR_SaveOK, new[] { "f1", "f2", "f3" });

            await testSubject.BeginChangeBatchAsync(); // all modifications are expected to be in a batch
            await testSubject.DeleteFolderAsync(dirName);

            fileSystem.Verify(x => x.Directory.Delete(dirName, true), Times.Once);
            sccService.CheckQueryEditsCalled(dirName);
            sccService.CheckQuerySavesCalled("f1", "f2", "f3");
        }

        [TestMethod]
        public async Task DeleteDirectory_SccServiceIsCalledOnUIThread()
        {
            var calls = new List<string>();
            var threadHandling = CreateThreadHandlingWithRunOnUICallback(() => calls.Add("RunOnUIThread"));

            const string dirName = "c:\\aaa\\bbb";

            var testSubject = CreateTestSubject(out var fileSystem, out var sccService, threadHandling.Object);
            fileSystem.SetupGetFilesInDir(dirName, "any");

            sccService.SetupQueryEditResponse(tagVSQueryEditResult.QER_EditOK, new[] { dirName },
                () => calls.Add("QueryEdit"));

            sccService.SetupQuerySavesResponse(tagVSQuerySaveResult.QSR_SaveOK, new[] { "any" },
                () => calls.Add("QuerySave"));

            await testSubject.BeginChangeBatchAsync(); // all modifications are expected to be in a batch
            calls.Clear();

            // Act
            await testSubject.DeleteFolderAsync(dirName);

            calls.Should().ContainInOrder("RunOnUIThread", "QueryEdit", "RunOnUIThread", "QuerySave");
        }

        [TestMethod]
        [DataRow(tagVSQueryEditResult.QER_NoEdit_UserCanceled, true)]
        [DataRow(tagVSQueryEditResult.QER_EditNotOK, true)]
        [DataRow(tagVSQueryEditResult.QER_EditOK, false)]
        public async Task DeleteDirectory_CannotCheckOutDirectory_Throws(tagVSQueryEditResult result, bool shouldThrow)
        {
            const string dirName = "c:\\aaa\\bbb";

            var testSubject = CreateTestSubject(out var fileSystem, out var sccService);
            fileSystem.SetupGetFilesInDir("f1");

            sccService.SetupQueryEditResponse(result, new[] { dirName });
            sccService.SetupQuerySavesResponse(tagVSQuerySaveResult.QSR_SaveOK, new[] { "f1" } );

            await testSubject.BeginChangeBatchAsync(); // all modifications are expected to be in a batch
            Func<Task> act = () => testSubject.DeleteFolderAsync(dirName);

            if (shouldThrow)
            {
                await act.Should().ThrowAsync<InvalidOperationException>();
            }
            else
            {
                await act.Should().NotThrowAsync<InvalidOperationException>();
            }
        }

        [TestMethod]
        // Note: QSR_NoSave_NoisyPromptRequired has a special behavour so it isn't tested here
        [DataRow(tagVSQuerySaveResult.QSR_NoSave_UserCanceled, true)]
        [DataRow(tagVSQuerySaveResult.QSR_NoSave_Continue, true)]
        [DataRow(tagVSQuerySaveResult.QSR_NoSave_Continue, true)]
        [DataRow(tagVSQuerySaveResult.QSR_SaveOK, false)]
        public async Task DeleteDirectory_CannotCheckOutFile_Throws(tagVSQuerySaveResult result, bool shouldThrow)
        {
            const string dirName = "c:\\aaa\\bbb";

            var testSubject = CreateTestSubject(out var fileSystem, out var sccService);
            fileSystem.SetupGetFilesInDir(dirName, "f1");

            sccService.SetupQueryEditResponse(tagVSQueryEditResult.QER_EditOK, new[] { dirName } );
            sccService.SetupQuerySavesResponse(result, new[] { "f1" });

            await testSubject.BeginChangeBatchAsync(); // all modifications are expected to be in a batch
            Func<Task> act = () => testSubject.DeleteFolderAsync(dirName);

            if (shouldThrow)
            {
                await act.Should().ThrowAsync<InvalidOperationException>();
            }
            else
            {
                await act.Should().NotThrowAsync<InvalidOperationException>();
            }

            sccService.CheckQueryEditsCalled(dirName);
            sccService.CheckQuerySavesCalled("f1");
        }

        private static VsAwareFileSystem CreateTestSubject(
            out Mock<IFileSystem> fileSystem,
            out Mock<IVsQueryEditQuerySave2> queryEditSave,
            IThreadHandling threadHandling = null)
        {
            var logger = new TestLogger(logToConsole: true);
            threadHandling ??= new NoOpThreadHandler();

            queryEditSave = new Mock<IVsQueryEditQuerySave2>();
            var serviceProvider = CreateServiceProvider(queryEditSave.Object);

            fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File).Returns(new Mock<IFile>().Object);
            fileSystem.Setup(x => x.Directory).Returns(new Mock<IDirectory>().Object);
            return new VsAwareFileSystem(serviceProvider, logger, threadHandling, fileSystem.Object);
        }

        private static IServiceProvider CreateServiceProvider(IVsQueryEditQuerySave2 sccService = null)
        {
            var sp = new Mock<IServiceProvider>();
            sp.Setup(x => x.GetService(typeof(SVsQueryEditQuerySave))).Returns(sccService);
            return sp.Object;
        }

        private static Mock<IThreadHandling> CreateThreadHandlingWithRunOnUICallback(Action testOperation)
        {
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(productOperation =>
                {
                    testOperation();
                    productOperation.Invoke();
                });
            return threadHandling;
        }
    }

    namespace VsAwareFileSystemTestSpecificExtensions
    {
        public static class TestExtensions
        {
            public static void SetupFile(this Mock<IFileSystem> fileSystem, string fileName, string contents = null)
                => fileSystem.Setup(x => x.File.ReadAllText(fileName)).Returns(contents ?? "{empty}");

            public static void SetupGetFilesInDir(this Mock<IFileSystem> fileSystem, string dirName, params string[] files)
                => fileSystem.Setup(x => x.Directory.GetFiles(dirName, "*.*", System.IO.SearchOption.AllDirectories))
                .Returns(files);

            public static void SetupQueryEditResponse(this Mock<IVsQueryEditQuerySave2> queryEditSave,
                tagVSQueryEditResult verdict, string[] files, Action callback = null)
            {
                var verdictAsUint = (uint)verdict;
                uint moreInfoAsUint = 123;
                queryEditSave.Setup(x => x.QueryEditFiles(
                        It.IsAny<uint>(),
                        files.Length,
                        files,
                        null,
                        null,
                        out verdictAsUint,
                        out moreInfoAsUint))
                    .Callback<IInvocation>(x => callback?.Invoke())
                    .Returns(VSConstants.S_OK);
            }

            public static void SetupQuerySavesResponse(this Mock<IVsQueryEditQuerySave2> queryEditSave,
                tagVSQuerySaveResult result, string[] files, Action callback = null)
            {
                var resultAsUint = (uint)result;
                queryEditSave.Setup(x => x.QuerySaveFiles(
                        It.IsAny<uint>(),
                        files.Length,
                        files,
                        It.IsAny<uint[]>(),
                        null,
                        out resultAsUint))
                    .Callback<IInvocation>(x => callback?.Invoke())
                    .Returns(VSConstants.S_OK);
            }

            public static void CheckBeginBatchCalled(this Mock<IVsQueryEditQuerySave2> service)
                => service.Verify(x => x.BeginQuerySaveBatch(), Times.Once());

            public static void CheckBeginBatchIsNotCalled(this Mock<IVsQueryEditQuerySave2> service)
                => service.Verify(x => x.BeginQuerySaveBatch(), Times.Never());

            public static void CheckEndBatchCalled(this Mock<IVsQueryEditQuerySave2> service)
                => service.Verify(x => x.EndQuerySaveBatch(), Times.Once());

            public static void CheckEndBatchIsNotCalled(this Mock<IVsQueryEditQuerySave2> service)
                => service.Verify(x => x.EndQuerySaveBatch(), Times.Never());

            public static void CheckQueryEditsCalled(this Mock<IVsQueryEditQuerySave2> service, params string[] fileNames)
            {
                service.Verify(x => x.QueryEditFiles(It.IsAny<uint>(),
                    fileNames.Length,
                    fileNames,
                    It.IsAny<uint[]>(), It.IsAny<VSQEQS_FILE_ATTRIBUTE_DATA[]>(), out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny),
                    Times.Once());
            }

            public static void CheckQuerySavesCalled(this Mock<IVsQueryEditQuerySave2> queryEditSave,
                params string[] files)
            {
                queryEditSave.Verify(x => x.QuerySaveFiles(
                        It.IsAny<uint>(),
                        files.Length,
                        files,
                        It.IsAny<uint[]>(),
                        null,
                        out It.Ref<uint>.IsAny),
                        Times.Once
                );
            }

            public static void CheckFileIsWritten(this Mock<IFileSystem> fileSystem, string fileName, string fileContents)
                => fileSystem.Verify(x => x.File.WriteAllText(fileName, fileContents), Times.Once);

            public static void CheckFileIsNotWritten(this Mock<IFileSystem> fileSystem, string fileName)
                => fileSystem.Verify(x => x.File.WriteAllText(fileName, It.IsAny<string>()), Times.Never);
        }
    }
}
