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
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.ConnectedMode.Migration.FileProviders;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration.FileProviders
{
    [TestClass]
    public class XmlFileProviderTests
    {
        // Non-empty provider used by tests that don't care about the specific projects,
        // just that there are some. CreateTestSubject will use this provider by default.
        private static readonly IRoslynProjectProvider NonEmptyRoslynProjectProvider = CreateRoslynProjectProvider("c:\\any\\x.proj").Object;

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<XmlFileProvider, IFileProvider>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<IRoslynProjectProvider>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<XmlFileProvider>();

        [TestMethod]
        public async Task GetFiles_NoRoslynProjects_ReturnsEmptyList()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var fileFinder = new Mock<XmlFileProvider.IFileFinder>();
            var projectProvider = CreateRoslynProjectProvider( /* no project */ );
            
            var testSubject = CreateTestSubject(serviceProvider.Object, fileFinder.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEmpty();
            projectProvider.Verify(x => x.Get(), Times.Once);
            serviceProvider.Invocations.Should().BeEmpty();
            fileFinder.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetFiles_HasRoslynProjectsButNoOtherFiles_ReturnsRoslynFilePaths()
        {
            var solution = CreateIVsSolution("c:\\searchPath");
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var fileFinder = CreateFileFinder();
            var projectProvider = CreateRoslynProjectProvider("c:\\any.proj", "x:\\any.proj2");

            var testSubject = CreateTestSubject(serviceProvider.Object, fileFinder.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEquivalentTo("c:\\any.proj", "x:\\any.proj2");
            projectProvider.Verify(x => x.Get(), Times.Once);
            fileFinder.Invocations.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task GetFiles_NoSolutionFolder_ReturnsOnlyProjectFiles()
        {
            var solution = CreateIVsSolution(null /* no solution folder */);
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);
            var projectProvider = CreateRoslynProjectProvider("project1", "project2");

            var fileFinder = new Mock<XmlFileProvider.IFileFinder>();

            var testSubject = CreateTestSubject(serviceProvider.Object, fileFinder.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEquivalentTo("project1", "project2");
            fileFinder.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public async Task GetFiles_HasSolutionFolder_ExpectedFilesReturned()
        {
            var solution = CreateIVsSolution("root dir");
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);
            var projectProvider = CreateRoslynProjectProvider("project1", "project2");

            var filesToReturn = new string[] { "file1", "file2", "file3" };
            var fileSystem = CreateFileFinder(filesToReturn);

            var testSubject = CreateTestSubject(serviceProvider.Object, fileSystem.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            // Expecting the project files and any files found by searching
            actual.Should().BeEquivalentTo("file1", "file2", "file3", "project1", "project2");
            solution.VerifyAll();

            fileSystem.Verify(x => x.GetFiles("root dir", SearchOption.AllDirectories, XmlFileProvider.FileSearchPatterns), Times.Once);
        }

        [TestMethod]
        public async Task GetFiles_DuplicatesAreIgnored()
        {
            var solution = CreateIVsSolution("root dir");
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);
            var projectProvider = CreateRoslynProjectProvider("the project file");

            var filesInFileSystem = new string[] {
                // duplicates - should only appear once
                "file_1",
                "FILE_1",
                "file_1",

                // non-duplicates - should be included
                "file_11",
                "file_111"
            };

            var fileSystem = CreateFileFinder(filesInFileSystem);

            var testSubject = CreateTestSubject(serviceProvider.Object, fileSystem.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEquivalentTo("file_1", "file_11", "file_111",
                "the project file"); // project file will always be included
        }

        private static XmlFileProvider CreateTestSubject(IServiceProvider serviceProvider = null,
            XmlFileProvider.IFileFinder fileFinder = null,
            IRoslynProjectProvider projectProvider = null,
            ILogger logger = null,
            IThreadHandling threadHandling = null)
        {
            serviceProvider ??= CreateServiceProviderWithSolution(CreateIVsSolution(null).Object).Object;
            fileFinder ??= Mock.Of<XmlFileProvider.FileFinder>();
            projectProvider ??= NonEmptyRoslynProjectProvider;
            logger ??= new TestLogger(logToConsole: true);
            threadHandling ??= new NoOpThreadHandler();

            return new XmlFileProvider(serviceProvider, projectProvider, logger, threadHandling, fileFinder);
        }

        private static Mock<IServiceProvider> CreateServiceProviderWithSolution(IVsSolution solution)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(solution);

            return serviceProvider;
        }

        private static Mock<XmlFileProvider.IFileFinder> CreateFileFinder(params string[] filesToReturn)
        {
            var fileSystem = new Mock<XmlFileProvider.IFileFinder>();
            fileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<SearchOption>(), It.IsAny<string[]>()))
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

        private static Mock<IRoslynProjectProvider> CreateRoslynProjectProvider(params string[] projectFilePaths)
        {
            var projectProvider = new Mock<IRoslynProjectProvider>();

            var projects = CreateProjects(projectFilePaths);
            projectProvider.Setup(x => x.Get()).Returns(projects);

            return projectProvider;
        }

        private static IReadOnlyList<Project> CreateProjects(params string[] projectFilePaths)
        {
            var projectInfos = projectFilePaths.Select(CreateProjectInfo);

            // We can't directly create or mock a Roslyn Project. However, we can 
            // create a real Roslyn AdhocWorkspace and use that to create a Solution
            // with Projects.
            AdhocWorkspace adhocWorkspace = new AdhocWorkspace();

            var slnInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, null,
                projectInfos);
            adhocWorkspace.AddSolution(slnInfo);

            return adhocWorkspace.CurrentSolution.Projects.ToList();
        }

        private static ProjectInfo CreateProjectInfo(string projectFilePath)
        {
            var guid = Guid.NewGuid();
            return ProjectInfo.Create(ProjectId.CreateFromSerialized(guid),
                VersionStamp.Default,
                guid.ToString(),
                guid.ToString(),
                LanguageNames.CSharp,
                filePath: projectFilePath);
        }
    }
}
