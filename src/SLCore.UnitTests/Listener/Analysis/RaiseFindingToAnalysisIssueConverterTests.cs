/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using CleanCodeAttribute = SonarLint.VisualStudio.SLCore.Common.Models.CleanCodeAttribute;
using SoftwareQuality = SonarLint.VisualStudio.SLCore.Common.Models.SoftwareQuality;
using HotspotStatus = SonarLint.VisualStudio.SLCore.Common.Models.HotspotStatus;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Analysis;

[TestClass]
public class RaiseFindingToAnalysisIssueConverterTests
{
    private readonly FileUri fileUri = new(@"C:\file");
    private RaiseFindingToAnalysisIssueConverter testSubject;
    private ILogger logger;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
        testSubject = new RaiseFindingToAnalysisIssueConverter(logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RaiseFindingToAnalysisIssueConverter, IRaiseFindingToAnalysisIssueConverter>(MefTestHelpers.CreateExport<ILogger>(logger));

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RaiseFindingToAnalysisIssueConverter>();

    [TestMethod]
    public void Ctor_RegistersContextForLogs() => logger.Received(1).ForContext(nameof(RaiseFindingToAnalysisIssueConverter));

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
        var issue1 = CreateRaisedIssueDto(
            id: IssueWithFlowsAndQuickFixesUseCase.Issue1Id,
            serverKey: "serverKey1",
            ruleKey: "ruleKey1",
            primaryMessage: "PrimaryMessage1",
            introductionDate: dateTimeOffset,
            isOnNewCode: true,
            resolved: false,
            textRange: new TextRangeDto(1, 2, 3, 4),
            flows: null,
            quickFixes: null,
            ruleDescriptionContextKey: "context1",
            severityMode: new StandardModeDetails(IssueSeverity.MAJOR, RuleType.CODE_SMELL));

        var issue2 = CreateRaisedIssueDto(
            id: IssueWithFlowsAndQuickFixesUseCase.Issue2Id,
            serverKey: "serverKey2",
            ruleKey: "ruleKey2",
            primaryMessage: "PrimaryMessage2",
            introductionDate: dateTimeOffset,
            isOnNewCode: true,
            resolved: false,
            textRange: new TextRangeDto(61, 62, 63, 64),
            flows: [IssueWithFlowsAndQuickFixesUseCase.Issue2Flow1, IssueWithFlowsAndQuickFixesUseCase.Issue2Flow2],
            quickFixes: [IssueWithFlowsAndQuickFixesUseCase.Issue2Fix1, IssueWithFlowsAndQuickFixesUseCase.Issue2Fix2],
            ruleDescriptionContextKey: "context2",
            severityMode: new MQRModeDetails(CleanCodeAttribute.COMPLETE, IssueWithFlowsAndQuickFixesUseCase.Issue2Impacts));

        var result = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedIssueDto> { issue1, issue2 }).ToList();

        IssueWithFlowsAndQuickFixesUseCase.VerifyDtosConvertedCorrectly(result);
    }

    [TestMethod]
    public void GetAnalysisIssues_HasHotspot_ConvertsCorrectly()
    {
        var dateTimeOffset = DateTimeOffset.Now;
        var issue1 = CreateRaisedHotspotDto(
            id: IssueWithFlowsAndQuickFixesUseCase.Issue1Id,
            serverKey: "serverKey1",
            ruleKey: "ruleKey1",
            primaryMessage: "PrimaryMessage1",
            introductionDate: dateTimeOffset,
            isOnNewCode: true,
            resolved: false,
            textRange: new TextRangeDto(1, 2, 3, 4),
            flows: null,
            quickFixes: null,
            ruleDescriptionContextKey: "context1",
            vulnerabilityProbability: VulnerabilityProbability.HIGH,
            status: HotspotStatus.FIXED,
            severityMode: new StandardModeDetails(IssueSeverity.MAJOR, RuleType.CODE_SMELL));

        var issue2 = CreateRaisedHotspotDto(
            id: IssueWithFlowsAndQuickFixesUseCase.Issue2Id,
            serverKey: "serverKey2",
            ruleKey: "ruleKey2",
            primaryMessage: "PrimaryMessage2",
            introductionDate: dateTimeOffset,
            isOnNewCode: true,
            resolved: false,
            textRange: new TextRangeDto(61, 62, 63, 64),
            flows: [IssueWithFlowsAndQuickFixesUseCase.Issue2Flow1, IssueWithFlowsAndQuickFixesUseCase.Issue2Flow2],
            quickFixes: [IssueWithFlowsAndQuickFixesUseCase.Issue2Fix1, IssueWithFlowsAndQuickFixesUseCase.Issue2Fix2],
            ruleDescriptionContextKey: "context2",
            vulnerabilityProbability: VulnerabilityProbability.HIGH,
            status: HotspotStatus.FIXED,
            severityMode: new MQRModeDetails(CleanCodeAttribute.COMPLETE, IssueWithFlowsAndQuickFixesUseCase.Issue2Impacts));

        var result = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedFindingDto> { issue1, issue2 }).ToList();

        IssueWithFlowsAndQuickFixesUseCase.VerifyDtosConvertedCorrectly(result);
    }

    [TestMethod]
    public void GetAnalysisIssues_IssueHasUnflattenedFlows_FlattensIntoSingleFlow()
    {
        var analysisIssues = testSubject.GetAnalysisIssues(UnflattenedFlowsUseCase.FileUri, new List<RaisedIssueDto>
        {
            CreateRaisedIssueDto(flows: UnflattenedFlowsUseCase.UnflattenedFlows)
        });

        UnflattenedFlowsUseCase.VerifyFlattenedFlow(analysisIssues);
    }

    [TestMethod]
    public void GetAnalysisIssues_HotspotHasUnflattenedFlows_FlattensIntoSingleFlow()
    {
        var analysisIssues = testSubject.GetAnalysisIssues(UnflattenedFlowsUseCase.FileUri, new List<RaisedHotspotDto>
        {
            CreateRaisedHotspotDto(flows: UnflattenedFlowsUseCase.UnflattenedFlows)
        });

        UnflattenedFlowsUseCase.VerifyFlattenedFlow(analysisIssues);
    }

    [TestMethod]
    [DataRow(VulnerabilityProbability.HIGH, HotspotPriority.High)]
    [DataRow(VulnerabilityProbability.MEDIUM, HotspotPriority.Medium)]
    [DataRow(VulnerabilityProbability.LOW, HotspotPriority.Low)]
    public void GetAnalysisIssues_HotspotHasVulnerabilityProbability_AnalysisHotspotIssueIsCreatedWithCorrectHotspotPriority(
        VulnerabilityProbability vulnerabilityProbability,
        HotspotPriority expectedHotspotPriority)
    {
        var analysisIssues = testSubject.GetAnalysisIssues(fileUri, new List<RaisedHotspotDto>
        {
            CreateRaisedHotspotDto(
                vulnerabilityProbability: vulnerabilityProbability)
        });

        analysisIssues.Single().Should().BeOfType<AnalysisHotspotIssue>().Which.HotspotPriority.Should().Be(expectedHotspotPriority);
    }

    [TestMethod]
    public void GetAnalysisIssues_HotspotHasNoVulnerabilityProbability_AnalysisHotspotIssueIsCreatedWithNoHotspotPriority()
    {
        var analysisIssues = testSubject.GetAnalysisIssues(fileUri, new List<RaisedHotspotDto>
        {
            CreateRaisedHotspotDto(vulnerabilityProbability: null)
        });

        analysisIssues.Single().Should().BeOfType<AnalysisHotspotIssue>().Which.HotspotPriority.Should().BeNull();
    }

    [TestMethod]
    [DataRow(ImpactSeverity.BLOCKER, SoftwareQualitySeverity.Blocker)]
    [DataRow(ImpactSeverity.HIGH, SoftwareQualitySeverity.High)]
    [DataRow(ImpactSeverity.MEDIUM, SoftwareQualitySeverity.Medium)]
    [DataRow(ImpactSeverity.LOW, SoftwareQualitySeverity.Low)]
    [DataRow(ImpactSeverity.INFO, SoftwareQualitySeverity.Info)]
    public void GetAnalysisIssues_HotspotWithTwoHighImpactsForDifferentQualities_GetsTheHighestSoftwareQuality(ImpactSeverity severity, SoftwareQualitySeverity expectedSoftwareQualitySeverity)
    {
        var analysisIssues = testSubject.GetAnalysisIssues(fileUri, new List<RaisedHotspotDto>
        {
            CreateRaisedHotspotDto(
                severityMode: new MQRModeDetails(default,
                [
                    new ImpactDto(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.INFO),
                    new ImpactDto(SoftwareQuality.RELIABILITY, severity),
                    new ImpactDto(SoftwareQuality.SECURITY, severity)
                ]))
        });

        var hotspotIssue = analysisIssues.SingleOrDefault() as AnalysisHotspotIssue;
        hotspotIssue.Should().NotBeNull();
        hotspotIssue.HighestImpact.Should().NotBeNull();
        hotspotIssue.HighestImpact.Quality.Should().Be(VisualStudio.Core.Analysis.SoftwareQuality.Security);
        hotspotIssue.HighestImpact.Severity.Should().Be(expectedSoftwareQualitySeverity);
    }

    [TestMethod]
    [DataRow(ImpactSeverity.BLOCKER, SoftwareQualitySeverity.Blocker)]
    [DataRow(ImpactSeverity.HIGH, SoftwareQualitySeverity.High)]
    [DataRow(ImpactSeverity.MEDIUM, SoftwareQualitySeverity.Medium)]
    [DataRow(ImpactSeverity.LOW, SoftwareQualitySeverity.Low)]
    [DataRow(ImpactSeverity.INFO, SoftwareQualitySeverity.Info)]
    public void GetAnalysisIssues_IssueWithTwoHighImpactsForDifferentQualities_GetsTheHighestSoftwareQuality(ImpactSeverity severity, SoftwareQualitySeverity expectedSoftwareQualitySeverity)
    {
        var analysisIssues = testSubject.GetAnalysisIssues(fileUri, new List<RaisedIssueDto>
        {
            CreateRaisedIssueDto(
                severityMode: new MQRModeDetails(default,
                [
                    new ImpactDto(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.INFO),
                    new ImpactDto(SoftwareQuality.RELIABILITY, severity),
                    new ImpactDto(SoftwareQuality.SECURITY, severity)
                ]))
        });

        var issue = analysisIssues.SingleOrDefault() as AnalysisIssue;
        issue.Should().NotBeNull();
        issue.HighestImpact.Should().NotBeNull();
        issue.HighestImpact.Quality.Should().Be(VisualStudio.Core.Analysis.SoftwareQuality.Security);
        issue.HighestImpact.Severity.Should().Be(expectedSoftwareQualitySeverity);
    }

    /// <summary>
    /// File level issues do not have a TextRange
    /// </summary>
    [TestMethod]
    public void GetAnalysisIssues_TextRangeDtoIsNull_ConvertsCorrectly()
    {
        const string primaryMessage = "PrimaryMessage1";
        var issue1 = CreateRaisedHotspotDto(
            id: IssueWithFlowsAndQuickFixesUseCase.Issue1Id,
            primaryMessage: primaryMessage,
            textRange: null);

        var analysisIssues = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedFindingDto> { issue1 }).ToList();

        var issue = analysisIssues.SingleOrDefault() as AnalysisIssue;
        issue.Should().NotBeNull();
        issue.PrimaryLocation.TextRange.Should().BeNull();
        issue.PrimaryLocation.Message.Should().Be(primaryMessage);
    }

    [TestMethod]
    public void GetAnalysisIssues_IssueWithQuickFixSplitIntoTwoFileEdits_ReturnsIssueWithSingleQuickFix()
    {
        var issue = CreateRaisedIssueDto(
            quickFixes:
            [
                new QuickFixDto([
                    new FileEditDto(new FileUri("C:\\IssueFile.cs"), [new TextEditDto(new TextRangeDto(5, 6, 7, 8), "new text")]),
                    new FileEditDto(new FileUri("C:\\IssueFile.cs"), [new TextEditDto(new TextRangeDto(9, 10, 11, 12), "another text")]),
                    new FileEditDto(new FileUri("C:\\AnotherFile.cs"), [new TextEditDto(new TextRangeDto(20, 10, 21, 12), "skip this fix")])
                ], "QuickFix")
            ]);

        var analysisIssues = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedFindingDto> { issue }).ToList();

        analysisIssues.Should().NotBeNull();
        analysisIssues.Should().ContainSingle();
        analysisIssues[0].Fixes.Should().ContainSingle();
        analysisIssues[0].Fixes[0].Should().BeAssignableTo<ITextBasedQuickFix>().Which.Edits.Should().HaveCount(2);
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public void GetAnalysisIssues_IsResolvedSetCorrectly(bool isResolved)
    {
        var issue = new RaisedIssueDto(Guid.NewGuid(),
            "serverKey",
            "ruleKey",
            "PrimaryMessage",
            DateTimeOffset.Now,
            true,
            isResolved,
            new TextRangeDto(1, 2, 3, 4),
            [],
            [],
            "context",
            new StandardModeDetails(IssueSeverity.MAJOR, RuleType.CODE_SMELL));

        var analysisIssues = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedFindingDto> { issue }).ToList();

        analysisIssues.Should().NotBeNull();
        analysisIssues.Should().ContainSingle();
        analysisIssues[0].IsResolved.Should().Be(isResolved);
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public void GetAnalysisIssues_IsOnNewCodeSetCorrectly(bool isOnNewCode)
    {
        var issue = new RaisedIssueDto(Guid.NewGuid(),
            "serverKey",
            "ruleKey",
            "PrimaryMessage",
            DateTimeOffset.Now,
            isOnNewCode,
            false,
            new TextRangeDto(1, 2, 3, 4),
            [],
            [],
            "context",
            new StandardModeDetails(IssueSeverity.MAJOR, RuleType.CODE_SMELL));

        var analysisIssues = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedFindingDto> { issue }).ToList();

        analysisIssues.Should().NotBeNull();
        analysisIssues.Should().ContainSingle();
        analysisIssues[0].IsOnNewCode.Should().Be(isOnNewCode);
    }

    [TestMethod]
    public void GetAnalysisIssues_IssueWithRoslynQuickFix_ReturnsIssueWithRoslynQuickFix()
    {
        var expectedId = Guid.NewGuid();
        var roslynQuickFix = new RoslynQuickFix(expectedId);
        var serializedFix = roslynQuickFix.GetStorageValue();

        var issue = CreateRaisedIssueDto(quickFixes: [new QuickFixDto([], serializedFix)]);

        var analysisIssues = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedFindingDto> { issue }).ToList();

        analysisIssues.Should().NotBeNull();
        analysisIssues.Should().ContainSingle();
        analysisIssues[0].Fixes.Should().BeEquivalentTo(roslynQuickFix);
    }

    [TestMethod]
    public void GetAnalysisIssues_TwoIssuesAndOneIsInvalidIssueDto_ReturnsOneIssueAndLogsTheInvalidOne()
    {
        var dateTimeOffset = DateTimeOffset.Now;
        var issue1 = new RaisedIssueDto(
            IssueWithFlowsAndQuickFixesUseCase.Issue1Id,
            "serverKey1",
            "ruleKey1",
            "PrimaryMessage1",
            dateTimeOffset,
            true,
            false,
            new TextRangeDto(1, 2, 3, 4),
            null,
            null,
            "context1",
            new StandardModeDetails(IssueSeverity.MAJOR, RuleType.CODE_SMELL));
        var invalidIssue = new RaisedIssueDto(
            IssueWithFlowsAndQuickFixesUseCase.Issue2Id,
            "serverKey2",
            "ruleKey2",
            "PrimaryMessage1",
            dateTimeOffset,
            true,
            false,
            new TextRangeDto(1, 2, 3, 4),
            null,
            null,
            "context1",
            severityMode: null);

        var analysisIssues = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedFindingDto> { issue1, invalidIssue }).ToList();

        analysisIssues.Should().NotBeNull();
        analysisIssues.Should().ContainSingle();
        IssueWithFlowsAndQuickFixesUseCase.VerifyIssue1ConvertedCorrectly(analysisIssues[0]);
        logger.Received(1).WriteLine(SLCoreStrings.RaiseFindingToAnalysisIssueConverter_CreateAnalysisIssueFailed, Arg.Is<object[]>(x => x[0].ToString() == "ruleKey2"));
    }

    [TestMethod]
    public void GetAnalysisIssues_TwoIssuesAndOneIsInvalidHotspotDto_ReturnsOneIssueAndLogsTheInvalidOne()
    {
        var dateTimeOffset = DateTimeOffset.Now;
        var issue1 = new RaisedIssueDto(
            IssueWithFlowsAndQuickFixesUseCase.Issue1Id,
            "serverKey1",
            "ruleKey1",
            "PrimaryMessage1",
            dateTimeOffset,
            true,
            false,
            new TextRangeDto(1, 2, 3, 4),
            null,
            null,
            "context1",
            new StandardModeDetails(IssueSeverity.MAJOR, RuleType.CODE_SMELL));
        var invalidIssue = new RaisedHotspotDto(IssueWithFlowsAndQuickFixesUseCase.Issue2Id,
            "serverKey2",
            "ruleKey2",
            "PrimaryMessage1",
            dateTimeOffset,
            true,
            false,
            textRange: null,
            null,
            null,
            "context1",
            VulnerabilityProbability.HIGH,
            HotspotStatus.FIXED,
            severityMode: null);

        var analysisIssues = testSubject.GetAnalysisIssues(new FileUri("C:\\IssueFile.cs"), new List<RaisedFindingDto> { issue1, invalidIssue }).ToList();

        analysisIssues.Should().NotBeNull();
        analysisIssues.Should().ContainSingle();
        IssueWithFlowsAndQuickFixesUseCase.VerifyIssue1ConvertedCorrectly(analysisIssues[0]);
        logger.Received(1).WriteLine(SLCoreStrings.RaiseFindingToAnalysisIssueConverter_CreateAnalysisIssueFailed, Arg.Is<object[]>(x => x[0].ToString() == "ruleKey2"));
    }

    [TestMethod]
    [DataRow(HotspotStatus.TO_REVIEW)]
    [DataRow(HotspotStatus.ACKNOWLEDGED)]
    [DataRow(HotspotStatus.FIXED)]
    [DataRow(HotspotStatus.SAFE)]
    public void GetAnalysisIssues_Hotspot_AnalysisHotspotIssueHasHotspotStatus(HotspotStatus hotspotStatus)
    {
        var analysisIssues = testSubject.GetAnalysisIssues(fileUri, new List<RaisedHotspotDto>
        {
            CreateRaisedHotspotDto(
                vulnerabilityProbability: null,
                status: hotspotStatus)
        });

        var hotspotIssue = analysisIssues.SingleOrDefault() as AnalysisHotspotIssue;
        hotspotIssue.Should().NotBeNull();
        hotspotIssue.HotspotStatus.Should().Be(hotspotStatus.ToHotspotStatus());
    }


    private static RaisedIssueDto CreateRaisedIssueDto(
        Guid? id = null,
        string serverKey = null,
        string ruleKey = "rule:key",
        string primaryMessage = "Primary message",
        DateTimeOffset? introductionDate = null,
        bool isOnNewCode = true,
        bool resolved = false,
        TextRangeDto textRange = null,
        List<IssueFlowDto> flows = null,
        List<QuickFixDto> quickFixes = null,
        string ruleDescriptionContextKey = null,
        Either<StandardModeDetails, MQRModeDetails> severityMode = null)
    {
        return new RaisedIssueDto(
            id ?? Guid.NewGuid(),
            serverKey,
            ruleKey,
            primaryMessage,
            introductionDate ?? DateTimeOffset.Now,
            isOnNewCode,
            resolved,
            textRange,
            flows ?? [],
            quickFixes ?? [],
            ruleDescriptionContextKey,
            severityMode ?? new StandardModeDetails(IssueSeverity.MAJOR, RuleType.CODE_SMELL));
    }


    private static RaisedHotspotDto CreateRaisedHotspotDto(
        Guid? id = null,
        string serverKey = null,
        string ruleKey = "rule:key",
        string primaryMessage = "Primary message",
        DateTimeOffset? introductionDate = null,
        bool isOnNewCode = true,
        bool resolved = false,
        TextRangeDto textRange = null,
        List<IssueFlowDto> flows = null,
        List<QuickFixDto> quickFixes = null,
        string ruleDescriptionContextKey = null,
        VulnerabilityProbability? vulnerabilityProbability = VulnerabilityProbability.HIGH,
        HotspotStatus status = HotspotStatus.TO_REVIEW,
        Either<StandardModeDetails, MQRModeDetails> severityMode = null) =>
        new(
            id ?? Guid.NewGuid(),
            serverKey,
            ruleKey,
            primaryMessage,
            introductionDate ?? DateTimeOffset.Now,
            isOnNewCode,
            resolved,
            textRange,
            flows ?? [],
            quickFixes ?? [],
            ruleDescriptionContextKey,
            vulnerabilityProbability,
            status,
            severityMode ?? new StandardModeDetails(IssueSeverity.MAJOR, RuleType.CODE_SMELL));

    private static class UnflattenedFlowsUseCase
    {
        internal static FileUri FileUri => new("C:\\IssueFile.cs");

        internal static List<IssueFlowDto> UnflattenedFlows =>
        [
            new([new IssueLocationDto(new TextRangeDto(1, 1, 1, 1), "1", FileUri)]),
            new([new IssueLocationDto(new TextRangeDto(2, 2, 2, 2), "2", FileUri)]),
            new([new IssueLocationDto(new TextRangeDto(3, 3, 3, 3), "3", FileUri)]),
            new([new IssueLocationDto(new TextRangeDto(4, 4, 4, 4), "4", FileUri)]),
            new([new IssueLocationDto(new TextRangeDto(5, 5, 5, 5), "5", FileUri)])
        ];

        internal static void VerifyFlattenedFlow(IEnumerable<IAnalysisIssue> analysisIssues) =>
            analysisIssues.Single()
                .Flows.Should()
                .ContainSingle()
                .Which.Locations.Should()
                .HaveCount(5)
                .And.BeEquivalentTo(new AnalysisIssueLocation("1", FileUri.LocalPath, new TextRange(1, 1, 1, 1, null)),
                    new AnalysisIssueLocation("2", FileUri.LocalPath, new TextRange(2, 2, 2, 2, null)), new AnalysisIssueLocation("3", FileUri.LocalPath, new TextRange(3, 3, 3, 3, null)),
                    new AnalysisIssueLocation("4", FileUri.LocalPath, new TextRange(4, 4, 4, 4, null)), new AnalysisIssueLocation("5", FileUri.LocalPath, new TextRange(5, 5, 5, 5, null)));
    }

    private static class IssueWithFlowsAndQuickFixesUseCase
    {
        private static ImpactDto Issue2Impact1 => new(SoftwareQuality.SECURITY, ImpactSeverity.LOW);
        private static ImpactDto Issue2Impact2 => new(SoftwareQuality.MAINTAINABILITY, ImpactSeverity.MEDIUM);
        private static ImpactDto Issue2Impact3 => new(SoftwareQuality.RELIABILITY, ImpactSeverity.HIGH);
        internal static List<ImpactDto> Issue2Impacts => [Issue2Impact1, Issue2Impact2, Issue2Impact3];
        private static IssueLocationDto Issue2Flow1Location1 => new(new TextRangeDto(11, 12, 13, 14), "Flow1Location1Message", new FileUri("C:\\flowFile1.cs"));
        private static IssueLocationDto Issue2Flow1Location2 => new(new TextRangeDto(21, 22, 23, 24), "Flow1Location2Message", new FileUri("C:\\flowFile1.cs"));
        internal static IssueFlowDto Issue2Flow1 => new([Issue2Flow1Location1, Issue2Flow1Location2]);
        private static IssueLocationDto Issue2Flow2Location1 => new(new TextRangeDto(31, 32, 33, 34), "Flow2Location1Message", new FileUri("C:\\flowFile2.cs"));
        private static IssueLocationDto Issue2Flow2Location2 => new(new TextRangeDto(41, 42, 43, 44), "Flow2Location2Message", new FileUri("C:\\flowFile2.cs"));
        internal static IssueFlowDto Issue2Flow2 => new([Issue2Flow2Location1, Issue2Flow2Location2]);
        private static FileEditDto Issue2Fix1FileEdit1 => new(new FileUri("C:\\DifferentFile.cs"), []);
        internal static QuickFixDto Issue2Fix1 => new([Issue2Fix1FileEdit1], "issue 2 fix 1");
        private static TextEditDto Issue2Fix2FileEdit1Textedit1 => new(new TextRangeDto(51, 52, 53, 54), "new text");
        private static FileEditDto Issue2Fix2FileEdit1 => new(new FileUri("C:\\IssueFile.cs"), [Issue2Fix2FileEdit1Textedit1]);
        internal static QuickFixDto Issue2Fix2 => new([Issue2Fix2FileEdit1], "issue 2 fix 2");
        internal static Guid Issue1Id { get; } = Guid.NewGuid();
        internal static Guid Issue2Id { get; } = Guid.NewGuid();

        internal static void VerifyDtosConvertedCorrectly(List<IAnalysisIssue> result)
        {
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            VerifyIssue1ConvertedCorrectly(result[0]);
            VerifyIssue2ConvertedCorrectly(result);
        }

        internal static void VerifyIssue1ConvertedCorrectly(IAnalysisIssue issue)
        {
            issue.Id.Should().Be(Issue1Id);
            issue.RuleKey.Should().Be("ruleKey1");
            issue.IssueServerKey.Should().Be("serverKey1");
            issue.Severity.Should().Be(AnalysisIssueSeverity.Major);
            issue.Type.Should().Be(AnalysisIssueType.CodeSmell);
            issue.HighestImpact.Should().BeNull();

            issue.PrimaryLocation.FilePath.Should().Be("C:\\IssueFile.cs");
            issue.PrimaryLocation.Message.Should().Be("PrimaryMessage1");
            issue.PrimaryLocation.TextRange.StartLine.Should().Be(1);
            issue.PrimaryLocation.TextRange.StartLineOffset.Should().Be(2);
            issue.PrimaryLocation.TextRange.EndLine.Should().Be(3);
            issue.PrimaryLocation.TextRange.EndLineOffset.Should().Be(4);
            issue.PrimaryLocation.TextRange.LineHash.Should().BeNull();

            issue.Flows.Should().BeEmpty();
            issue.Fixes.Should().BeEmpty();
        }

        private static void VerifyIssue2ConvertedCorrectly(List<IAnalysisIssue> result)
        {
            result[1].Id.Should().Be(Issue2Id);
            result[1].IssueServerKey.Should().Be("serverKey2");
            result[1].RuleKey.Should().Be("ruleKey2");
            result[1].Severity.Should().BeNull();
            result[1].Type.Should().BeNull();
            result[1].HighestImpact.Severity.Should().Be(SoftwareQualitySeverity.High);

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
            var quickFix = result[1].Fixes[0].Should().BeAssignableTo<ITextBasedQuickFix>().Subject;
            quickFix.Message.Should().Be("issue 2 fix 2");
            quickFix.Edits.Should().HaveCount(1);
            quickFix.Edits[0].RangeToReplace.StartLine.Should().Be(51);
            quickFix.Edits[0].RangeToReplace.StartLineOffset.Should().Be(52);
            quickFix.Edits[0].RangeToReplace.EndLine.Should().Be(53);
            quickFix.Edits[0].RangeToReplace.EndLineOffset.Should().Be(54);
            quickFix.Edits[0].RangeToReplace.LineHash.Should().BeNull();
        }
    }
}
