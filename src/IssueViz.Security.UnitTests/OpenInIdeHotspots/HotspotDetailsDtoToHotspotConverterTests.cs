/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using NSubstitute;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIdeHotspots;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIdeHotspots;

[TestClass]
public class HotspotDetailsDtoToHotspotConverterTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<HotspotDetailsDtoToHotspotConverter, IHotspotDetailsDtoToHotspotConverter>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<HotspotDetailsDtoToHotspotConverter>();
    }
    
    [TestMethod]
    public void Convert_CalculatesChecksumForCodeSnippet()
    {
        const string codeSnippet = "code snippet; 123";
        const string checksum = "checksum123";
        var checksumCalculator = Substitute.For<IChecksumCalculator>();
        checksumCalculator.Calculate(codeSnippet).Returns(checksum);
        var testSubject = new HotspotDetailsDtoToHotspotConverter(checksumCalculator);

        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                new TextRangeDto(1, 2, 3, 4),
                "author",
                "status",
                "resolution",
                new HotspotRuleDto("rule:key",
                    "ruleName",
                    "security category",
                    "LOW",
                    "riskDescription",
                    "vulnerability description",
                    "fix recomendations"),
                codeSnippet),
            "some\\path");

        hotspot.PrimaryLocation.TextRange.LineHash.Should().BeSameAs(checksum);
    }
    
    [TestMethod]
    public void Convert_PathTranslated()
    {
        var testSubject = new HotspotDetailsDtoToHotspotConverter(Substitute.For<IChecksumCalculator>());

        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                new TextRangeDto(1, 2, 3, 4),
                "author",
                "status",
                "resolution",
                new HotspotRuleDto("rule:key",
                    "ruleName",
                    "security category",
                    "LOW",
                    "riskDescription",
                    "vulnerability description",
                    "fix recomendations"),
                "code snippet"),
            "some\\path");

        hotspot.PrimaryLocation.FilePath.Should().Be("some\\path\\ide\\path");
    }
    
    [TestMethod]
    public void Convert_PrimaryRangeAndMessagePreserved()
    {
        const int startLine = 1;
        const int startLineOffset = 2;
        const int endLine = 11;
        const int endLineOffset = 22;
        const string message = "msg";
        var testSubject = new HotspotDetailsDtoToHotspotConverter(Substitute.For<IChecksumCalculator>());

        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                message,
                "ide\\path",
                new TextRangeDto(startLine, startLineOffset, endLine, endLineOffset),
                "author",
                "status",
                "resolution",
                new HotspotRuleDto("rule:key",
                    "ruleName",
                    "security category",
                    "LOW",
                    "riskDescription",
                    "vulnerability description",
                    "fix recomendations"),
                "code snippet"),
            "some\\path");

        hotspot.PrimaryLocation.Message.Should().BeSameAs(message);
        hotspot.PrimaryLocation.TextRange.Should().BeEquivalentTo(new TextRange(startLine, endLine, startLineOffset, endLineOffset, "hash"), options => options.Excluding(info => info.LineHash));
    }
    
    [TestMethod]
    public void Convert_RuleKeyPreserved()
    {
        const string ruleKey = "ruleKey:123";
        var testSubject = new HotspotDetailsDtoToHotspotConverter(Substitute.For<IChecksumCalculator>());

        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                new TextRangeDto(1, 2, 3, 4),
                "author",
                "status",
                "resolution",
                new HotspotRuleDto(ruleKey,
                    "ruleName",
                    "security category",
                    "LOW",
                    "riskDescription",
                    "vulnerability description",
                    "fix recomendations"),
                "code snippet"),
            "some\\path");

        hotspot.RuleKey.Should().BeSameAs(ruleKey);
    }
    
    [TestMethod]
    public void Convert_HotspotKeyPreserved()
    {
        const string hotspotKey = "hotspotKey123";
        var testSubject = new HotspotDetailsDtoToHotspotConverter(Substitute.For<IChecksumCalculator>());
    
        var hotspot = testSubject.Convert(new HotspotDetailsDto(hotspotKey,
                "msg",
                "ide\\path",
                new TextRangeDto(1, 2, 3, 4),
                "author",
                "status",
                "resolution",
                new HotspotRuleDto("rule:key",
                    "ruleName",
                    "security category",
                    "LOW",
                    "riskDescription",
                    "vulnerability description",
                    "fix recomendations"),
                "code snippet"),
            "some\\path").Should().BeOfType<Hotspot>().Subject;
    
        hotspot.HotspotKey.Should().BeSameAs(hotspotKey);
    }
    
    [TestMethod]
    public void Convert_HotspotRulePreserved()
    {
        var testSubject = new HotspotDetailsDtoToHotspotConverter(Substitute.For<IChecksumCalculator>());

        var hotspotRuleDto = new HotspotRuleDto("rule:key",
            "ruleName",
            "security category",
            "LOW",
            "riskDescription",
            "vulnerability description",
            "fix recomendations");
        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                new TextRangeDto(1, 2, 3, 4),
                "author",
                "status",
                "resolution",
                hotspotRuleDto,
                "code snippet"),
            "some\\path").Should().BeOfType<Hotspot>().Subject;

        hotspot.Rule.RuleKey.Should().BeSameAs(hotspotRuleDto.key);
        hotspot.Rule.RuleName.Should().BeSameAs(hotspotRuleDto.name);
        hotspot.Rule.SecurityCategory.Should().BeSameAs(hotspotRuleDto.securityCategory);
        hotspot.Rule.RiskDescription.Should().BeSameAs(hotspotRuleDto.riskDescription);
        hotspot.Rule.VulnerabilityDescription.Should().BeSameAs(hotspotRuleDto.vulnerabilityDescription);
        hotspot.Rule.FixRecommendations.Should().BeSameAs(hotspotRuleDto.fixRecommendations);
    }
    
    [DataTestMethod]
    [DataRow("LOW", HotspotPriority.Low)]
    [DataRow("low", HotspotPriority.Low)]
    [DataRow("lOw", HotspotPriority.Low)]
    [DataRow("MEDIUM", HotspotPriority.Medium)]
    [DataRow("medium", HotspotPriority.Medium)]
    [DataRow("Medium", HotspotPriority.Medium)]
    [DataRow("HIGH", HotspotPriority.High)]
    [DataRow("high", HotspotPriority.High)]
    [DataRow("hIGh", HotspotPriority.High)]
    public void Convert_HotspotPriorityConverted(string vulnerabilityProbability, HotspotPriority priority)
    {
        var testSubject = new HotspotDetailsDtoToHotspotConverter(Substitute.For<IChecksumCalculator>());

        var hotspotRuleDto = new HotspotRuleDto("rule:key",
            "ruleName",
            "security category",
            vulnerabilityProbability,
            "riskDescription",
            "vulnerability description",
            "fix recomendations");
        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                new TextRangeDto(1, 2, 3, 4),
                "author",
                "status",
                "resolution",
                hotspotRuleDto,
                "code snippet"),
            "some\\path").Should().BeOfType<Hotspot>().Subject;

        hotspot.Rule.Priority.Should().Be(priority);
    }
    
    [TestMethod]
    public void Convert_PriorityOutOfRange_Throws()
    {
        var testSubject = new HotspotDetailsDtoToHotspotConverter(Substitute.For<IChecksumCalculator>());

        var act  = () => testSubject.Convert(new HotspotDetailsDto("key",
            "msg",
            "ide\\path",
            new TextRangeDto(1, 2, 3, 4),
            "author",
            "status",
            "resolution",
            new HotspotRuleDto("rule:key",
                "ruleName",
                "security category",
                "SOMEOUTOFRANGEVALUE",
                "riskDescription",
                "vulnerability description",
                "fix recomendations"),
            "code snippet"),
            "some\\path");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
    
    [TestMethod]
    public void Convert_FlowsAndRuleDescriptionContextSetToDefault()
    {
        var testSubject = new HotspotDetailsDtoToHotspotConverter(Substitute.For<IChecksumCalculator>());

        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
            "msg",
            "ide\\path",
            new TextRangeDto(1, 2, 3, 4),
            "author",
            "status",
            "resolution",
            new HotspotRuleDto("rule:key",
                "ruleName",
                "security category",
                "LOW",
                "riskDescription",
                "vulnerability description",
                "fix recomendations"),
            "code snippet"),
            "some\\path");

        hotspot.Flows.Should().BeEmpty();
        hotspot.RuleDescriptionContextKey.Should().BeNull();
    }
}
