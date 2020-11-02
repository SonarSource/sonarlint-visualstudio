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
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using NuGet;
using SonarLint.VisualStudio.IssueVisualization.Editor;
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
            const string filePath = "some path";
            var expectedIssueViz = Mock.Of<IAnalysisIssueVisualization>();
            var sonarQubeHotspot = CreateSonarQubeHotspot(filePath, probability: "high", line: 123, message: "message", ruleKey: "rule key");

            var testSubject = CreateTestSubject(filePath, out var converter, expectedIssueViz);
            var issueViz = testSubject.Convert(sonarQubeHotspot);
            issueViz.Should().Be(expectedIssueViz);

            converter.Verify(x => x.Convert(
                    It.Is((IHotspot hotspot) =>
                        hotspot.Priority == HotspotPriority.High &&
                        hotspot.LineHash == null &&
                        hotspot.Flows.IsEmpty() &&
                        hotspot.StartLine == 123 &&
                        hotspot.EndLine == 123 &&
                        hotspot.StartLineOffset == 0 &&
                        hotspot.EndLineOffset == 0 &&
                        hotspot.Message == "message" &&
                        hotspot.RuleKey == "rule key" &&
                        hotspot.FilePath== filePath),
                    It.IsAny<ITextSnapshot>()),
                Times.Once);
        }

        [TestMethod]
        public void Convert_NullVulnerabilityProbability_ArgumentNullException()
        {
            const string filePath = "some path";
            var sonarQubeHotspot = CreateSonarQubeHotspot(filePath, probability: null);

            var testSubject = CreateTestSubject(filePath, out _);
            Action act = () => testSubject.Convert(sonarQubeHotspot);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("vulnerabilityProbability");
        }

        [TestMethod]
        public void Convert_UnknownVulnerabilityProbability_ArgumentOutOfRangeException()
        {
            const string filePath = "some path";
            var sonarQubeHotspot = CreateSonarQubeHotspot(filePath, probability: "some probability");

            var testSubject = CreateTestSubject(filePath, out _);
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
            const string filePath = "some path";
            var sonarQubeHotspot = CreateSonarQubeHotspot(filePath, probability);

            var testSubject = CreateTestSubject(filePath, out var converter);
            testSubject.Convert(sonarQubeHotspot);

            converter.Verify(x => x.Convert(
                    It.Is((IHotspot hotspot) => hotspot.Priority == expectedPriority),
                    It.IsAny<ITextSnapshot>()),
                Times.Once);
        }

        private SonarQubeHotspot CreateSonarQubeHotspot(string filePath, string probability, int line = 123, string message = "message", string ruleKey = "rule key") =>
            new SonarQubeHotspot("some key",
                message,
                "assignee",
                "status",
                line,
                "org",
                "projectKey",
                "projectName",
                "componentKey",
                filePath,
                ruleKey,
                "ruleName",
                "securityCategory",
                probability);

        private HotspotToIssueVisualizationConverter CreateTestSubject(string hotspotFilePath, out Mock<IAnalysisIssueVisualizationConverter> issueVizConverter, IAnalysisIssueVisualization expectedIssueViz = null)
        {
            var textSnapshot = Mock.Of<ITextSnapshot>();
            var textView = new Mock<ITextView>();
            textView.SetupGet(x => x.TextBuffer.CurrentSnapshot).Returns(textSnapshot);

            var documentNavigator = new Mock<IDocumentNavigator>();
            documentNavigator.Setup(x => x.Open(hotspotFilePath)).Returns(textView.Object);

            expectedIssueViz ??= Mock.Of<IAnalysisIssueVisualization>();
            issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();
            issueVizConverter
                .Setup(x => x.Convert(It.IsAny<IHotspot>(), textSnapshot))
                .Returns(expectedIssueViz);

            var testSubject = new HotspotToIssueVisualizationConverter(issueVizConverter.Object, documentNavigator.Object);

            return testSubject;
        }
    }
}
