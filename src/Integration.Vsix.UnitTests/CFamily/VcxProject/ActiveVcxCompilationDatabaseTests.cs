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

using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject;

[TestClass]
public class ActiveVcxCompilationDatabaseTests
{
    private const string DatabasePath = "some path";
    private const string EntryFilePath = "some file path";
    private readonly CompilationDatabaseEntry entry = new() { File = EntryFilePath };
    private IVcxCompilationDatabaseStorage storage;
    private IThreadHandling threadHandling;
    private ICompilationDatabaseEntryGenerator generator;
    private ActiveVcxCompilationDatabase testSubject;
    private IAsyncLockFactory asyncLockFactory;

    [TestInitialize]
    public void TestInitialize()
    {
        storage = Substitute.For<IVcxCompilationDatabaseStorage>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        generator = Substitute.For<ICompilationDatabaseEntryGenerator>();
        asyncLockFactory = Substitute.For<IAsyncLockFactory>();
        testSubject = new ActiveVcxCompilationDatabase(storage, threadHandling, generator, asyncLockFactory);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ActiveVcxCompilationDatabase, IActiveVcxCompilationDatabase>(
            MefTestHelpers.CreateExport<IVcxCompilationDatabaseStorage>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<IAsyncLockFactory>(),
            MefTestHelpers.CreateExport<ICompilationDatabaseEntryGenerator>());

    [TestMethod]
    public void DatabasePath_Uninitialized_ReturnsNull()
    {
        testSubject.DatabasePath.Should().BeNull();
        VerifyRequiresSynchronousBackgroundExecution(1);
    }

    [TestMethod]
    public async Task InitializeDatabase_CreatesAndSavesFilePath()
    {
        storage.CreateDatabase().Returns(DatabasePath);

        (await testSubject.InitializeDatabaseAsync()).Should().Be(DatabasePath);

        VerifyRequiresAsynchronousBackgroundExecution<string>(1);
        testSubject.DatabasePath.Should().Be(DatabasePath);
    }

    [TestMethod]
    public async Task InitializeDatabase_AlreadyInitialized_Throws()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        await Initialize();

        var act = () => testSubject.InitializeDatabaseAsync();

        act.Should().ThrowAsync<InvalidOperationException>().WithMessage(CFamilyStrings.ActiveVcxCompilationDatabase_AlreadyInitialized);
        VerifyRequiresAsynchronousBackgroundExecution<string>(1);
    }

    [TestMethod]
    public async Task DropDatabase_Uninitialized_DoesNothing()
    {
        await testSubject.DropDatabaseAsync();

        storage.DidNotReceiveWithAnyArgs().DeleteDatabase(default);
        VerifyRequiresAsynchronousBackgroundExecution<int>(1);
        testSubject.DatabasePath.Should().BeNull();
    }

    [TestMethod]
    public async Task DropDatabase_Initialized_Removes()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        await Initialize();

        await testSubject.DropDatabaseAsync();

        VerifyRequiresAsynchronousBackgroundExecution<int>(1);
        testSubject.DatabasePath.Should().BeNull();
        storage.Received(1).DeleteDatabase(DatabasePath);
    }

    [TestMethod]
    public async Task AddFile_NotInitialized_Throws()
    {
        generator.CreateOrNull(EntryFilePath).Returns(entry);

        var act = () => testSubject.AddFileAsync(EntryFilePath);

        act.Should().ThrowAsync<InvalidOperationException>().WithMessage(CFamilyStrings.ActiveVcxCompilationDatabase_NotInitialized);
        VerifyRequiresAsynchronousBackgroundExecution<int>(1);
    }

    [TestMethod]
    public async Task AddFile_Initialized_AddsFileViaStorage()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        generator.CreateOrNull(EntryFilePath).Returns(entry);
        await Initialize();

        await testSubject.AddFileAsync(EntryFilePath);

        storage.Received(1).UpdateDatabaseEntry(DatabasePath, entry);
        VerifyRequiresAsynchronousBackgroundExecution<int>(1);
    }


    [TestMethod]
    public async Task AddFile_Initialized_EntryCannotBeGenerated_DoesNothing()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        generator.CreateOrNull(EntryFilePath).ReturnsNull();
        await Initialize();

        await testSubject.AddFileAsync(EntryFilePath);

        storage.DidNotReceiveWithAnyArgs().UpdateDatabaseEntry(default, default);
        VerifyRequiresAsynchronousBackgroundExecution<int>(1);
    }

    [TestMethod]
    public async Task RemoveFile_NotInitialized_DoesNothing()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        generator.CreateOrNull(EntryFilePath).Returns(entry);

        await testSubject.RemoveFileAsync(EntryFilePath);

        storage.DidNotReceiveWithAnyArgs().RemoveDatabaseEntry(default, default);
        VerifyRequiresAsynchronousBackgroundExecution<int>(1);
    }

    [TestMethod]
    public async Task RemoveFile_Initialized_RemovesFileViaStorage()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        generator.CreateOrNull(EntryFilePath).Returns(entry);
        await Initialize();

        await testSubject.RemoveFileAsync(EntryFilePath);

        storage.Received(1).RemoveDatabaseEntry(DatabasePath, EntryFilePath);
        VerifyRequiresAsynchronousBackgroundExecution<int>(1);

    }

    private async Task Initialize()
    {
        await testSubject.InitializeDatabaseAsync();
        ClearReceivedCalls();
    }

    private void ClearReceivedCalls()
    {
        threadHandling.ClearReceivedCalls();
        asyncLockFactory.ClearReceivedCalls();
        asyncLockFactory.Create().ClearReceivedCalls();
        storage.ClearReceivedCalls();
        generator.ClearReceivedCalls();
    }

    private void VerifyRequiresSynchronousBackgroundExecution(int count)
    {
        threadHandling.Received(count).ThrowIfOnUIThread();
        asyncLockFactory.Create().Received(count).Acquire();
    }

    private void VerifyRequiresAsynchronousBackgroundExecution<T>(int count)
    {
        threadHandling.Received(count).RunOnBackgroundThread(Arg.Any<Func<Task<T>>>());
        asyncLockFactory.Create().Received(count).AcquireAsync();
    }
}
