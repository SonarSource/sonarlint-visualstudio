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
using System.Reflection;
using System.Xml;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.CSharpVB;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.CSharpVB;

[TestClass]
public class ImportsBeforeFileGeneratorTests
{
    private static readonly string PathToDirectory = GetPathToImportBefore();
    private static readonly string PathToFile = Path.Combine(PathToDirectory, "SQVS.targets");
    private const string ResourceContent = "some content";
    private IFileSystemService fileSystem;
    private ILogger logger;
    private ImportBeforeFileGenerator testSubject;
    private IEmbeddedResourceReader embeddedResourceReader;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private MockableInitializationProcessor createdInitializationProcessor;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        fileSystem = Substitute.For<IFileSystemService>();
        embeddedResourceReader = Substitute.For<IEmbeddedResourceReader>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<ImportBeforeFileGenerator>(
            new NoOpThreadHandler(),
            logger,
            processor => createdInitializationProcessor = processor);

        testSubject = new ImportBeforeFileGenerator(logger, fileSystem, embeddedResourceReader, initializationProcessorFactory);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

        fileSystem.ClearReceivedCalls();
        fileSystem.Directory.ClearReceivedCalls();
        fileSystem.File.ClearReceivedCalls();
    }

    [TestMethod]
    public void MefCtor_CheckExports() =>
        MefTestHelpers.CheckTypeCanBeImported<ImportBeforeFileGenerator, IImportBeforeFileGenerator>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IFileSystemService>(),
            MefTestHelpers.CreateExport<IEmbeddedResourceReader>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void Mef_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ImportBeforeFileGenerator>();

    [TestMethod]
    public void Logger_LogContextIsSet() => logger.Received(1).ForContext(Strings.ImportsBeforeFileGeneratorLogContext);

    [TestMethod]
    public void FileDoesNotExist_CreatesFileWithWritesCorrectContent()
    {
        MockEmbeddedResourceReader(ResourceContent);
        MockDirectoryExists(PathToDirectory, exists: true);
        MockReadAllText(PathToFile, ResourceContent, exists: false);

        testSubject.UpdateOrCreateTargetsFile();

        fileSystem.File.Received(1).WriteAllText(PathToFile, ResourceContent);
    }

    [TestMethod]
    public void FileExists_DifferentText_CreatesFile()
    {
        MockEmbeddedResourceReader(ResourceContent);
        MockDirectoryExists(PathToDirectory, exists: true);
        MockReadAllText(PathToFile, "wrong text");

        testSubject.UpdateOrCreateTargetsFile();

        fileSystem.File.Received(1).WriteAllText(PathToFile, ResourceContent);
    }

    [TestMethod]
    public void FileExists_SameText_DoesNotCreateFile()
    {
        MockEmbeddedResourceReader(ResourceContent);
        MockDirectoryExists(PathToDirectory, exists: true);
        MockReadAllText(PathToFile, ResourceContent);

        testSubject.UpdateOrCreateTargetsFile();

        fileSystem.File.DidNotReceive().WriteAllText(PathToFile, Arg.Any<string>());
    }

    [TestMethod]
    public void FileExists_EmbeddedResourceCanNotBeRead_DoesNotUpdateFileAndLogs()
    {
        MockEmbeddedResourceReader(content: null);
        MockDirectoryExists(PathToDirectory, exists: true);
        MockReadAllText(PathToFile, ResourceContent);

        testSubject.UpdateOrCreateTargetsFile();

        fileSystem.File.DidNotReceive().WriteAllText(PathToFile, Arg.Any<string>());
        logger.Received(1).LogVerbose(Strings.ImportBeforeFileGenerator_ContentOfTargetsFileCanNotBeRead, "SQVS.targets");
    }

    [TestMethod]
    public void DirectoryDoesNotExist_CreatesDirectory()
    {
        MockEmbeddedResourceReader(ResourceContent);
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

    [TestMethod]
    public void Initialization_PassesEmptyDependencies_AndAccessesFileSystemAfterBarrier()
    {
        MockEmbeddedResourceReader(ResourceContent);
        MockDirectoryExists(PathToDirectory, exists: true);
        MockReadAllText(PathToFile, ResourceContent);
        fileSystem.Directory.ClearReceivedCalls();
        var uninitializedTestSubject = CreateUninitializedTestSubject(out var barrier);

        fileSystem.Directory.DidNotReceive().Exists(Arg.Any<string>());
        initializationProcessorFactory.Received(1).Create<ImportBeforeFileGenerator>(
            Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.Count == 0),
            Arg.Any<Func<IThreadHandling, Task>>());

        barrier.SetResult(1);
        uninitializedTestSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

        createdInitializationProcessor.Received().InitializeAsync();
        fileSystem.Directory.Received(1).Exists(PathToDirectory);
    }

    private ImportBeforeFileGenerator CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<ImportBeforeFileGenerator>(
            new NoOpThreadHandler(),
            logger,
            processor =>
            {
                MockableInitializationProcessor.ConfigureWithWait(processor, tcs);
                createdInitializationProcessor = processor;
            });
        return new ImportBeforeFileGenerator(logger, fileSystem, embeddedResourceReader, initializationProcessorFactory);
    }

    private static string GetPathToImportBefore()
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        var pathToImportsBefore = Path.Combine(localAppData, "Microsoft", "MSBuild", "Current", "Microsoft.Common.targets", "ImportBefore");

        return pathToImportsBefore;
    }

    private static string GetTargetFileContent()
    {
        var resourcePath = "SonarLint.VisualStudio.Integration.CSharpVB.SQVS.targets.xml";
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

    private void MockEmbeddedResourceReader(string content) => embeddedResourceReader.Read(Arg.Any<Assembly>(), Arg.Any<string>()).Returns(content);
}
