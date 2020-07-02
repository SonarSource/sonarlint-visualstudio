/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.Integration.UnitTests.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class CFamily_CLangAnalyzerTests
    {
        [TestMethod]
        public void CallAnalyzer_Succeeds_NotifiesOfSuccess()
        {
            var statusNotifierMock = new Mock<IAnalysisStatusNotifier>();
            var dummyProcessRunner = new DummyProcessRunner(MockResponse());
            var request = new Request { File = "test.cpp" };

            GetResponse(dummyProcessRunner, request, new TestLogger(), statusNotifierMock.Object, CancellationToken.None);

            statusNotifierMock.Verify(x => x.AnalysisStarted("test.cpp"), Times.Once);
            statusNotifierMock.Verify(x => x.AnalysisFinished("test.cpp"), Times.Once);
            statusNotifierMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void CallAnalyzer_Fails_NotifiesOfFailure()
        {
            var statusNotifierMock = new Mock<IAnalysisStatusNotifier>();
            var dummyProcessRunner = new DummyProcessRunner(MockBadEndResponse());
            var request = new Request { File = "test.cpp" };

            GetResponse(dummyProcessRunner, request, new TestLogger(), statusNotifierMock.Object, CancellationToken.None);

            statusNotifierMock.Verify(x => x.AnalysisStarted("test.cpp"), Times.Once);
            statusNotifierMock.Verify(x => x.AnalysisFailed("test.cpp"), Times.Once);
            statusNotifierMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void CallAnalyzer_AnalysisIsCancelled_NotifiesOfCancellation()
        {
            var statusNotifierMock = new Mock<IAnalysisStatusNotifier>();
            var dummyProcessRunner = new DummyProcessRunner(MockResponse());
            var request = new Request { File = "test.cpp" };

            GetResponse(dummyProcessRunner, request, new TestLogger(), statusNotifierMock.Object, new CancellationToken(true));

            statusNotifierMock.Verify(x => x.AnalysisStarted("test.cpp"), Times.Once);
            statusNotifierMock.Verify(x => x.AnalysisCancelled("test.cpp"), Times.Once);
            statusNotifierMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void CallAnalyzer_Succeeds_ReturnsMessages()
        {
            // Arrange
            var dummyProcessRunner = new DummyProcessRunner(MockResponse());

            // Act
            var response = GetResponse(dummyProcessRunner, new Request(), new TestLogger());

            // Assert
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();

            response.Count().Should().Be(1);
            response[0].Filename.Should().Be("file.cpp");
        }

        [TestMethod]
        public void CallAnalyzer_Fails_ReturnsZeroMessages()
        {
            // Arrange
            var dummyProcessRunner = new DummyProcessRunner(MockEmptyResponse());

            // Act
            var response = GetResponse(dummyProcessRunner, new Request(), new TestLogger());

            // Assert
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();

            response.Should().BeEmpty();
        }

        [TestMethod]
        public void CallAnalyzer_RequestWithReproducer_ReturnsZeroMessages()
        {
            // Arrange
            var request = new Request { Flags = Request.CreateReproducer };
            var dummyProcessRunner = new DummyProcessRunner(MockBadEndResponse());
            var result = GetResponse(dummyProcessRunner, request, new TestLogger());

            // Act and Assert
            result.Should().BeEmpty();
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();
        }

        [TestMethod]
        public void CallAnalyzer_BadResponse_FailsSilentlyAndReturnsZeroMessages()
        {
            // Arrange
            var logger = new TestLogger();
            var dummyProcessRunner = new DummyProcessRunner(MockBadEndResponse());
            var result = GetResponse(dummyProcessRunner, new Request(), logger);

            // Act and Assert
            result.Should().BeEmpty();
            logger.AssertPartialOutputStrings("Failed to analyze");
            dummyProcessRunner.ExecuteCalled.Should().BeTrue();
        }

        [TestMethod]
        public void TestIsIssueForActiveRule()
        {
            var rulesConfig = new DummyCFamilyRulesConfig("any")
                .AddRule("rule1", isActive: true)
                .AddRule("rule2", isActive: false);

            // 1. Match - active
            var message = new Message("rule1", "filename", 0, 0, 0, 0, "msg", false, null);
            CLangAnalyzer.IsIssueForActiveRule(message, rulesConfig).Should().BeTrue();

            // 2. Match - not active
            message = new Message("rule2", "filename", 0, 0, 0, 0, "msg", false, null);
            CLangAnalyzer.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();

            // 3. No match - case-sensitivity
            message = new Message("RULE1", "filename", 0, 0, 0, 0, "msg", false, null);
            CLangAnalyzer.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();

            // 4. No match
            message = new Message("xxx", "filename", 0, 0, 0, 0, "msg", false, null);
            CLangAnalyzer.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();
        }

        private static List<Message> GetResponse(DummyProcessRunner dummyProcessRunner, Request request, ILogger logger)
        {
            return GetResponse(dummyProcessRunner, request, logger, Mock.Of<IAnalysisStatusNotifier>(), CancellationToken.None);
        }

        private static List<Message> GetResponse(DummyProcessRunner dummyProcessRunner, Request request, ILogger logger, IAnalysisStatusNotifier analysisStatusNotifier, CancellationToken cancellationToken)
        {
            var messages = new List<Message>();

            CFamilyHelper.CallClangAnalyzer(messages.Add, request, dummyProcessRunner, analysisStatusNotifier, logger, cancellationToken);

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

            public void Execute(ProcessRunnerArguments runnerArgs)
            {
                ExecuteCalled = true;

                runnerArgs.Should().NotBeNull();

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
