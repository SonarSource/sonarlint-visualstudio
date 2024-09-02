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

using System.IO.Abstractions;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.Persistence;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Persistence;

[TestClass]
public class JsonFileHandlerTest
{
    private JsonFileHandler testSubject;
    private ILogger logger;
    private IJsonSerializer serializer;
    private IFileSystem fileSystem;
    private record TestType(string PropName);
    private const string FilePath = "dummyPath";

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        serializer = Substitute.For<IJsonSerializer>();
        fileSystem = Substitute.For<IFileSystem>();
        testSubject = new JsonFileHandler(fileSystem, serializer, logger);
    }

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<JsonFileHandler, IJsonFileHandler>(
            MefTestHelpers.CreateExport<IJsonSerializer>(), 
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void Mef_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsNonSharedMefComponent<JsonFileHandler>();
    }

    [TestMethod]
    public void TryReadFile_FileDoesNotExist_ReturnsFalse()
    {
        fileSystem.File.Exists(FilePath).Returns(false);

        var succeeded = testSubject.TryReadFile(FilePath, out TestType deserializedContent);

        succeeded.Should().BeFalse();
        deserializedContent.Should().BeNull();
        fileSystem.File.Received(1).Exists(FilePath);
    }

    [TestMethod]
    public void TryReadFile_FileExists_ReturnsTrueAndDeserializeContent()
    {
        var expectedContent = new TestType("test");
        var serializedContent = JsonConvert.SerializeObject(expectedContent);
        fileSystem.File.Exists(FilePath).Returns(true);
        fileSystem.File.ReadAllText(FilePath).Returns(serializedContent);
        serializer.TryDeserialize(Arg.Any<string>(), out Arg.Any<TestType>()).Returns(true);

        var succeeded = testSubject.TryReadFile(FilePath, out TestType _);

        succeeded.Should().BeTrue();
        Received.InOrder(() =>
        {
            fileSystem.File.Exists(FilePath);
            fileSystem.File.ReadAllText(FilePath);
            serializer.TryDeserialize(Arg.Any<string>(), out Arg.Any<TestType>());
        });
    }

    [TestMethod]
    public void TryReadFile_ReadingFileThrowsException_WritesLogAndReturnsFalse()
    {
        var exceptionMsg = "IO failed";
        fileSystem.File.Exists(FilePath).Returns(true);
        fileSystem.File.When(x => x.ReadAllText(FilePath)).Do(x => throw new Exception(exceptionMsg));

        var succeeded = testSubject.TryReadFile(FilePath, out TestType _);

        succeeded.Should().BeFalse();
        logger.Received(1).WriteLine(exceptionMsg);
    }

    [TestMethod]
    public void TryReadFile_DeserializationThrowsException_WritesLogAndReturnsFalse()
    {
        var exceptionMsg = "deserialization failed";
        fileSystem.File.Exists(FilePath).Returns(true);
        serializer.When(x => x.TryDeserialize(Arg.Any<string>(), out Arg.Any<TestType>())).Do(x => throw new Exception(exceptionMsg));

        var succeeded = testSubject.TryReadFile(FilePath, out TestType _);

        succeeded.Should().BeFalse();
        logger.Received(1).WriteLine(exceptionMsg);
    }

    [TestMethod]
    public void TryReadFile_DeserializationFails_WritesLogAndReturnsFalse()
    {
        fileSystem.File.Exists(FilePath).Returns(true);
        serializer.TryDeserialize(Arg.Any<string>(), out Arg.Any<TestType>()).Returns(false);

        var succeeded = testSubject.TryReadFile(FilePath, out TestType _);

        succeeded.Should().BeFalse();
    }

    [TestMethod]
    public void ReadFile_ReadingFileThrowsException_TrowsException()
    {
        var exceptionMsg = "IO failed";
        fileSystem.File.When(x => x.ReadAllText(FilePath)).Do(x => throw new Exception(exceptionMsg));

        Action act  = () => testSubject.ReadFile<TestType>(FilePath);

        act.Should().Throw<Exception>().WithMessage(exceptionMsg);
    }

    [TestMethod]
    public void ReadFile_DeserializationThrowsException_TrowsException()
    {
        var exceptionMsg = "IO failed";
        serializer.When(x => x.Deserialize<TestType>(Arg.Any<string>())).Do(x => throw new Exception(exceptionMsg));

        Action act = () => testSubject.ReadFile<TestType>(FilePath);

        act.Should().Throw<Exception>().WithMessage(exceptionMsg);
    }

    [TestMethod]
    public void TryWriteToFile_FolderDoesNotExist_CreatesFolder()
    {
        fileSystem.Directory.Exists(Arg.Any<string>()).Returns(false);

        testSubject.TryWriteToFile(FilePath, new TestType("abc"));

        fileSystem.Directory.Received(1).CreateDirectory(Arg.Any<string>());
    }

    [TestMethod]
    public void TryWriteToFile_SerializationFails_ReturnsFalse()
    {
        MockTrySerialize(false);

        var succeeded = testSubject.TryWriteToFile(FilePath, new TestType("abc"));

        succeeded.Should().BeFalse();
    }

    [TestMethod]
    public void TryWriteToFile_SerializationThrowsException_ReturnsFalseAndLogs()
    {
        var exceptionMsg = "serialization failed";
        serializer.When(x => x.TrySerialize(Arg.Any<TestType>(), out Arg.Any<string>(), Formatting.Indented)).Do(x => throw new Exception(exceptionMsg));

        var succeeded = testSubject.TryWriteToFile(FilePath, new TestType("abc"));

        succeeded.Should().BeFalse();
        logger.Received(1).WriteLine(exceptionMsg);
    }

    [TestMethod]
    public void TryWriteToFile_WritingToFileThrowsException_ReturnsFalseAndLogs()
    {
        var exceptionMsg = "writing to disk failed";
        MockTrySerialize(true);
        fileSystem.File.When(x => x.WriteAllText(FilePath, Arg.Any<string>())).Do(x => throw new Exception(exceptionMsg));

        var succeeded = testSubject.TryWriteToFile(FilePath, new TestType("abc"));

        succeeded.Should().BeFalse();
        logger.Received(1).WriteLine(exceptionMsg);
    }

    [TestMethod]
    public void TryWriteToFile_WritingToFileSucceeded_ReturnsTrue()
    {
        MockTrySerialize(true);

        var succeeded = testSubject.TryWriteToFile(FilePath, new TestType("abc"));

        succeeded.Should().BeTrue();
        Received.InOrder(() =>
        {
            fileSystem.Directory.CreateDirectory(Arg.Any<string>());
            serializer.TrySerialize(Arg.Any<TestType>(), out Arg.Any<string>(), Formatting.Indented);
            fileSystem.File.WriteAllText(FilePath, Arg.Any<string>());
        });
    }

    private void MockTrySerialize(bool success)
    {
        serializer.TrySerialize(Arg.Any<TestType>(), out Arg.Any<string>(), Formatting.Indented).Returns(success);
    }
}
