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

using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class FolderWorkspaceServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<FolderWorkspaceService, IFolderWorkspaceService>(
                MefTestHelpers.CreateExport<ISolutionInfoProvider>());
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
        [DataRow(true)]
        [DataRow(false)]
        public void IsFolderWorkspace_GetsResultFromSolutionInfoProvider(bool isOpenAsFolder)
        {
            var testSubject = CreateTestSubject(isOpenAsFolder, solutionDirectory: "some directory");

            var result = testSubject.IsFolderWorkspace();

            result.Should().Be(isOpenAsFolder);
        }

        private FolderWorkspaceService CreateTestSubject(bool isOpenAsFolder, string solutionDirectory)
        {
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            solutionInfoProvider.Setup(x => x.GetSolutionDirectory()).Returns(solutionDirectory);
            solutionInfoProvider.Setup(x => x.IsFolderWorkspace()).Returns(isOpenAsFolder);

            return new FolderWorkspaceService(solutionInfoProvider.Object);
        }
    }
}
