/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Files;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation
{
    [TestClass]
    public class ListFilesListenerTests
    {
        private IFolderWorkspaceService folderWorkspaceService;
        private ISolutionWorkspaceService solutionWorkspaceService;
        private IActiveConfigScopeTracker activeConfigScopeTracker;
        private IClientFileDtoFactory clientFileDtoFactory;
        private ListFilesListener testSubject;
        private ISharedBindingConfigProvider sharedBindingConfigProvider;
        private const string ConfigScopeId = "Some ID";

        [TestInitialize]
        public void TestInitialize()
        {
            folderWorkspaceService = Substitute.For<IFolderWorkspaceService>();
            solutionWorkspaceService = Substitute.For<ISolutionWorkspaceService>();
            activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
            SetUpActiveConfigScopeTracker();
            clientFileDtoFactory = Substitute.For<IClientFileDtoFactory>();
            sharedBindingConfigProvider = Substitute.For<ISharedBindingConfigProvider>();
            testSubject = new ListFilesListener(folderWorkspaceService, solutionWorkspaceService, sharedBindingConfigProvider, activeConfigScopeTracker, clientFileDtoFactory);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<ListFilesListener, ISLCoreListener>(
                MefTestHelpers.CreateExport<IFolderWorkspaceService>(),
                MefTestHelpers.CreateExport<ISolutionWorkspaceService>(),
                MefTestHelpers.CreateExport<ISharedBindingConfigProvider>(),
                MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
                MefTestHelpers.CreateExport<IClientFileDtoFactory>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ListFilesListener>();

        [TestMethod]
        public async Task ListFilesAsync_DifferentConfigScopeId_ReturnsEmpty()
        {
            activeConfigScopeTracker.Current.Returns(new ConfigurationScope("Not Matching Id"));

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            result.files.Should().BeEmpty();
        }

        [DataTestMethod]
        [DataRow("C:\\Code\\Project", "C:\\Code\\Project\\")]
        [DataRow("C:\\Code\\Project\\", "C:\\Code\\Project\\")]
        public async Task ListFilesAsync_FolderWorkSpace_UsesFolderWorkspaceService(string root, string notmalizedRoot)
        {
            string[] filePaths = ["C:\\Code\\Project\\File1.js", "C:\\Code\\Project\\File2.js", "C:\\Code\\Project\\Folder1\\File3.js"];
            var clientFileDtos = SetUpDefaultDtosForFiles(filePaths, notmalizedRoot);
            SetUpFolderWorkSpaceService(root, filePaths);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().BeEquivalentTo(clientFileDtos);
            solutionWorkspaceService.DidNotReceive().ListFiles();
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, notmalizedRoot);
        }

        [TestMethod]
        public async Task ListFilesAsync_FolderWorkSpace_IgnoresNonConvertedFiles()
        {
            const string root = "C:\\Code\\Project\\";
            string[] filePaths = ["C:\\Code\\Project\\File1.js", "C:\\Code\\Project\\File2.js"];
            var filePath0Dto = CreateDefaultClientFileDto();
            SetUpDtoFactory(filePaths[0], filePath0Dto, root);
            SetUpDtoFactory(filePaths[1], null, root);
            SetUpFolderWorkSpaceService(root, filePaths);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().BeEquivalentTo([filePath0Dto]);
            solutionWorkspaceService.DidNotReceive().ListFiles();
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, root);
        }

        [DataTestMethod]
        [DataRow("C:\\Code\\My Project", "C:\\Code\\My Project\\File1.js", "C:\\Code\\My Project\\")] // supports whitespace
        [DataRow("C:\\привет", "C:\\привет\\project\\file1.js", "C:\\привет\\")] // supports localized
        [DataRow("\\\\servername\\work", "\\\\servername\\work\\project\\file2.js", "\\\\servername\\work\\")] // supports UNC
        public async Task ListFilesAsync_FolderWorkSpace_UpdatesRootPath(string workspaceRootPath, string filePath, string expectedRootPath)
        {
            string[] filePaths = [filePath];
            var clientFileDtos = SetUpDefaultDtosForFiles(filePaths, expectedRootPath);
            SetUpFolderWorkSpaceService(workspaceRootPath, filePath);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().BeEquivalentTo(clientFileDtos);
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, expectedRootPath);
        }

        [TestMethod]
        public async Task ListFilesAsync_SolutionWorkSpace_UsesSolutionWorkspaceService()
        {
            SetUpFolderWorkSpaceService(null);
            string[] filePaths = ["C:\\Code\\Project\\File1.js", "C:\\Code\\Project\\File2.js", "C:\\Code\\Project\\Folder1\\File3.js"];
            var clientFileDtos = SetUpDefaultDtosForFiles(filePaths, "C:\\");
            SetUpSolutionWorkspaceService(filePaths);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().BeEquivalentTo(clientFileDtos);
            folderWorkspaceService.DidNotReceive().ListFiles();
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, "C:\\");
        }

        [TestMethod]
        public async Task ListFilesAsync_SolutionWorkSpace_IgnoresNonConvertedFiles()
        {
            SetUpFolderWorkSpaceService(null);
            string[] filePaths = ["C:\\Code\\Project\\File1.js", "D:\\Code\\Project\\CanNotConvert.js"];
            var filePath0Dto = CreateDefaultClientFileDto();
            SetUpDtoFactory(filePaths[0], filePath0Dto, "C:\\");
            SetUpDtoFactory(filePaths[1], null, "C:\\");
            SetUpSolutionWorkspaceService(filePaths);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().BeEquivalentTo([filePath0Dto]);
            folderWorkspaceService.DidNotReceive().ListFiles();
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, "C:\\");
        }

        [DataTestMethod]
        [DataRow("C:\\Code\\My Project\\File1.js", "C:\\Code\\My Project\\My Favorite File2.js", "C:\\")] // supports whitespace
        [DataRow("C:\\привет\\project\\file1.js", "C:\\привет\\project\\file2.js", "C:\\")] // supports localized
        [DataRow("\\\\servername\\work\\project\\file1.js", "\\\\servername\\work\\project\\file2.js", "\\\\servername\\work\\")] // supports UNC
        public async Task ListFilesAsync_SolutionWorkSpace_UpdatesRootPath(string filePath1, string filepath2, string expectedRootPath)
        {
            SetUpFolderWorkSpaceService(null);
            string[] filePaths = [filePath1, filepath2];
            var clientFileDtos = SetUpDefaultDtosForFiles(filePaths, expectedRootPath);
            SetUpSolutionWorkspaceService(filePaths);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().BeEquivalentTo(clientFileDtos);
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, expectedRootPath);
        }

        [TestMethod]
        public async Task ListFilesAsync_ConfigScopeChanged_ReturnsEmpty()
        {
            SetUpFolderWorkSpaceService(null);
            string[] filePaths = ["C:\\Code\\Project\\File1.js", "C:\\Code\\Project\\File2.js", "C:\\Code\\Project\\Folder1\\File3.js"];
            SetUpDefaultDtosForFiles(filePaths, "C:\\");
            SetUpSolutionWorkspaceService(filePaths);
            SetUpActiveConfigScopeTracker(true);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();
            files.Should().BeEmpty();
        }

        private List<ClientFileDto> SetUpDefaultDtosForFiles(string[] filePaths, string rootPath)
        {
            var clientFileDtos = filePaths
                .Select(x =>
                {
                    var dto = CreateDefaultClientFileDto();
                    SetUpDtoFactory(x, dto, rootPath);
                    return dto;
                })
                .ToList();
            return clientFileDtos;
        }

        private void SetUpDtoFactory(
            string filePath,
            ClientFileDto clientFileDto,
            string rootPath) =>
            clientFileDtoFactory.CreateOrNull(ConfigScopeId, rootPath, Arg.Is<SourceFile>(x => x.FilePath == filePath)).Returns(clientFileDto);

        private static ClientFileDto CreateDefaultClientFileDto() => new(default, default, default, default, default, default);

        private void SetUpFolderWorkSpaceService(string root, params string[] files)
        {
            folderWorkspaceService.IsFolderWorkspace().Returns(root != null);
            folderWorkspaceService.FindRootDirectory().Returns(root);
            folderWorkspaceService.ListFiles().Returns(files);
        }

        private void SetUpSolutionWorkspaceService(params string[] files)
        {
            solutionWorkspaceService.ListFiles().Returns(files);
        }

        private void SetUpActiveConfigScopeTracker(bool shouldFailSetRoot = false)
        {
            activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId));
            activeConfigScopeTracker.TryUpdateRootOnCurrentConfigScope(ConfigScopeId, Arg.Any<string>()).Returns(!shouldFailSetRoot);
        }
    }
}
