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
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class XmlFileProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<XmlFileProvider, IFileProvider>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<XmlFileProvider>();

        [TestMethod]
        public async Task GetFiles_NoSolutionFolder_ReturnsEmptyList()
        {
            var solution = CreateIVsSolution(null /* no solution folder */);
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var fileSystem = new Mock<IFileSystem>();

            var testSubject = CreateTestSubject(serviceProvider.Object, fileSystem.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEmpty();
            fileSystem.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public async Task GetFiles_HasSolutionFolder_ExpectedFilesReturned()
        {
            var solution = CreateIVsSolution("root dir");
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var filesToReturn = new string[] { "file1", "file2", "file3"};
            var fileSystem = CreateFileSystem(filesToReturn);

            var testSubject = CreateTestSubject(serviceProvider.Object, fileSystem.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEquivalentTo(filesToReturn);
            solution.VerifyAll();

            fileSystem.Verify(x => x.Directory.GetFiles("root dir", "*.ruleset", SearchOption.AllDirectories), Times.Once);
            fileSystem.Verify(x => x.Directory.GetFiles("root dir", "*.props", SearchOption.AllDirectories), Times.Once);
            fileSystem.Verify(x => x.Directory.GetFiles("root dir", "*.targets", SearchOption.AllDirectories), Times.Once);
            fileSystem.Verify(x => x.Directory.GetFiles("root dir", "*.csproj", SearchOption.AllDirectories), Times.Once);
            fileSystem.Verify(x => x.Directory.GetFiles("root dir", "*.vbproj", SearchOption.AllDirectories), Times.Once);
        }

        [TestMethod]
        public async Task GetFiles_FilesInExcludedDirectoriesAreNotReturned()
        {
            var solution = CreateIVsSolution("root dir");
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var filesInFileSystem = new string[] {
                "should be included1",
                "\\bin\\file1",
                "aaa\\bin\\zzz",
                "should be included2\\obj", // doesn't contain \obj\
                "ccc\\obj\\xxx",
                "should be included3"
            };
            
            var fileSystem = CreateFileSystem(filesInFileSystem);

            var testSubject = CreateTestSubject(serviceProvider.Object, fileSystem.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEquivalentTo("should be included1", "should be included2\\obj", "should be included3");
        }

        [TestMethod]
        public async Task GetFiles_DuplicatesAreIgnored()
        {
            var solution = CreateIVsSolution("root dir");
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var filesInFileSystem = new string[] {
                // duplicates - should only appear once
                "file_1",
                "FILE_1",
                "file_1",

                // non-duplicates - should be included
                "file_11",
                "file_111"
            };

            var fileSystem = CreateFileSystem(filesInFileSystem);

            var testSubject = CreateTestSubject(serviceProvider.Object, fileSystem.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEquivalentTo("file_1", "file_11", "file_111");
        }

        private static XmlFileProvider CreateTestSubject(IServiceProvider serviceProvider = null,
            IFileSystem fileSystem = null, ILogger logger = null, IThreadHandling threadHandling = null)
        {
            serviceProvider ??= CreateServiceProviderWithSolution(CreateIVsSolution(null).Object).Object;
            fileSystem ??= new System.IO.Abstractions.TestingHelpers.MockFileSystem();
            logger ??= new TestLogger(logToConsole: true);
            threadHandling ??= new NoOpThreadHandler();
            return new XmlFileProvider(serviceProvider, logger, threadHandling, fileSystem);
        }

        private static Mock<IServiceProvider> CreateServiceProviderWithSolution(IVsSolution solution)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(solution);

            return serviceProvider;
        }

        private static Mock<IFileSystem> CreateFileSystem(params string[] filesToReturn)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x =>x.Directory.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
                .Returns(filesToReturn);

            return fileSystem;
        }

        private static Mock<IVsSolution> CreateIVsSolution(string pathToReturn)
        {
            var solution = new Mock<IVsSolution>();

            object solutionDirectory = pathToReturn;
            solution
                .Setup(x => x.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out solutionDirectory))
                .Returns(VSConstants.S_OK);
        
            return solution;
        }
    }
}
