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
using SonarLint.VisualStudio.Integration.LocalServices;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.File;
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices;

[TestClass]
public class FileTrackerTests
{
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
        
        testSubject.AddFiles("C:\\Users\\test\\TestProject\\AFile.cs");

        testLogger.AssertOutputStrings(Strings.FileTrackerGetRpcServiceError);
    }

    [TestMethod]
    public void AddFiles_ShouldForwardFilesToSlCore()
    {
        var testSubject = CreateTestSubject(out var fileRpcSlCoreService);
        DidUpdateFileSystemParams result = null;
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.AddFiles("C:\\Users\\test\\TestProject\\AFile.cs");

        result.removedFiles.Should().BeEmpty();
        result.addedOrChangedFiles.Should().ContainSingle();
    }

    [TestMethod]
    public void RemoveFiles_ShouldForwardFilesToSlCore()
    {
        var testSubject = CreateTestSubject(out var fileRpcSlCoreService);
        DidUpdateFileSystemParams result = null;
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.RemoveFiles("C:\\Users\\test\\TestProject\\AFile.cs");

        result.removedFiles.Should().ContainSingle();
        result.removedFiles[0].Should().BeEquivalentTo(new FileUri("C:\\Users\\test\\TestProject\\AFile.cs"));
        result.addedOrChangedFiles.Should().BeEmpty();
    }

    [TestMethod]
    public void RenameFiles_ShouldForwardFilesToSlCore()
    {
        var testSubject = CreateTestSubject(out var fileRpcSlCoreService);
        DidUpdateFileSystemParams result = null;
        fileRpcSlCoreService.DidUpdateFileSystem(Arg.Do<DidUpdateFileSystemParams>(parameters => result = parameters));

        testSubject.RenameFiles(["C:\\Users\\test\\TestProject\\AFile.cs"],
            ["C:\\Users\\test\\TestProject\\ARenamedFile.cs"]);

        result.removedFiles.Should().ContainSingle();
        result.removedFiles[0].Should().BeEquivalentTo(new FileUri("C:\\Users\\test\\TestProject\\AFile.cs"));
        result.addedOrChangedFiles.Should().ContainSingle();
    }

    private static FileTracker CreateTestSubject(out IFileRpcSLCoreService slCoreService)
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
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope("CONFIG_SCOPE_ID", RootPath: "C:\\"));
        var clientFileDtoFactory = Substitute.For<IClientFileDtoFactory>();

        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>())
            .Returns(async info => await info.Arg<Func<Task<int>>>()());

        var logger = Substitute.For<ILogger>();
        
        return new FileTracker(serviceProvider, activeConfigScopeTracker, threadHandling, clientFileDtoFactory, logger);
    }
}
