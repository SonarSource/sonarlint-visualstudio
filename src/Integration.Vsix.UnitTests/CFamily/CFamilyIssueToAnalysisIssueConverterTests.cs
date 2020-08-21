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

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyIssueToAnalysisIssueConverterTests
    {
        private Mock<ILineHashCalculator> lineHashCalculatorMock;
        private Mock<IFileSystem> fileSystemMock;

        private CFamilyIssueToAnalysisIssueConverter testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            lineHashCalculatorMock = new Mock<ILineHashCalculator>();

            fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(false);

            testSubject = new CFamilyIssueToAnalysisIssueConverter(lineHashCalculatorMock.Object, fileSystemMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var lineHashCalculatorExport = MefTestHelpers.CreateExport<ILineHashCalculator>(lineHashCalculatorMock.Object);

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<CFamilyIssueToAnalysisIssueConverter, ICFamilyIssueToAnalysisIssueConverter>(null, new[] { lineHashCalculatorExport });
        }

        [TestMethod]
        public void Convert_NoMessageParts_IssueWithoutFlows()
        {
            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, new MessagePart[0]);

            var issue = Convert(message);

            issue.Flows.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_HasMessageParts_IssueWithSingleFlowAndLocationsInReverseOrder()
        {
            var messageParts = new List<MessagePart>
            {
                new MessagePart("test1.cpp", 1, 2, 3, 4, "this is a test 1"),
                new MessagePart("test2.cpp", 5, 6, 7, 8, "this is a test 2")
            };

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, messageParts.ToArray());

            var issue = Convert(message);

            var expectedLocations = new List<AnalysisIssueLocation>
            {
                new AnalysisIssueLocation("this is a test 2", "test2.cpp", 5, 7, 5, 7, null),
                new AnalysisIssueLocation("this is a test 1", "test1.cpp", 1, 3, 1, 3, null)
            };

            var expectedFlows = new List<AnalysisIssueFlow>
            {
                new AnalysisIssueFlow(expectedLocations)
            };

            issue.Flows.Count.Should().Be(1);
            issue.Flows.Should().BeEquivalentTo(expectedFlows, config => config.WithStrictOrdering());
        }

        [TestMethod]
        public void Convert_IssueEndLineIsNotZero_OffsetsAreCalculatedCorrectly()
        {
            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, new MessagePart[0]);

            // Act
            var issue = Convert(message);

            // Assert
            issue.StartLine.Should().Be(4);
            issue.StartLineOffset.Should().Be(3 - 1);

            issue.EndLine.Should().Be(2);
            issue.EndLineOffset.Should().Be(1 - 1);
        }

        [TestMethod]
        public void Convert_IssueEndLineIsZero_OffsetsAreIgnored()
        {
            // Special case: ignore column offsets if EndLine is zero
            var message = new Message("rule2", "ff", 101, 1, 0, 3, "test endline is zero", true, new MessagePart[0]);

            // Act
            var issue = Convert(message);

            // Assert
            issue.StartLine.Should().Be(101);
            issue.StartLineOffset.Should().Be(0);

            issue.EndLine.Should().Be(0);
            issue.EndLineOffset.Should().Be(0);
        }

        [TestMethod]
        public void Convert_LocationEndLineIsNotZero_OffsetsAreCalculatedCorrectly()
        {
            var messagePart = new MessagePart("file", 10, 2, 30, 4, "text");
            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, new[] { messagePart });

            var issue = Convert(message);
            var convertedLocation = issue.Flows.First().Locations.First();

            convertedLocation.StartLine.Should().Be(10);
            convertedLocation.StartLineOffset.Should().Be(1);

            convertedLocation.EndLine.Should().Be(30);
            convertedLocation.EndLineOffset.Should().Be(3);
        }

        [TestMethod]
        public void Convert_LocationEndLineIsZero_OffsetsAreIgnored()
        {
            var messagePart = new MessagePart("file", 10, 2, 0, 4, "text");
            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, new[]{ messagePart });

            var issue = Convert(message);
            var convertedLocation = issue.Flows.First().Locations.First();

            convertedLocation.StartLine.Should().Be(10);
            convertedLocation.StartLineOffset.Should().Be(0);

            convertedLocation.EndLine.Should().Be(0);
            convertedLocation.EndLineOffset.Should().Be(0);
        }

        [TestMethod]
        public void Convert_NoMessageParts_LineHashCalculatedForIssueOnly()
        {
            const string filePath = "file1.cpp";
            const int line = 10;
            var issueHash = SetupLineHash(filePath, line);

            var message = new Message("rule2", filePath, line, 3, 2, 1, "this is a test", false, new MessagePart[0]);

            var issue = Convert(message);

            issue.LineHash.Should().Be(issueHash);
        }

        [TestMethod]
        public void Convert_HasMessageParts_LineHashCalculatedForIssueAndLocations()
        {
            const string issueFilePath = "file1.cpp";
            const int issueLine = 10;
            var issueHash = SetupLineHash(issueFilePath, issueLine);

            const string firstLocationPath = "file2.cpp";
            const int firstLocationLine = 20;
            var firstLocationHash = SetupLineHash(firstLocationPath, firstLocationLine);

            const string secondLocationPath = "file3.cpp";
            const int secondLocationLine = 30;
            var secondLocationHash = SetupLineHash(secondLocationPath, secondLocationLine);

            var messageParts = new List<MessagePart>
            {
                new MessagePart(firstLocationPath, firstLocationLine, 2, 3, 4, "this is a test 1"),
                new MessagePart(secondLocationPath, secondLocationLine, 6, 7, 8, "this is a test 2")
            };

            var message = new Message("rule2", issueFilePath, issueLine, 3, 2, 1, "this is a test", false, messageParts.ToArray());

            var issue = Convert(message);

            issue.LineHash.Should().Be(issueHash);

            // converted locations are in reverse order from message parts
            var firstLocation = issue.Flows[0].Locations[1];
            var secondLocation = issue.Flows[0].Locations[0];

            secondLocation.LineHash.Should().Be(secondLocationHash);
            firstLocation.LineHash.Should().Be(firstLocationHash);
        }

        [TestMethod]
        public void Convert_HasMessageParts_LineHashCalculatedForNonFileLevelLocationsOnly()
        {
            const string nonFileLevelLocationFilePath = "file2.cpp";
            const int nonFileLevelLocationLine = 20;
            var nonFileLevelLocationHash = SetupLineHash(nonFileLevelLocationFilePath, nonFileLevelLocationLine);

            var messageParts = new List<MessagePart>
            {
                new MessagePart(nonFileLevelLocationFilePath, nonFileLevelLocationLine, 2, 3, 4, "this is a test 1"),
                new MessagePart("file3.cpp", 1, 1, 0, 0, "this is a test 2")
            };

            var fileLevelIssue = new Message("rule2", "file1.pp", 1, 0, 0, 0, "this is a test", false, messageParts.ToArray());

            var issue = Convert(fileLevelIssue);

            issue.LineHash.Should().BeNull();

            // converted locations are in reverse order from message parts
            var nonFileLevelLocation = issue.Flows[0].Locations[1];
            var fileLevelLocation = issue.Flows[0].Locations[0];

            fileLevelLocation.LineHash.Should().BeNull();
            nonFileLevelLocation.LineHash.Should().Be(nonFileLevelLocationHash);
        }

        [TestMethod]
        public void Convert_FileDoesNotExist_NullLineHash()
        {
            const string firstLocationPath = "file2.cpp";
            const int firstLocationLine = 20;
            var firstLocationHash = SetupLineHash(firstLocationPath, firstLocationLine);

            var messageParts = new List<MessagePart>
            {
                new MessagePart(firstLocationPath, firstLocationLine, 2, 3, 4, "this is a test 1"),
                new MessagePart("non existing path", 2, 6, 7, 8, "this is a test 2")
            };

            var message = new Message("rule2", "non existing path", 3, 3, 2, 1, "this is a test", false, messageParts.ToArray());

            var issue = Convert(message);
            issue.LineHash.Should().BeNull();

            // converted locations are in reverse order from message parts
            var firstLocation = issue.Flows[0].Locations[1];
            var secondLocation = issue.Flows[0].Locations[0];

            secondLocation.LineHash.Should().BeNull();
            firstLocation.LineHash.Should().Be(firstLocationHash);
        }

        [TestMethod]
        public void Convert_HasMessageParts_EachFileIsLoadedOnlyOnce()
        {
            var messageParts = new List<MessagePart>
            {
                new MessagePart("file1.cpp", 10, 2, 3, 4, "this is a test 1"),
                new MessagePart("file1.cpp", 20, 6, 7, 8, "this is a test 2"),
                new MessagePart("file2.cpp", 30, 6, 7, 8, "this is a test 2")
            };

            var message = new Message("rule2", "file2.cpp", 40, 3, 2, 1, "this is a test", false, messageParts.ToArray());

            Convert(message);

            fileSystemMock.Verify(x=> x.File.Exists("file1.cpp"), Times.Once);
            fileSystemMock.Verify(x=> x.File.Exists("file2.cpp"), Times.Once);
            fileSystemMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow("rule2", AnalysisIssueSeverity.Info, AnalysisIssueType.CodeSmell)]
        [DataRow("rule3", AnalysisIssueSeverity.Critical, AnalysisIssueType.Vulnerability)]
        public void Convert_SeverityAndTypeLookup(string ruleKey, AnalysisIssueSeverity severity, AnalysisIssueType type)
        {
            var message = new Message(ruleKey, "any", 4, 3, 2, 1, "message", false, new MessagePart[0]);
            var issue = Convert(message);

            issue.RuleKey.Should().Be($"lang1:{ruleKey}");
            issue.Severity.Should().Be(severity);
            issue.Type.Should().Be(type);
        }

        [TestMethod]
        [DataRow(IssueSeverity.Blocker, AnalysisIssueSeverity.Blocker)]
        [DataRow(IssueSeverity.Critical, AnalysisIssueSeverity.Critical)]
        [DataRow(IssueSeverity.Info, AnalysisIssueSeverity.Info)]
        [DataRow(IssueSeverity.Major, AnalysisIssueSeverity.Major)]
        [DataRow(IssueSeverity.Minor, AnalysisIssueSeverity.Minor)]
        public void ConvertFromIssueSeverity(IssueSeverity cfamilySeverity, AnalysisIssueSeverity analysisIssueSeverity)
        {
            CFamilyIssueToAnalysisIssueConverter.Convert(cfamilySeverity).Should().Be(analysisIssueSeverity);
        }

        [TestMethod]
        public void ConvertFromIssueSeverity_InvalidValue_Throws()
        {
            Action act = () => CFamilyIssueToAnalysisIssueConverter.Convert((IssueSeverity)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueSeverity");
        }

        [TestMethod]
        [DataRow(IssueType.Bug, AnalysisIssueType.Bug)]
        [DataRow(IssueType.CodeSmell, AnalysisIssueType.CodeSmell)]
        [DataRow(IssueType.Vulnerability, AnalysisIssueType.Vulnerability)]
        public void ConvertFromIssueType(IssueType cfamilyIssueType, AnalysisIssueType analysisIssueType)
        {
            CFamilyIssueToAnalysisIssueConverter.Convert(cfamilyIssueType).Should().Be(analysisIssueType);

            Action act = () => CFamilyIssueToAnalysisIssueConverter.Convert((IssueType)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueType");
        }

        [TestMethod]
        public void ConvertFromIssueType_InvalidValue_Throws()
        {
            Action act = () => CFamilyIssueToAnalysisIssueConverter.Convert((IssueType)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueType");
        }

        private static ICFamilyRulesConfig GetDummyRulesConfiguration()
        {
            var config = new DummyCFamilyRulesConfig("any")
                .AddRule("rule1", IssueSeverity.Blocker, isActive: false,
                    parameters: new Dictionary<string, string>
                        {{"rule1 Param1", "rule1 Value1"}, {"rule1 Param2", "rule1 Value2"}})
                .AddRule("rule2", IssueSeverity.Info, isActive: true,
                    parameters: new Dictionary<string, string>
                        {{"rule2 Param1", "rule2 Value1"}, {"rule2 Param2", "rule2 Value2"}})
                .AddRule("rule3", IssueSeverity.Critical, isActive: true,
                    parameters: new Dictionary<string, string>
                        {{"rule3 Param1", "rule3 Value1"}, {"rule3 Param2", "rule3 Value2"}});

            config.RulesMetadata["rule1"].Type = IssueType.Bug;
            config.RulesMetadata["rule2"].Type = IssueType.CodeSmell;
            config.RulesMetadata["rule3"].Type = IssueType.Vulnerability;

            return config;
        }

        private string SetupLineHash(string filePath, int line)
        {
            var content = Guid.NewGuid().ToString();
            var hash = Guid.NewGuid().ToString();

            fileSystemMock.Setup(x => x.File.Exists(filePath)).Returns(true);
            fileSystemMock.Setup(x => x.File.ReadAllText(filePath)).Returns(content);

            lineHashCalculatorMock.Setup(x => x.Calculate(content, line)).Returns(hash);

            return hash;
        }

        private IAnalysisIssue Convert(Message message)
        {
            return testSubject.Convert(message, "lang1", GetDummyRulesConfiguration());
        }
    }
}
