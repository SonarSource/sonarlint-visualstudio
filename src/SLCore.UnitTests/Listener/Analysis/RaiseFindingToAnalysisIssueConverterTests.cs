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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using CleanCodeAttribute = SonarLint.VisualStudio.SLCore.Common.Models.CleanCodeAttribute;
using SoftwareQuality = SonarLint.VisualStudio.SLCore.Common.Models.SoftwareQuality;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Analysis;

[TestClass]
public class RaiseFindingToAnalysisIssueConverterTests
{
    private RaiseFindingToAnalysisIssueConverter testSubject;
    private readonly FileUri fileUri = new(@"C:\file");

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new RaiseFindingToAnalysisIssueConverter();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<RaiseFindingToAnalysisIssueConverter, IRaiseFindingToAnalysisIssueConverter>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<RaiseFindingToAnalysisIssueConverter>();
    }

    [TestMethod]
    public void GetAnalysisIssues_HasNoIssues_ReturnsEmpty()
    {
        var result = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedIssueDto>());

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void GetAnalysisIssues_HasIssues_ConvertsCorrectly()
    {
        var dateTimeOffset = DateTimeOffset.Now;
        var issue1 = new RaisedIssueDto(
            Guid.NewGuid(), 
            "serverKey1", 
            "ruleKey1", 
            "PrimaryMessage1", 
            IssueSeverity.MAJOR, 
            RuleType.CODE_SMELL, 
            CleanCodeAttribute.EFFICIENT,
            [],
            dateTimeOffset, 
            true, 
            false, 
            new TextRangeDto(1, 2, 3, 4), 
            null, 
            null, 
            "context1");
        var issue2 = new RaisedIssueDto(Guid.NewGuid(),
            "serverKey2",
            "ruleKey2",
            "PrimaryMessage2",
            IssueSeverity.CRITICAL,
            RuleType.BUG,
            CleanCodeAttribute.LOGICAL,
            IssueWithFlowsAndQuickFixesUseCase.Issue2Impacts,
            dateTimeOffset,
            true,
            false,
            new TextRangeDto(61, 62, 63, 64),
            [IssueWithFlowsAndQuickFixesUseCase.Issue2Flow1, IssueWithFlowsAndQuickFixesUseCase.Issue2Flow2],
            [IssueWithFlowsAndQuickFixesUseCase.Issue2Fix1, IssueWithFlowsAndQuickFixesUseCase.Issue2Fix2],
            "context2");

        var result = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedIssueDto> { issue1, issue2 }).ToList();

        IssueWithFlowsAndQuickFixesUseCase.VerifyDtosConvertedCorrectly(result);
    }

    [TestMethod]
    public void GetAnalysisIssues_HasHotspot_ConvertsCorrectly()
    {
        var dateTimeOffset = DateTimeOffset.Now;
        var issue1 = new RaisedHotspotDto(Guid.NewGuid(),
            "serverKey1",
            "ruleKey1",
            "PrimaryMessage1", 
            IssueSeverity.MAJOR, 
            RuleType.CODE_SMELL, 
            CleanCodeAttribute.EFFICIENT,
            [], 
            dateTimeOffset, 
            true, 
            false, 
            new TextRangeDto(1, 2, 3, 4), 
            null, 
            null, 
            "context1", 
            VulnerabilityProbability.HIGH, 
            HotspotStatus.FIXED);
        var issue2 = new RaisedHotspotDto(Guid.NewGuid(),
            "serverKey2",
            "ruleKey2",
            "PrimaryMessage2",
            IssueSeverity.CRITICAL,
            RuleType.BUG,
            CleanCodeAttribute.LOGICAL,
            IssueWithFlowsAndQuickFixesUseCase.Issue2Impacts,
            dateTimeOffset,
            true,
            false,
            new TextRangeDto(61, 62, 63, 64),
            [IssueWithFlowsAndQuickFixesUseCase.Issue2Flow1, IssueWithFlowsAndQuickFixesUseCase.Issue2Flow2],
            [IssueWithFlowsAndQuickFixesUseCase.Issue2Fix1, IssueWithFlowsAndQuickFixesUseCase.Issue2Fix2],
            "context2", VulnerabilityProbability.HIGH,
            HotspotStatus.FIXED);

        var result = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedFindingDto> { issue1, issue2 }).ToList();

        IssueWithFlowsAndQuickFixesUseCase.VerifyDtosConvertedCorrectly(result);
    }

    [TestMethod]
    public void GetAnalysisIssues_IssueHasUnflattenedFlows_FlattensIntoSingleFlow()
    {
        var analysisIssues = testSubject.GetAnalysisIssues(UnflattenedFlowsUseCase.FileUri, new List<RaisedIssueDto>
        {
            new(Guid.Empty,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                new TextRangeDto(1,
                    2,
                    3,
                    4),
                UnflattenedFlowsUseCase.UnflattenedFlows,
                default,
                default)
        });

        UnflattenedFlowsUseCase.VerifyFlattenedFlow(analysisIssues);
    }

    [TestMethod]
    public void GetAnalysisIssues_HotspotHasUnflattenedFlows_FlattensIntoSingleFlow()
    {
        var analysisIssues = testSubject.GetAnalysisIssues(UnflattenedFlowsUseCase.FileUri, new List<RaisedHotspotDto>
        {
            new(Guid.Empty,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                new TextRangeDto(1,
                    2,
                    3,
                    4),
                UnflattenedFlowsUseCase.UnflattenedFlows,
                default,
                default,
                VulnerabilityProbability.HIGH,
                HotspotStatus.SAFE)
        });

        UnflattenedFlowsUseCase.VerifyFlattenedFlow(analysisIssues);
    }

    [TestMethod]
    [DataRow(VulnerabilityProbability.HIGH, HotspotPriority.High)]
    [DataRow(VulnerabilityProbability.MEDIUM, HotspotPriority.Medium)]
    [DataRow(VulnerabilityProbability.LOW, HotspotPriority.Low)]
    public void GetAnalysisIssues_HotspotHasVulnerabilityProbability_AnalysisHotspotIssueIsCreatedWithCorrectHotspotPriority(VulnerabilityProbability vulnerabilityProbability, HotspotPriority expectedHotspotPriority)
    {
        var analysisIssues = testSubject.GetAnalysisIssues(fileUri, new List<RaisedHotspotDto>
        {
            new(Guid.Empty, 
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                new TextRangeDto(1,
                    2,
                    3,
                    4),
                [],
                default,
                default,
                vulnerabilityProbability,
                HotspotStatus.SAFE)
        });

        analysisIssues.Single().Should().BeOfType<AnalysisHotspotIssue>().Which.HotspotPriority.Should().Be(expectedHotspotPriority);
    }

    [TestMethod]
    public void GetAnalysisIssues_HotspotHasNoVulnerabilityProbability_AnalysisHotspotIssueIsCreatedWithNoHotspotPriority()
    {
        var analysisIssues = testSubject.GetAnalysisIssues(fileUri, new List<RaisedHotspotDto>
        {
            new(Guid.Empty,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                new TextRangeDto(1,
                    2,
                    3,
                    4),
                [],
                default,
                default,
                null,
                HotspotStatus.SAFE)
        });

        analysisIssues.Single().Should().BeOfType<AnalysisHotspotIssue>().Which.HotspotPriority.Should().BeNull();
    }

    private static class UnflattenedFlowsUseCase
    {
        internal static FileUri FileUri => new("C:\\IssueFile.cs");

        internal static List<IssueFlowDto> UnflattenedFlows => [
            new IssueFlowDto([new IssueLocationDto(new TextRangeDto(1, 1, 1, 1), "1", FileUri)]),
            new IssueFlowDto([new IssueLocationDto(new TextRangeDto(2, 2, 2, 2), "2", FileUri)]),
            new IssueFlowDto([new IssueLocationDto(new TextRangeDto(3, 3, 3, 3), "3", FileUri)]),
            new IssueFlowDto([new IssueLocationDto(new TextRangeDto(4, 4, 4, 4), "4", FileUri)]),
            new IssueFlowDto([new IssueLocationDto(new TextRangeDto(5, 5, 5, 5), "5", FileUri)]),
        ];

        internal static void VerifyFlattenedFlow(IEnumerable<IAnalysisIssue> analysisIssues)
        {
            analysisIssues.Single()
                .Flows.Should()
                .ContainSingle()
                .Which.Locations.Should()
                .HaveCount(5)
                .And.BeEquivalentTo([
                    new AnalysisIssueLocation("1", FileUri.LocalPath, new TextRange(1, 1, 1, 1, null)),
                    new AnalysisIssueLocation("2", FileUri.LocalPath, new TextRange(2, 2, 2, 2, null)),
                    new AnalysisIssueLocation("3", FileUri.LocalPath, new TextRange(3, 3, 3, 3, null)),
                    new AnalysisIssueLocation("4", FileUri.LocalPath, new TextRange(4, 4, 4, 4, null)),
                    new AnalysisIssueLocation("5", FileUri.LocalPath, new TextRange(5, 5, 5, 5, null)),
                ]);
        }
    }

    private static class IssueWithFlowsAndQuickFixesUseCase
    {
        private static ImpactDto Issue2Impact1 => new(SoftwareQuality.SECURITY, ImpactSeverity.LOW);
        private static ImpactDto Issue2Impact2 => new(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.MEDIUM);
        private static ImpactDto Issue2Impact3 => new(SoftwareQuality.RELIABILITY, ImpactSeverity.HIGH);
        internal static List<ImpactDto> Issue2Impacts => [Issue2Impact1, Issue2Impact2, Issue2Impact3];
        private static IssueLocationDto Issue2Flow1Location1 => new(new TextRangeDto(11, 12, 13, 14), "Flow1Location1Message", new FileUri("C:\\flowFile1.cs"));
        private static IssueLocationDto Issue2Flow1Location2 => new(new TextRangeDto(21, 22, 23, 24), "Flow1Location2Message", new FileUri("C:\\flowFile1.cs"));
        internal static IssueFlowDto Issue2Flow1 => new ([Issue2Flow1Location1, Issue2Flow1Location2]);
        private static IssueLocationDto Issue2Flow2Location1 => new(new TextRangeDto(31, 32, 33, 34), "Flow2Location1Message", new FileUri("C:\\flowFile2.cs"));
        private static IssueLocationDto Issue2Flow2Location2 => new(new TextRangeDto(41, 42, 43, 44), "Flow2Location2Message", new FileUri("C:\\flowFile2.cs"));
        internal static IssueFlowDto Issue2Flow2 => new([Issue2Flow2Location1, Issue2Flow2Location2]);
        private static FileEditDto Issue2Fix1FileEdit1 => new(new FileUri("C:\\DifferentFile.cs"), []);
        internal static QuickFixDto Issue2Fix1 => new([Issue2Fix1FileEdit1], "issue 2 fix 1");
        private static TextEditDto Issue2Fix2FileEdit1Textedit1 => new(new TextRangeDto(51, 52, 53, 54), "new text");
        private static FileEditDto Issue2Fix2FileEdit1 => new(new FileUri("C:\\IssueFile.cs"), [Issue2Fix2FileEdit1Textedit1]);
        internal static QuickFixDto Issue2Fix2 => new([Issue2Fix2FileEdit1], "issue 2 fix 2");


        internal static void VerifyDtosConvertedCorrectly(List<IAnalysisIssue> result)
        {
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            result[0].RuleKey.Should().Be("ruleKey1");
            result[0].Severity.Should().Be(AnalysisIssueSeverity.Major);
            result[0].Type.Should().Be(AnalysisIssueType.CodeSmell);
            result[0].HighestSoftwareQualitySeverity.Should().BeNull();
            result[0].RuleDescriptionContextKey.Should().Be("context1");

            result[0].PrimaryLocation.FilePath.Should().Be("C:\\IssueFile.cs");
            result[0].PrimaryLocation.Message.Should().Be("PrimaryMessage1");
            result[0].PrimaryLocation.TextRange.StartLine.Should().Be(1);
            result[0].PrimaryLocation.TextRange.StartLineOffset.Should().Be(2);
            result[0].PrimaryLocation.TextRange.EndLine.Should().Be(3);
            result[0].PrimaryLocation.TextRange.EndLineOffset.Should().Be(4);
            result[0].PrimaryLocation.TextRange.LineHash.Should().BeNull();

            result[0].Flows.Should().BeEmpty();
            result[0].Fixes.Should().BeEmpty();

            result[1].RuleKey.Should().Be("ruleKey2");
            result[1].Severity.Should().Be(AnalysisIssueSeverity.Critical);
            result[1].Type.Should().Be(AnalysisIssueType.Bug);
            result[1].HighestSoftwareQualitySeverity.Should().Be(SoftwareQualitySeverity.High);
            result[1].RuleDescriptionContextKey.Should().Be("context2");

            result[1].PrimaryLocation.FilePath.Should().Be("C:\\IssueFile.cs");
            result[1].PrimaryLocation.Message.Should().Be("PrimaryMessage2");
            result[1].PrimaryLocation.TextRange.StartLine.Should().Be(61);
            result[1].PrimaryLocation.TextRange.StartLineOffset.Should().Be(62);
            result[1].PrimaryLocation.TextRange.EndLine.Should().Be(63);
            result[1].PrimaryLocation.TextRange.EndLineOffset.Should().Be(64);
            result[1].PrimaryLocation.TextRange.LineHash.Should().BeNull();

            result[1].Flows.Should().HaveCount(2);
            result[1].Flows[0].Locations.Should().HaveCount(2);

            result[1].Flows[0].Locations[0].FilePath.Should().Be("C:\\flowFile1.cs");
            result[1].Flows[0].Locations[0].Message.Should().Be("Flow1Location1Message");
            result[1].Flows[0].Locations[0].TextRange.StartLine.Should().Be(11);
            result[1].Flows[0].Locations[0].TextRange.StartLineOffset.Should().Be(12);
            result[1].Flows[0].Locations[0].TextRange.EndLine.Should().Be(13);
            result[1].Flows[0].Locations[0].TextRange.EndLineOffset.Should().Be(14);
            result[1].Flows[0].Locations[0].TextRange.LineHash.Should().BeNull();
            result[1].Flows[0].Locations[1].FilePath.Should().Be("C:\\flowFile1.cs");
            result[1].Flows[0].Locations[1].Message.Should().Be("Flow1Location2Message");
            result[1].Flows[0].Locations[1].TextRange.StartLine.Should().Be(21);
            result[1].Flows[0].Locations[1].TextRange.StartLineOffset.Should().Be(22);
            result[1].Flows[0].Locations[1].TextRange.EndLine.Should().Be(23);
            result[1].Flows[0].Locations[1].TextRange.EndLineOffset.Should().Be(24);
            result[1].Flows[0].Locations[1].TextRange.LineHash.Should().BeNull();

            result[1].Flows[1].Locations[0].FilePath.Should().Be("C:\\flowFile2.cs");
            result[1].Flows[1].Locations[0].Message.Should().Be("Flow2Location1Message");
            result[1].Flows[1].Locations[0].TextRange.StartLine.Should().Be(31);
            result[1].Flows[1].Locations[0].TextRange.StartLineOffset.Should().Be(32);
            result[1].Flows[1].Locations[0].TextRange.EndLine.Should().Be(33);
            result[1].Flows[1].Locations[0].TextRange.EndLineOffset.Should().Be(34);
            result[1].Flows[1].Locations[0].TextRange.LineHash.Should().BeNull();
            result[1].Flows[1].Locations[1].FilePath.Should().Be("C:\\flowFile2.cs");
            result[1].Flows[1].Locations[1].Message.Should().Be("Flow2Location2Message");
            result[1].Flows[1].Locations[1].TextRange.StartLine.Should().Be(41);
            result[1].Flows[1].Locations[1].TextRange.StartLineOffset.Should().Be(42);
            result[1].Flows[1].Locations[1].TextRange.EndLine.Should().Be(43);
            result[1].Flows[1].Locations[1].TextRange.EndLineOffset.Should().Be(44);
            result[1].Flows[1].Locations[1].TextRange.LineHash.Should().BeNull();

            result[1].Fixes.Should().HaveCount(1);
            result[1].Fixes[0].Message.Should().Be("issue 2 fix 2");
            result[1].Fixes[0].Edits.Should().HaveCount(1);
            result[1].Fixes[0].Edits[0].RangeToReplace.StartLine.Should().Be(51);
            result[1].Fixes[0].Edits[0].RangeToReplace.StartLineOffset.Should().Be(52);
            result[1].Fixes[0].Edits[0].RangeToReplace.EndLine.Should().Be(53);
            result[1].Fixes[0].Edits[0].RangeToReplace.EndLineOffset.Should().Be(54);
            result[1].Fixes[0].Edits[0].RangeToReplace.LineHash.Should().BeNull();
        }

    }
}
