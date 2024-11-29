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

using System.IO;
using Newtonsoft.Json;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject;

[TestClass]
public class VCXCompilationDatabaseStorageTests
{
    private const string CompileCommand = "compile";
    private const string SourceDirectory = @"C:\a\b\c";
    private const string SourceFileName = "source.cpp";
    private static readonly string SourceFilePath = Path.Combine(SourceDirectory, SourceFileName);
    private IFileSystemService fileSystemService;
    private IThreadHandling threadHandling;
    private IVCXCompilationDatabaseStorage testSubject;
    private TestLogger testLogger;

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<VCXCompilationDatabaseStorage, IVCXCompilationDatabaseStorage>(
            MefTestHelpers.CreateExport<IFileSystemService>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<VCXCompilationDatabaseStorage>();

    [TestInitialize]
    public void TestInitialize()
    {
        fileSystemService = Substitute.For<IFileSystemService>();
        threadHandling = Substitute.For<IThreadHandling>();
        testLogger = new TestLogger();
        testSubject = new VCXCompilationDatabaseStorage(fileSystemService, threadHandling, testLogger);
    }

    [TestMethod]
    public void CreateDatabase_NonCriticalException_ReturnsNull()
    {
        fileSystemService.Directory.CreateDirectory(default).ThrowsForAnyArgs<NotImplementedException>();

        var database = testSubject.CreateDatabase(Substitute.For<IFileConfig>());

        database.Should().BeNull();
        testLogger.AssertPartialOutputStrings(nameof(NotImplementedException));
    }

    [TestMethod]
    public void CreateDatabase_CriticalException_Throws()
    {
        fileSystemService.Directory.CreateDirectory(default).ThrowsForAnyArgs<DivideByZeroException>();

        var act = () => testSubject.CreateDatabase(Substitute.For<IFileConfig>());

        act.Should().Throw<DivideByZeroException>();
    }

    [TestMethod]
    public void CreateDatabase_FileWritten_ReturnsPathToDatabaseWithCorrectContent()
    {
        var expectedDirectory = Path.Combine(Path.GetTempPath(), "SLVS", "VCXCD", PathHelper.PerVsInstanceFolderName.ToString());
        var expectedPath = Path.Combine(expectedDirectory, $"{SourceFileName}.{SourceFilePath.GetHashCode()}.json");
        var fileConfig = SetUpFileConfig();

        var databasePath = testSubject.CreateDatabase(fileConfig);

        databasePath.Should().Be(expectedPath);
        threadHandling.Received().ThrowIfOnUIThread();
        fileSystemService.Directory.Received().CreateDirectory(expectedDirectory);
        fileSystemService.File.Received().WriteAllText(expectedPath, Arg.Any<string>());
        VerifyDatabaseContents();
    }

    private static IFileConfig SetUpFileConfig()
    {
        var fileConfig = Substitute.For<IFileConfig>();
        fileConfig.CDFile.Returns(SourceFilePath);
        fileConfig.CDDirectory.Returns(SourceDirectory);
        fileConfig.CDCommand.Returns(CompileCommand);
        return fileConfig;
    }

    private void VerifyDatabaseContents()
    {
        var serializedCompilationDatabase = fileSystemService.File.ReceivedCalls().Single().GetArguments()[1] as string;
        var compilationDatabaseEntries = JsonConvert.DeserializeObject<CompilationDatabaseEntry[]>(serializedCompilationDatabase);
        compilationDatabaseEntries.Should().BeEquivalentTo(new CompilationDatabaseEntry { Directory = SourceDirectory, File = SourceFilePath, Command = CompileCommand, Arguments = null });
    }
}
