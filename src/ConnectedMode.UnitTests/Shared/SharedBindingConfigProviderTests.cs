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

using System.IO.Abstractions;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Shared
{
    [TestClass]
    public class SharedBindingConfigProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SharedBindingConfigProvider, ISharedBindingConfigProvider>(
                MefTestHelpers.CreateExport<IGitWorkspaceService>(),
                MefTestHelpers.CreateExport<ISharedFolderProvider>(),
                MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
                MefTestHelpers.CreateExport<ISharedBindingConfigFileProvider>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SharedBindingConfigProvider>();
        }

        [DataRow("Path", true, true)]
        [DataRow("Path", false, false)]
        [DataRow(null, true, false)]
        [DataRow(null, false, false)]
        [TestMethod]
        public void HasSharedBinding_ReturnsCorrect(string sharedFolder, bool fileExist, bool expectedResult)
        {
            var sharedFolderProvider = CreateSharedFolderProvider(sharedFolder);
            var fileSystem = CreateFileSystem(fileExist);

            var testSubject = CreateTestSubject(sharedFolderProvider: sharedFolderProvider, fileSystem: fileSystem);

            var result = testSubject.HasSharedBinding();

            result.Should().Be(expectedResult);
        }

        [TestMethod]
        public void GetSharedBinding_NoSharedFolder_ReturnsNull()
        {
            var sharedFolderProvider = CreateSharedFolderProvider();
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(sharedFolderProvider: sharedFolderProvider, logger: logger);

            var result = testSubject.GetSharedBinding();

            result.Should().BeNull();
            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStringExists("SonarLint shared folder was not found");
        }

        [TestMethod]
        public void GetSharedBinding_HasSharedFolder_ReturnsConfig()
        {
            var config = new SharedBindingConfigModel();

            var sharedFolderProvider = CreateSharedFolderProvider("C:\\Folder\\.sonarlint");
            var solutionInfoProvider = CreateSolutionInfoProvider("Solution");

            var sharedBindingConfigFileProvider = new Mock<ISharedBindingConfigFileProvider>();
            sharedBindingConfigFileProvider.Setup(x => x.ReadSharedBindingConfigFile("C:\\Folder\\.sonarlint\\Solution.json")).Returns(config);

            var testSubject = CreateTestSubject(sharedFolderProvider: sharedFolderProvider, solutionInfoProvider: solutionInfoProvider, sharedBindingConfigFileProvider: sharedBindingConfigFileProvider.Object);

            var result = testSubject.GetSharedBinding();

            result.Should().Be(config);
        }

        [TestMethod]
        public void SaveSharedBinding_HasSharedFolder_Saves()
        {
            var config = new SharedBindingConfigModel();

            var sharedFolderProvider = CreateSharedFolderProvider("C:\\Folder\\.sonarlint");
            var solutionInfoProvider = CreateSolutionInfoProvider("Solution");

            var sharedBindingConfigFileProvider = new Mock<ISharedBindingConfigFileProvider>();
            sharedBindingConfigFileProvider.Setup(x => x.WriteSharedBindingConfigFile("C:\\Folder\\.sonarlint\\Solution.json", config)).Returns(true);

            var testSubject = CreateTestSubject(sharedFolderProvider: sharedFolderProvider, solutionInfoProvider: solutionInfoProvider, sharedBindingConfigFileProvider: sharedBindingConfigFileProvider.Object);

            var result = testSubject.SaveSharedBinding(config);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void SaveSharedBinding_HasNotSharedFolder_InGit_Saves()
        {
            var config = new SharedBindingConfigModel();

            var sharedFolderProvider = CreateSharedFolderProvider();
            var solutionInfoProvider = CreateSolutionInfoProvider("Solution");
            var gitWorkspaceService = CreateGitWorkspaceService("C:\\Folder");

            var sharedBindingConfigFileProvider = new Mock<ISharedBindingConfigFileProvider>();
            sharedBindingConfigFileProvider.Setup(x => x.WriteSharedBindingConfigFile("C:\\Folder\\.sonarlint\\Solution.json", config)).Returns(true);

            var testSubject = CreateTestSubject(sharedFolderProvider: sharedFolderProvider, solutionInfoProvider: solutionInfoProvider, gitWorkspaceService: gitWorkspaceService, sharedBindingConfigFileProvider: sharedBindingConfigFileProvider.Object);

            var result = testSubject.SaveSharedBinding(config);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void SaveSharedBinding_HasNotSharedFolder_NotInGit_ReturnsFalse()
        {
            var config = new SharedBindingConfigModel();

            var sharedFolderProvider = CreateSharedFolderProvider();
            var gitWorkspaceService = CreateGitWorkspaceService(null);

            var sharedBindingConfigFileProvider = new Mock<ISharedBindingConfigFileProvider>();
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(sharedFolderProvider: sharedFolderProvider, gitWorkspaceService: gitWorkspaceService, sharedBindingConfigFileProvider: sharedBindingConfigFileProvider.Object, logger: logger);

            var result = testSubject.SaveSharedBinding(config);

            result.Should().BeFalse();
            sharedBindingConfigFileProvider.VerifyNoOtherCalls();
            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStringExists("no SonarLint shared folder or solution is not under git");
        }

        private IGitWorkspaceService CreateGitWorkspaceService(string gitRepo)
        {
            var gitService = new Mock<IGitWorkspaceService>();
            gitService.Setup(gs => gs.GetRepoRoot()).Returns(gitRepo);

            return gitService.Object;
        }

        private ISolutionInfoProvider CreateSolutionInfoProvider(string solutionName)
        {
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            solutionInfoProvider.Setup(sip => sip.GetSolutionName()).Returns(solutionName);

            return solutionInfoProvider.Object;
        }

        private IFileSystem CreateFileSystem(bool doesExist)
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.Exists(It.IsAny<string>())).Returns(doesExist);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(fs => fs.File).Returns(file.Object);

            return fileSystem.Object;
        }

        private ISharedFolderProvider CreateSharedFolderProvider(string folder = null)
        {
            var sharedFolderProvider = new Mock<ISharedFolderProvider>();
            sharedFolderProvider.Setup(sfp => sfp.GetSharedFolderPath()).Returns(folder);

            return sharedFolderProvider.Object;
        }

        private ISharedBindingConfigProvider CreateTestSubject(IGitWorkspaceService gitWorkspaceService = null,
            ISharedFolderProvider sharedFolderProvider = null,
            ISolutionInfoProvider solutionInfoProvider = null,
            ISharedBindingConfigFileProvider sharedBindingConfigFileProvider = null,
            ILogger logger = null,
            IFileSystem fileSystem = null)
        {
            gitWorkspaceService ??= Mock.Of<IGitWorkspaceService>();
            sharedFolderProvider ??= Mock.Of<ISharedFolderProvider>();
            solutionInfoProvider ??= Mock.Of<ISolutionInfoProvider>();
            sharedBindingConfigFileProvider ??= Mock.Of<ISharedBindingConfigFileProvider>();
            logger ??= Mock.Of<ILogger>();
            fileSystem ??= Mock.Of<IFileSystem>();

            return new SharedBindingConfigProvider(gitWorkspaceService, sharedFolderProvider, solutionInfoProvider, sharedBindingConfigFileProvider, logger, fileSystem);
        }
    }
}
