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
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Files;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation
{
    [TestClass]
    public class ListFilesListenerTests
    {
        private const string ConfigScopeId = "Some ID";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ListFilesListener, ISLCoreListener>(
                MefTestHelpers.CreateExport<IFolderWorkspaceService>(),
                MefTestHelpers.CreateExport<ISolutionWorkspaceService>(),
                MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
                MefTestHelpers.CreateExport<IClientFileDtoFactory>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<ListFilesListener>();
        }

        [TestMethod]
        public async Task ListFilesAsync_DifferentConfigScopeId_ReturnsEmpty()
        {
            var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
            activeConfigScopeTracker.Current.Returns(new ConfigurationScope("Not Matching Id"));

            var testSubject = CreateTestSubject(activeConfigScopeTracker: activeConfigScopeTracker);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            result.files.Should().BeEmpty();
        }

        [DataTestMethod]
        [DataRow("C:\\Code\\Project")]
        [DataRow("C:\\Code\\Project\\")]
        public async Task ListFilesAsync_FolderWorkSpace_UsesFolderWorkspaceService(string root)
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(root, "C:\\Code\\Project\\File1.js", "C:\\Code\\Project\\File2.js", "C:\\Code\\Project\\Folder1\\File3.js");
            var solutionWorkspaceService = CreateSolutionWorkspaceService();
            var activeConfigScopeTracker = CreateActiveConfigScopeTracker();

            var testSubject = CreateTestSubject(folderWorkspaceService: folderWorkspaceService, solutionWorkspaceService: solutionWorkspaceService, activeConfigScopeTracker: activeConfigScopeTracker);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().HaveCount(3);
            solutionWorkspaceService.DidNotReceive().ListFiles();
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, "C:\\Code\\Project\\");
        }

        [DataTestMethod]
        [DataRow("C:\\Code\\My Project", "C:\\Code\\My Project\\File1.js", "C:\\Code\\My Project\\")] // supports whitespace
        [DataRow("C:\\привет", "C:\\привет\\project\\file1.js", "C:\\привет\\")] // supports localized
        public async Task ListFilesAsync_FolderWorkSpace_UpdatesRootPath(string workspaceRootPath, string filePath, string expectedRootPath)
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(workspaceRootPath, filePath);
            var solutionWorkspaceService = CreateSolutionWorkspaceService();
            var activeConfigScopeTracker = CreateActiveConfigScopeTracker();

            var testSubject = CreateTestSubject(folderWorkspaceService: folderWorkspaceService, solutionWorkspaceService: solutionWorkspaceService, activeConfigScopeTracker: activeConfigScopeTracker);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().ContainSingle();
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, expectedRootPath);
        }

        [TestMethod]
        public async Task ListFilesAsync_SolutionWorkSpace_UsesSolutionWorkspaceService()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(null);
            var solutionWorkspaceService = CreateSolutionWorkspaceService("C:\\Code\\Project\\File1.js", "C:\\Code\\Project\\File2.js", "C:\\Code\\Project\\Folder1\\File3.js");
            var activeConfigScopeTracker = CreateActiveConfigScopeTracker();

            var testSubject = CreateTestSubject(folderWorkspaceService: folderWorkspaceService, solutionWorkspaceService: solutionWorkspaceService, activeConfigScopeTracker: activeConfigScopeTracker);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().HaveCount(3);
            folderWorkspaceService.DidNotReceive().ListFiles();
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, "C:\\");
        }

        [DataTestMethod]
        [DataRow("C:\\Code\\My Project\\File1.js", "C:\\Code\\My Project\\My Favorite File2.js", "C:\\")] // supports whitespace
        [DataRow("C:\\привет\\project\\file1.js", "C:\\привет\\project\\file2.js", "C:\\")] // supports localized
        [DataRow("\\\\servername\\work\\project\\file1.js", "\\\\servername\\work\\project\\file2.js", "\\\\servername\\work\\")] // supports UNC
        public async Task ListFilesAsync_SolutionWorkSpace_UpdatesRootPath(string filePath1, string filepath2, string expectedRootPath)
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(null);
            var solutionWorkspaceService = CreateSolutionWorkspaceService(filePath1, filepath2);
            var activeConfigScopeTracker = CreateActiveConfigScopeTracker();

            var testSubject = CreateTestSubject(folderWorkspaceService: folderWorkspaceService, solutionWorkspaceService: solutionWorkspaceService, activeConfigScopeTracker: activeConfigScopeTracker);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().HaveCount(2);
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, expectedRootPath);
        }

        [TestMethod]
        public async Task ListFilesAsync_ConfigScopeChanged_ReturnsEmpty()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(null);
            var solutionWorkspaceService = CreateSolutionWorkspaceService("C:\\Code\\Project\\File1.js", "C:\\Code\\Project\\File2.js", "C:\\Code\\Project\\Folder1\\File3.js");
            var activeConfigScopeTracker = CreateActiveConfigScopeTracker("changedId");

            var testSubject = CreateTestSubject(folderWorkspaceService: folderWorkspaceService, solutionWorkspaceService: solutionWorkspaceService, activeConfigScopeTracker: activeConfigScopeTracker);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().BeEmpty();
        }

        private IFolderWorkspaceService CreateFolderWorkSpaceService(string root, params string[] files)
        {
            var folderWorkspaceService = Substitute.For<IFolderWorkspaceService>();
            folderWorkspaceService.IsFolderWorkspace().Returns(root != null);
            folderWorkspaceService.FindRootDirectory().Returns(root);
            folderWorkspaceService.ListFiles().Returns(files);

            return folderWorkspaceService;
        }

        private ISolutionWorkspaceService CreateSolutionWorkspaceService(params string[] files)
        {
            var solutionWorkspaceService = Substitute.For<ISolutionWorkspaceService>();
            solutionWorkspaceService.ListFiles().Returns(files);

            return solutionWorkspaceService;
        }

        private IActiveConfigScopeTracker CreateActiveConfigScopeTracker(string changedActiveConfigScopeId = null)
        {
            changedActiveConfigScopeId ??= ConfigScopeId;

            var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
            activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId));
            activeConfigScopeTracker.TryUpdateRootOnCurrentConfigScope(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
            activeConfigScopeTracker.TryUpdateRootOnCurrentConfigScope(changedActiveConfigScopeId, Arg.Any<string>()).Returns(true);

            return activeConfigScopeTracker;
        }

        private ListFilesListener CreateTestSubject(IFolderWorkspaceService folderWorkspaceService = null,
            ISolutionWorkspaceService solutionWorkspaceService = null, IActiveConfigScopeTracker activeConfigScopeTracker = null,
            IClientFileDtoFactory clientFileDtoFactory = null)
        {
            folderWorkspaceService ??= Substitute.For<IFolderWorkspaceService>();
            solutionWorkspaceService ??= Substitute.For<ISolutionWorkspaceService>();
            activeConfigScopeTracker ??= CreateActiveConfigScopeTracker();
            clientFileDtoFactory ??= Substitute.For<IClientFileDtoFactory>();

            var testSubject = new ListFilesListener(folderWorkspaceService, solutionWorkspaceService, activeConfigScopeTracker, clientFileDtoFactory);
            return testSubject;
        }
    }
}
