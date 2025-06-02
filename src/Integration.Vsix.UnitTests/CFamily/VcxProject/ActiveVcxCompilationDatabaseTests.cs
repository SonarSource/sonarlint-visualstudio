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

    [TestInitialize]
    public void TestInitialize()
    {
        storage = Substitute.For<IVcxCompilationDatabaseStorage>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        generator = Substitute.For<ICompilationDatabaseEntryGenerator>();
        testSubject = new ActiveVcxCompilationDatabase(storage, threadHandling, generator);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ActiveVcxCompilationDatabase, IActiveVcxCompilationDatabase>(
            MefTestHelpers.CreateExport<IVcxCompilationDatabaseStorage>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ICompilationDatabaseEntryGenerator>());

    [TestMethod]
    public void DatabasePath_Uninitialized_ReturnsNull()
    {
        testSubject.DatabasePath.Should().BeNull();
        VerifyThrowIfOnUIThread(1);
    }

    [TestMethod]
    public void InitializeDatabase_CreatesAndSavesFilePath()
    {
        storage.CreateDatabase().Returns(DatabasePath);

        testSubject.InitializeDatabase().Should().Be(DatabasePath);

        VerifyThrowIfOnUIThread(1);
        testSubject.DatabasePath.Should().Be(DatabasePath);
    }

    [TestMethod]
    public void InitializeDatabase_AlreadyInitialized_Throws()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        testSubject.InitializeDatabase();

        var act = () => testSubject.InitializeDatabase();

        act.Should().Throw<InvalidOperationException>().WithMessage(CFamilyStrings.ActiveVcxCompilationDatabase_AlreadyInitialized);
        VerifyThrowIfOnUIThread(2);
    }

    [TestMethod]
    public void DropDatabase_Uninitialized_DoesNothing()
    {
        testSubject.DropDatabase();

        storage.DidNotReceiveWithAnyArgs().DeleteDatabase(default);
        VerifyThrowIfOnUIThread(1);
        testSubject.DatabasePath.Should().BeNull();
    }

    [TestMethod]
    public void DropDatabase_Initialized_Removes()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        testSubject.InitializeDatabase();

        testSubject.DropDatabase();

        VerifyThrowIfOnUIThread(2);
        testSubject.DatabasePath.Should().BeNull();
        storage.Received(1).DeleteDatabase(DatabasePath);
    }

    [TestMethod]
    public void AddFile_NotInitialized_Throws()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        generator.CreateOrNull(EntryFilePath).Returns(entry);

        var act = () => testSubject.AddFile(EntryFilePath);

        act.Should().Throw<InvalidOperationException>().WithMessage(CFamilyStrings.ActiveVcxCompilationDatabase_NotInitialized);
        VerifyThrowIfOnUIThread(1);
    }

    [TestMethod]
    public void AddFile_Initialized_AddsFileViaStorage()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        generator.CreateOrNull(EntryFilePath).Returns(entry);
        testSubject.InitializeDatabase();

        testSubject.AddFile(EntryFilePath);

        storage.Received(1).UpdateDatabaseEntry(DatabasePath, entry);
        VerifyThrowIfOnUIThread(2);
    }

    [TestMethod]
    public void AddFile_Initialized_EntryCannotBeGenerated_DoesNothing()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        generator.CreateOrNull(EntryFilePath).ReturnsNull();
        testSubject.InitializeDatabase();

        testSubject.AddFile(EntryFilePath);

        storage.DidNotReceiveWithAnyArgs().UpdateDatabaseEntry(default, default);
        VerifyThrowIfOnUIThread(2);
    }

    [TestMethod]
    public void RemoveFile_NotInitialized_DoesNothing()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        generator.CreateOrNull(EntryFilePath).Returns(entry);

        testSubject.RemoveFile(EntryFilePath);

        storage.DidNotReceiveWithAnyArgs().RemoveDatabaseEntry(default, default);
        VerifyThrowIfOnUIThread(1);
    }

    [TestMethod]
    public void RemoveFile_Initialized_RemovesFileViaStorage()
    {
        storage.CreateDatabase().Returns(DatabasePath);
        generator.CreateOrNull(EntryFilePath).Returns(entry);
        testSubject.InitializeDatabase();

        testSubject.RemoveFile(EntryFilePath);

        storage.Received(1).RemoveDatabaseEntry(DatabasePath, EntryFilePath);
        VerifyThrowIfOnUIThread(2);
    }

    private void VerifyThrowIfOnUIThread(int count) => threadHandling.Received(count).ThrowIfOnUIThread();
}
