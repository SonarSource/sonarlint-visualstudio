/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.CFamily.SubProcess;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Configuration;
using SonarLint.VisualStudio.Infrastructure.VS.Editor;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using Edit = SonarLint.VisualStudio.CFamily.SubProcess.Edit;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyIssueToAnalysisIssueConverterTests
    {
        private static readonly IContentType DummyContentType = Mock.Of<IContentType>();

        [TestMethod]
        public void Convert_NoMessageParts_IssueWithoutFlows()
        {
            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, new MessagePart[0], Array.Empty<Fix>());

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

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, messageParts.ToArray(), Array.Empty<Fix>());

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            var expectedLocations = new List<AnalysisIssueLocation>
            {
                new AnalysisIssueLocation("this is a test 1", "c:\\test1.cpp", new TextRange(1, 3, 1, 3, null)),
                new AnalysisIssueLocation("this is a test 2", "c:\\test2.cpp", new TextRange(5, 7, 5, 7, null)),
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

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", true, messageParts.ToArray(), Array.Empty<Fix>());

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            var expectedLocations = new List<AnalysisIssueLocation>
            {
                new AnalysisIssueLocation("this is a test 2", "c:\\test2.cpp", new TextRange(5, 7, 5, 7, null)),
                new AnalysisIssueLocation("this is a test 1", "c:\\test1.cpp", new TextRange(1, 3, 1, 3, null)),
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
            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, new MessagePart[0], Array.Empty<Fix>());

            // Act
            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            // Assert
            issue.PrimaryLocation.TextRange.StartLine.Should().Be(4);
            issue.PrimaryLocation.TextRange.StartLineOffset.Should().Be(3 - 1);

            issue.PrimaryLocation.TextRange.EndLine.Should().Be(2);
            issue.PrimaryLocation.TextRange.EndLineOffset.Should().Be(1 - 1);
        }

        [TestMethod]
        public void Convert_IssueEndLineIsZero_OffsetsAreIgnored()
        {
            // Special case: ignore column offsets if EndLine is zero
            var message = new Message("rule2", "ff", 101, 1, 0, 3, "test endline is zero", true, new MessagePart[0], Array.Empty<Fix>());

            // Act
            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            // Assert
            issue.PrimaryLocation.TextRange.StartLine.Should().Be(101);
            issue.PrimaryLocation.TextRange.StartLineOffset.Should().Be(0);

            issue.PrimaryLocation.TextRange.EndLine.Should().Be(0);
            issue.PrimaryLocation.TextRange.EndLineOffset.Should().Be(0);
        }

        [TestMethod]
        public void Convert_LocationEndLineIsNotZero_OffsetsAreCalculatedCorrectly()
        {
            var messagePart = new MessagePart("file", 10, 2, 30, 4, "text");
            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, new[] { messagePart }, Array.Empty<Fix>());

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);
            var convertedLocation = issue.Flows.First().Locations.First();

            convertedLocation.TextRange.StartLine.Should().Be(10);
            convertedLocation.TextRange.StartLineOffset.Should().Be(1);

            convertedLocation.TextRange.EndLine.Should().Be(30);
            convertedLocation.TextRange.EndLineOffset.Should().Be(3);
        }

        [TestMethod]
        public void Convert_LocationEndLineIsZero_OffsetsAreIgnored()
        {
            var messagePart = new MessagePart("file", 10, 2, 0, 4, "text");
            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, new[] { messagePart }, Array.Empty<Fix>());

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);
            var convertedLocation = issue.Flows.First().Locations.First();

            convertedLocation.TextRange.StartLine.Should().Be(10);
            convertedLocation.TextRange.StartLineOffset.Should().Be(0);

            convertedLocation.TextRange.EndLine.Should().Be(0);
            convertedLocation.TextRange.EndLineOffset.Should().Be(0);
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

            var message = new Message("rule2", filePath, line, 3, 2, 1, "this is a test", false, new MessagePart[0], Array.Empty<Fix>());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);
            var issue = Convert(testSubject, message);

            issue.PrimaryLocation.TextRange.LineHash.Should().Be(issueHash);
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

            var message = new Message("rule2", issueFilePath, issueLine, 3, 2, 1, "this is a test", false, messageParts.ToArray(), Array.Empty<Fix>());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);
            var issue = Convert(testSubject, message);

            issue.PrimaryLocation.TextRange.LineHash.Should().Be(issueHash);

            var firstLocation = issue.Flows[0].Locations[0];
            var secondLocation = issue.Flows[0].Locations[1];

            secondLocation.TextRange.LineHash.Should().Be(secondLocationHash);
            firstLocation.TextRange.LineHash.Should().Be(firstLocationHash);
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

            var fileLevelIssue = new Message("rule2", "file1.pp", 1, 0, 0, 0, "this is a test", false, messageParts.ToArray(), Array.Empty<Fix>());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);
            var issue = Convert(testSubject, fileLevelIssue);

            issue.PrimaryLocation.TextRange.LineHash.Should().BeNull();

            var nonFileLevelLocation = issue.Flows[0].Locations[0];
            var fileLevelLocation = issue.Flows[0].Locations[1];

            fileLevelLocation.TextRange.LineHash.Should().BeNull();
            nonFileLevelLocation.TextRange.LineHash.Should().Be(nonFileLevelLocationHash);
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

            var message = new Message("rule2", "non existing path", 3, 3, 2, 1, "this is a test", false, messageParts.ToArray(), Array.Empty<Fix>());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);
            var issue = Convert(testSubject, message);
            issue.PrimaryLocation.TextRange.LineHash.Should().BeNull();

            var firstLocation = issue.Flows[0].Locations[0];
            var secondLocation = issue.Flows[0].Locations[1];

            secondLocation.TextRange.LineHash.Should().BeNull();
            firstLocation.TextRange.LineHash.Should().Be(expectedHash);

            // verify that the mock was called only for firstLocationPath
            textDocumentFactoryService.Verify(x => x.CreateAndLoadTextDocument(existingFilePath, DummyContentType), Times.Once);
            textDocumentFactoryService.Verify(x => x.CreateAndLoadTextDocument(It.IsAny<string>(), It.IsAny<IContentType>()), Times.Once);
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

            var message = new Message("rule2", filePath, 3, 3, 2, 1, "this is a test", false, Array.Empty<MessagePart>(), Array.Empty<Fix>());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);

            var issue = Convert(testSubject, message);
            issue.PrimaryLocation.TextRange.LineHash.Should().BeNull();
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

            var message = new Message("rule2", filePath, 3, 3, 2, 1, "this is a test", false, Array.Empty<MessagePart>(), Array.Empty<Fix>());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);

            var issue = Convert(testSubject, message);
            issue.PrimaryLocation.TextRange.LineHash.Should().BeNull();
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

            var message = new Message("rule2", filePath, 3, 3, 2, 1, "this is a test", false, Array.Empty<MessagePart>(), Array.Empty<Fix>());

            var testSubject = CreateTestSubject(lineHashCalculator.Object, fileSystemMock.Object, textDocumentFactoryService.Object);

            var issue = Convert(testSubject, message);
            issue.PrimaryLocation.TextRange.LineHash.Should().BeNull();
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

            var message = new Message("rule2", "file2.cpp", 40, 3, 2, 1, "this is a test", false, messageParts.ToArray(), Array.Empty<Fix>());

            var fileSystemMock = CreateFileSystemMock(fileExists: true);
            var textDocFactory = new Mock<ITextDocumentFactoryService>();

            var testSubject = CreateTestSubject(fileSystem: fileSystemMock.Object, textDocumentFactoryService: textDocFactory.Object);

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
            var message1 = new Message("rule2", "file2.cpp", 40, 3, 2, 1, "this is a test", false, messageParts1.ToArray(), Array.Empty<Fix>());

            var messageParts2 = new List<MessagePart>
            {
                new MessagePart("FILE1.cpp", 100, 2, 3, 4, "this is a test 1"),
                new MessagePart("FILE2.cpp", 300, 6, 7, 8, "this is a test 2")
            };
            var message2 = new Message("rule2", "file2.cpp", 40, 3, 2, 1, "this is another test", false, messageParts2.ToArray(), Array.Empty<Fix>());

            var fileSystemMock = CreateFileSystemMock(fileExists: true);
            var textDocFactory = new Mock<ITextDocumentFactoryService>();

            var testSubject = CreateTestSubject(fileSystem: fileSystemMock.Object, textDocumentFactoryService: textDocFactory.Object);

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
            var message = new Message(ruleKey, "any", 4, 3, 2, 1, "message", false, new MessagePart[0], Array.Empty<Fix>());
            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            issue.RuleKey.Should().Be($"lang1:{ruleKey}");
            issue.Severity.Should().Be(severity);
            issue.Type.Should().Be(type);
        }

        [TestMethod]
        [DataRow(true, IssueType.Bug, SoftwareQualitySeverity.High)]
        [DataRow(false, IssueType.Bug, null)]
        [DataRow(true, IssueType.SecurityHotspot, null)]
        public void Convert_NewCCTEnabled_FillsSoftwareQualitySeverity(bool isCCTEnabled, IssueType type, SoftwareQualitySeverity? expectedSoftwareQualitySeverity)
        {
            var message = new Message("key", "any", 4, 3, 2, 1, "message", false, new MessagePart[0], Array.Empty<Fix>());

            var impacts = new Dictionary<SoftwareQuality, SoftwareQualitySeverity>
            {
                { SoftwareQuality.Maintainability, SoftwareQualitySeverity.High }
            };

            var ruleMetaData = CreateRuleMetaData(impacts, type);
            var rulesMetaData = new Dictionary<string, RuleMetadata>
            {
                {"key", ruleMetaData }
            };

            var CFamilyconfig = new Mock<ICFamilyRulesConfig>();
            CFamilyconfig.Setup(c => c.RulesMetadata).Returns(rulesMetaData);

            var CMConfig = new Mock<IConnectedModeFeaturesConfiguration>();
            CMConfig.Setup(c => c.IsNewCctAvailable()).Returns(isCCTEnabled);

            var testSubject = CreateTestSubject(connectedModeFeaturesConfiguration: CMConfig.Object);
            var issue = testSubject.Convert(message, "lang", CFamilyconfig.Object);

            issue.HighestSoftwareQualitySeverity.Should().Be(expectedSoftwareQualitySeverity);
        }

        [TestMethod]
        [Description("Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/2149")]
        [DataRow("", "")] // empty should not throw
        [DataRow("a.txt", "a.txt")] // not-rooted should stay the same
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

            var message = new Message("rule2", "file2.cpp", 40, 3, 2, 1, "this is a test", false, messageParts.ToArray(), Array.Empty<Fix>());

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
            var message = new Message("rule2", originalPath, 40, 3, 2, 1, "this is a test", false, Array.Empty<MessagePart>(), Array.Empty<Fix>());

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            issue.PrimaryLocation.FilePath.Should().Be(expectedPath);
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
        [DataRow(IssueType.SecurityHotspot, AnalysisIssueType.SecurityHotspot)]
        public void ConvertFromIssueType(IssueType cfamilyIssueType, AnalysisIssueType analysisIssueType)
        {
            CFamilyIssueToAnalysisIssueConverter.Convert(cfamilyIssueType).Should().Be(analysisIssueType);
        }

        [TestMethod]
        [DataRow(SoftwareQualitySeverity.High, SoftwareQualitySeverity.High)]
        [DataRow(SoftwareQualitySeverity.Medium, SoftwareQualitySeverity.Medium)]
        [DataRow(SoftwareQualitySeverity.Low, SoftwareQualitySeverity.Low)]
        [DataRow(null, null)]
        public void GetHighestSoftwareQualitySeverity(SoftwareQualitySeverity? softwareQualitySeverity, SoftwareQualitySeverity? highestSoftwareQualitySeverity)
        {
            var impacts = new Dictionary<SoftwareQuality, SoftwareQualitySeverity>();

            if (softwareQualitySeverity.HasValue)
            {
                impacts.Add(SoftwareQuality.Maintainability, softwareQualitySeverity.Value);
            }

            RuleMetadata ruleMetaData = CreateRuleMetaData(impacts);

            CFamilyIssueToAnalysisIssueConverter.GetHighestSoftwareQualitySeverity(ruleMetaData).Should().Be(highestSoftwareQualitySeverity);
        }

        [TestMethod]
        [DataRow(new SoftwareQualitySeverity[] { SoftwareQualitySeverity.Low, SoftwareQualitySeverity.Medium }, SoftwareQualitySeverity.Medium)]
        [DataRow(new SoftwareQualitySeverity[] { SoftwareQualitySeverity.Low, SoftwareQualitySeverity.High }, SoftwareQualitySeverity.High)]
        [DataRow(new SoftwareQualitySeverity[] { SoftwareQualitySeverity.Medium, SoftwareQualitySeverity.High }, SoftwareQualitySeverity.High)]
        public void GetHighestSoftwareQualitySeverity_HasTwoImpacts_GetsTheHighestOne(SoftwareQualitySeverity[] softwareQualitySeverities, SoftwareQualitySeverity? highestSoftwareQualitySeverity)
        {
            var impacts = new Dictionary<SoftwareQuality, SoftwareQualitySeverity>
            {
                { SoftwareQuality.Maintainability, softwareQualitySeverities[0] },
                { SoftwareQuality.Reliability, softwareQualitySeverities[1] }
            };

            RuleMetadata ruleMetaData = CreateRuleMetaData(impacts);

            CFamilyIssueToAnalysisIssueConverter.GetHighestSoftwareQualitySeverity(ruleMetaData).Should().Be(highestSoftwareQualitySeverity);
        }

        [TestMethod]
        public void GetHighestSoftwareQualitySeverity_HasThreeImpacts_GetsTheHighestOne()
        {
            var impacts = new Dictionary<SoftwareQuality, SoftwareQualitySeverity>
            {
                { SoftwareQuality.Maintainability, SoftwareQualitySeverity.Low },
                { SoftwareQuality.Reliability, SoftwareQualitySeverity.High },
                { SoftwareQuality.Security, SoftwareQualitySeverity.Medium }
            };

            RuleMetadata ruleMetaData = CreateRuleMetaData(impacts);

            CFamilyIssueToAnalysisIssueConverter.GetHighestSoftwareQualitySeverity(ruleMetaData).Should().Be(SoftwareQualitySeverity.High);
        }

        [TestMethod]
        public void ConvertFromIssueType_InvalidValue_Throws()

        {
            Action act = () => CFamilyIssueToAnalysisIssueConverter.Convert((IssueType)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueType");
        }

        [TestMethod]
        public void Convert_Issue_WithSingleFixSingleEdit()
        {
            var fix1 = new Fix("Fix 1", new Edit[] { new Edit(1, 2, 3, 4, "Edit 1") });

            var fixes = new Fix[] { fix1 };

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, new MessagePart[0], fixes);

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            issue.Fixes.Count.Should().Be(1);

            CompareFixes(fix1, issue.Fixes[0]);
        }

        [TestMethod]
        public void Convert_Issue_WithSingleFixMultipleEdits()
        {
            var fix1 = new Fix("Fix 1", new Edit[] { new Edit(11, 12, 13, 14, "Edit 1"), new Edit(21, 22, 23, 24, "Edit 2"), new Edit(31, 32, 33, 34, "Edit 3"), new Edit(41, 42, 43, 44, "Edit 4") });

            var fixes = new[] { fix1 };

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, new MessagePart[0], fixes);

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            issue.Fixes.Count.Should().Be(1);

            CompareFixes(fix1, issue.Fixes[0]);
        }

        [TestMethod]
        public void Convert_Issue_WithMultipleFixesMultipleEdits()
        {
            var fix1 = new Fix("Fix 1", new Edit[] { new Edit(11, 12, 13, 14, "Edit 1"), new Edit(21, 22, 23, 24, "Edit 2"), new Edit(31, 32, 33, 34, "Edit 3"), new Edit(41, 42, 43, 44, "Edit 4") });
            var fix2 = new Fix("Fix 2", new Edit[] { new Edit(51, 52, 53, 54, "Edit 5"), new Edit(61, 62, 63, 64, "Edit 6"), new Edit(71, 72, 73, 74, "Edit 7"), new Edit(81, 82, 83, 84, "Edit 8") });

            var fixes = new Fix[] { fix1, fix2 };

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, new MessagePart[0], fixes);

            var testSubject = CreateTestSubject();
            var issue = Convert(testSubject, message);

            issue.Fixes.Count.Should().Be(2);

            CompareFixes(fix1, issue.Fixes[0]);
            CompareFixes(fix2, issue.Fixes[1]);
        }

        [TestMethod]
        public void Convert_Issue_WithSingleFixNullEdit_Throws()
        {
            var fix1 = new Fix("Fix 1", null);

            var fixes = new Fix[] { fix1 };

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, new MessagePart[0], fixes);

            var testSubject = CreateTestSubject();
            Action act = () => Convert(testSubject, message);

            act.Should().ThrowExactly<ArgumentException>().And.ParamName.Should().Be("edits");
        }

        [TestMethod]
        public void Convert_Issue_WithSingleFixEmptyEdit_Throws()
        {
            var fix1 = new Fix("Fix 1", Array.Empty<Edit>());

            var fixes = new Fix[] { fix1 };

            var message = new Message("rule2", "file", 4, 3, 2, 1, "this is a test", false, new MessagePart[0], fixes);

            var testSubject = CreateTestSubject();
            Action act = () => Convert(testSubject, message);

            act.Should().ThrowExactly<ArgumentException>().And.ParamName.Should().Be("edits");
        }

        private static void CompareFixes(Fix fix, IQuickFix quickFix)
        {
            quickFix.Edits.Count.Should().Be(fix.Edits.Length, $"because number of edits were not equal in {fix.Message}");

            for (int i = 0; i < fix.Edits.Length; i++)
            {
                fix.Edits[i].StartColumn.Should().Be(quickFix.Edits[i].RangeToReplace.StartLineOffset + 1, $"because StartColumn was not equal in {fix.Message}, edit: {i} ");
                fix.Edits[i].EndColumn.Should().Be(quickFix.Edits[i].RangeToReplace.EndLineOffset + 1, $"because EndColumn was not equal in {fix.Message}, edit: {i} ");
                fix.Edits[i].StartLine.Should().Be(quickFix.Edits[i].RangeToReplace.StartLine, $"because StartLine was not equal in {fix.Message}, edit: {i} ");
                fix.Edits[i].EndLine.Should().Be(quickFix.Edits[i].RangeToReplace.EndLine, $"because EndLine was not equal in {fix.Message}, edit: {i} ");
                fix.Edits[i].Text.Should().Be(quickFix.Edits[i].NewText, $"because NewText was not equal in {fix.Message}, edit: {i} ");
            }
        }

        private static ICFamilyRulesConfig GetDummyRulesConfiguration()
        {
            var config = new Mock<ICFamilyRulesConfig>();
            config.Setup(x => x.LanguageKey).Returns("any");

            var keyToMetadataMap = new Dictionary<string, RuleMetadata>
            {
                { "rule1", new RuleMetadata { DefaultSeverity = IssueSeverity.Blocker, Type = IssueType.Bug } },
                { "rule2", new RuleMetadata { DefaultSeverity = IssueSeverity.Info, Type = IssueType.CodeSmell } },
                { "rule3", new RuleMetadata { DefaultSeverity = IssueSeverity.Critical, Type = IssueType.Vulnerability} },
            };

            config.Setup(x => x.RulesMetadata).Returns(keyToMetadataMap);

            return config.Object;
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

        private static CFamilyIssueToAnalysisIssueConverter CreateTestSubject(
            ILineHashCalculator lineHashCalculator = null,
            IFileSystem fileSystem = null,
            ITextDocumentFactoryService textDocumentFactoryService = null,
            IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration = null)
        {
            var contentTypeRegistryService = CreateContentTypeRegistryService();

            lineHashCalculator ??= Mock.Of<ILineHashCalculator>();
            fileSystem ??= CreateFileSystemMock().Object;
            textDocumentFactoryService ??= Mock.Of<ITextDocumentFactoryService>();

            if (connectedModeFeaturesConfiguration == null)
            {
                var connectedModeFeaturesConfigurationMock = new Mock<IConnectedModeFeaturesConfiguration>();
                connectedModeFeaturesConfigurationMock.Setup(c => c.IsNewCctAvailable()).Returns(false);

                connectedModeFeaturesConfiguration = connectedModeFeaturesConfigurationMock.Object;
            }

            var testSubject = new CFamilyIssueToAnalysisIssueConverter(textDocumentFactoryService, contentTypeRegistryService, connectedModeFeaturesConfiguration, lineHashCalculator, fileSystem);

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

        private static RuleMetadata CreateRuleMetaData(Dictionary<SoftwareQuality, SoftwareQualitySeverity> impacts, IssueType type = default)
        {
            var code = new Code { Impacts = impacts };
            var ruleMetaData = new RuleMetadata { Code = code, Type = type };
            return ruleMetaData;
        }
    }
}
