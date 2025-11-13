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

using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject;

[TestClass]
public class VcxCompilationDatabaseStorageTests
{
    private const string CompileCommand = "compile";
    private const string SourceDirectory = @"C:\a\b\c";
    private const string SourceFileName = "source.cpp";

    private const string defaultDatabaseDirectory = @"C:\dir";
    private static readonly IEnumerable<string> EnvValue = ["envincludevalue"];
    private static readonly string SourceFilePath = Path.Combine(SourceDirectory, SourceFileName);
    private static readonly string defaultDatabaseFilePath = Path.Combine(defaultDatabaseDirectory, "database.json");
    private IFileSystemService fileSystemService;
    private TestLogger testLogger;
    private VcxCompilationDatabaseStorage testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        fileSystemService = Substitute.For<IFileSystemService>();
        testLogger = new TestLogger();
        testSubject = new VcxCompilationDatabaseStorage(fileSystemService, testLogger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<VcxCompilationDatabaseStorage, IVcxCompilationDatabaseStorage>(
            MefTestHelpers.CreateExport<IFileSystemService>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<VcxCompilationDatabaseStorage>();

    [TestMethod]
    public void CreateDatabase_NonCriticalException_ReturnsNull()
    {
        fileSystemService.Directory.CreateDirectory(default).ThrowsForAnyArgs<NotImplementedException>();

        var database = testSubject.CreateDatabase();

        database.Should().BeNull();
        testLogger.AssertPartialOutputStrings(nameof(NotImplementedException));
    }

    [TestMethod]
    public void CreateDatabase_CriticalException_Throws()
    {
        fileSystemService.Directory.CreateDirectory(default).ThrowsForAnyArgs<DivideByZeroException>();

        var act = () => testSubject.CreateDatabase();

        act.Should().Throw<DivideByZeroException>();
    }

    [TestMethod]
    public void CreateDatabase_FileOfEmptyDatabaseWritten_ReturnsPathToDatabase()
    {
        var expectedDirectory = Path.Combine(Path.GetTempPath(), "SLVS", "VCXCD", PathHelper.PerVsInstanceFolderName.ToString());

        var databasePath = testSubject.CreateDatabase();

        VerifyDatabaseUpdated(databasePath, expectedDirectory, []);
        fileSystemService.Directory.Received().CreateDirectory(expectedDirectory);
    }

    [TestMethod]
    public void CreateDatabase_MultipleTimes_CreatesUniqueDatabaseEachTime()
    {
        var expectedDirectory = Path.Combine(Path.GetTempPath(), "SLVS", "VCXCD", PathHelper.PerVsInstanceFolderName.ToString());

        var databasePath1 = testSubject.CreateDatabase();
        var databasePath2 = testSubject.CreateDatabase();

        databasePath1.Should().NotBe(databasePath2);
        fileSystemService.Directory.Received(2).CreateDirectory(expectedDirectory);
        fileSystemService.File.ReceivedWithAnyArgs(2).WriteAllText(default, default);
        VerifyDatabaseUpdated(databasePath1, expectedDirectory, []);
        VerifyDatabaseUpdated(databasePath2, expectedDirectory, []);
    }

    [TestMethod]
    public void CreateDatabase_Disposed_Throws()
    {
        testSubject.Dispose();

        var act = () => testSubject.CreateDatabase();

        act.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void DeleteDatabase_NonCriticalException_LogsAndIgnores()
    {
        fileSystemService.File.When(x => x.Delete(Arg.Any<string>())).Do(x => throw new NotImplementedException());

        testSubject.DeleteDatabase("any");

        testLogger.AssertPartialOutputStrings(nameof(NotImplementedException));
    }

    [TestMethod]
    public void DeleteDatabase_CriticalException_Throws()
    {
        fileSystemService.File.When(x => x.Delete(Arg.Any<string>())).Do(x => throw new DivideByZeroException());

        var act = () => testSubject.DeleteDatabase("any");

        act.Should().Throw<DivideByZeroException>();
    }

    [TestMethod]
    public void DeleteDatabase_RemovesFile()
    {
        const string databasePath = "dbtodelete";

        testSubject.DeleteDatabase(databasePath);

        fileSystemService.File.Received(1).Delete(databasePath);
    }

    [TestMethod]
    public void DeleteDatabase_Disposed_Throws()
    {
        testSubject.Dispose();

        var act = () => testSubject.DeleteDatabase("any");

        act.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void UpdateDatabaseEntry_NonCriticalException_LogsAndIgnores()
    {
        fileSystemService.File.ReadAllText(default).ThrowsForAnyArgs<NotImplementedException>();

        testSubject.UpdateDatabaseEntry("any", new CompilationDatabaseEntry());

        testLogger.AssertPartialOutputStrings(nameof(NotImplementedException));
    }

    [TestMethod]
    public void UpdateDatabaseEntry_CriticalException_Throws()
    {
        fileSystemService.File.ReadAllText(default).ThrowsForAnyArgs<DivideByZeroException>();

        var act = () => testSubject.UpdateDatabaseEntry("any", new CompilationDatabaseEntry());

        act.Should().Throw<DivideByZeroException>();
    }

    [TestMethod]
    public void UpdateDatabaseEntry_EmptyDatabase_Adds()
    {
        fileSystemService.File.ReadAllText(defaultDatabaseFilePath).Returns("[]");
        var compilationDatabaseEntry = new CompilationDatabaseEntry { Command = "1", Directory = "1", File = "1", Environment = ["1"] };

        testSubject.UpdateDatabaseEntry(defaultDatabaseFilePath, compilationDatabaseEntry);

        VerifyDatabaseUpdated(defaultDatabaseFilePath, defaultDatabaseDirectory, [compilationDatabaseEntry]);
    }

    [TestMethod]
    public void UpdateDatabaseEntry_DatabaseWithNonConflictingEntries_Adds()
    {
        var entry1 = new CompilationDatabaseEntry { Command = "1", Directory = "1", File = "1", Environment = ["1"] };
        var entry2 = new CompilationDatabaseEntry { Command = "2", Directory = "2", File = "2", Environment = ["2"] };
        var originalDbEntries = new[] { entry1, entry2 };
        fileSystemService.File.ReadAllText(defaultDatabaseFilePath).Returns(JsonConvert.SerializeObject(originalDbEntries));
        var entry3 = new CompilationDatabaseEntry { Command = "3", Directory = "3", File = "add", Environment = ["3"] };

        testSubject.UpdateDatabaseEntry(defaultDatabaseFilePath, entry3);

        VerifyDatabaseUpdated(defaultDatabaseFilePath, defaultDatabaseDirectory, [entry1, entry2, entry3]);
    }

    [TestMethod]
    public void UpdateDatabaseEntry_DatabaseWithConflictingEntry_Replaces()
    {
        var entry1 = new CompilationDatabaseEntry { Command = "1", Directory = "1", File = "1", Environment = ["1"] };
        var entry2Old = new CompilationDatabaseEntry { Command = "2", Directory = "2", File = "replace", Environment = ["2"] };
        var entry3 = new CompilationDatabaseEntry { Command = "3", Directory = "3", File = "3", Environment = ["3"] };
        var originalDbEntries = new[] { entry1, entry2Old, entry3 };
        fileSystemService.File.ReadAllText(defaultDatabaseFilePath).Returns(JsonConvert.SerializeObject(originalDbEntries));
        var entry2New = new CompilationDatabaseEntry { Command = "22", Directory = "22", File = "replace", Environment = ["22"] };

        testSubject.UpdateDatabaseEntry(defaultDatabaseFilePath, entry2New);

        VerifyDatabaseUpdated(defaultDatabaseFilePath, defaultDatabaseDirectory, [entry1, entry2New, entry3]);
    }

    [TestMethod]
    public void UpdateDatabaseEntry_Disposed_Throws()
    {
        testSubject.Dispose();

        var act = () => testSubject.UpdateDatabaseEntry("any", new CompilationDatabaseEntry());

        act.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void RemoveDatabaseEntry_NonCriticalException_LogsAndIgnores()
    {
        fileSystemService.File.ReadAllText(default).ThrowsForAnyArgs<NotImplementedException>();

        testSubject.RemoveDatabaseEntry("any", "any");

        testLogger.AssertPartialOutputStrings(nameof(NotImplementedException));
    }

    [TestMethod]
    public void RemoveDatabaseEntry_CriticalException_Throws()
    {
        fileSystemService.File.ReadAllText(default).ThrowsForAnyArgs<DivideByZeroException>();

        var act = () => testSubject.RemoveDatabaseEntry("any", "any");

        act.Should().Throw<DivideByZeroException>();
    }

    [TestMethod]
    public void RemoveDatabaseEntry_EmptyDatabase_DoesNothing()
    {
        fileSystemService.File.ReadAllText(defaultDatabaseFilePath).Returns("[]");

        testSubject.RemoveDatabaseEntry(defaultDatabaseFilePath, "remove");

        fileSystemService.File.DidNotReceiveWithAnyArgs().WriteAllText(default, default);
    }

    [TestMethod]
    public void RemoveDatabaseEntry_DatabaseWithNonConflictingEntries_DoesNothing()
    {
        var entry1 = new CompilationDatabaseEntry { Command = "1", Directory = "1", File = "1", Environment = ["1"] };
        var entry2 = new CompilationDatabaseEntry { Command = "2", Directory = "2", File = "2", Environment = ["2"] };
        var originalDbEntries = new[] { entry1, entry2 };
        fileSystemService.File.ReadAllText(defaultDatabaseFilePath).Returns(JsonConvert.SerializeObject(originalDbEntries));

        testSubject.RemoveDatabaseEntry(defaultDatabaseFilePath, "remove");

        fileSystemService.File.DidNotReceiveWithAnyArgs().WriteAllText(default, default);
    }

    [TestMethod]
    public void RemoveDatabaseEntry_DatabaseWithExistingEntry_Removes()
    {
        var entry1 = new CompilationDatabaseEntry { Command = "1", Directory = "1", File = "1", Environment = ["1"] };
        var entry2 = new CompilationDatabaseEntry { Command = "2", Directory = "2", File = "remove", Environment = ["2"] };
        var entry3 = new CompilationDatabaseEntry { Command = "3", Directory = "3", File = "3", Environment = ["3"] };
        var originalDbEntries = new[] { entry1, entry2, entry3 };
        fileSystemService.File.ReadAllText(defaultDatabaseFilePath).Returns(JsonConvert.SerializeObject(originalDbEntries));

        testSubject.RemoveDatabaseEntry(defaultDatabaseFilePath, "remove");

        VerifyDatabaseUpdated(defaultDatabaseFilePath, defaultDatabaseDirectory, [entry1, entry3]);
    }

    [TestMethod]
    public void RemoveDatabaseEntry_Disposed_Throws()
    {
        testSubject.Dispose();

        var act = () => testSubject.RemoveDatabaseEntry("any", "any");

        act.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void Dispose_RemovesDirectory()
    {
        var expectedDirectory = Path.Combine(Path.GetTempPath(), "SLVS", "VCXCD", PathHelper.PerVsInstanceFolderName.ToString());

        testSubject.Dispose();

        fileSystemService.Directory.Received().Delete(expectedDirectory, true);
        testLogger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void Dispose_MultipleTimes_ActsOnlyOnce()
    {
        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        fileSystemService.Directory.ReceivedWithAnyArgs(1).Delete(default, default);
    }

    [TestMethod]
    public void Dispose_CatchesAndLogsException()
    {
        var exception = new Exception("testexc");
        fileSystemService.Directory.When(x => x.Delete(Arg.Any<string>(), Arg.Any<bool>())).Throw(exception);

        var act = () => testSubject.Dispose();

        act.Should().NotThrow();
        testLogger.AssertPartialOutputStringExists(exception.ToString());
    }

    private void VerifyDatabaseUpdated(string databasePath, string expectedDirectory, List<CompilationDatabaseEntry> expectedEntries)
    {
        databasePath.Should().NotBeNullOrWhiteSpace();
        expectedDirectory.Should().NotBeNullOrWhiteSpace();
        expectedEntries.Should().NotBeNull();
        Directory.GetParent(databasePath)!.FullName.Should().BeEquivalentTo(expectedDirectory);
        Path.GetExtension(databasePath).Should().Be(".json");
        fileSystemService.File.Received(1).WriteAllText(databasePath, Arg.Any<string>());
        JsonConvert.DeserializeObject<List<CompilationDatabaseEntry>>(GetSerializedDbByPath(databasePath)).Should().BeEquivalentTo(expectedEntries);
    }

    private string GetSerializedDbByPath(string databasePath) =>
        (string)fileSystemService.File.ReceivedCalls().First(x => x.GetMethodInfo().Name == nameof(IFile.WriteAllText) && (string)x.GetArguments()[0] == databasePath).GetArguments()[1];
}
