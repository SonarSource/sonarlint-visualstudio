/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIdeHotspots;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIdeHotspots;

[TestClass]
public class HotspotDetailsDtoToHotspotConverterTests
{
    private IChecksumCalculator checksumCalculator;
    private HotspotDetailsDtoToHotspotConverter testSubject;
    private readonly TextRangeDto textRangeDto = new TextRangeDto(1, 2, 3, 4);
    private readonly HotspotRuleDto hotspotRuleDto = new HotspotRuleDto("rule:key",
        "ruleName",
        "security category",
        "LOW",
        "riskDescription",
        "vulnerability description",
        "fix recomendations");

    [TestInitialize]
    public void TestInitialize()
    {
        checksumCalculator = Substitute.For<IChecksumCalculator>();
        testSubject = new HotspotDetailsDtoToHotspotConverter(checksumCalculator);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<HotspotDetailsDtoToHotspotConverter, IHotspotDetailsDtoToHotspotConverter>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<HotspotDetailsDtoToHotspotConverter>();

    [TestMethod]
    public void Convert_CalculatesChecksumForCodeSnippet()
    {
        const string codeSnippet = "code snippet; 123";
        const string checksum = "checksum123";
        checksumCalculator.Calculate(codeSnippet).Returns(checksum);

        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                textRangeDto,
                "author",
                "status",
                "resolution",
                hotspotRuleDto,
                codeSnippet),
            "some\\path");

        hotspot.PrimaryLocation.TextRange.LineHash.Should().BeSameAs(checksum);
    }

    [TestMethod]
    public void Convert_PathTranslated()
    {
        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                textRangeDto,
                "author",
                "status",
                "resolution",
                hotspotRuleDto,
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

        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                message,
                "ide\\path",
                new TextRangeDto(startLine, startLineOffset, endLine, endLineOffset),
                "author",
                "status",
                "resolution",
                hotspotRuleDto,
                "code snippet"),
            "some\\path");

        hotspot.PrimaryLocation.Message.Should().BeSameAs(message);
        hotspot.PrimaryLocation.TextRange.Should().BeEquivalentTo(new TextRange(startLine, endLine, startLineOffset, endLineOffset, "hash"), options => options.Excluding(info => info.LineHash));
    }

    [TestMethod]
    public void Convert_RuleKeyPreserved()
    {
        const string ruleKey = "ruleKey:123";

        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                textRangeDto,
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

        var hotspot = testSubject.Convert(new HotspotDetailsDto(hotspotKey,
                "msg",
                "ide\\path",
                textRangeDto,
                "author",
                "status",
                "resolution",
                hotspotRuleDto,
                "code snippet"),
            "some\\path").Should().BeOfType<Hotspot>().Subject;

        hotspot.IssueServerKey.Should().BeSameAs(hotspotKey);
    }

    [TestMethod]
    public void Convert_HotspotRulePreserved()
    {
        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                textRangeDto,
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

    [TestMethod]
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
        var ruleDto = new HotspotRuleDto("rule:key",
            "ruleName",
            "security category",
            vulnerabilityProbability,
            "riskDescription",
            "vulnerability description",
            "fix recomendations");
        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                textRangeDto,
                "author",
                "status",
                "resolution",
                ruleDto,
                "code snippet"),
            "some\\path").Should().BeOfType<Hotspot>().Subject;

        hotspot.Rule.Priority.Should().Be(priority);
    }

    [TestMethod]
    public void Convert_PriorityOutOfRange_Throws()
    {
        var act = () => testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                textRangeDto,
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
    public void Convert_PriorityNull_Throws()
    {
        var act = () => testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                textRangeDto,
                "author",
                "status",
                "resolution",
                new HotspotRuleDto("rule:key",
                    "ruleName",
                    "security category",
                    null,
                    "riskDescription",
                    "vulnerability description",
                    "fix recomendations"),
                "code snippet"),
            "some\\path");

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Convert_Flows()
    {
        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                textRangeDto,
                "author",
                "status",
                "resolution",
                hotspotRuleDto,
                "code snippet"),
            "some\\path");

        hotspot.Flows.Should().BeEmpty();
    }

    [TestMethod]
    public void Convert_IsResolved_DefaultsToFalse()
    {
        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                textRangeDto,
                "author",
                "status",
                "resolution",
                hotspotRuleDto,
                "code snippet"),
            "some\\path");

        hotspot.IsResolved.Should().BeFalse();
    }

    [TestMethod]
    public void Convert_CodeSnippetIsNull_SetsLineHashToNull()
    {
        var hotspot = testSubject.Convert(new HotspotDetailsDto("key",
                "msg",
                "ide\\path",
                textRangeDto,
                "author",
                "status",
                "resolution",
                hotspotRuleDto,
                codeSnippet: null),
            "some\\path");

        hotspot.PrimaryLocation.TextRange.LineHash.Should().BeNull();
    }
}
