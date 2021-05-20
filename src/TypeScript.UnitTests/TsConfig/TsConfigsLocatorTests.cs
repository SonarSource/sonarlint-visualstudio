/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.TsConfig;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.TsConfig
{
    [TestClass]
    public class TsConfigsLocatorTests
    {
        private const string ValidSourceFilePath = "source-file.ts";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TsConfigsLocator, ITsConfigsLocator>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>()),
                MefTestHelpers.CreateExport<IVsHierarchyLocator>(Mock.Of<IVsHierarchyLocator>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>()),
            });
        }

        [TestMethod]
        public void Locate_OpenAsFolderProject_SearchesFilesUnderSolutionDirectory()
        {
            var testSuite = new TestSuite(isOpenAsFolder: true);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEquivalentTo(TestSuite.TsConfigUnderSolution, TestSuite.TsConfigUnderProject);

            testSuite.VsHierarchyLocator.Invocations.Should().BeEmpty();
            testSuite.VsProject.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Locate_OpenAsFolderProject_ProblemRetrievingSolutionDirectory_EmptyList()
        {
            var testSuite = new TestSuite(isOpenAsFolder: true, solutionDirectory: null);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEmpty();

            testSuite.VsHierarchyLocator.Invocations.Should().BeEmpty();
            testSuite.VsProject.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow(null)] // the API returns null for VS2015
        [DataRow(false)]
        public void Locate_RegularProject_SearchesFilesUnderProjectDirectory(bool? isOpenAsFolder)
        {
            var testSuite = new TestSuite(isOpenAsFolder: isOpenAsFolder);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEquivalentTo(TestSuite.TsConfigUnderProject);

            testSuite.VsHierarchyLocator.Verify(x => x.GetVsHierarchyForFile(ValidSourceFilePath), Times.Once);
            testSuite.VerifyVsProjectWasChecked();
        }

        [TestMethod]
        [DataRow(null)] // the API returns null for VS2015
        [DataRow(false)]
        public void Locate_RegularProject_ProblemRetrievingProjectDirectory_EmptyList(bool? isOpenAsFolder)
        {
            var testSuite = new TestSuite(isOpenAsFolder: isOpenAsFolder, projectDirectory: null);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEmpty();

            testSuite.VsHierarchyLocator.Verify(x=> x.GetVsHierarchyForFile(ValidSourceFilePath), Times.Once);
            testSuite.VerifyVsProjectWasChecked();
        }

        [TestMethod]
        [DataRow(null)] // the API returns null for VS2015
        [DataRow(false)]
        public void Locate_RegularProject_ProblemRetrievingVsHierarchy_EmptyList(bool? isOpenAsFolder)
        {
            var testSuite = new TestSuite(isOpenAsFolder: isOpenAsFolder, isNullVsProject: true);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEmpty();

            testSuite.VsHierarchyLocator.Verify(x => x.GetVsHierarchyForFile(ValidSourceFilePath), Times.Once);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        [DataRow(null)] // the API returns null for VS2015
        public void Locate_SearchesFilesUnderTheDirectory_IgnoresNodeModules(bool? isOpenAsFolder)
        {
            var testSuite = new TestSuite(isOpenAsFolder: isOpenAsFolder);
            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().NotContain(TestSuite.TsConfigUnderNodeModules);
        }

        [TestMethod]
        public void Locate_SearchesFilesUnderTheDirectory_FindsCorrectFiles()
        {
            var testSuite = new TestSuite(isOpenAsFolder: true);
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

            var rootDirectory = TestSuite.SolutionDirectory;

            foreach (var file in files)
            {
                testSuite.FileSystem.AddFile(Path.Combine(rootDirectory, file.Key), new MockFileData("some content"));
            }

            var expectedFiles = files
                .Where(x => x.Value)
                .Select(x => Path.Combine(rootDirectory, x.Key));

            var result = testSuite.TestSubject.Locate(ValidSourceFilePath);

            result.Should().BeEquivalentTo(expectedFiles);
        }

        private class TestSuite
        {
            public const string SolutionDirectory = "c:\\test\\solution\\";
            public const string ProjectDirectory = "c:\\test\\solution\\project\\";

            public static readonly string TsConfigUnderSolution = Path.Combine(SolutionDirectory, "tsconfig.json");
            public static readonly string TsConfigUnderProject = Path.Combine(ProjectDirectory, "tsconfig.json");
            public static readonly string TsConfigUnderNodeModules = Path.Combine(ProjectDirectory, "node_modules", "tsconfig.json");

            public TestSuite(bool? isOpenAsFolder, 
                bool isNullVsProject = false,
                string solutionDirectory = SolutionDirectory,
                string projectDirectory = ProjectDirectory)
            {
                VsSolution = SetupVsSolution(isOpenAsFolder, solutionDirectory);

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

                TestSubject = CreateTestSubject(VsSolution.Object, VsHierarchyLocator.Object, FileSystem);
            }

            public Mock<IVsSolution> VsSolution { get; }
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

        private static Mock<IVsSolution> SetupVsSolution(bool? isOpenAsFolder, object solutionDirectory)
        {
            object result = isOpenAsFolder;
            var vsSolution = new Mock<IVsSolution>();

            vsSolution
                .Setup(x => x.GetProperty(TsConfigsLocator.VSPROPID_IsInOpenFolderMode, out result))
                .Returns(VSConstants.S_OK);

            vsSolution
                .Setup(x => x.GetProperty((int) __VSPROPID.VSPROPID_SolutionDirectory, out solutionDirectory))
                .Returns(VSConstants.S_OK);

            return vsSolution;
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

        private static TsConfigsLocator CreateTestSubject(IVsSolution vsSolution,
            IVsHierarchyLocator vsHierarchyLocator,
            IFileSystem fileSystem,
            ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(vsSolution);

            return new TsConfigsLocator(serviceProvider.Object, vsHierarchyLocator, fileSystem, logger);
        }
    }
}
