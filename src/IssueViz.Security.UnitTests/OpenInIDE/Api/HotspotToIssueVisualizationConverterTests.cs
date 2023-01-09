﻿/*
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using NuGet;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
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
            var creationDate = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(10342));
            var lastUpdated = DateTimeOffset.Now;

            var sonarQubeHotspot = CreateSonarQubeHotspot(
                hotspotKey: "some key",
                filePath: "some path",
                probability: "high",
                textRange: new IssueTextRange(5, 10, 15, 20),
                message: "message",
                ruleKey: "rule key",

                lineHash: "hash-xxx");

            var absoluteFilePathLocator = SetupAbsoluteFilePathLocator("some path", "some absolute path");

            var expectedIssueViz = Mock.Of<IAnalysisIssueVisualization>();
            var issueVizConverter = SetupIssueVizConverter(expectedIssueViz);

            var testSubject = new HotspotToIssueVisualizationConverter(issueVizConverter.Object, absoluteFilePathLocator.Object);
            var issueViz = testSubject.Convert(sonarQubeHotspot);
            issueViz.Should().Be(expectedIssueViz);

            issueVizConverter.Verify(x => x.Convert(
                    It.Is((IHotspot hotspot) =>
                        hotspot.HotspotKey == "some key" &&
                        hotspot.Rule.Priority == HotspotPriority.High &&
                        hotspot.Flows.IsEmpty() &&
                        hotspot.RuleKey == "rule key" &&
                        hotspot.Rule.RuleKey == "rule key" &&
                        hotspot.ServerFilePath== "some path" &&
                        hotspot.PrimaryLocation.Message == "message" &&
                        hotspot.PrimaryLocation.FilePath == "some absolute path" &&
                        hotspot.PrimaryLocation.TextRange.LineHash == "hash-xxx" &&
                        hotspot.PrimaryLocation.TextRange.StartLine == 5 &&
                        hotspot.PrimaryLocation.TextRange.EndLine == 10 &&
                        hotspot.PrimaryLocation.TextRange.StartLineOffset == 15 &&
                        hotspot.PrimaryLocation.TextRange.EndLineOffset == 20),
                    It.IsAny<ITextSnapshot>()),
                Times.Once);
        }

        [TestMethod]
        public void Convert_CannotGetAbsoluteFilePath_FilePathIsNull()
        {
            const string originalPath = "some path";

            var sonarQubeHotspot = CreateSonarQubeHotspot(filePath: originalPath);
            var absoluteFilePathLocator = SetupAbsoluteFilePathLocator(originalPath, null);

            var expectedIssueViz = Mock.Of<IAnalysisIssueVisualization>();
            var issueVizConverter = SetupIssueVizConverter(expectedIssueViz);

            var testSubject = new HotspotToIssueVisualizationConverter(issueVizConverter.Object, absoluteFilePathLocator.Object);
            var issueViz = testSubject.Convert(sonarQubeHotspot);
            issueViz.Should().Be(expectedIssueViz);

            issueVizConverter.Verify(x => x.Convert(
                    It.Is((IHotspot hotspot) => 
                        hotspot.PrimaryLocation.FilePath == null &&
                        hotspot.ServerFilePath == originalPath),
                    It.IsAny<ITextSnapshot>()),
                Times.Once);
        }

        [TestMethod]
        public void Convert_NullVulnerabilityProbability_ArgumentNullException()
        {
            var sonarQubeHotspot = CreateSonarQubeHotspot(probability: null);
            var issueVizConverter = SetupIssueVizConverter(Mock.Of<IAnalysisIssueVisualization>());

            var testSubject = new HotspotToIssueVisualizationConverter(issueVizConverter.Object, Mock.Of<IAbsoluteFilePathLocator>());
            Action act = () => testSubject.Convert(sonarQubeHotspot);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("vulnerabilityProbability");
        }

        [TestMethod]
        public void Convert_UnknownVulnerabilityProbability_ArgumentOutOfRangeException()
        {
            var sonarQubeHotspot = CreateSonarQubeHotspot(probability: "some probability");
            var issueVizConverter = SetupIssueVizConverter(Mock.Of<IAnalysisIssueVisualization>());

            var testSubject = new HotspotToIssueVisualizationConverter(issueVizConverter.Object, Mock.Of<IAbsoluteFilePathLocator>());
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
            var sonarQubeHotspot = CreateSonarQubeHotspot(probability: probability);
            var issueVizConverter = SetupIssueVizConverter(Mock.Of<IAnalysisIssueVisualization>());

            var testSubject = new HotspotToIssueVisualizationConverter(issueVizConverter.Object, Mock.Of<IAbsoluteFilePathLocator>());
            testSubject.Convert(sonarQubeHotspot);

            issueVizConverter.Verify(x => x.Convert(
                    It.Is((IHotspot hotspot) => hotspot.Rule.Priority == expectedPriority),
                    It.IsAny<ITextSnapshot>()),
                Times.Once);
        }

        private SonarQubeHotspot CreateSonarQubeHotspot(string hotspotKey = "some key", string probability = "high", string filePath = "some path", IssueTextRange textRange = null, string message = "message",
            string ruleKey = "rule key", DateTimeOffset creationDate = default(DateTimeOffset), DateTimeOffset updateDate = default(DateTimeOffset), string lineHash = "linehash") =>
            new SonarQubeHotspot(hotspotKey,
                message,
                lineHash,
                "assignee",
                "status",
                "org",
                "projectKey",
                "projectName",
                "componentKey",
                filePath,
                creationDate,
                updateDate,
                new SonarQubeHotspotRule(ruleKey, "rule name", "sec category", probability, "risk desc", "vuln desc", "fix req"),
                textRange ?? new IssueTextRange(5, 10, 15, 20));

        private static Mock<IAnalysisIssueVisualizationConverter> SetupIssueVizConverter(IAnalysisIssueVisualization expectedIssueViz)
        {
            var issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();

            issueVizConverter
                .Setup(x => x.Convert(It.IsAny<IHotspot>(), null))
                .Returns(expectedIssueViz);

            return issueVizConverter;
        }

        private static Mock<IAbsoluteFilePathLocator> SetupAbsoluteFilePathLocator(string originalPath, string expectedAbsolutePath)
        {
            var absoluteFilePathLocator = new Mock<IAbsoluteFilePathLocator>();

            absoluteFilePathLocator
                .Setup(x => x.Locate(originalPath))
                .Returns(expectedAbsolutePath);

            return absoluteFilePathLocator;
        }
    }
}
