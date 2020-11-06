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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using NuGet;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Api
{
    [TestClass]
    public class HotspotToIssueVisualizationConverterTests
    {
        [TestMethod]
        public void Convert_CreatedIssueVisualization()
        {
            var sonarQubeHotspot = CreateSonarQubeHotspot(
                filePath: "some path",
                probability: "high",
                textRange: new IssueTextRange(5, 10, 15, 20),
                message: "message",
                ruleKey: "rule key");

            var expectedIssueViz = Mock.Of<IAnalysisIssueVisualization>();
            var issueVizConverter = SetupIssueVizConverter(expectedIssueViz);

            var testSubject = new HotspotToIssueVisualizationConverter(issueVizConverter.Object);
            var issueViz = testSubject.Convert(sonarQubeHotspot);
            issueViz.Should().Be(expectedIssueViz);

            issueVizConverter.Verify(x => x.Convert(
                    It.Is((IHotspot hotspot) =>
                        hotspot.Priority == HotspotPriority.High &&
                        hotspot.LineHash == null &&
                        hotspot.Flows.IsEmpty() &&
                        hotspot.Message == "message" &&
                        hotspot.RuleKey == "rule key" &&
                        hotspot.FilePath== "some path" &&
                        hotspot.StartLine == 5 &&
                        hotspot.EndLine == 10 &&
                        hotspot.StartLineOffset == 15 &&
                        hotspot.EndLineOffset == 20),
                    It.IsAny<ITextSnapshot>()),
                Times.Once);
        }

        [TestMethod]
        public void Convert_NullVulnerabilityProbability_ArgumentNullException()
        {
            var sonarQubeHotspot = CreateSonarQubeHotspot(probability: null);
            var issueVizConverter = SetupIssueVizConverter(Mock.Of<IAnalysisIssueVisualization>());

            var testSubject = new HotspotToIssueVisualizationConverter(issueVizConverter.Object);
            Action act = () => testSubject.Convert(sonarQubeHotspot);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("vulnerabilityProbability");
        }

        [TestMethod]
        public void Convert_UnknownVulnerabilityProbability_ArgumentOutOfRangeException()
        {
            var sonarQubeHotspot = CreateSonarQubeHotspot(probability: "some probability");
            var issueVizConverter = SetupIssueVizConverter(Mock.Of<IAnalysisIssueVisualization>());

            var testSubject = new HotspotToIssueVisualizationConverter(issueVizConverter.Object);
            Action act = () => testSubject.Convert(sonarQubeHotspot);

            act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("vulnerabilityProbability");
        }

        [TestMethod]
        [DataRow("high", HotspotPriority.High)]
        [DataRow("HiGh", HotspotPriority.High)]
        [DataRow("HIGH", HotspotPriority.High)]
        [DataRow("medium", HotspotPriority.Medium)]
        [DataRow("low", HotspotPriority.Low)]
        public void Convert_VulnerabilityProbability_ConvertedToHotspotPriority(string probability, HotspotPriority expectedPriority)
        {
            var sonarQubeHotspot = CreateSonarQubeHotspot(probability);
            var issueVizConverter = SetupIssueVizConverter(Mock.Of<IAnalysisIssueVisualization>());

            var testSubject = new HotspotToIssueVisualizationConverter(issueVizConverter.Object);
            testSubject.Convert(sonarQubeHotspot);

            issueVizConverter.Verify(x => x.Convert(
                    It.Is((IHotspot hotspot) => hotspot.Priority == expectedPriority),
                    It.IsAny<ITextSnapshot>()),
                Times.Once);
        }

        private SonarQubeHotspot CreateSonarQubeHotspot(string probability, string filePath = "some path", IssueTextRange textRange = null, string message = "message", string ruleKey = "rule key") =>
            new SonarQubeHotspot("some key",
                message,
                "assignee",
                "status",
                "org",
                "projectKey",
                "projectName",
                "componentKey",
                filePath,
                ruleKey,
                "ruleName",
                "securityCategory",
                probability,
                textRange ?? new IssueTextRange(5, 10, 15, 20));

        private static Mock<IAnalysisIssueVisualizationConverter> SetupIssueVizConverter(IAnalysisIssueVisualization expectedIssueViz)
        {
            var issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();
            issueVizConverter
                .Setup(x => x.Convert(It.IsAny<IHotspot>(), null))
                .Returns(expectedIssueViz);
            return issueVizConverter;
        }
    }
}
