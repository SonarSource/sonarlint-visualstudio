/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Shared;

[TestClass]
public class SharedBindingConfigProviderTests
{
    private IGitWorkspaceService gitWorkspaceService;
    private ISharedFolderProvider sharedFolderProvider;
    private ISolutionInfoProvider solutionInfoProvider;
    private ISharedBindingConfigFileProvider sharedBindingConfigFileProvider;
    private TestLogger logger;
    private SharedBindingConfigProvider testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        gitWorkspaceService = Substitute.For<IGitWorkspaceService>();
        sharedFolderProvider = Substitute.For<ISharedFolderProvider>();
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        sharedBindingConfigFileProvider = Substitute.For<ISharedBindingConfigFileProvider>();
        logger = new TestLogger();

        testSubject = new SharedBindingConfigProvider(gitWorkspaceService, sharedFolderProvider, solutionInfoProvider, sharedBindingConfigFileProvider, logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SharedBindingConfigProvider, ISharedBindingConfigProvider>(
            MefTestHelpers.CreateExport<IGitWorkspaceService>(),
            MefTestHelpers.CreateExport<ISharedFolderProvider>(),
            MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
            MefTestHelpers.CreateExport<ISharedBindingConfigFileProvider>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SharedBindingConfigProvider>();

    [TestMethod]
    public void Ctor_SetsLogContext()
    {
        var mockLogger = Substitute.For<ILogger>();

        _ = new SharedBindingConfigProvider(gitWorkspaceService, sharedFolderProvider, solutionInfoProvider, sharedBindingConfigFileProvider, mockLogger);

        mockLogger.Received().ForContext(Resources.ConnectedModeLogContext, Resources.SharedBindingConfigProvider_LogContext);
    }

    [TestMethod]
    public void GetSharedBinding_NoSharedFolder_ReturnsNull()
    {
        SetUpSharedFolderProvider();

        var result = testSubject.GetSharedBinding();

        result.Should().BeNull();
        logger.AssertOutputStrings(1);
        logger.AssertPartialOutputStringExists(string.Format(Resources.SharedBindingConfigProvider_SharedFileNotFound, Resources.SharedBindingConfigProvider_SharedFileNotFound_ProbePathDefaultValue));
    }

    [TestMethod]
    public void GetSharedBinding_HasSharedFolder_ReturnsConfig()
    {
        const string filePath = "C:\\Folder\\.sonarlint\\Solution.json";
        var config = new SharedBindingConfigModel();
        SetUpSharedFolderProvider("C:\\Folder\\.sonarlint");
        SetUpSolutionInfoProvider("Solution");
        sharedBindingConfigFileProvider.Exists(filePath).Returns(true);
        sharedBindingConfigFileProvider.Read(filePath).Returns(config);

        var result = testSubject.GetSharedBinding();

        result.Should().Be(config);
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void GetSharedBinding_HasSharedFolderButNoConfig_ReturnsNull()
    {
        const string filePath = "C:\\Folder\\.sonarlint\\Solution.json";
        SetUpSharedFolderProvider("C:\\Folder\\.sonarlint");
        SetUpSolutionInfoProvider("Solution");
        sharedBindingConfigFileProvider.Exists(filePath).Returns(false);

        var result = testSubject.GetSharedBinding();

        result.Should().BeNull();
        logger.AssertOutputStrings(1);
        logger.AssertPartialOutputStringExists(string.Format(Resources.SharedBindingConfigProvider_SharedFileNotFound, filePath));
    }

    [TestMethod]
    public void GetSharedBindingFilePathOrNull_NoSharedFolder_ReturnsNull()
    {
        SetUpSharedFolderProvider();

        var result = testSubject.GetSharedBindingFilePathOrNull();

        result.Should().BeNull();
        sharedBindingConfigFileProvider.DidNotReceiveWithAnyArgs().Exists(default);
        sharedBindingConfigFileProvider.DidNotReceiveWithAnyArgs().Read(default);
        logger.AssertOutputStrings(1);
        logger.AssertPartialOutputStringExists(string.Format(Resources.SharedBindingConfigProvider_SharedFileNotFound, Resources.SharedBindingConfigProvider_SharedFileNotFound_ProbePathDefaultValue));
    }

    [TestMethod]
    public void GetSharedBindingFilePathOrNull_HasSharedFolder_ReturnsConfig()
    {
        const string filePath = "C:\\Folder\\.sonarlint\\Solution.json";
        SetUpSharedFolderProvider("C:\\Folder\\.sonarlint");
        SetUpSolutionInfoProvider("Solution");
        sharedBindingConfigFileProvider.Exists(filePath).Returns(true);

        var result = testSubject.GetSharedBindingFilePathOrNull();

        result.Should().Be(filePath);
        sharedBindingConfigFileProvider.DidNotReceiveWithAnyArgs().Read(default);
    }

    [TestMethod]
    public void GetSharedBindingFilePathOrNull_HasSharedFolderButNoConfig_ReturnsNull()
    {
        const string filePath = "C:\\Folder\\.sonarlint\\Solution.json";
        SetUpSharedFolderProvider("C:\\Folder\\.sonarlint");
        SetUpSolutionInfoProvider("Solution");
        sharedBindingConfigFileProvider.Exists(filePath).Returns(false);

        var result = testSubject.GetSharedBindingFilePathOrNull();

        result.Should().BeNull();
        sharedBindingConfigFileProvider.DidNotReceiveWithAnyArgs().Read(default);
        logger.AssertOutputStrings(1);
        logger.AssertPartialOutputStringExists(string.Format(Resources.SharedBindingConfigProvider_SharedFileNotFound, filePath));
    }

    [TestMethod]
    public void SaveSharedBinding_HasSharedFolder_Saves()
    {
        var config = new SharedBindingConfigModel();
        SetUpSharedFolderProvider("C:\\Folder\\.sonarlint");
        SetUpSolutionInfoProvider("Solution");
        sharedBindingConfigFileProvider.Write("C:\\Folder\\.sonarlint\\Solution.json", config).Returns(true);

        var result = testSubject.SaveSharedBinding(config);

        result.Should().Be("C:\\Folder\\.sonarlint\\Solution.json");
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void SaveSharedBinding_HasNotSharedFolder_InGit_Saves()
    {
        var config = new SharedBindingConfigModel();
        SetUpSharedFolderProvider();
        SetUpSolutionInfoProvider("Solution");
        CreateGitWorkspaceService("C:\\Folder");
        sharedBindingConfigFileProvider.Write("C:\\Folder\\.sonarlint\\Solution.json", config).Returns(true);

        var result = testSubject.SaveSharedBinding(config);

        result.Should().Be("C:\\Folder\\.sonarlint\\Solution.json");
        logger.AssertOutputStrings(1);
        logger.AssertPartialOutputStringExists(Resources.SharedBindingConfigProvider_SharedFolderNotFound);
    }

    [TestMethod]
    public void SaveSharedBinding_HasNotSharedFolder_NotInGit_ReturnsFalse()
    {
        var config = new SharedBindingConfigModel();

        SetUpSharedFolderProvider();
        CreateGitWorkspaceService(null);

        var result = testSubject.SaveSharedBinding(config);

        result.Should().BeNull();
        sharedBindingConfigFileProvider.DidNotReceiveWithAnyArgs().Write(default, default);
        logger.AssertOutputStrings(3);
        logger.AssertPartialOutputStringExists(Resources.SharedBindingConfigProvider_GitRootNotFound);
        logger.AssertPartialOutputStringExists(Resources.SharedBindingConfigProvider_SharedFolderNotFound);
        logger.AssertPartialOutputStringExists(Resources.SharedBindingConfigProvider_NoSaveLocationFound);
    }

    private void CreateGitWorkspaceService(string gitRepo) => gitWorkspaceService.GetRepoRoot().Returns(gitRepo);

    private void SetUpSolutionInfoProvider(string solutionName) => solutionInfoProvider.GetSolutionName().Returns(solutionName);

    private void SetUpSharedFolderProvider(string folder = null) => sharedFolderProvider.GetSharedFolderPath().Returns(folder);
}
