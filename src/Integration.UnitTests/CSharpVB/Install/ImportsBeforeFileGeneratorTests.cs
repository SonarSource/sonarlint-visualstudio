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

using System.IO;
using System.Xml;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.CSharpVB.Install;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.CSharpVB.Install;

[TestClass]
public class ImportsBeforeFileGeneratorTests
{
    private static readonly string PathToDirectory = GetPathToImportBefore();
    private static readonly string PathToFile = Path.Combine(PathToDirectory, "SonarLint.targets");
    private IFileSystemService fileSystem;
    private ILogger logger;
    private ImportBeforeFileGenerator testSubject;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        fileSystem = Substitute.For<IFileSystemService>();
        threadHandling = Substitute.For<IThreadHandling>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);

        testSubject = new ImportBeforeFileGenerator(logger, fileSystem, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckExports() =>
        MefTestHelpers.CheckTypeCanBeImported<ImportBeforeFileGenerator, IImportBeforeFileGenerator>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IFileSystemService>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void Mef_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ImportBeforeFileGenerator>();

    [TestMethod]
    public void Logger_LogContextIsSet() => logger.Received(1).ForContext(Strings.ImportsBeforeFileGeneratorLogContext);

    [TestMethod]
    public async Task UpdateOrCreateTargetsFileAsync_RunsOnBackgroundThread()
    {
        await testSubject.UpdateOrCreateTargetsFileAsync();

        threadHandling.ReceivedCalls().Should().ContainSingle(x => x.GetMethodInfo().Name == nameof(IThreadHandling.RunOnBackgroundThread));
    }

    [TestMethod]
    public void FileDoesNotExist_CreatesFileWithWritesCorrectContent()
    {
        var fileContent = GetTargetFileContent();
        MockDirectoryExists(PathToDirectory, exists: true);
        MockReadAllText(PathToFile, fileContent, exists: false);

        testSubject.UpdateOrCreateTargetsFile();

        fileSystem.File.Received(1).WriteAllText(PathToFile, fileContent);
    }

    [TestMethod]
    public void FileExists_DifferentText_CreatesFile()
    {
        MockDirectoryExists(PathToDirectory, exists: true);
        MockReadAllText(PathToFile, "wrong text");

        testSubject.UpdateOrCreateTargetsFile();

        fileSystem.File.Received(1).WriteAllText(PathToFile, Arg.Any<string>());
    }

    [TestMethod]
    public void FileExists_SameText_DoesNotCreateFile()
    {
        MockDirectoryExists(PathToDirectory, exists: true);
        MockReadAllText(PathToFile, GetTargetFileContent());

        testSubject.UpdateOrCreateTargetsFile();

        fileSystem.File.DidNotReceive().WriteAllText(PathToFile, Arg.Any<string>());
    }

    [TestMethod]
    public void DirectoryDoesNotExist_CreatesDirectory()
    {
        MockDirectoryExists(PathToDirectory, exists: false);
        MockFileExists(PathToFile, exists: false);

        testSubject.UpdateOrCreateTargetsFile();

        fileSystem.Directory.Received(1).CreateDirectory(PathToDirectory);
    }

    [TestMethod]
    public void ThrowsNonCriticalException_Catches()
    {
        fileSystem.Directory.Exists(Arg.Any<string>()).Throws(new NotImplementedException("this is a test"));

        testSubject.UpdateOrCreateTargetsFile();

        logger.Received(1).WriteLine(Arg.Is<string>(x => x.Contains(Strings.ImportBeforeFileGenerator_FailedToWriteFile)), "this is a test");
    }

    [TestMethod]
    public void ThrowsCriticalException_ThrowsException()
    {
        fileSystem.Directory.Exists(Arg.Any<string>()).Throws(new StackOverflowException());

        var act = () => testSubject.UpdateOrCreateTargetsFile();

        act.Should().Throw<StackOverflowException>();
    }

    [TestMethod]
    public void ConvertResourceToXml_DoesNotThrow()
    {
        var fileContent = GetTargetFileContent();

        var xmlDoc = new XmlDocument();
        var act = () => xmlDoc.LoadXml(fileContent);

        act.Should().NotThrow<XmlException>();
    }

    private static string GetPathToImportBefore()
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        var pathToImportsBefore = Path.Combine(localAppData, "Microsoft", "MSBuild", "Current", "Microsoft.Common.targets", "ImportBefore");

        return pathToImportsBefore;
    }

    private static string GetTargetFileContent()
    {
        var resourcePath = "SonarLint.VisualStudio.Integration.CSharpVB.Install.SonarLintTargets.xml";
        using var stream = new StreamReader(typeof(ImportBeforeFileGenerator).Assembly.GetManifestResourceStream(resourcePath));

        return stream.ReadToEnd();
    }

    private void MockDirectoryExists(string path, bool exists) => fileSystem.Directory.Exists(path).Returns(exists);

    private void MockFileExists(string path, bool exists) => fileSystem.File.Exists(path).Returns(exists);

    private void MockReadAllText(string path, string content, bool exists = true)
    {
        MockFileExists(path, exists);
        fileSystem.File.ReadAllText(path).Returns(content);
    }
}
