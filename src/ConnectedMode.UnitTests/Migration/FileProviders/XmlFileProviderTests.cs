/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.ConnectedMode.Migration.FileProviders;
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
                MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
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
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            var fileFinder = new Mock<XmlFileProvider.IFileFinder>();
            var projectProvider = CreateRoslynProjectProvider( /* no project */ );
            
            var testSubject = CreateTestSubject(solutionInfoProvider.Object, fileFinder.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEmpty();
            projectProvider.Verify(x => x.Get(), Times.Once);
            solutionInfoProvider.Invocations.Should().BeEmpty();
            fileFinder.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetFiles_HasRoslynProjectsButNoOtherFiles_ReturnsRoslynFilePaths()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider("c:\\searchPath");

            var fileFinder = CreateFileFinder(); // no solution or files in project folders
            var projectProvider = CreateRoslynProjectProvider("c:\\any.proj", "x:\\any.proj2");

            var testSubject = CreateTestSubject(solutionInfoProvider.Object, fileFinder.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEquivalentTo("c:\\any.proj", "x:\\any.proj2");
            projectProvider.Verify(x => x.Get(), Times.Once);

            CheckSearch(fileFinder, "c:\\searchPath", SearchOption.AllDirectories, XmlFileProvider.FileSearchPatterns);
            CheckSearch(fileFinder, "c:\\", SearchOption.TopDirectoryOnly, XmlFileProvider.FileSearchPatterns);
            CheckSearch(fileFinder, "x:\\", SearchOption.TopDirectoryOnly, XmlFileProvider.FileSearchPatterns);
            fileFinder.Invocations.Should().HaveCount(3);            
        }

        [TestMethod]
        public async Task GetFiles_NoSolutionFolder_StillSearchesProjectFolders()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider(null /* no solution folder */);
            var projectProvider = CreateRoslynProjectProvider("c:\\project1", "c:\\any\\project2");

            var fileFinder = new Mock<XmlFileProvider.IFileFinder>();

            var testSubject = CreateTestSubject(solutionInfoProvider.Object, fileFinder.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEquivalentTo("c:\\project1", "c:\\any\\project2");
            CheckSearch(fileFinder, "c:\\", SearchOption.TopDirectoryOnly, XmlFileProvider.FileSearchPatterns);
            CheckSearch(fileFinder, "c:\\any", SearchOption.TopDirectoryOnly, XmlFileProvider.FileSearchPatterns);
            fileFinder.Invocations.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task GetFiles_HasFilesInProjectDirectories_ReturnsExpectedFiles()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider(null /* no solution folder */);
            var projectProvider = CreateRoslynProjectProvider("c:\\folder1\\project1.proj", "c:\\folder2\\project2.proj");

            var fileFinder = new Mock<XmlFileProvider.IFileFinder>();
            SetupFilesInFolder(fileFinder, "c:\\folder1", "project 1 file 1");
            SetupFilesInFolder(fileFinder, "c:\\folder2", "project 2 file 1", "project 2 file 2");

            var testSubject = CreateTestSubject(solutionInfoProvider.Object, fileFinder.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEquivalentTo(
                // The project files should be returned
                "c:\\folder1\\project1.proj", "c:\\folder2\\project2.proj",
                // The files under the project folders should also be returned
                "project 1 file 1", "project 2 file 1", "project 2 file 2");

            CheckSearch(fileFinder, "c:\\folder1", SearchOption.TopDirectoryOnly, XmlFileProvider.FileSearchPatterns);
            CheckSearch(fileFinder, "c:\\folder2", SearchOption.TopDirectoryOnly, XmlFileProvider.FileSearchPatterns);
            fileFinder.Invocations.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task GetFiles_HasSolutionFolder_ExpectedFilesReturned()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider("root dir");
            var projectProvider = CreateRoslynProjectProvider("project1", "project2");

            var filesToReturn = new string[] { "file1", "file2", "file3" };
            var fileFinder = CreateFileFinder(filesToReturn);

            var testSubject = CreateTestSubject(solutionInfoProvider.Object, fileFinder.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            // Expecting the project files and any files found by searching
            actual.Should().BeEquivalentTo("file1", "file2", "file3", "project1", "project2");
            solutionInfoProvider.VerifyAll();

            fileFinder.Verify(x => x.GetFiles("root dir", SearchOption.AllDirectories, XmlFileProvider.FileSearchPatterns), Times.Once);
        }

        [TestMethod]
        public async Task GetFiles_DuplicatesAreIgnored()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider("solution dir");
            var projectProvider = CreateRoslynProjectProvider("c:\\projectFolder\\the project file");

            var filesInSolutionFolder = new string[] {
                // duplicates - should only appear once
                "sln_file_1",
                "sln_FILE_1",
                "sln_file_1",
                "shared file",

                // non-duplicates - should be included
                "sln_file_11",
                "sln_file_111"
            };

            var filesInProjectFolder = new string[] {
                // duplicates - should only appear once
                "shared file",

                // non-duplicates - should be included
                "unique project file",
            };

            var fileSystem = new Mock<XmlFileProvider.IFileFinder>();
            SetupFilesInFolder(fileSystem, "solution dir", filesInSolutionFolder);
            SetupFilesInFolder(fileSystem, "c:\\projectFolder", filesInProjectFolder);

            var testSubject = CreateTestSubject(solutionInfoProvider.Object, fileSystem.Object, projectProvider.Object);

            var actual = await testSubject.GetFilesAsync(CancellationToken.None);

            actual.Should().BeEquivalentTo(
                "sln_file_1", "sln_file_11", "sln_file_111",
                "shared file",
                "unique project file",
                "c:\\projectFolder\\the project file"); // project file will always be included
        }

        private static XmlFileProvider CreateTestSubject(ISolutionInfoProvider solutionInfoProvider = null,
            XmlFileProvider.IFileFinder fileFinder = null,
            IRoslynProjectProvider projectProvider = null,
            ILogger logger = null,
            IThreadHandling threadHandling = null)
        {
            solutionInfoProvider ??= Mock.Of<ISolutionInfoProvider>();
            fileFinder ??= Mock.Of<XmlFileProvider.FileFinder>();
            projectProvider ??= NonEmptyRoslynProjectProvider;
            logger ??= new TestLogger(logToConsole: true);
            threadHandling ??= new NoOpThreadHandler();

            return new XmlFileProvider(solutionInfoProvider, projectProvider, logger, threadHandling, fileFinder);
        }

        private static void CheckSearch(Mock<XmlFileProvider.IFileFinder> fileFinder, string expectedRoot, SearchOption expectedSearchOption, string[] expectedPatterns)
            => fileFinder.Verify(x => x.GetFiles(expectedRoot, expectedSearchOption, expectedPatterns), Times.Once);

        private static Mock<XmlFileProvider.IFileFinder> CreateFileFinder(params string[] filesToReturn)
        {
            var fileFinder = new Mock<XmlFileProvider.IFileFinder>();
            fileFinder.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<SearchOption>(), It.IsAny<string[]>()))
                .Returns(filesToReturn);

            return fileFinder;
        }

        private static void SetupFilesInFolder(Mock<XmlFileProvider.IFileFinder> fileFinder, string rootFolder, params string[] filesToReturn)
            => fileFinder.Setup(x => x.GetFiles(rootFolder, It.IsAny<SearchOption>(), It.IsAny<string[]>()))
                .Returns(filesToReturn);

        private static Mock<ISolutionInfoProvider> CreateSolutionInfoProvider(string solutionDirectoryToReturn)
        {
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            solutionInfoProvider.Setup(x => x.GetSolutionDirectoryAsync()).ReturnsAsync(solutionDirectoryToReturn);
            return solutionInfoProvider;
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
