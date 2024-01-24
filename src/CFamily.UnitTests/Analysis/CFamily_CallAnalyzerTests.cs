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

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.SubProcess;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CFamily.Analysis.UnitTests
{
    [TestClass]
    public class CFamily_CLangAnalyzerTests
    {
        [TestMethod]
        public void CallAnalyzer_Succeeds_ReturnsMessages()
        {
            // Arrange
            var dummyProcessRunner = new DummyProcessRunner(MockResponse());

            // Act
            var response = GetResponse(dummyProcessRunner, CreateRequest(), new TestLogger());

            // Assert
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();

            response.Count.Should().Be(1);
            response[0].Filename.Should().Be("file.cpp");
        }

        [TestMethod]
        public void CallAnalyzer_Fails_ReturnsZeroMessages()
        {
            // Arrange
            var dummyProcessRunner = new DummyProcessRunner(MockEmptyResponse());

            // Act
            var response = GetResponse(dummyProcessRunner, CreateRequest(), new TestLogger());

            // Assert
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();

            response.Should().BeEmpty();
        }

        [TestMethod]
        public void CallAnalyzer_RequestWithEnvironmentVariables_EnvVarsArePassed()
        {
            // Arrange
            var envVars = new Dictionary<string, string> { { "aaa", "bbb" } };

            var request = CreateRequestMock();
            request.SetupGet(x => x.EnvironmentVariables).Returns(envVars);

            var dummyProcessRunner = new DummyProcessRunner(MockResponse());
            var result = GetResponse(dummyProcessRunner, request.Object, new TestLogger());

            // Act and Assert
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();
            dummyProcessRunner.SuppliedProcessRunnerArguments.EnvironmentVariables.Should().BeSameAs(envVars);
        }

        [TestMethod]
        public void CallAnalyzer_RequestWithReproducer_ReturnsZeroMessages()
        {
            // Arrange
            var request = CreateRequest(new CFamilyAnalyzerOptions { CreateReproducer = true });
            var dummyProcessRunner = new DummyProcessRunner(MockBadEndResponse());
            var result = GetResponse(dummyProcessRunner, request, new TestLogger());

            // Act and Assert
            result.Should().BeEmpty();
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();
        }

        [TestMethod]
        public void CallAnalyzer_RequestWithReproducer_DiagnosticsFileIsSaved()
        {
            // Arrange
            var logger = new TestLogger();
            var fileSystem = CreateInitializedFileSystem();
            var requestMock = CreateRequestMock(new CFamilyAnalyzerOptions { CreateReproducer = true });
            var dummyProcessRunner = new DummyProcessRunner(MockResponse());
            GetResponse(dummyProcessRunner, requestMock.Object, logger, fileSystem);

            // Act and Assert
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();

            fileSystem.AllFiles.Should().BeEquivalentTo(SubProcessFilePaths.RequestConfigFilePath);
            requestMock.Verify(x => x.WriteRequestDiagnostics(It.IsAny<StreamWriter>()), Times.Once);

            logger.AssertPartialOutputStringExists(SubProcessFilePaths.RequestConfigFilePath);
            logger.AssertPartialOutputStringExists(SubProcessFilePaths.ReproducerFilePath);
        }

        [TestMethod]
        public void CallAnalyzer_RequestWithoutReproducer_DiagnosticsFileIsNotSaved()
        {
            // Arrange
            var logger = new TestLogger();
            var fileSystem = CreateInitializedFileSystem();
            var requestMock = CreateRequestMock(new CFamilyAnalyzerOptions { CreateReproducer = false });
            var dummyProcessRunner = new DummyProcessRunner(MockResponse());
            GetResponse(dummyProcessRunner, requestMock.Object, logger, fileSystem);

            // Act and Assert
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();

            fileSystem.AllFiles.Should().BeEmpty();
            requestMock.Verify(x => x.WriteRequestDiagnostics(It.IsAny<StreamWriter>()), Times.Never);

            logger.AssertPartialOutputStringDoesNotExist(SubProcessFilePaths.RequestConfigFilePath);
            logger.AssertPartialOutputStringDoesNotExist(SubProcessFilePaths.ReproducerFilePath);
        }

        [TestMethod]
        public void CallAnalyzer_BadResponse_ThrowsException()
        {
            // Arrange
            var logger = new TestLogger();
            var dummyProcessRunner = new DummyProcessRunner(MockBadEndResponse());

            Action act = () => GetResponse(dummyProcessRunner, CreateRequest(), logger);

            // Act and Assert
            act.Should().Throw<InvalidDataException>().And.Message.Should().Be("Communication issue with the C/C++ analyzer");
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();
        }

        private static IRequest CreateRequest(CFamilyAnalyzerOptions analyzerOptions = null) =>
            CreateRequestMock(analyzerOptions).Object;

        private static Mock<IRequest> CreateRequestMock(CFamilyAnalyzerOptions analyzerOptions = null)
        {
            var context = new RequestContext(null, null, null, null, analyzerOptions, false);
            var request = new Mock<IRequest>();
            request.Setup(x => x.Context).Returns(context);
            return request;
        }

        private static MockFileSystem CreateInitializedFileSystem()
        {
            var fileSystem = new MockFileSystem();

            // Make sure the expected working directory exists
            fileSystem.Directory.CreateDirectory(SubProcessFilePaths.WorkingDirectory);
            return fileSystem;
        }

        private static List<Message> GetResponse(DummyProcessRunner dummyProcessRunner, IRequest request, ILogger logger,
            IFileSystem fileSystem = null)
        {
            return GetResponse(dummyProcessRunner, request, logger, CancellationToken.None, fileSystem ?? new FileSystem());
        }

        private static List<Message> GetResponse(DummyProcessRunner dummyProcessRunner, IRequest request, ILogger logger, CancellationToken cancellationToken,
            IFileSystem fileSystem)
        {
            var messages = new List<Message>();

            CLangAnalyzer.ExecuteSubProcess(messages.Add, request, dummyProcessRunner, logger, cancellationToken, fileSystem);

            return messages;
        }

        private class DummyProcessRunner : IProcessRunner
        {
            private readonly byte[] responseToReturn;

            public DummyProcessRunner(byte[] responseToReturn)
            {
                this.responseToReturn = responseToReturn;
            }

            public bool ExecuteCalled { get; private set; }

            public ProcessRunnerArguments SuppliedProcessRunnerArguments { get; private set; }

            public void Execute(ProcessRunnerArguments runnerArgs)
            {
                ExecuteCalled = true;

                runnerArgs.Should().NotBeNull();
                SuppliedProcessRunnerArguments = runnerArgs;

                // Expecting a single file name as input
                runnerArgs.CmdLineArgs.Count().Should().Be(1);

                using (var stream = new MemoryStream(responseToReturn))
                using (var streamReader = new StreamReader(stream))
                {
                    runnerArgs.HandleOutputStream(streamReader);
                }
            }
        }

        private byte[] MockEmptyResponse()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");

                // 0 issues

                // 0 measures
                Protocol.WriteUTF(writer, "measures");
                Protocol.WriteInt(writer, 0);

                // 0 symbols
                Protocol.WriteUTF(writer, "symbols");
                Protocol.WriteInt(writer, 0);

                Protocol.WriteUTF(writer, "END");
                return stream.ToArray();
            }
        }

        private byte[] MockResponse()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");

                // 1 issue
                Protocol.WriteUTF(writer, "message");

                Protocol.WriteUTF(writer, "ruleKey");
                Protocol.WriteUTF(writer, "file.cpp");
                Protocol.WriteInt(writer, 10);
                Protocol.WriteInt(writer, 11);
                Protocol.WriteInt(writer, 12);
                Protocol.WriteInt(writer, 13);
                Protocol.WriteInt(writer, 100);
                Protocol.WriteUTF(writer, "Issue message");
                writer.Write(true);

                // 1 flow
                Protocol.WriteInt(writer, 1);
                Protocol.WriteUTF(writer, "another.cpp");
                Protocol.WriteInt(writer, 14);
                Protocol.WriteInt(writer, 15);
                Protocol.WriteInt(writer, 16);
                Protocol.WriteInt(writer, 17);
                Protocol.WriteUTF(writer, "Flow message");

                // 0 Data Flow
                Protocol.WriteInt(writer, 0);

                // 0 fixes
                writer.Write(false);
                Protocol.WriteInt(writer, 0);

                // 1 measure
                Protocol.WriteUTF(writer, "measures");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteUTF(writer, "file.cpp");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);

                byte[] execLines = new byte[] { 1, 2, 3, 4 };
                Protocol.WriteInt(writer, execLines.Length);
                writer.Write(execLines);

                // 1 symbol
                Protocol.WriteUTF(writer, "symbols");
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);
                Protocol.WriteInt(writer, 1);

                Protocol.WriteUTF(writer, "END");
                return stream.ToArray();
            }
        }

        private byte[] MockBadEndResponse()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                Protocol.WriteUTF(writer, "OUT");
                Protocol.WriteUTF(writer, "FOO");
                return stream.ToArray();
            }
        }
    }
}
