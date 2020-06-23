///*
// * SonarLint for Visual Studio
// * Copyright (C) 2016-2020 SonarSource SA
// * mailto:info AT sonarsource DOT com
// *
// * This program is free software; you can redistribute it and/or
// * modify it under the terms of the GNU Lesser General Public
// * License as published by the Free Software Foundation; either
// * version 3 of the License, or (at your option) any later version.
// *
// * This program is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// * Lesser General Public License for more details.
// *
// * You should have received a copy of the GNU Lesser General Public License
// * along with this program; if not, write to the Free Software Foundation,
// * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// */

//using System.IO;
//using System.Linq;
//using System.Threading;
//using FluentAssertions;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using SonarLint.VisualStudio.Integration.UnitTests;
//using SonarLint.VisualStudio.Integration.UnitTests.CFamily;

//namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
//{
//    [TestClass]
//    public class CFamily_CLangAnalyzerTests
//    {
//        [TestMethod]
//        public void CallAnalyzer_Succeeds()
//        {
//            // Arrange
//            var dummyProcessRunner = new DummyProcessRunner(MockResponse(), true);

//            // Act
//            var response = CFamilyHelper.CallClangAnalyzer(new Request(), dummyProcessRunner, new TestLogger(), CancellationToken.None);

//            // Assert
//            dummyProcessRunner.ExecuteCalled.Should().BeTrue();
//            File.Exists(dummyProcessRunner.ExchangeFileName).Should().BeFalse();

//            response.Should().NotBeNull();
//            response.Messages.Count().Should().Be(1);
//            response.Messages[0].Filename.Should().Be("file.cpp");
//        }

//        [TestMethod]
//        public void CallAnalyzer_Fails()
//        {
//            // Arrange
//            var dummyProcessRunner = new DummyProcessRunner(MockEmptyResponse(), false);

//            // Act
//            var response = CFamilyHelper.CallClangAnalyzer(new Request(), dummyProcessRunner, new TestLogger(), CancellationToken.None);

//            // Assert
//            dummyProcessRunner.ExecuteCalled.Should().BeTrue();
//            File.Exists(dummyProcessRunner.ExchangeFileName).Should().BeFalse();

//            response.Should().BeNull();
//        }

//        [TestMethod]
//        public void CallAnalyzer_RequestWithReproducer_ReturnsNull()
//        {
//            // Arrange
//            var request = new Request {Flags = Request.CreateReproducer};
//            var dummyProcessRunner = new DummyProcessRunner(MockBadEndResponse(), true);
//            var result = CFamilyHelper.CallClangAnalyzer(request, dummyProcessRunner, new TestLogger(), CancellationToken.None);

//            // Act and Assert
//            result.Should().BeNull();
//            dummyProcessRunner.ExecuteCalled.Should().BeTrue();
//            File.Exists(dummyProcessRunner.ExchangeFileName).Should().BeFalse();
//        }

//        [TestMethod]
//        public void CallAnalyzer_BadResponse_FailsSilentlyAndReturnsNull()
//        {
//            // Arrange
//            var logger = new TestLogger();
//            var dummyProcessRunner = new DummyProcessRunner(MockBadEndResponse(), true);
//            var result = CFamilyHelper.CallClangAnalyzer(new Request(), dummyProcessRunner, logger, CancellationToken.None);

//            // Act and Assert
//            result.Should().BeNull();
//            logger.AssertPartialOutputStrings("Failed to execute analysis");
//            dummyProcessRunner.ExecuteCalled.Should().BeTrue();
//            File.Exists(dummyProcessRunner.ExchangeFileName).Should().BeFalse();
//        }

//        [TestMethod]
//        public void TestIsIssueForActiveRule()
//        {
//            var rulesConfig = new DummyCFamilyRulesConfig("any")
//                .AddRule("rule1", isActive: true)
//                .AddRule("rule2", isActive: false);

//            // 1. Match - active
//            var message = new Message("rule1", "filename", 0, 0, 0, 0, "msg", false, null);
//            CLangAnalyzer.IsIssueForActiveRule(message, rulesConfig).Should().BeTrue();

//            // 2. Match - not active
//            message = new Message("rule2", "filename", 0, 0, 0, 0, "msg", false, null);
//            CLangAnalyzer.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();

//            // 3. No match - case-sensitivity
//            message = new Message("RULE1", "filename", 0, 0, 0, 0, "msg", false, null);
//            CLangAnalyzer.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();

//            // 4. No match
//            message = new Message("xxx", "filename", 0, 0, 0, 0, "msg", false, null);
//            CLangAnalyzer.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();
//        }

//        private class DummyProcessRunner : IProcessRunner
//        {
//            private readonly byte[] responseToReturn;
//            private readonly bool successCodeToReturn;

//            public DummyProcessRunner(byte[] responseToReturn, bool successCodeToReturn)
//            {
//                this.responseToReturn = responseToReturn;
//                this.successCodeToReturn = successCodeToReturn;
//            }

//            public bool ExecuteCalled { get; private set; }
//            public string ExchangeFileName { get; private set; }

//            public bool Execute(ProcessRunnerArguments runnerArgs)
//            {
//                ExecuteCalled = true;

//                runnerArgs.Should().NotBeNull();

//                // Expecting a single file name as input
//                runnerArgs.CmdLineArgs.Count().Should().Be(1);
//                ExchangeFileName = runnerArgs.CmdLineArgs.First();
//                File.Exists(ExchangeFileName).Should().BeTrue();

//                // Replace the file with the response
//                File.Delete(ExchangeFileName);

//                WriteResponse(ExchangeFileName, responseToReturn);

//                return successCodeToReturn;
//            }

//            private static void WriteResponse(string fileName, byte[] data)
//            {
//                using (var stream = new FileStream(fileName, FileMode.CreateNew))
//                {
//                    stream.Write(data, 0, data.Length);
//                }
//            }
//        }

//        private byte[] MockEmptyResponse()
//        {
//            using (MemoryStream stream = new MemoryStream())
//            {
//                BinaryWriter writer = new BinaryWriter(stream);
//                Protocol.WriteUTF(writer, "OUT");

//                // 0 issues

//                // 0 measures
//                Protocol.WriteUTF(writer, "measures");
//                Protocol.WriteInt(writer, 0);

//                // 0 symbols
//                Protocol.WriteUTF(writer, "symbols");
//                Protocol.WriteInt(writer, 0);

//                Protocol.WriteUTF(writer, "END");
//                return stream.ToArray();
//            }
//        }

//        private byte[] MockResponse()
//        {
//            using (MemoryStream stream = new MemoryStream())
//            {
//                BinaryWriter writer = new BinaryWriter(stream);
//                Protocol.WriteUTF(writer, "OUT");

//                // 1 issue
//                Protocol.WriteUTF(writer, "message");

//                Protocol.WriteUTF(writer, "ruleKey");
//                Protocol.WriteUTF(writer, "file.cpp");
//                Protocol.WriteInt(writer, 10);
//                Protocol.WriteInt(writer, 11);
//                Protocol.WriteInt(writer, 12);
//                Protocol.WriteInt(writer, 13);
//                Protocol.WriteInt(writer, 100);
//                Protocol.WriteUTF(writer, "Issue message");
//                writer.Write(true);

//                // 1 flow
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteUTF(writer, "another.cpp");
//                Protocol.WriteInt(writer, 14);
//                Protocol.WriteInt(writer, 15);
//                Protocol.WriteInt(writer, 16);
//                Protocol.WriteInt(writer, 17);
//                Protocol.WriteUTF(writer, "Flow message");

//                // 1 measure
//                Protocol.WriteUTF(writer, "measures");
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteUTF(writer, "file.cpp");
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteInt(writer, 1);

//                byte[] execLines = new byte[] { 1, 2, 3, 4 };
//                Protocol.WriteInt(writer, execLines.Length);
//                writer.Write(execLines);

//                // 1 symbol
//                Protocol.WriteUTF(writer, "symbols");
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteInt(writer, 1);
//                Protocol.WriteInt(writer, 1);

//                Protocol.WriteUTF(writer, "END");
//                return stream.ToArray();
//            }
//        }

//        private byte[] MockBadEndResponse()
//        {
//            using (MemoryStream stream = new MemoryStream())
//            {
//                BinaryWriter writer = new BinaryWriter(stream);
//                Protocol.WriteUTF(writer, "OUT");
//                Protocol.WriteUTF(writer, "FOO");
//                return stream.ToArray();
//            }
//        }
//    }
//}
