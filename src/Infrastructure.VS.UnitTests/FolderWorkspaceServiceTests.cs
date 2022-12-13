﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
    public class FolderWorkspaceServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<FolderWorkspaceService, IFolderWorkspaceService>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<IWorkspaceService>());
        }

        [TestMethod]
        public void FindRootDirectory_OpenAsFolderProject_ProblemRetrievingSolutionDirectory_Null()
        {
            var testSubject = CreateTestSubject(isOpenAsFolder: true, solutionDirectory: null);

            var result = testSubject.FindRootDirectory();

            result.Should().BeNull();
        }

        [TestMethod]
        public void FindRootDirectory_OpenAsFolderProject_SucceededRetrievingSolutionDirectory_SolutionDirectory()
        {
            var testSubject = CreateTestSubject(isOpenAsFolder: true, solutionDirectory: "some directory");

            var result = testSubject.FindRootDirectory();

            result.Should().Be("some directory");
        }

        [TestMethod]
        [DataRow(null)] // the API returns null for VS2015
        [DataRow(false)]
        public void FindRootDirectory_NotOpenAsFolder_Null(bool? isOpenAsFolder)
        {
            var testSubject = CreateTestSubject(isOpenAsFolder, solutionDirectory: "some directory");

            var result = testSubject.FindRootDirectory();
            result.Should().BeNull();
        }

        [TestMethod]
        [DataRow(null)] // the API returns null for VS2015
        [DataRow(false)]
        public void IsFolderWorkspace_NotOpenAsFolder_False(bool? isOpenAsFolder)
        {
            var testSubject = CreateTestSubject(isOpenAsFolder, solutionDirectory: "some directory");

            var result = testSubject.IsFolderWorkspace();

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsFolderWorkspace_OpenAsFolder_True()
        {
            var testSubject = CreateTestSubject(true, solutionDirectory: "some directory");

            var result = testSubject.IsFolderWorkspace();

            result.Should().BeTrue();
        }

        private FolderWorkspaceService CreateTestSubject(bool? isOpenAsFolder, string solutionDirectory)
        {
            var vsSolution = SetupVsSolution(isOpenAsFolder);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(vsSolution.Object);

            var workspaceService = new Mock<IWorkspaceService>();
            workspaceService.Setup(x => x.FindRootDirectory()).Returns(solutionDirectory);

            return new FolderWorkspaceService(serviceProvider.Object, workspaceService.Object);
        }

        private static Mock<IVsSolution> SetupVsSolution(bool? isOpenAsFolder)
        {
            object result = isOpenAsFolder;
            var vsSolution = new Mock<IVsSolution>();

            vsSolution
                .Setup(x => x.GetProperty(FolderWorkspaceService.VSPROPID_IsInOpenFolderMode, out result))
                .Returns(VSConstants.S_OK);

            return vsSolution;
        }
    }
}
