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
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.TypeScript.TsConfig;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.TsConfig
{
    [TestClass]
    public class TsConfigsLocatorTests
    {
        private const string ValidSourceFilePath = "source-file.ts";
        public const string RootDirectory = "c:\\test\\solution\\";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TsConfigsLocator, ITsConfigsLocator>(
                MefTestHelpers.CreateExport<IFolderWorkspaceService>(),
                MefTestHelpers.CreateExport<IVsHierarchyLocator>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Locate_HasRootDirectory_SearchesFilesUnderRootDirectory()
        {
            var testSuite = new TestSuite(rootDirectory: RootDirectory);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEquivalentTo(TestSuite.TsConfigUnderSolution, TestSuite.TsConfigUnderProject);

            testSuite.VsHierarchyLocator.Invocations.Should().BeEmpty();
            testSuite.VsProject.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Locate_NoRootDirectory_SearchesFilesUnderProjectDirectory()
        {
            var testSuite = new TestSuite(rootDirectory: null);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEquivalentTo(TestSuite.TsConfigUnderProject);

            testSuite.VsHierarchyLocator.Verify(x => x.GetVsHierarchyForFile(ValidSourceFilePath), Times.Once);
            testSuite.VerifyVsProjectWasChecked();
        }

        [TestMethod]
        public void Locate_NoRootDirectory_ProblemRetrievingProjectDirectory_EmptyList()
        {
            var testSuite = new TestSuite(rootDirectory: null, projectDirectory: null);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEmpty();

            testSuite.VsHierarchyLocator.Verify(x=> x.GetVsHierarchyForFile(ValidSourceFilePath), Times.Once);
            testSuite.VerifyVsProjectWasChecked();
        }

        [TestMethod]
        public void Locate_NoRootDirectory_ProblemRetrievingVsHierarchy_EmptyList()
        {
            var testSuite = new TestSuite(rootDirectory: null, isNullVsProject: true);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEmpty();

            testSuite.VsHierarchyLocator.Verify(x => x.GetVsHierarchyForFile(ValidSourceFilePath), Times.Once);
        }

        [TestMethod]
        [DataRow(RootDirectory)]
        [DataRow(null)]
        public void Locate_SearchesFilesUnderTheDirectory_IgnoresNodeModules(string rootDirectory)
        {
            var testSuite = new TestSuite(rootDirectory);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().NotContain(TestSuite.TsConfigUnderNodeModules);
        }

        [TestMethod]
        public void Locate_SearchesFilesUnderTheDirectory_FindsCorrectFiles()
        {
            var testSuite = new TestSuite(RootDirectory);
            testSuite.ResetFileSystemMocks();

            var files = new Dictionary<string, bool>
            {
                { "..\\tsconfig.json", false}, // exact match + wrong root
                { "tsconfig.JSON", true}, // case-sensitivity
                { "a\\tsconfig.json", true}, // exact match + sub folder
                { "tsconfig.json.txt", false}, // wrong file extension
                { "atsconfig.json", false}, // wrong name
                { "b\\c\\TSCONFIG.json", true}, // case-sensitivity + sub folder
            };

            foreach (var file in files)
            {
                testSuite.FileSystem.AddFile(Path.Combine(RootDirectory, file.Key), new MockFileData("some content"));
            }

            var expectedFiles = files
                .Where(x => x.Value)
                .Select(x => Path.Combine(RootDirectory, x.Key));

            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEquivalentTo(expectedFiles);
        }

        private class TestSuite
        {
            public const string ProjectDirectory = "c:\\test\\solution\\project\\";

            public static readonly string TsConfigUnderSolution = Path.Combine(RootDirectory, "tsconfig.json");
            public static readonly string TsConfigUnderProject = Path.Combine(ProjectDirectory, "tsconfig.json");
            public static readonly string TsConfigUnderNodeModules = Path.Combine(ProjectDirectory, "node_modules", "tsconfig.json");

            public TestSuite(string rootDirectory, 
                bool isNullVsProject = false,
                string projectDirectory = ProjectDirectory)
            {
                FolderWorkspaceService = SetupFolderWorkspaceService(rootDirectory);

                if (isNullVsProject)
                {
                    VsHierarchyLocator = SetupVsHierarchyLocator(ValidSourceFilePath, null);
                }
                else
                {
                    VsProject = SetupVsProject(projectDirectory);
                    VsHierarchyLocator = SetupVsHierarchyLocator(ValidSourceFilePath, VsProject.Object);
                }

                FileSystem = SetupFileSystem(
                    TsConfigUnderSolution,
                    TsConfigUnderProject,
                    TsConfigUnderNodeModules);

                TestSubject = CreateTestSubject(FolderWorkspaceService.Object, VsHierarchyLocator.Object, FileSystem);
            }

            public Mock<IFolderWorkspaceService> FolderWorkspaceService { get; }
            public Mock<IVsHierarchy> VsProject { get; }
            public Mock<IVsHierarchyLocator> VsHierarchyLocator { get; }
            public MockFileSystem FileSystem { get; }
            public TsConfigsLocator TestSubject { get; }

            public void VerifyVsProjectWasChecked()
            {
                object projectDirectory = It.IsAny<string>();

                VsProject.Verify(x => x.GetProperty(
                    (uint)VSConstants.VSITEMID.Root,
                    (int)__VSHPROPID.VSHPROPID_ProjectDir,
                    out projectDirectory), Times.Once);
            }

            public void ResetFileSystemMocks()
            {
                var existingFiles = FileSystem.AllFiles.ToList();

                foreach (var file in existingFiles)
                {
                    FileSystem.RemoveFile(file);
                }
            }
        }

        private static MockFileSystem SetupFileSystem(params string[] files)
        {
            var fileSystem = new MockFileSystem();

            foreach (var file in files)
            {
                fileSystem.AddFile(file, new MockFileData("some content"));
            }

            return fileSystem;
        }

        private static Mock<IFolderWorkspaceService> SetupFolderWorkspaceService(string rootDirectory)
        {
            var folderWorkspaceService = new Mock<IFolderWorkspaceService>();
            
            folderWorkspaceService.Setup(x => x.FindRootDirectory()).Returns(rootDirectory);

            return folderWorkspaceService;
        }

        private static Mock<IVsHierarchyLocator> SetupVsHierarchyLocator(string sourceFilePath, IVsHierarchy vsHierarchy)
        {
            var vsHierarchyLocator = new Mock<IVsHierarchyLocator>();

            vsHierarchyLocator
                .Setup(x => x.GetVsHierarchyForFile(sourceFilePath))
                .Returns(vsHierarchy);

            return vsHierarchyLocator;
        }

        private static Mock<IVsHierarchy> SetupVsProject(object projectDirectory)
        {
            var vsHierarchy = new Mock<IVsHierarchy>();

            vsHierarchy.Setup(x => x.GetProperty(
                (uint) VSConstants.VSITEMID.Root,
                (int) __VSHPROPID.VSHPROPID_ProjectDir,
                out projectDirectory));

            return vsHierarchy;
        }

        private static TsConfigsLocator CreateTestSubject(IFolderWorkspaceService folderWorkspaceService,
            IVsHierarchyLocator vsHierarchyLocator,
            IFileSystem fileSystem,
            ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();

            return new TsConfigsLocator(folderWorkspaceService, vsHierarchyLocator, fileSystem, logger);
        }
    }
}
