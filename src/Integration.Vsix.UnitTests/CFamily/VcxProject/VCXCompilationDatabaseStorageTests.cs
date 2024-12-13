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
    private const string EnvIncludeValue = "envincludevalue";
    private const string CustomVariableName = "customvariablename";
    private const string CustomVariableValue = "customvariablenamevalue";
    private static readonly string SourceFilePath = Path.Combine(SourceDirectory, SourceFileName);
    private IFileSystemService fileSystemService;
    private IThreadHandling threadHandling;
    private IVCXCompilationDatabaseStorage testSubject;
    private TestLogger testLogger;
    private IEnvironmentVariableProvider environmentVariableProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        fileSystemService = Substitute.For<IFileSystemService>();
        threadHandling = Substitute.For<IThreadHandling>();
        testLogger = new TestLogger();
        environmentVariableProvider = Substitute.For<IEnvironmentVariableProvider>();
        environmentVariableProvider.GetAll().Returns([(CustomVariableName, CustomVariableValue)]);
        testSubject = new VCXCompilationDatabaseStorage(fileSystemService, environmentVariableProvider, threadHandling, testLogger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        var variableProvider = Substitute.For<IEnvironmentVariableProvider>();
        variableProvider.GetAll().Returns([]);
        MefTestHelpers.CheckTypeCanBeImported<VCXCompilationDatabaseStorage, IVCXCompilationDatabaseStorage>(
            MefTestHelpers.CreateExport<IFileSystemService>(),
            MefTestHelpers.CreateExport<IEnvironmentVariableProvider>(variableProvider),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<VCXCompilationDatabaseStorage>();

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
        var fileConfig = SetUpFileConfig();

        var databaseHandle = testSubject.CreateDatabase(fileConfig);

        var temporaryCompilationDatabaseHandle = databaseHandle.Should().BeOfType<TemporaryCompilationDatabaseHandle>().Subject;
        Directory.GetParent(temporaryCompilationDatabaseHandle.FilePath).FullName.Should().BeEquivalentTo(expectedDirectory);
        Path.GetExtension(temporaryCompilationDatabaseHandle.FilePath).Should().Be(".json");
        threadHandling.Received().ThrowIfOnUIThread();
        fileSystemService.Directory.Received().CreateDirectory(expectedDirectory);
        fileSystemService.File.Received().WriteAllText(temporaryCompilationDatabaseHandle.FilePath, Arg.Any<string>());
        VerifyDatabaseContents();
    }

    [TestMethod]
    public void CreateDatabase_CreatesDifferentHandlesForSameFile()
    {
        var expectedDirectory = Path.Combine(Path.GetTempPath(), "SLVS", "VCXCD", PathHelper.PerVsInstanceFolderName.ToString());
        var fileConfig = SetUpFileConfig();

        var databaseHandle1 = testSubject.CreateDatabase(fileConfig);
        var databaseHandle2 = testSubject.CreateDatabase(fileConfig);

        Directory.GetParent(databaseHandle1.FilePath).FullName.Should().BeEquivalentTo(expectedDirectory);
        Directory.GetParent(databaseHandle2.FilePath).FullName.Should().BeEquivalentTo(expectedDirectory);
        Path.GetFileNameWithoutExtension(databaseHandle1.FilePath).Should().NotBe(Path.GetFileNameWithoutExtension(databaseHandle2.FilePath));
        environmentVariableProvider.Received(1).GetAll();
    }

    [TestMethod]
    public void CreateDatabase_Disposed_Throws()
    {
        testSubject.Dispose();

        var act = () => testSubject.CreateDatabase(Substitute.For<IFileConfig>());

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

    private static IFileConfig SetUpFileConfig()
    {
        var fileConfig = Substitute.For<IFileConfig>();
        fileConfig.CDFile.Returns(SourceFilePath);
        fileConfig.CDDirectory.Returns(SourceDirectory);
        fileConfig.CDCommand.Returns(CompileCommand);
        fileConfig.EnvInclude.Returns(EnvIncludeValue);
        return fileConfig;
    }

    private void VerifyDatabaseContents()
    {
        var serializedCompilationDatabase = fileSystemService.File.ReceivedCalls().Single().GetArguments()[1] as string;
        var compilationDatabaseEntries = JsonConvert.DeserializeObject<CompilationDatabaseEntry[]>(serializedCompilationDatabase);
        var compilationDatabaseEntry = compilationDatabaseEntries.Single();
        compilationDatabaseEntry.Should().BeEquivalentTo(new CompilationDatabaseEntry { Directory = SourceDirectory, File = SourceFilePath, Command = CompileCommand, Environment = [$"INCLUDE={EnvIncludeValue}", $"{CustomVariableName}={CustomVariableValue}"]});
    }
}
