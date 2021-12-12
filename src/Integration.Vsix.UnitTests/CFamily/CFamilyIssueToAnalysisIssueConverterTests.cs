/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
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
        private static readonly IContentType DummyContentType = Mock.Of<IContentType>();

        [TestMethod]
        public void Convert_NoMessageParts_IssueWithoutFlows()
        {
            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, new MessagePart[0]);

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            issue.Flows.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_HasMessageParts_IssueWithSingleFlowAndLocations()
        {
            var messageParts = new List<MessagePart>
            {
                new MessagePart("c:\\test1.cpp", 1, 2, 3, 4, "this is a test 1"),
                new MessagePart("c:\\test2.cpp", 5, 6, 7, 8, "this is a test 2")
            };

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, messageParts.ToArray());

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            var expectedLocations = new List<AnalysisIssueLocation>
            {
                new AnalysisIssueLocation("this is a test 1", "c:\\test1.cpp", 1, 3, 1, 3, null),
                new AnalysisIssueLocation("this is a test 2", "c:\\test2.cpp", 5, 7, 5, 7, null),
            };

            var expectedFlows = new List<AnalysisIssueFlow>
            {
                new AnalysisIssueFlow(expectedLocations)
            };

            issue.Flows.Count.Should().Be(1);
            issue.Flows.Should().BeEquivalentTo(expectedFlows, config => config.WithStrictOrdering());
        }

        [TestMethod]
        public void Convert_HasMessagePartsMakeFlow_FlowsAreReversed()
        {
            var messageParts = new List<MessagePart>
            {
                new MessagePart("c:\\test1.cpp", 1, 2, 3, 4, "this is a test 1"),
                new MessagePart("c:\\test2.cpp", 5, 6, 7, 8, "this is a test 2")
            };

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", true, messageParts.ToArray());

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            var expectedLocations = new List<AnalysisIssueLocation>
            {
                new AnalysisIssueLocation("this is a test 2", "c:\\test2.cpp", 5, 7, 5, 7, null),
                new AnalysisIssueLocation("this is a test 1", "c:\\test1.cpp", 1, 3, 1, 3, null),
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
            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

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
            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

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

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);
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
            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, new[] { messagePart });

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);
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

            var fileSystemMock = CreateFileSystemMock();
            var lineHashCalculator = new Mock<ILineHashCalculator>();
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();

            var issueHash = SetupLineHash(fileSystemMock, lineHashCalculator, textDocumentFactoryService, filePath, line);

            var message = new Message("rule2", filePath, line, 3, 2, 1, "this is a test", false, new MessagePart[0]);

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);
            var issue = Convert(testSubject, message);

            issue.LineHash.Should().Be(issueHash);
        }

        [TestMethod]
        public void Convert_HasMessageParts_LineHashCalculatedForIssueAndLocations()
        {
            var fileSystemMock = CreateFileSystemMock();
            var lineHashCalculator = new Mock<ILineHashCalculator>();
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();

            const string issueFilePath = "file1.cpp";
            const int issueLine = 10;
            var issueHash = SetupLineHash(fileSystemMock, lineHashCalculator, textDocumentFactoryService, issueFilePath, issueLine);

            const string firstLocationPath = "file2.cpp";
            const int firstLocationLine = 20;
            var firstLocationHash = SetupLineHash(fileSystemMock, lineHashCalculator, textDocumentFactoryService, firstLocationPath, firstLocationLine);

            const string secondLocationPath = "file3.cpp";
            const int secondLocationLine = 30;
            var secondLocationHash = SetupLineHash(fileSystemMock, lineHashCalculator, textDocumentFactoryService, secondLocationPath, secondLocationLine);

            var messageParts = new List<MessagePart>
            {
                new MessagePart(firstLocationPath, firstLocationLine, 2, 3, 4, "this is a test 1"),
                new MessagePart(secondLocationPath, secondLocationLine, 6, 7, 8, "this is a test 2")
            };

            var message = new Message("rule2", issueFilePath, issueLine, 3, 2, 1, "this is a test", false, messageParts.ToArray());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);
            var issue = Convert(testSubject, message);

            issue.LineHash.Should().Be(issueHash);

            var firstLocation = issue.Flows[0].Locations[0];
            var secondLocation = issue.Flows[0].Locations[1];

            secondLocation.LineHash.Should().Be(secondLocationHash);
            firstLocation.LineHash.Should().Be(firstLocationHash);
        }

        [TestMethod]
        public void Convert_HasMessageParts_LineHashCalculatedForNonFileLevelLocationsOnly()
        {
            var fileSystemMock = CreateFileSystemMock();
            var lineHashCalculator = new Mock<ILineHashCalculator>();
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();

            const string nonFileLevelLocationFilePath = "file2.cpp";
            const int nonFileLevelLocationLine = 20;
            var nonFileLevelLocationHash = SetupLineHash(fileSystemMock, lineHashCalculator, textDocumentFactoryService, nonFileLevelLocationFilePath, nonFileLevelLocationLine);

            var messageParts = new List<MessagePart>
            {
                new MessagePart(nonFileLevelLocationFilePath, nonFileLevelLocationLine, 2, 3, 4, "this is a test 1"),
                new MessagePart("file3.cpp", 1, 1, 0, 0, "this is a test 2")
            };

            var fileLevelIssue = new Message("rule2", "file1.pp", 1, 0, 0, 0, "this is a test", false, messageParts.ToArray());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);
            var issue = Convert(testSubject, fileLevelIssue);

            issue.LineHash.Should().BeNull();

            var nonFileLevelLocation = issue.Flows[0].Locations[0];
            var fileLevelLocation = issue.Flows[0].Locations[1];

            fileLevelLocation.LineHash.Should().BeNull();
            nonFileLevelLocation.LineHash.Should().Be(nonFileLevelLocationHash);
        }

        [TestMethod]
        public void Convert_FileDoesNotExist_NullLineHash()
        {
            var fileSystemMock = CreateFileSystemMock();
            var lineHashCalculator = new Mock<ILineHashCalculator>();
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();

            const string existingFilePath = "file2.cpp";
            const int line = 20;
            var expectedHash = SetupLineHash(fileSystemMock, lineHashCalculator, textDocumentFactoryService, existingFilePath, line);

            var messageParts = new List<MessagePart>
            {
                new MessagePart(existingFilePath, line, 2, 3, 4, "this is a test 1"),
                new MessagePart("non existing path", 2, 6, 7, 8, "this is a test 2")
            };

            var message = new Message("rule2", "non existing path", 3, 3, 2, 1, "this is a test", false, messageParts.ToArray());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);
            var issue = Convert(testSubject, message);
            issue.LineHash.Should().BeNull();

            var firstLocation = issue.Flows[0].Locations[0];
            var secondLocation = issue.Flows[0].Locations[1];

            secondLocation.LineHash.Should().BeNull();
            firstLocation.LineHash.Should().Be(expectedHash);

            // verify that the mock was called only for firstLocationPath
            textDocumentFactoryService.Verify(x=> x.CreateAndLoadTextDocument(existingFilePath, DummyContentType), Times.Once);
            textDocumentFactoryService.Verify(x=> x.CreateAndLoadTextDocument(It.IsAny<string>(), It.IsAny<IContentType>()), Times.Once);
        }

        [TestMethod]
        public void Convert_ExistingFile_NoTextDocument_NullLineHash()
        {
            var fileSystemMock = CreateFileSystemMock();
            var lineHashCalculator = new Mock<ILineHashCalculator>();
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();

            const string filePath = "test.cpp";
            fileSystemMock.Setup(x => x.File.Exists(filePath)).Returns(true);

            SetupDocumentLoad(textDocumentFactoryService, filePath, textDocument: null);

            var message = new Message("rule2", filePath, 3, 3, 2, 1, "this is a test", false, Array.Empty<MessagePart>());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);

            var issue = Convert(testSubject, message);
            issue.LineHash.Should().BeNull();
            lineHashCalculator.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_ExistingFile_NoTextBuffer_NullLineHash()
        {
            var fileSystemMock = CreateFileSystemMock();
            var lineHashCalculator = new Mock<ILineHashCalculator>();
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();

            const string filePath = "test.cpp";
            fileSystemMock.Setup(x => x.File.Exists(filePath)).Returns(true);

            var textDocument = CreateTextDocument(null);
            SetupDocumentLoad(textDocumentFactoryService, filePath, textDocument);

            var message = new Message("rule2", filePath, 3, 3, 2, 1, "this is a test", false, Array.Empty<MessagePart>());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);

            var issue = Convert(testSubject, message);
            issue.LineHash.Should().BeNull();
            lineHashCalculator.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_ExistingFile_NoTextSnapshot_NullLineHash()
        {
            var fileSystemMock = CreateFileSystemMock();
            var lineHashCalculator = new Mock<ILineHashCalculator>();
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();

            const string filePath = "test.cpp";
            fileSystemMock.Setup(x => x.File.Exists(filePath)).Returns(true);

            var textBuffer = CreateTextBuffer(null);
            var textDocument = CreateTextDocument(textBuffer);
            SetupDocumentLoad(textDocumentFactoryService, filePath, textDocument);

            var message = new Message("rule2", filePath, 3, 3, 2, 1, "this is a test", false, Array.Empty<MessagePart>());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);

            var issue = Convert(testSubject, message);
            issue.LineHash.Should().BeNull();
            lineHashCalculator.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_HasMessageParts_EachFileIsLoadedOnlyOnce_PerCall()
        {
            var messageParts = new List<MessagePart>
            {
                new MessagePart("file1.cpp", 10, 2, 3, 4, "this is a test 1"),
                new MessagePart("file1.cpp", 20, 6, 7, 8, "this is a test 2"),
                new MessagePart("file2.cpp", 30, 6, 7, 8, "this is a test 2")
            };

            var message = new Message("rule2", "file2.cpp", 40, 3, 2, 1, "this is a test", false, messageParts.ToArray());

            var fileSystemMock = CreateFileSystemMock(fileExists: true);
            var textDocFactory = new Mock<ITextDocumentFactoryService>();

            var testSubject = CreateTestSubject(fileSystem: fileSystemMock.Object, textDocFactory: textDocFactory.Object);

            Convert(testSubject, message);

            fileSystemMock.Verify(x => x.File.Exists("file1.cpp"), Times.Once);
            fileSystemMock.Verify(x => x.File.Exists("file2.cpp"), Times.Once);
            fileSystemMock.VerifyNoOtherCalls();

            textDocFactory.Verify(x => x.CreateAndLoadTextDocument("file1.cpp", It.IsAny<IContentType>()), Times.Once);
            textDocFactory.Verify(x => x.CreateAndLoadTextDocument("file2.cpp", It.IsAny<IContentType>()), Times.Once);
            textDocFactory.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Convert_HasMessageParts_EachFileIsLoadedOnlyOnce_AcrossMultipleCalls()
        {
            var messageParts1 = new List<MessagePart>
            {
                new MessagePart("file1.cpp", 10, 2, 3, 4, "this is a test 1"),
                new MessagePart("file2.cpp", 30, 6, 7, 8, "this is a test 2")
            };
            var message1 = new Message("rule2", "file2.cpp", 40, 3, 2, 1, "this is a test", false, messageParts1.ToArray());

            var messageParts2 = new List<MessagePart>
            {
                new MessagePart("file1.cpp", 100, 2, 3, 4, "this is a test 1"),
                new MessagePart("file2.cpp", 300, 6, 7, 8, "this is a test 2")
            };
            var message2 = new Message("rule2", "file2.cpp", 40, 3, 2, 1, "this is another test", false, messageParts2.ToArray());

            var fileSystemMock = CreateFileSystemMock(fileExists: true);
            var textDocFactory = new Mock<ITextDocumentFactoryService>();

            var testSubject = CreateTestSubject(fileSystem: fileSystemMock.Object, textDocFactory: textDocFactory.Object);

            // Convert multiple times
            Convert(testSubject, message1);
            Convert(testSubject, message2);

            fileSystemMock.Verify(x => x.File.Exists("file1.cpp"), Times.Once);
            fileSystemMock.Verify(x => x.File.Exists("file2.cpp"), Times.Once);
            fileSystemMock.VerifyNoOtherCalls();

            textDocFactory.Verify(x => x.CreateAndLoadTextDocument("file1.cpp", It.IsAny<IContentType>()), Times.Once);
            textDocFactory.Verify(x => x.CreateAndLoadTextDocument("file2.cpp", It.IsAny<IContentType>()), Times.Once);
            textDocFactory.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow("rule2", AnalysisIssueSeverity.Info, AnalysisIssueType.CodeSmell)]
        [DataRow("rule3", AnalysisIssueSeverity.Critical, AnalysisIssueType.Vulnerability)]
        public void Convert_SeverityAndTypeLookup(string ruleKey, AnalysisIssueSeverity severity, AnalysisIssueType type)
        {
            var message = new Message(ruleKey, "any", 4, 3, 2, 1, "message", false, new MessagePart[0]);
            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            issue.RuleKey.Should().Be($"lang1:{ruleKey}");
            issue.Severity.Should().Be(severity);
            issue.Type.Should().Be(type);
        }

        [TestMethod]
        [Description("Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/2149")]
        [DataRow("c:\\a.txt", "c:\\a.txt")]
        [DataRow("c:/a.txt", "c:\\a.txt")]
        [DataRow("c:/a/b/c.txt", "c:\\a\\b\\c.txt")]
        [DataRow("c:/a\\b/c.txt", "c:\\a\\b\\c.txt")]
        public void Convert_HasMessageParts_QualifiedFilePath(string originalPath, string expectedPath)
        {
            var messageParts = new List<MessagePart>
            {
                new MessagePart(originalPath, 10, 2, 3, 4, "this is a test 1"),
            };

            var message = new Message("rule2", "file2.cpp", 40, 3, 2, 1, "this is a test", false, messageParts.ToArray());

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            issue.Flows[0].Locations[0].FilePath.Should().Be(expectedPath);
        }

        [TestMethod]
        [Description("Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/2557")]
        [DataRow("", "")] // empty should not throw
        [DataRow("a.txt", "a.txt")] // not-rooted should stay the same
        [DataRow("c:\\a.txt", "c:\\a.txt")]
        [DataRow("c:/a.txt", "c:\\a.txt")]
        [DataRow("c:/a/b/c.txt", "c:\\a\\b\\c.txt")]
        [DataRow("c:/a\\b/c.txt", "c:\\a\\b\\c.txt")]
        public void Convert_FilePath_QualifiedFilePath(string originalPath, string expectedPath)
        {
            var message = new Message("rule2", originalPath, 40, 3, 2, 1, "this is a test", false, Array.Empty<MessagePart>());

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            issue.FilePath.Should().Be(expectedPath);
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

        private static string SetupLineHash(Mock<IFileSystem> fileSystemMock, 
            Mock<ILineHashCalculator> lineHashCalculatorMock, 
            Mock<ITextDocumentFactoryService> textDocumentFactoryService,
            string filePath, 
            int line)
        {
            fileSystemMock.Setup(x => x.File.Exists(filePath)).Returns(true);

            var textSnapshot = Mock.Of<ITextSnapshot>();
            var textBuffer = CreateTextBuffer(textSnapshot);
            var textDocument = CreateTextDocument(textBuffer);

            SetupDocumentLoad(textDocumentFactoryService, filePath, textDocument);

            var hash = Guid.NewGuid().ToString();

            lineHashCalculatorMock.Setup(x => x.Calculate(textSnapshot, line)).Returns(hash);

            return hash;
        }

        private static IAnalysisIssue Convert(CFamilyIssueToAnalysisIssueConverter testSubject, Message message) =>
            testSubject.Convert(message, "lang1", GetDummyRulesConfiguration());

        private static CFamilyIssueToAnalysisIssueConverter CreateTestSubject(IFileSystem fileSystem = null,
            ITextDocumentFactoryService textDocFactory = null) =>
            CreateTestSubject(Mock.Of<ILineHashCalculator>(),
                fileSystem ?? CreateFileSystemMock().Object,
                textDocFactory ?? Mock.Of<ITextDocumentFactoryService>());

        private static CFamilyIssueToAnalysisIssueConverter CreateTestSubject(
            ILineHashCalculator lineHashCalculator,
            IFileSystem fileSystem,
            ITextDocumentFactoryService textDocumentFactoryService)
        {
            var contentTypeRegistryService = CreateContentTypeRegistryService();
            var testSubject = new CFamilyIssueToAnalysisIssueConverter(textDocumentFactoryService, contentTypeRegistryService, lineHashCalculator, fileSystem);

            return testSubject;
        }

        private static Mock<IFileSystem> CreateFileSystemMock(bool fileExists = false)
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(fileExists);

            return fileSystemMock;
        }

        private static IContentTypeRegistryService CreateContentTypeRegistryService()
        {
            var contentTypeRegistryService = new Mock<IContentTypeRegistryService>();
            contentTypeRegistryService.Setup(x => x.UnknownContentType).Returns(DummyContentType);

            return contentTypeRegistryService.Object;
        }

        private static void SetupDocumentLoad(Mock<ITextDocumentFactoryService> textDocumentFactoryService, string filePath, ITextDocument textDocument)
        {
            textDocumentFactoryService
                .Setup(x => x.CreateAndLoadTextDocument(filePath, DummyContentType))
                .Returns(textDocument);
        }

        private static ITextDocument CreateTextDocument(ITextBuffer textBuffer)
        {
            var textDocument = new Mock<ITextDocument>();
            textDocument.Setup(x => x.TextBuffer).Returns(textBuffer);

            return textDocument.Object;
        }

        private static ITextBuffer CreateTextBuffer(ITextSnapshot textSnapshot)
        {
            var textBuffer = new Mock<ITextBuffer>();
            textBuffer.Setup(x => x.CurrentSnapshot).Returns(textSnapshot);

            return textBuffer.Object;
        }
    }
}
