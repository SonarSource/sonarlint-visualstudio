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
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class ProjectDirectoryProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ProjectDirectoryProvider, IProjectDirectoryProvider>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>()),
                MefTestHelpers.CreateExport<IVsHierarchyLocator>(Mock.Of<IVsHierarchyLocator>()),
            });
        }

        [TestMethod]
        public void GetProjectDirectory_OpenAsFolderProject_ProblemRetrievingSolutionDirectory_Null()
        {
            var vsSolution = SetupVsSolution(isOpenAsFolder: true, solutionDirectory: null);
            var testSubject = CreateTestSubject(vsSolution.Object);

            var result = testSubject.GetProjectDirectory("some file");

            result.Should().BeNull();
        }

        [TestMethod]
        public void GetProjectDirectory_OpenAsFolderProject_SucceededRetrievingSolutionDirectory_SolutionDirectory()
        {
            var vsSolution = SetupVsSolution(isOpenAsFolder: true, solutionDirectory: "some directory");
            var testSubject = CreateTestSubject(vsSolution.Object);

            var result = testSubject.GetProjectDirectory("some file");

            result.Should().Be("some directory");
        }

        [TestMethod]
        [DataRow(null)] // the API returns null for VS2015
        [DataRow(false)]
        public void Locate_RegularProject_ProblemRetrievingVsHierarchy_EmptyList(bool? isOpenAsFolder)
        {
            var vsHierarchyLocator = SetupVsHierarchyLocator("some file", null);
            var vsSolution = SetupVsSolution(isOpenAsFolder, solutionDirectory: "some directory");
            var testSubject = CreateTestSubject(vsSolution.Object, vsHierarchyLocator.Object);

            var result = testSubject.GetProjectDirectory("some file");
            result.Should().BeNull();

            vsHierarchyLocator.Verify(x => x.GetVsHierarchyForFile("some file"), Times.Once);
        }

        [TestMethod]
        [DataRow(null)] // the API returns null for VS2015
        [DataRow(false)]
        public void GetProjectDirectory_RegularProject_NoProjectDirectory_Null(bool? isOpenAsFolder)
        {
            var vsProject = SetupVsProject(null);
            var vsHierarchyLocator = SetupVsHierarchyLocator("some file", vsProject.Object);
            var vsSolution = SetupVsSolution(isOpenAsFolder, solutionDirectory: "some directory");
            var testSubject = CreateTestSubject(vsSolution.Object, vsHierarchyLocator.Object);

            var result = testSubject.GetProjectDirectory("some file");
            result.Should().BeNull();

            vsHierarchyLocator.Verify(x => x.GetVsHierarchyForFile("some file"), Times.Once);
            VerifyVsProjectWasChecked(vsProject);
        }

        [TestMethod]
        [DataRow(null)] // the API returns null for VS2015
        [DataRow(false)]
        public void GetProjectDirectory_RegularProject_ReturnsProjectDirectory(bool? isOpenAsFolder)
        {
            var vsProject = SetupVsProject("some project directory");
            var vsHierarchyLocator = SetupVsHierarchyLocator("some file", vsProject.Object);
            var vsSolution = SetupVsSolution(isOpenAsFolder, solutionDirectory: "some directory");
            var testSubject = CreateTestSubject(vsSolution.Object, vsHierarchyLocator.Object);

            var result = testSubject.GetProjectDirectory("some file");
            result.Should().Be("some project directory");

            vsHierarchyLocator.Verify(x => x.GetVsHierarchyForFile("some file"), Times.Once);
        }

        private static Mock<IVsSolution> SetupVsSolution(bool? isOpenAsFolder, object solutionDirectory)
        {
            object result = isOpenAsFolder;
            var vsSolution = new Mock<IVsSolution>();

            vsSolution
                .Setup(x => x.GetProperty(ProjectDirectoryProvider.VSPROPID_IsInOpenFolderMode, out result))
                .Returns(VSConstants.S_OK);

            vsSolution
                .Setup(x => x.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out solutionDirectory))
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
                (uint)VSConstants.VSITEMID.Root,
                (int)__VSHPROPID.VSHPROPID_ProjectDir,
                out projectDirectory));

            return vsHierarchy;
        }

        public void VerifyVsProjectWasChecked(Mock<IVsHierarchy> vsProject)
        {
            object projectDirectory = It.IsAny<string>();

            vsProject.Verify(x => x.GetProperty(
                (uint)VSConstants.VSITEMID.Root,
                (int)__VSHPROPID.VSHPROPID_ProjectDir,
                out projectDirectory), Times.Once);
        }

        private static ProjectDirectoryProvider CreateTestSubject(IVsSolution vsSolution, 
            IVsHierarchyLocator vsHierarchyLocator = null)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(vsSolution);

            return new ProjectDirectoryProvider(serviceProvider.Object, vsHierarchyLocator);
        }
    }
}
