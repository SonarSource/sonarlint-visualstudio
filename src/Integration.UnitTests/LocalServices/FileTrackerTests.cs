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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Integration.LocalServices;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;
using SonarLint.VisualStudio.SLCore.Service.File;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices;

[TestClass]
public class FileTrackerTests
{
    private const string ConfigScopeId = "CONFIG_SCOPE_ID";
    private const string RootPath = "C:\\";
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IClientFileDtoFactory clientFileDtoFactory;
    private IFileRpcSLCoreService fileRpcSlCoreService;
    private TestLogger logger;
    private ISLCoreServiceProvider serviceProvider;
    private FileTracker testSubject;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        fileRpcSlCoreService = Substitute.For<IFileRpcSLCoreService>();
        serviceProvider.TryGetTransientService(out IFileRpcSLCoreService _).Returns(x =>
        {
            x[0] = fileRpcSlCoreService;
            return true;
        });

        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, RootPath: RootPath));
        clientFileDtoFactory = Substitute.For<IClientFileDtoFactory>();

        threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>())
            .Returns(async info => await info.Arg<Func<Task<int>>>()());

        logger = new TestLogger();

        testSubject = new FileTracker(serviceProvider, activeConfigScopeTracker, threadHandling, clientFileDtoFactory, logger);
    }

    [TestMethod]
    public void MefCtor_CheckExports() =>
        MefTestHelpers.CheckTypeCanBeImported<FileTracker, IFileTracker>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<IClientFileDtoFactory>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<FileTracker>();

    [TestMethod]
    public void Ctor_SetsLogContext()
    {
        var logger = Substitute.For<ILogger>();

        _ = new FileTracker(
            Substitute.For<ISLCoreServiceProvider>(),
            Substitute.For<IActiveConfigScopeTracker>(),
            Substitute.For<IThreadHandling>(),
            Substitute.For<IClientFileDtoFactory>(),
            logger);

        logger.Received().ForContext(SLCoreStrings.SLCoreName, SLCoreStrings.FileSubsystem_LogContext, SLCoreStrings.FileTracker_LogContext);
    }

    [TestMethod]
    public void AddFiles_ServiceProviderFailed_LogsError()
    {
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>())
            .Returns(async info => await info.Arg<Func<Task<int>>>()());
        serviceProvider.TryGetTransientService(out Arg.Any<IFileRpcSLCoreService>()).Returns(false);

        testSubject.AddFiles(new SourceFile("C:\\Users\\test\\TestProject\\AFile.cs"));

        logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void AddFiles_NoActiveConfigScope_EventIgnored()
    {
        const string filePath = "C:\\Users\\test\\TestProject\\AFile.cs";
        activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);

        testSubject.AddFiles(new SourceFile(filePath));

        clientFileDtoFactory.DidNotReceiveWithAnyArgs().CreateOrNull(default, default, default);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidUpdateFileSystem(default);
        logger.AssertPartialOutputStringExists(SLCoreStrings.ConfigScopeNotInitialized);
    }


    [TestMethod]
    public void AddFiles_NoRootForConfigScope_EventIgnored()
    {
        const string filePath = "C:\\Users\\test\\TestProject\\AFile.cs";
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, RootPath: null));

        testSubject.AddFiles(new SourceFile(filePath));

        clientFileDtoFactory.DidNotReceiveWithAnyArgs().CreateOrNull(default, default, default);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidUpdateFileSystem(default);
        logger.AssertPartialOutputStringExists(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void AddFiles_ShouldForwardFilesToSlCore()
    {
        const string filePath = "C:\\Users\\test\\TestProject\\AFile.cs";
        DidUpdateFileSystemParams result = null;
        var clientFileDto = CreateDefaultClientFileDto();
        SetUpDtoFactory(filePath, clientFileDto);
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.AddFiles(new SourceFile(filePath));

        result.removedFiles.Should().BeEmpty();
        result.addedFiles.Should().BeEmpty();
        result.changedFiles.Should().BeEquivalentTo([clientFileDto]);
    }

    [TestMethod]
    public void AddFiles_CanNotConvertSomeDtos_SkipsNotConvertedDtos()
    {
        var filePath1 = "C:\\Users\\test\\TestProject\\AFile.cs";
        var clientFileDto1 = CreateDefaultClientFileDto();
        var filePath2 = "C:\\Users\\test\\TestProject\\BFile.cs";
        DidUpdateFileSystemParams result = null;
        SetUpDtoFactory(filePath1, clientFileDto1);
        SetUpDtoFactory(filePath2, null);
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.AddFiles(new SourceFile(filePath1), new SourceFile(filePath2));

        result.removedFiles.Should().BeEmpty();
        result.addedFiles.Should().BeEmpty();
        result.changedFiles.Should().BeEquivalentTo([clientFileDto1]);
    }

    [TestMethod]
    public void AddFiles_CanNotCreateDto_ShouldNotNotifySlCore()
    {
        var filePath = "C:\\Users\\test\\TestProject\\AFile.cs";
        SetUpDtoFactory(filePath, null);

        testSubject.AddFiles(new SourceFile(filePath));

        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidUpdateFileSystem(default);
    }

    [TestMethod]
    public void RemoveFiles_ShouldForwardFilesToSlCore()
    {
        DidUpdateFileSystemParams result = null;
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.RemoveFiles("C:\\Users\\test\\TestProject\\AFile.cs");

        result.removedFiles.Should().ContainSingle();
        result.removedFiles[0].Should().BeEquivalentTo(new FileUri("C:\\Users\\test\\TestProject\\AFile.cs"));
        result.addedFiles.Should().BeEmpty();
        result.changedFiles.Should().BeEmpty();
    }

    [TestMethod]
    public void RemoveFiles_NoActiveConfigScope_EventIgnored()
    {
        const string filePath = "C:\\Users\\test\\TestProject\\AFile.cs";
        activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);

        testSubject.RemoveFiles(filePath);

        clientFileDtoFactory.DidNotReceiveWithAnyArgs().CreateOrNull(default, default, default);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidUpdateFileSystem(default);
        logger.AssertPartialOutputStringExists(SLCoreStrings.ConfigScopeNotInitialized);
    }


    [TestMethod]
    public void RemoveFiles_NoRootForConfigScope_EventIgnored()
    {
        const string filePath = "C:\\Users\\test\\TestProject\\AFile.cs";
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, RootPath: null));

        testSubject.RemoveFiles(filePath);

        clientFileDtoFactory.DidNotReceiveWithAnyArgs().CreateOrNull(default, default, default);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidUpdateFileSystem(default);
        logger.AssertPartialOutputStringExists(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void RenameFiles_ShouldForwardFilesToSlCore()
    {
        const string renamedFilePath = "C:\\Users\\test\\TestProject\\ARenamedFile.cs";
        var clientFileDto = CreateDefaultClientFileDto();
        SetUpDtoFactory(renamedFilePath, clientFileDto);
        DidUpdateFileSystemParams result = null;
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.RenameFiles(["C:\\Users\\test\\TestProject\\AFile.cs"],
            [new SourceFile(renamedFilePath)]);

        result.removedFiles.Should().ContainSingle();
        result.removedFiles[0].Should().BeEquivalentTo(new FileUri("C:\\Users\\test\\TestProject\\AFile.cs"));
        result.addedFiles.Should().BeEmpty();
        result.changedFiles.Should().BeEquivalentTo([clientFileDto]);
    }

    [TestMethod]
    public void RenameFiles_CanNotCreateDto_ShouldForwardOnlyRemovedFilesToSlCore()
    {
        const string renamedFilePath = "C:\\Users\\test\\TestProject\\ARenamedFile.cs";
        SetUpDtoFactory(renamedFilePath, null);
        DidUpdateFileSystemParams result = null;
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.RenameFiles(["C:\\Users\\test\\TestProject\\AFile.cs"],
            [new SourceFile(renamedFilePath)]);

        result.removedFiles.Should().ContainSingle();
        result.removedFiles[0].Should().BeEquivalentTo(new FileUri("C:\\Users\\test\\TestProject\\AFile.cs"));
        result.addedFiles.Should().BeEmpty();
        result.changedFiles.Should().BeEmpty();
    }

    [TestMethod]
    public void RenameFiles_NoActiveConfigScope_EventIgnored()
    {
        const string filePath = "C:\\Users\\test\\TestProject\\ARenamedFile.cs";
        activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);

        testSubject.RenameFiles(["C:\\Users\\test\\TestProject\\AFile.cs"], [new SourceFile(filePath)]);

        clientFileDtoFactory.DidNotReceiveWithAnyArgs().CreateOrNull(default, default, default);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidUpdateFileSystem(default);
        logger.AssertPartialOutputStringExists(SLCoreStrings.ConfigScopeNotInitialized);
    }


    [TestMethod]
    public void RenameFiles_NoRootForConfigScope_EventIgnored()
    {
        const string filePath = "C:\\Users\\test\\TestProject\\ARenamedFile.cs";
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, RootPath: null));

        testSubject.RenameFiles(["C:\\Users\\test\\TestProject\\AFile.cs"], [new SourceFile(filePath)]);

        clientFileDtoFactory.DidNotReceiveWithAnyArgs().CreateOrNull(default, default, default);
        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidUpdateFileSystem(default);
        logger.AssertPartialOutputStringExists(SLCoreStrings.ConfigScopeNotInitialized);
    }

    private void SetUpDtoFactory(string filePath, ClientFileDto clientFileDto) =>
        clientFileDtoFactory.CreateOrNull(ConfigScopeId, RootPath, Arg.Is<SourceFile>(x => x.FilePath == filePath)).Returns(clientFileDto);

    private static ClientFileDto CreateDefaultClientFileDto() => new(default, default, default, default, default, default);
}
