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

using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using SonarLint.VisualStudio.Core;
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
                MefTestHelpers.CreateExport<IActiveConfigScopeTracker>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<ClientInfoListener>();
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

        [TestMethod]
        public async Task ListFilesAsync_FolderWorkSpace_UsesFolderWorkspaceService()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(true, "C:\\Code\\Project", "C:\\Code\\Project\\File1.js", "C:\\Code\\Project\\File2.js", "C:\\Code\\Project\\Folder1\\File3.js");
            var solutionWorkspaceService = CreateSolutionWorkspaceService();
            var activeConfigScopeTracker = CreateActiveConfigScopeTracker();

            var testSubject = CreateTestSubject(folderWorkspaceService: folderWorkspaceService, solutionWorkspaceService: solutionWorkspaceService, activeConfigScopeTracker: activeConfigScopeTracker);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();

            files.Should().HaveCount(3);

            files[0].uri.Should().Be("file://QzpcQ29kZVxQcm9qZWN0XEZpbGUxLmpz");
            files[0].ideRelativePath.Should().Be("\\File1.js");
            files[0].configScopeId.Should().Be(ConfigScopeId);
            files[0].isTest.Should().BeNull();
            files[0].charset.Should().Be("utf-8");
            files[0].fsPath.Should().Be("C:\\Code\\Project\\File1.js");
            files[0].content.Should().BeNull();

            files[1].uri.Should().Be("file://QzpcQ29kZVxQcm9qZWN0XEZpbGUyLmpz");
            files[1].ideRelativePath.Should().Be("\\File2.js");
            files[1].configScopeId.Should().Be(ConfigScopeId);
            files[1].isTest.Should().BeNull();
            files[1].charset.Should().Be("utf-8");
            files[1].fsPath.Should().Be("C:\\Code\\Project\\File2.js");
            files[1].content.Should().BeNull();

            files[2].uri.Should().Be("file://QzpcQ29kZVxQcm9qZWN0XEZvbGRlcjFcRmlsZTMuanM=");
            files[2].ideRelativePath.Should().Be("\\Folder1\\File3.js");
            files[2].configScopeId.Should().Be(ConfigScopeId);
            files[2].isTest.Should().BeNull();
            files[2].charset.Should().Be("utf-8");
            files[2].fsPath.Should().Be("C:\\Code\\Project\\Folder1\\File3.js");
            files[2].content.Should().BeNull();

            solutionWorkspaceService.DidNotReceive().ListFiles();
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, "C:\\Code\\Project");
        }

        [TestMethod]
        public async Task ListFilesAsync_SolutionWorkSpace_UsesSoluionWorkspaceService()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(false, null);
            var solutionWorkspaceService = CreateSolutionWorkspaceService("C:\\Code\\Project\\File1.js", "C:\\Code\\Project\\File2.js", "C:\\Code\\Project\\Folder1\\File3.js");
            var activeConfigScopeTracker = CreateActiveConfigScopeTracker();

            var testSubject = CreateTestSubject(folderWorkspaceService: folderWorkspaceService, solutionWorkspaceService: solutionWorkspaceService, activeConfigScopeTracker: activeConfigScopeTracker);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();

            files.Should().HaveCount(3);

            files[0].uri.Should().Be("file://QzpcQ29kZVxQcm9qZWN0XEZpbGUxLmpz");
            files[0].ideRelativePath.Should().Be("\\Code\\Project\\File1.js");
            files[0].configScopeId.Should().Be(ConfigScopeId);
            files[0].isTest.Should().BeNull();
            files[0].charset.Should().Be("utf-8");
            files[0].fsPath.Should().Be("C:\\Code\\Project\\File1.js");
            files[0].content.Should().BeNull();

            files[1].uri.Should().Be("file://QzpcQ29kZVxQcm9qZWN0XEZpbGUyLmpz");
            files[1].ideRelativePath.Should().Be("\\Code\\Project\\File2.js");
            files[1].configScopeId.Should().Be(ConfigScopeId);
            files[1].isTest.Should().BeNull();
            files[1].charset.Should().Be("utf-8");
            files[1].fsPath.Should().Be("C:\\Code\\Project\\File2.js");
            files[1].content.Should().BeNull();

            files[2].uri.Should().Be("file://QzpcQ29kZVxQcm9qZWN0XEZvbGRlcjFcRmlsZTMuanM=");
            files[2].ideRelativePath.Should().Be("\\Code\\Project\\Folder1\\File3.js");
            files[2].configScopeId.Should().Be(ConfigScopeId);
            files[2].isTest.Should().BeNull();
            files[2].charset.Should().Be("utf-8");
            files[2].fsPath.Should().Be("C:\\Code\\Project\\Folder1\\File3.js");
            files[2].content.Should().BeNull();

            folderWorkspaceService.DidNotReceive().ListFiles();
            activeConfigScopeTracker.Received(1).TryUpdateRootOnCurrentConfigScope(ConfigScopeId, "C:");
        }

        [TestMethod]
        public async Task ListFilesAsync_ConfigScopeChanged_ReturnsEmpty()
        {
            var folderWorkspaceService = CreateFolderWorkSpaceService(false, null);
            var solutionWorkspaceService = CreateSolutionWorkspaceService("C:\\Code\\Project\\File1.js", "C:\\Code\\Project\\File2.js", "C:\\Code\\Project\\Folder1\\File3.js");
            var activeConfigScopeTracker = CreateActiveConfigScopeTracker("changedId");

            var testSubject = CreateTestSubject(folderWorkspaceService: folderWorkspaceService, solutionWorkspaceService: solutionWorkspaceService, activeConfigScopeTracker: activeConfigScopeTracker);

            var result = await testSubject.ListFilesAsync(new ListFilesParams(ConfigScopeId));

            var files = result.files.ToList();

            files.Should().BeEmpty();
        }

        private IFolderWorkspaceService CreateFolderWorkSpaceService(bool isFolderWorkspace, string root, params string[] files)
        {
            var folderWorkspaceService = Substitute.For<IFolderWorkspaceService>();
            folderWorkspaceService.IsFolderWorkspace().Returns(isFolderWorkspace);
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

        private ListFilesListener CreateTestSubject(IFolderWorkspaceService folderWorkspaceService = null, ISolutionWorkspaceService solutionWorkspaceService = null, IActiveConfigScopeTracker activeConfigScopeTracker = null)
        {
            folderWorkspaceService ??= Substitute.For<IFolderWorkspaceService>();
            solutionWorkspaceService ??= Substitute.For<ISolutionWorkspaceService>();
            activeConfigScopeTracker ??= CreateActiveConfigScopeTracker();

            var testSubject = new ListFilesListener(folderWorkspaceService, solutionWorkspaceService, activeConfigScopeTracker);
            return testSubject;
        }
    }
}
