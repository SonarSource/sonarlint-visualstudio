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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;
using ITaintIssue = SonarQube.Client.Models.ServerSentEvents.ClientContract.ITaintIssue;
using ITextRange = SonarQube.Client.Models.ServerSentEvents.ClientContract.ITextRange;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint
{
    [TestClass]
    public class TaintIssueToIssueVisualizationConverterTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TaintIssueToIssueVisualizationConverter, ITaintIssueToIssueVisualizationConverter>(
                MefTestHelpers.CreateExport<IAnalysisIssueVisualizationConverter>(),
                MefTestHelpers.CreateExport<IAbsoluteFilePathLocator>());
        }

        [TestMethod]
        public void Convert_FromSonarQubeIssue_ServerIssueHasNoTextRange_ArgumentNullException()
        {
            var serverIssue = CreateServerIssue(textRange: null);

            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Convert(serverIssue);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("TextRange");
        }

        [TestMethod]
        public void Convert_FromSonarQubeIssue_FlowLocationHasNoTextRange_ArgumentNullException()
        {
            var serverLocation = CreateServerLocation(textRange: null);
            var serverFlow = CreateServerFlow(serverLocation);
            var sonarQubeIssue = CreateServerIssue(textRange: new IssueTextRange(1, 1, 1, 1), flows: serverFlow);

            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Convert(sonarQubeIssue);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("TextRange");
        }

        [TestMethod]
        public void Convert_FromSonarQubeIssue_IssueVizConverterCalledWithCorrectParameters_ReturnsConvertedIssueVizWithReversedLocations()
        {
            var location1 = CreateServerLocation("path1", "message1", new IssueTextRange(1, 2, 3, 4));
            var location2 = CreateServerLocation("path2", "message2", new IssueTextRange(5, 6, 7, 8));
            var flow1 = CreateServerFlow(location1, location2);

            var location3 = CreateServerLocation("path3", "message3", new IssueTextRange(9, 10, 11, 12));
            var flow2 = CreateServerFlow(location3);

            var created = DateTimeOffset.Parse("2001-12-30T01:02:03+0000");
            var lastUpdate = DateTimeOffset.Parse("2009-02-01T13:14:15+0200");

            var issue = CreateServerIssue("issue key", "path4", "hash", "message4", "rule", SonarQubeIssueSeverity.Major,
                new IssueTextRange(13, 14, 15, 16), created, lastUpdate, "contextKey", flow1, flow2);

            var expectedConvertedIssueViz = CreateIssueViz();
            var issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();
            issueVizConverter
                .Setup(x => x.Convert(It.IsAny<IAnalysisIssueBase>(), null))
                .Returns(expectedConvertedIssueViz);

            var testSubject = CreateTestSubject(issueVizConverter.Object);
            var result = testSubject.Convert(issue);

            result.Should().BeSameAs(expectedConvertedIssueViz);

            issueVizConverter.Verify(x => x.Convert(
                    It.Is((TaintIssue taintIssue) =>
                        taintIssue.IssueKey == "issue key" &&
                        taintIssue.RuleKey == "rule" &&
                        taintIssue.Severity == AnalysisIssueSeverity.Major &&
                        taintIssue.RuleDescriptionContextKey == "contextKey" &&

                        taintIssue.PrimaryLocation.FilePath == "path4" &&
                        taintIssue.PrimaryLocation.Message == "message4" &&
                        taintIssue.PrimaryLocation.TextRange.LineHash == "hash" &&
                        taintIssue.PrimaryLocation.TextRange.StartLine == 13 &&
                        taintIssue.PrimaryLocation.TextRange.EndLine == 14 &&
                        taintIssue.PrimaryLocation.TextRange.StartLineOffset == 15 &&
                        taintIssue.PrimaryLocation.TextRange.EndLineOffset == 16 &&

                        taintIssue.CreationTimestamp == created &&
                        taintIssue.LastUpdateTimestamp == lastUpdate &&

                        taintIssue.Flows.Count == 2 &&
                        taintIssue.Flows[0].Locations.Count == 2 &&
                        taintIssue.Flows[1].Locations.Count == 1 &&

                        taintIssue.Flows[0].Locations[0].Message == "message2" &&
                        taintIssue.Flows[0].Locations[0].FilePath == "path2" &&
                        taintIssue.Flows[0].Locations[0].TextRange.LineHash == null &&
                        taintIssue.Flows[0].Locations[0].TextRange.StartLine == 5 &&
                        taintIssue.Flows[0].Locations[0].TextRange.EndLine == 6 &&
                        taintIssue.Flows[0].Locations[0].TextRange.StartLineOffset == 7 &&
                        taintIssue.Flows[0].Locations[0].TextRange.EndLineOffset == 8 &&

                        taintIssue.Flows[0].Locations[1].Message == "message1" &&
                        taintIssue.Flows[0].Locations[1].FilePath == "path1" &&
                        taintIssue.Flows[0].Locations[1].TextRange.LineHash == null &&
                        taintIssue.Flows[0].Locations[1].TextRange.StartLine == 1 &&
                        taintIssue.Flows[0].Locations[1].TextRange.EndLine == 2 &&
                        taintIssue.Flows[0].Locations[1].TextRange.StartLineOffset == 3 &&
                        taintIssue.Flows[0].Locations[1].TextRange.EndLineOffset == 4 &&

                        taintIssue.Flows[1].Locations[0].Message == "message3" &&
                        taintIssue.Flows[1].Locations[0].FilePath == "path3" &&
                        taintIssue.Flows[1].Locations[0].TextRange.LineHash == null &&
                        taintIssue.Flows[1].Locations[0].TextRange.StartLine == 9 &&
                        taintIssue.Flows[1].Locations[0].TextRange.EndLine == 10 &&
                        taintIssue.Flows[1].Locations[0].TextRange.StartLineOffset == 11 &&
                        taintIssue.Flows[1].Locations[0].TextRange.EndLineOffset == 12
                    ),
                    It.IsAny<ITextSnapshot>()),
                Times.Once);
        }

        [TestMethod]
        [DataRow(SonarQubeIssueSeverity.Blocker, AnalysisIssueSeverity.Blocker)]
        [DataRow(SonarQubeIssueSeverity.Critical, AnalysisIssueSeverity.Critical)]
        [DataRow(SonarQubeIssueSeverity.Info, AnalysisIssueSeverity.Info)]
        [DataRow(SonarQubeIssueSeverity.Major, AnalysisIssueSeverity.Major)]
        [DataRow(SonarQubeIssueSeverity.Minor, AnalysisIssueSeverity.Minor)]
        public void Convert_KnownSeverity_ConvertedToAnalysisIssueSeverity(SonarQubeIssueSeverity sqSeverity, AnalysisIssueSeverity expectedSeverity)
        {
            var result = TaintIssueToIssueVisualizationConverter.Convert(sqSeverity);

            result.Should().Be(expectedSeverity);
        }

        [TestMethod]
        [DataRow(SonarQubeIssueSeverity.Unknown)]
        [DataRow((SonarQubeIssueSeverity)1234)]
        public void Convert_UnknownSeverity_ArgumentOutOfRangeException(SonarQubeIssueSeverity sqSeverity)
        {
            Action act = () => TaintIssueToIssueVisualizationConverter.Convert(sqSeverity);

            act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueSeverity");
        }

        public enum OriginalIssueType
        {
            SonarQubeIssue,
            TaintSonarQubeIssue
        }

        [TestMethod]
        [DataRow(OriginalIssueType.SonarQubeIssue)]
        [DataRow(OriginalIssueType.TaintSonarQubeIssue)]
        public void Convert_CalculatesLocalFilePaths(OriginalIssueType originalIssueType)
        {
            var locationViz1 = CreateLocationViz("server-path1");
            var locationViz2 = CreateLocationViz("server-path2");
            var locationViz3 = CreateLocationViz("server-path3");
            var expectedIssueViz = CreateIssueViz("server-path4", locationViz1, locationViz2, locationViz3);

            var issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();
            issueVizConverter
                .Setup(x => x.Convert(It.IsAny<IAnalysisIssueBase>(), null))
                .Returns(expectedIssueViz);

            var absoluteFilePathLocator = new Mock<IAbsoluteFilePathLocator>();
            absoluteFilePathLocator.Setup(x => x.Locate("server-path1")).Returns("local1");
            absoluteFilePathLocator.Setup(x => x.Locate("server-path2")).Returns((string)null);
            absoluteFilePathLocator.Setup(x => x.Locate("server-path3")).Returns("local3");
            absoluteFilePathLocator.Setup(x => x.Locate("server-path4")).Returns("local4");

            var testSubject = CreateTestSubject(issueVizConverter.Object, absoluteFilePathLocator.Object);

            var result = originalIssueType == OriginalIssueType.SonarQubeIssue
                ? testSubject.Convert(CreateDummySonarQubeIssue())
                : testSubject.Convert(CreateDummyTaintSonarQubeIssue());

            result.Should().Be(expectedIssueViz);

            expectedIssueViz.CurrentFilePath.Should().Be("local4");

            var secondaryLocations = expectedIssueViz.GetSecondaryLocations().ToList();
            secondaryLocations[0].CurrentFilePath.Should().Be("local1");
            secondaryLocations[1].CurrentFilePath.Should().Be(null);
            secondaryLocations[2].CurrentFilePath.Should().Be("local3");
        }

        [TestMethod]
        public void Convert_FromTaintIssue_IssueVizConverterCalledWithCorrectParameters_ReturnsConvertedIssueVizWithReversedLocations()
        {
            var location1 = CreateTaintServerLocation("path1", "message1", CreateTaintTextRange(1, 2, 3, 4, "hash1"));
            var location2 = CreateTaintServerLocation("path2", "message2", CreateTaintTextRange(5, 6, 7, 8, "hash2"));
            var flow1 = CreateTaintServerFlow(location1, location2);

            var location3 = CreateTaintServerLocation("path3", "message3", CreateTaintTextRange(9, 10, 11, 12, "hash3"));
            var flow2 = CreateTaintServerFlow(location3);

            var mainLocation = CreateTaintServerLocation("path4", "message4", CreateTaintTextRange(13, 14, 15, 16, "hash4"));
            var creationDate = DateTimeOffset.UtcNow;
            var issue = CreateTaintServerIssue("issue key", "rule", creationDate, SonarQubeIssueSeverity.Major, mainLocation, flow1, flow2);

            var expectedConvertedIssueViz = CreateIssueViz();
            var issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();
            issueVizConverter
                .Setup(x => x.Convert(It.IsAny<IAnalysisIssueBase>(), null))
                .Returns(expectedConvertedIssueViz);

            var testSubject = CreateTestSubject(issueVizConverter.Object);
            var result = testSubject.Convert(issue);

            result.Should().BeSameAs(expectedConvertedIssueViz);

            issueVizConverter.Verify(x => x.Convert(
                    It.Is((TaintIssue taintIssue) =>
                        taintIssue.IssueKey == "issue key" &&
                        taintIssue.RuleKey == "rule" &&
                        taintIssue.Severity == AnalysisIssueSeverity.Major &&

                        taintIssue.PrimaryLocation.FilePath == "path4" &&
                        taintIssue.PrimaryLocation.Message == "message4" &&
                        taintIssue.PrimaryLocation.TextRange.LineHash == "hash4" &&
                        taintIssue.PrimaryLocation.TextRange.StartLine == 13 &&
                        taintIssue.PrimaryLocation.TextRange.EndLine == 14 &&
                        taintIssue.PrimaryLocation.TextRange.StartLineOffset == 15 &&
                        taintIssue.PrimaryLocation.TextRange.EndLineOffset == 16 &&

                        taintIssue.CreationTimestamp == creationDate &&
                        taintIssue.LastUpdateTimestamp == default &&

                        taintIssue.Flows.Count == 2 &&
                        taintIssue.Flows[0].Locations.Count == 2 &&
                        taintIssue.Flows[1].Locations.Count == 1 &&

                        taintIssue.Flows[0].Locations[0].Message == "message2" &&
                        taintIssue.Flows[0].Locations[0].FilePath == "path2" &&
                        taintIssue.Flows[0].Locations[0].TextRange.LineHash == "hash2" &&
                        taintIssue.Flows[0].Locations[0].TextRange.StartLine == 5 &&
                        taintIssue.Flows[0].Locations[0].TextRange.EndLine == 6 &&
                        taintIssue.Flows[0].Locations[0].TextRange.StartLineOffset == 7 &&
                        taintIssue.Flows[0].Locations[0].TextRange.EndLineOffset == 8 &&

                        taintIssue.Flows[0].Locations[1].Message == "message1" &&
                        taintIssue.Flows[0].Locations[1].FilePath == "path1" &&
                        taintIssue.Flows[0].Locations[1].TextRange.LineHash == "hash1" &&
                        taintIssue.Flows[0].Locations[1].TextRange.StartLine == 1 &&
                        taintIssue.Flows[0].Locations[1].TextRange.EndLine == 2 &&
                        taintIssue.Flows[0].Locations[1].TextRange.StartLineOffset == 3 &&
                        taintIssue.Flows[0].Locations[1].TextRange.EndLineOffset == 4 &&

                        taintIssue.Flows[1].Locations[0].Message == "message3" &&
                        taintIssue.Flows[1].Locations[0].FilePath == "path3" &&
                        taintIssue.Flows[1].Locations[0].TextRange.LineHash == "hash3" &&
                        taintIssue.Flows[1].Locations[0].TextRange.StartLine == 9 &&
                        taintIssue.Flows[1].Locations[0].TextRange.EndLine == 10 &&
                        taintIssue.Flows[1].Locations[0].TextRange.StartLineOffset == 11 &&
                        taintIssue.Flows[1].Locations[0].TextRange.EndLineOffset == 12
                    ),
                    It.IsAny<ITextSnapshot>()),
                Times.Once);
        }

        private static TaintIssueToIssueVisualizationConverter CreateTestSubject(IAnalysisIssueVisualizationConverter issueVizConverter = null, IAbsoluteFilePathLocator absoluteFilePathLocator = null)
        {
            issueVizConverter ??= Mock.Of<IAnalysisIssueVisualizationConverter>();
            absoluteFilePathLocator ??= Mock.Of<IAbsoluteFilePathLocator>();

            return new TaintIssueToIssueVisualizationConverter(issueVizConverter, absoluteFilePathLocator);
        }

        private static SonarQubeIssue CreateServerIssue(string issueKey = "issue key", string filePath = "test.cpp", string hash = "hash", string message = "message", string rule = "rule",
            SonarQubeIssueSeverity severity = SonarQubeIssueSeverity.Info, IssueTextRange textRange = null,
            DateTimeOffset created = default, DateTimeOffset lastUpdate = default, string context = null, params IssueFlow[] flows) =>
            new(issueKey, filePath, hash, message, null, rule, true, severity, created, lastUpdate, textRange, flows.ToList(), context);

        private static IssueLocation CreateServerLocation(string filePath = "test.cpp", string message = "message",
            IssueTextRange textRange = null) => new(filePath, null, textRange, message);

        private static IssueFlow CreateServerFlow(params IssueLocation[] locations) => new(locations.ToList());

        private static IAnalysisIssueVisualization CreateIssueViz(string serverFilePath = null, params IAnalysisIssueLocationVisualization[] locationVizs)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();

            var flowViz = new Mock<IAnalysisIssueFlowVisualization>();
            flowViz.Setup(x => x.Locations).Returns(locationVizs);

            issueViz.Setup(x => x.Flows).Returns(new[] { flowViz.Object });
            issueViz.SetupGet(x => x.Location.FilePath).Returns(serverFilePath);
            issueViz.SetupProperty(x => x.CurrentFilePath);

            return issueViz.Object;
        }

        private static IAnalysisIssueLocationVisualization CreateLocationViz(string serverFilePath)
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.SetupGet(x => x.Location.FilePath).Returns(serverFilePath);
            locationViz.SetupProperty(x => x.CurrentFilePath);

            return locationViz.Object;
        }

        private SonarQubeIssue CreateDummySonarQubeIssue()
        {
            return CreateServerIssue(textRange: new IssueTextRange(1, 2, 3, 4));
        }

        private ITaintIssue CreateDummyTaintSonarQubeIssue()
        {
            return CreateTaintServerIssue("key", "rule", DateTimeOffset.UtcNow, SonarQubeIssueSeverity.Blocker,
                CreateTaintServerLocation(serverFilePath: "path", message: "blah",
                    textRange: CreateTaintTextRange(1, 2, 3, 4, "hash")));
        }

        private static ITaintIssue CreateTaintServerIssue(string issueKey, string ruleKey, DateTimeOffset creationDate, SonarQubeIssueSeverity severity, ILocation mainLocation, params IFlow[] flows)
        {
            var issue = new Mock<ITaintIssue>();

            issue.SetupGet(x => x.Key).Returns(issueKey);
            issue.SetupGet(x => x.RuleKey).Returns(ruleKey);
            issue.SetupGet(x => x.Severity).Returns(severity);
            issue.SetupGet(x => x.Flows).Returns(flows);
            issue.SetupGet(x => x.CreationDate).Returns(creationDate);
            issue.SetupGet(x => x.MainLocation).Returns(mainLocation);

            return issue.Object;
        }

        private static IFlow CreateTaintServerFlow(params ILocation[] locations)
        {
            var flow = new Mock<IFlow>();

            flow.SetupGet(x => x.Locations).Returns(locations);

            return flow.Object;
        }

        private static ILocation CreateTaintServerLocation(string serverFilePath, string message, ITextRange textRange)
        {
            var location = new Mock<ILocation>();

            location.SetupGet(x => x.FilePath).Returns(serverFilePath);
            location.SetupGet(x => x.Message).Returns(message);
            location.SetupGet(x => x.TextRange).Returns(textRange);

            return location.Object;
        }

        private static ITextRange CreateTaintTextRange(int startLine, int endLine, int startLineOffset, int endLineOffset, string hash)
        {
            var textRange = new Mock<ITextRange>();

            textRange.SetupGet(x => x.StartLine).Returns(startLine);
            textRange.SetupGet(x => x.EndLine).Returns(endLine);
            textRange.SetupGet(x => x.StartLineOffset).Returns(startLineOffset);
            textRange.SetupGet(x => x.EndLineOffset).Returns(endLineOffset);
            textRange.SetupGet(x => x.Hash).Returns(hash);

            return textRange.Object;
        }
    }
}
