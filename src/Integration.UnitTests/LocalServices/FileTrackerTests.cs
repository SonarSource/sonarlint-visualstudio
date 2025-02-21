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
    const string configScopeId = "CONFIG_SCOPE_ID";
    const string rootPath = "C:\\";

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<FileTracker, IFileTracker>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<IClientFileDtoFactory>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void AddFiles_ServiceProviderFailed_LogsError()
    {
        var serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        var clientFileDtoFactory = Substitute.For<IClientFileDtoFactory>();
        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>())
            .Returns(async info => await info.Arg<Func<Task<int>>>()());
        var testLogger = new TestLogger();

        var testSubject = new FileTracker(serviceProvider, activeConfigScopeTracker, threadHandling, clientFileDtoFactory, testLogger);

        testSubject.AddFiles(new SourceFile("C:\\Users\\test\\TestProject\\AFile.cs"));

        testLogger.AssertOutputStrings($"[FileTracker] {SLCoreStrings.ServiceProviderNotInitialized}");
    }

    [TestMethod]
    public void AddFiles_ShouldForwardFilesToSlCore()
    {
        var testSubject = CreateTestSubject(out var fileRpcSlCoreService, out var factory);
        const string filePath = "C:\\Users\\test\\TestProject\\AFile.cs";
        DidUpdateFileSystemParams result = null;
        var clientFileDto = CreateDefaultClientFileDto();
        SetUpDtoFactory(factory, filePath, clientFileDto);
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.AddFiles(new SourceFile(filePath));

        result.removedFiles.Should().BeEmpty();
        result.addedFiles.Should().BeEmpty();
        result.changedFiles.Should().BeEquivalentTo([clientFileDto]);
    }

    [TestMethod]
    public void AddFiles_CanNotConvertSomeDtos_SkipsNotConvertedDtos()
    {
        var testSubject = CreateTestSubject(out var fileRpcSlCoreService, out var factory);
        var filePath1 = "C:\\Users\\test\\TestProject\\AFile.cs";
        var clientFileDto1 = CreateDefaultClientFileDto();
        var filePath2 = "C:\\Users\\test\\TestProject\\BFile.cs";
        DidUpdateFileSystemParams result = null;
        SetUpDtoFactory(factory, filePath1, clientFileDto1);
        SetUpDtoFactory(factory, filePath2, null);
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.AddFiles(new SourceFile(filePath1), new SourceFile(filePath2));

        result.removedFiles.Should().BeEmpty();
        result.addedFiles.Should().BeEmpty();
        result.changedFiles.Should().BeEquivalentTo([clientFileDto1]);
    }

    [TestMethod]
    public void AddFiles_CanNotCreateDto_ShouldNotNotifySlCore()
    {
        var testSubject = CreateTestSubject(out var fileRpcSlCoreService, out var factory);
        var filePath = "C:\\Users\\test\\TestProject\\AFile.cs";
        var clientFileDto = (ClientFileDto)null;
        factory.CreateOrNull(configScopeId, rootPath, Arg.Is<SourceFile>(x => x.FilePath == filePath)).Returns(clientFileDto);

        testSubject.AddFiles(new SourceFile(filePath));

        fileRpcSlCoreService.DidNotReceiveWithAnyArgs().DidUpdateFileSystem(default);
    }

    [TestMethod]
    public void RemoveFiles_ShouldForwardFilesToSlCore()
    {
        var testSubject = CreateTestSubject(out var fileRpcSlCoreService, out _);
        DidUpdateFileSystemParams result = null;
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.RemoveFiles("C:\\Users\\test\\TestProject\\AFile.cs");

        result.removedFiles.Should().ContainSingle();
        result.removedFiles[0].Should().BeEquivalentTo(new FileUri("C:\\Users\\test\\TestProject\\AFile.cs"));
        result.addedFiles.Should().BeEmpty();
        result.changedFiles.Should().BeEmpty();
    }

    [TestMethod]
    public void RenameFiles_ShouldForwardFilesToSlCore()
    {
        const string renamedFilePath = "C:\\Users\\test\\TestProject\\ARenamedFile.cs";
        var clientFileDto = CreateDefaultClientFileDto();
        var testSubject = CreateTestSubject(out var fileRpcSlCoreService, out var factory);
        SetUpDtoFactory(factory, renamedFilePath, clientFileDto);
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
        var testSubject = CreateTestSubject(out var fileRpcSlCoreService, out var factory);
        SetUpDtoFactory(factory, renamedFilePath, null);
        DidUpdateFileSystemParams result = null;
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.RenameFiles(["C:\\Users\\test\\TestProject\\AFile.cs"],
            [new SourceFile(renamedFilePath)]);

        result.removedFiles.Should().ContainSingle();
        result.removedFiles[0].Should().BeEquivalentTo(new FileUri("C:\\Users\\test\\TestProject\\AFile.cs"));
        result.addedFiles.Should().BeEmpty();
        result.changedFiles.Should().BeEmpty();
    }

    private static void SetUpDtoFactory(IClientFileDtoFactory factory, string filePath, ClientFileDto clientFileDto) => factory.CreateOrNull(configScopeId, rootPath, Arg.Is<SourceFile>(x => x.FilePath == filePath)).Returns(clientFileDto);

    private static ClientFileDto CreateDefaultClientFileDto() => new(default, default, default, default, default, default);

    private static FileTracker CreateTestSubject(out IFileRpcSLCoreService slCoreService, out IClientFileDtoFactory clientFileDtoFactory)
    {
        var serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        var fileRpcSlCoreService = Substitute.For<IFileRpcSLCoreService>();
        serviceProvider.TryGetTransientService(out IFileRpcSLCoreService _).Returns(x =>
        {
            x[0] = fileRpcSlCoreService;
            return true;
        });
        slCoreService = fileRpcSlCoreService;

        var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(configScopeId, RootPath: rootPath));
        clientFileDtoFactory = Substitute.For<IClientFileDtoFactory>();

        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>())
            .Returns(async info => await info.Arg<Func<Task<int>>>()());

        var logger = Substitute.For<ILogger>();

        return new FileTracker(serviceProvider, activeConfigScopeTracker, threadHandling, clientFileDtoFactory, logger);
    }
}
