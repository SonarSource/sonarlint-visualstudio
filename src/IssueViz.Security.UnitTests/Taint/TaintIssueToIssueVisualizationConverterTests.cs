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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint
{
    [TestClass]
    public class TaintIssueToIssueVisualizationConverterTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TaintIssueToIssueVisualizationConverter, ITaintIssueToIssueVisualizationConverter>(null, new[]
            {
                MefTestHelpers.CreateExport<IAnalysisIssueVisualizationConverter>(Mock.Of<IAnalysisIssueVisualizationConverter>()),
                MefTestHelpers.CreateExport<IAbsoluteFilePathLocator>(Mock.Of<IAbsoluteFilePathLocator>())
            });
        }

        [TestMethod]
        public void Convert_ServerIssueHasNoTextRange_ArgumentNullException()
        {
            var serverIssue = CreateServerIssue(textRange: null);
            
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Convert(serverIssue);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("TextRange");
        }

        [TestMethod]
        public void Convert_FlowLocationHasNoTextRange_ArgumentNullException()
        {
            var serverLocation = CreateServerLocation(textRange: null);
            var serverFlow = CreateServerFlow(serverLocation);
            var sonarQubeIssue = CreateServerIssue(textRange: new IssueTextRange(1, 1, 1, 1), flows: serverFlow);
            
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Convert(sonarQubeIssue);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("TextRange");
        }

        [TestMethod]
        public void Convert_IssueVizConverterCalledWithCorrectParameters_ReturnsConvertedIssueViz()
        {
            var location1 = CreateServerLocation("path1", "message1", new IssueTextRange(1,2,3,4));
            var location2 = CreateServerLocation("path2", "message2", new IssueTextRange(5, 6, 7, 8));
            var flow1 = CreateServerFlow(location1, location2);

            var location3 = CreateServerLocation("path3", "message3", new IssueTextRange(9, 10, 11, 12));
            var flow2 = CreateServerFlow(location3);

            var created = DateTimeOffset.Parse("2001-12-30T01:02:03+0000");
            var lastUpdate = DateTimeOffset.Parse("2009-02-01T13:14:15+0200");

            var issue = CreateServerIssue("issue key", "path4", "hash", "message4", "rule", SonarQubeIssueSeverity.Major,
                new IssueTextRange(13, 14, 15, 16), created, lastUpdate, flow1, flow2);

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
                        taintIssue.FilePath == "path4" &&
                        taintIssue.RuleKey == "rule" &&
                        taintIssue.LineHash == "hash" &&
                        taintIssue.Message == "message4" &&
                        taintIssue.StartLine == 13 &&
                        taintIssue.EndLine == 14 &&
                        taintIssue.StartLineOffset == 15 &&
                        taintIssue.EndLineOffset == 16 &&
                        taintIssue.Severity == AnalysisIssueSeverity.Major &&

                        taintIssue.CreationTimestamp == created &&
                        taintIssue.LastUpdateTimestamp == lastUpdate &&

                        taintIssue.Flows.Count == 2 &&
                        taintIssue.Flows[0].Locations.Count == 2 &&
                        taintIssue.Flows[1].Locations.Count == 1 &&

                        taintIssue.Flows[0].Locations[0].LineHash == null &&
                        taintIssue.Flows[0].Locations[0].Message == "message1" &&
                        taintIssue.Flows[0].Locations[0].FilePath == "path1" &&
                        taintIssue.Flows[0].Locations[0].StartLine == 1 &&
                        taintIssue.Flows[0].Locations[0].EndLine == 2 &&
                        taintIssue.Flows[0].Locations[0].StartLineOffset == 3 &&
                        taintIssue.Flows[0].Locations[0].EndLineOffset == 4 &&

                        taintIssue.Flows[0].Locations[1].LineHash == null &&
                        taintIssue.Flows[0].Locations[1].Message == "message2" &&
                        taintIssue.Flows[0].Locations[1].FilePath == "path2" &&
                        taintIssue.Flows[0].Locations[1].StartLine == 5 &&
                        taintIssue.Flows[0].Locations[1].EndLine == 6 &&
                        taintIssue.Flows[0].Locations[1].StartLineOffset == 7 &&
                        taintIssue.Flows[0].Locations[1].EndLineOffset == 8 &&

                        taintIssue.Flows[1].Locations[0].LineHash == null &&
                        taintIssue.Flows[1].Locations[0].Message == "message3" &&
                        taintIssue.Flows[1].Locations[0].FilePath == "path3" &&
                        taintIssue.Flows[1].Locations[0].StartLine == 9 &&
                        taintIssue.Flows[1].Locations[0].EndLine == 10 &&
                        taintIssue.Flows[1].Locations[0].StartLineOffset == 11 &&
                        taintIssue.Flows[1].Locations[0].EndLineOffset == 12
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
        public void Convert_Severity(SonarQubeIssueSeverity sqSeverity, AnalysisIssueSeverity expectedSeverity)
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

        [TestMethod]
        public void Convert_CalculatesLocalFilePaths()
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

            var testSubject = CreateTestSubject(issueVizConverter.Object);

            var serverIssue = CreateServerIssue(filePath:"server-path4",textRange: new IssueTextRange(1, 2, 3, 4));
            var result = testSubject.Convert(serverIssue);

            result.Should().Be(expectedIssueViz);

            expectedIssueViz.CurrentFilePath.Should().Be("local4");

            var secondaryLocations = expectedIssueViz.GetSecondaryLocations().ToList();
            secondaryLocations[0].CurrentFilePath.Should().Be("local1");
            secondaryLocations[1].CurrentFilePath.Should().Be(null);
            secondaryLocations[2].CurrentFilePath.Should().Be("local3");
        }

        private TaintIssueToIssueVisualizationConverter CreateTestSubject(IAnalysisIssueVisualizationConverter issueVizConverter = null, IAbsoluteFilePathLocator absoluteFilePathLocator = null)
        {
            issueVizConverter ??= Mock.Of<IAnalysisIssueVisualizationConverter>();
            absoluteFilePathLocator ??= Mock.Of<IAbsoluteFilePathLocator>();

            return new TaintIssueToIssueVisualizationConverter(issueVizConverter, absoluteFilePathLocator);
        }

        private SonarQubeIssue CreateServerIssue(string issueKey = "issue key", string filePath = "test.cpp", string hash = "hash", string message = "message", string rule = "rule",
            SonarQubeIssueSeverity severity = SonarQubeIssueSeverity.Info, IssueTextRange textRange = null,
            DateTimeOffset created = default, DateTimeOffset lastUpdate = default, params IssueFlow[] flows) => 
            new SonarQubeIssue(issueKey, filePath, hash, message, null, rule, true, severity, created, lastUpdate, textRange, flows.ToList());

        private IssueLocation CreateServerLocation(string filePath = "test.cpp", string message = "message", IssueTextRange textRange = null) => 
            new IssueLocation(filePath, null, textRange, message);

        private IssueFlow CreateServerFlow(params IssueLocation[] locations) => 
            new IssueFlow(locations.ToList());

        private IAnalysisIssueVisualization CreateIssueViz(string serverFilePath = null, params IAnalysisIssueLocationVisualization[] locationVizs)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();

            var flowViz = new Mock<IAnalysisIssueFlowVisualization>();
            flowViz.Setup(x => x.Locations).Returns(locationVizs);

            issueViz.Setup(x => x.Flows).Returns(new[] { flowViz.Object });
            issueViz.SetupGet(x => x.Location.FilePath).Returns(serverFilePath);
            issueViz.SetupProperty(x => x.CurrentFilePath);

            return issueViz.Object;
        }

        private IAnalysisIssueLocationVisualization CreateLocationViz(string serverFilePath)
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.SetupGet(x => x.Location.FilePath).Returns(serverFilePath);
            locationViz.SetupProperty(x => x.CurrentFilePath);

            return locationViz.Object;
        }
    }
}
