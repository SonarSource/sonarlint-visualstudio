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
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class SolutionBindingFileLoaderTests
{
    private const string MockFilePath = "c:\\test.txt";
    private const string MockDirectory = "c:\\";
    private BindingJsonModel bindingJsonModel;
    private IFileSystem fileSystem;
    private ILogger logger;
    private string serializedProject;
    private SolutionBindingFileLoader testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        fileSystem = Substitute.For<IFileSystem>();

        testSubject = new SolutionBindingFileLoader(logger, fileSystem);

        fileSystem.Directory.Exists(MockDirectory).Returns(true);

        bindingJsonModel = new BindingJsonModel
        {
            ServerUri = new Uri("http://xxx.www.zzz/yyy:9000"),
            Organization = null,
            ProjectKey = "MyProject Key",
            ProjectName = "projectName",
            ServerConnectionId = null,
        };

        serializedProject = @"{
  ""ServerUri"": ""http://xxx.www.zzz/yyy:9000"",
  ""ProjectKey"": ""MyProject Key"",
  ""ProjectName"": ""projectName""
}";
    }

    [TestMethod]
    public void Ctor_NullLogger_Exception()
    {
        Action act = () => new SolutionBindingFileLoader(null, null);

        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    [TestMethod]
    public void Ctor_NullFileSystem_Exception()
    {
        Action act = () => new SolutionBindingFileLoader(logger, null);

        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
    }

    [TestMethod]
    public void Save_DirectoryDoesNotExist_DirectoryIsCreated()
    {
        fileSystem.Directory.Exists(MockDirectory).Returns(false);

        testSubject.Save(MockFilePath, bindingJsonModel);

        fileSystem.Directory.Received(1).CreateDirectory(MockDirectory);
    }

    [TestMethod]
    public void Save_DirectoryExists_DirectoryNotCreated()
    {
        fileSystem.Directory.Exists(MockDirectory).Returns(true);

        testSubject.Save(MockFilePath, bindingJsonModel);

        fileSystem.Directory.DidNotReceive().CreateDirectory(Arg.Any<string>());
    }

    [TestMethod]
    public void Save_ReturnsTrue()
    {
        var actual = testSubject.Save(MockFilePath, bindingJsonModel);
        actual.Should().BeTrue();
    }

    [TestMethod]
    public void Save_FileSerializedAndWritten()
    {
        testSubject.Save(MockFilePath, bindingJsonModel);

        fileSystem.File.Received(1).WriteAllText(MockFilePath, serializedProject);
    }

    [TestMethod]
    public void Save_NonCriticalException_False()
    {
        fileSystem.File.When(x => x.WriteAllText(MockFilePath, Arg.Any<string>())).Throw<PathTooLongException>();

        var actual = testSubject.Save(MockFilePath, bindingJsonModel);
        actual.Should().BeFalse();
    }

    [TestMethod]
    public void Save_CriticalException_Exception()
    {
        fileSystem.File.When(x => x.WriteAllText(MockFilePath, Arg.Any<string>())).Throw<StackOverflowException>();

        Action act = () => testSubject.Save(MockFilePath, bindingJsonModel);

        act.Should().ThrowExactly<StackOverflowException>();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    public void Load_FilePathIsNull_Null(string filePath)
    {
        var actual = testSubject.Load(filePath);
        actual.Should().Be(null);
    }

    [TestMethod]
    public void Load_FileDoesNotExist_Null()
    {
        MockFileNotExists(MockFilePath);

        var actual = testSubject.Load(MockFilePath);
        actual.Should().Be(null);
    }

    [TestMethod]
    public void Load_InvalidJson_Null()
    {
        MockFileExists(MockFilePath);
        fileSystem.File.ReadAllText(MockFilePath).Returns("bad json");

        var actual = testSubject.Load(MockFilePath);
        actual.Should().Be(null);
    }

    [TestMethod]
    public void Load_NonCriticalException_Null()
    {
        MockFileExists(MockFilePath);
        fileSystem.File.ReadAllText(MockFilePath).Throws<PathTooLongException>();

        var actual = testSubject.Load(MockFilePath);
        actual.Should().Be(null);
    }

    [TestMethod]
    public void Load_CriticalException_Exception()
    {
        MockFileExists(MockFilePath);
        fileSystem.File.ReadAllText(MockFilePath).Throws<StackOverflowException>();

        Action act = () => testSubject.Load(MockFilePath);

        act.Should().ThrowExactly<StackOverflowException>();
    }

    [TestMethod]
    public void Load_FileExists_DeserializedProject()
    {
        MockFileExists(MockFilePath);
        fileSystem.File.ReadAllText(MockFilePath).Returns(serializedProject);

        var actual = testSubject.Load(MockFilePath);
        actual.Should().BeEquivalentTo(bindingJsonModel);
    }

    [TestMethod]
    public void DeleteBindingDirectory_ConfigFilePathNotExists_ReturnsFalseAndLogs()
    {
        MockFileNotExists(MockFilePath);

        var result = testSubject.DeleteBindingDirectory(MockFilePath);

        result.Should().BeFalse();
        fileSystem.Directory.DidNotReceive().Delete(MockDirectory, true);
        logger.Received(1).LogVerbose(PersistenceStrings.BindingDirectoryNotDeleted, MockFilePath);
    }

    [TestMethod]
    public void DeleteBindingDirectory_ConfigFilePathExists_DeletesBindingDirectoryRecursively()
    {
        MockFileExists(MockFilePath);

        var result = testSubject.DeleteBindingDirectory(MockFilePath);

        result.Should().BeTrue();
        fileSystem.Directory.Received(1).Delete(MockDirectory, true);
    }

    [TestMethod]
    public void DeleteBindingDirectory_DeletingDirectoryThrows_ReturnsFalseAndLogs()
    {
        MockFileExists(MockFilePath);
        fileSystem.Directory.When(x => x.Delete(MockDirectory, true)).Throw<UnauthorizedAccessException>();

        var result = testSubject.DeleteBindingDirectory(MockFilePath);

        result.Should().BeFalse();
        fileSystem.Directory.Received(1).Delete(MockDirectory, true);
        logger.Received(1).WriteLine(Arg.Any<string>());
    }

    private void MockFileExists(string filePath) => fileSystem.File.Exists(filePath).Returns(true);

    private void MockFileNotExists(string filePath) => fileSystem.File.Exists(filePath).Returns(false);
}
