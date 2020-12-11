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

            var issue = CreateServerIssue("path4", "hash", "message4", "rule", SonarQubeIssueSeverity.Major, new IssueTextRange(13, 14, 15, 16), flow1, flow2);

            var absoluteFilePathLocator = new Mock<IAbsoluteFilePathLocator>();
            absoluteFilePathLocator.Setup(x => x.Locate("path1")).Returns("found1");
            absoluteFilePathLocator.Setup(x => x.Locate("path2")).Returns("found2");
            absoluteFilePathLocator.Setup(x => x.Locate("path3")).Returns("found3");
            absoluteFilePathLocator.Setup(x => x.Locate("path4")).Returns("found4");

            var expectedConvertedIssueViz = Mock.Of<IAnalysisIssueVisualization>();
            var issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();
            issueVizConverter
                .Setup(x => x.Convert(It.IsAny<IAnalysisIssueBase>(), null))
                .Returns(expectedConvertedIssueViz);

            var testSubject = CreateTestSubject(issueVizConverter.Object, absoluteFilePathLocator.Object);
            var result = testSubject.Convert(issue);

            result.Should().BeSameAs(expectedConvertedIssueViz);

            issueVizConverter.Verify(x => x.Convert(
                    It.Is((AnalysisIssue issueBase) =>
                        issueBase.FilePath == "found4" &&
                        issueBase.RuleKey == "rule" &&
                        issueBase.LineHash == "hash" &&
                        issueBase.Message == "message4" &&
                        issueBase.StartLine == 13 &&
                        issueBase.EndLine == 14 &&
                        issueBase.StartLineOffset == 15 &&
                        issueBase.EndLineOffset == 16 &&
                        issueBase.Severity == AnalysisIssueSeverity.Major &&
                        issueBase.Type == AnalysisIssueType.Vulnerability &&

                        issueBase.Flows.Count == 2 &&
                        issueBase.Flows[0].Locations.Count == 2 &&
                        issueBase.Flows[1].Locations.Count == 1 &&

                        issueBase.Flows[0].Locations[0].LineHash == null &&
                        issueBase.Flows[0].Locations[0].Message == "message1" &&
                        issueBase.Flows[0].Locations[0].FilePath == "found1" &&
                        issueBase.Flows[0].Locations[0].StartLine == 1 &&
                        issueBase.Flows[0].Locations[0].EndLine == 2 &&
                        issueBase.Flows[0].Locations[0].StartLineOffset == 3 &&
                        issueBase.Flows[0].Locations[0].EndLineOffset == 4 &&

                        issueBase.Flows[0].Locations[1].LineHash == null &&
                        issueBase.Flows[0].Locations[1].Message == "message2" &&
                        issueBase.Flows[0].Locations[1].FilePath == "found2" &&
                        issueBase.Flows[0].Locations[1].StartLine == 5 &&
                        issueBase.Flows[0].Locations[1].EndLine == 6 &&
                        issueBase.Flows[0].Locations[1].StartLineOffset == 7 &&
                        issueBase.Flows[0].Locations[1].EndLineOffset == 8 &&

                        issueBase.Flows[1].Locations[0].LineHash == null &&
                        issueBase.Flows[1].Locations[0].Message == "message3" &&
                        issueBase.Flows[1].Locations[0].FilePath == "found3" &&
                        issueBase.Flows[1].Locations[0].StartLine == 9 &&
                        issueBase.Flows[1].Locations[0].EndLine == 10 &&
                        issueBase.Flows[1].Locations[0].StartLineOffset == 11 &&
                        issueBase.Flows[1].Locations[0].EndLineOffset == 12
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

        private TaintIssueToIssueVisualizationConverter CreateTestSubject(IAnalysisIssueVisualizationConverter issueVizConverter = null, IAbsoluteFilePathLocator absoluteFilePathLocator = null)
        {
            issueVizConverter ??= Mock.Of<IAnalysisIssueVisualizationConverter>();
            absoluteFilePathLocator ??= Mock.Of<IAbsoluteFilePathLocator>();

            return new TaintIssueToIssueVisualizationConverter(issueVizConverter, absoluteFilePathLocator);
        }

        private SonarQubeIssue CreateServerIssue(string filePath = "test.cpp", string hash = "hash", string message = "message", string rule = "rule", SonarQubeIssueSeverity severity = SonarQubeIssueSeverity.Info, IssueTextRange textRange = null, params IssueFlow[] flows) => 
            new SonarQubeIssue(filePath, hash, message, null, rule, true, severity, textRange, flows.ToList());

        private IssueLocation CreateServerLocation(string filePath = "test.cpp", string message = "message", IssueTextRange textRange = null) => 
            new IssueLocation(filePath, null, textRange, message);

        private IssueFlow CreateServerFlow(params IssueLocation[] locations) => 
            new IssueFlow(locations.ToList());
    }
}
