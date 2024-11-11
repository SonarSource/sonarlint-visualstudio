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
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using SoftwareQuality = SonarLint.VisualStudio.SLCore.Common.Models.SoftwareQuality;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint;

[TestClass]
public class TaintIssueToIssueVisualizationConverterTests
{
    private IAnalysisIssueVisualizationConverter issueVizConverter;
    private TaintIssueToIssueVisualizationConverter testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        issueVizConverter = Substitute.For<IAnalysisIssueVisualizationConverter>();
        testSubject = new TaintIssueToIssueVisualizationConverter(issueVizConverter);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<TaintIssueToIssueVisualizationConverter, ITaintIssueToIssueVisualizationConverter>(
            MefTestHelpers.CreateExport<IAnalysisIssueVisualizationConverter>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<TaintIssueToIssueVisualizationConverter>();

    [TestMethod]
    public void Convert_IssueVizConverterCalledWithCorrectParameters_ReturnsConvertedIssueVizWithReversedLocations()
    {
        var created = DateTimeOffset.Parse("2001-12-30T01:02:03+0000");
        var taintDto = new TaintVulnerabilityDto(
            Guid.Parse("efa697a2-9cfd-4faf-ba21-71b378667a81"),
            "serverkey",
            true,
            "rulekey:S123",
            "message1",
            "file\\path\\1",
            created,
            new StandardModeDetails(IssueSeverity.MINOR, RuleType.VULNERABILITY),
            [
                new TaintFlowDto(
                    [
                        new TaintLocationDto(
                            new TextRangeWithHashDto(5, 6, 7, 8, "hash2"),
                            "message2",
                            "file\\path\\2"),
                        new TaintLocationDto(
                            new TextRangeWithHashDto(9, 10, 11, 12, "hash3"),
                            "message3",
                            "file\\path\\3"),
                    ]),
                new TaintFlowDto(
                [
                    new TaintLocationDto(
                        new TextRangeWithHashDto(13, 14, 15, 16, "hash4"),
                        "message4",
                        "file\\path\\4")
                ])
            ],
            new TextRangeWithHashDto(1, 2, 3, 4, "hash1"),
            "rulecontext",
            false);

        var expectedConvertedIssueViz = CreateIssueViz();
        issueVizConverter.Convert(Arg.Any<IAnalysisIssueBase>(), null)
            .Returns(expectedConvertedIssueViz);

        var result = testSubject.Convert(taintDto, "C:\\root");

        result.Should().BeSameAs(expectedConvertedIssueViz);
        issueVizConverter.Received().Convert(
            Arg.Is((TaintIssue taintIssue) =>
                taintIssue.IssueKey == "serverkey" &&
                taintIssue.RuleKey == "rulekey:S123" &&
                taintIssue.Severity == AnalysisIssueSeverity.Minor &&
                taintIssue.HighestSoftwareQualitySeverity == null &&
                taintIssue.RuleDescriptionContextKey == "rulecontext" &&
                taintIssue.PrimaryLocation.FilePath == @"C:\root\file\path\1" &&
                taintIssue.PrimaryLocation.Message == "message1" &&
                taintIssue.PrimaryLocation.TextRange.LineHash == "hash1" &&
                taintIssue.PrimaryLocation.TextRange.StartLine == 1 &&
                taintIssue.PrimaryLocation.TextRange.EndLine == 3 &&
                taintIssue.PrimaryLocation.TextRange.StartLineOffset == 2 &&
                taintIssue.PrimaryLocation.TextRange.EndLineOffset == 4 &&
                taintIssue.CreationTimestamp == created &&
                taintIssue.Flows.Count == 2 &&
                taintIssue.Flows[0].Locations.Count == 2 &&
                taintIssue.Flows[1].Locations.Count == 1 &&
                taintIssue.Flows[0].Locations[0].Message == "message2" &&
                taintIssue.Flows[0].Locations[0].FilePath == @"C:\root\file\path\2" &&
                taintIssue.Flows[0].Locations[0].TextRange.LineHash == "hash2" &&
                taintIssue.Flows[0].Locations[0].TextRange.StartLine == 5 &&
                taintIssue.Flows[0].Locations[0].TextRange.EndLine == 7 &&
                taintIssue.Flows[0].Locations[0].TextRange.StartLineOffset == 6 &&
                taintIssue.Flows[0].Locations[0].TextRange.EndLineOffset == 8 &&
                taintIssue.Flows[0].Locations[1].Message == "message3" &&
                taintIssue.Flows[0].Locations[1].FilePath == @"C:\root\file\path\3" &&
                taintIssue.Flows[0].Locations[1].TextRange.LineHash == "hash3" &&
                taintIssue.Flows[0].Locations[1].TextRange.StartLine == 9 &&
                taintIssue.Flows[0].Locations[1].TextRange.EndLine == 11 &&
                taintIssue.Flows[0].Locations[1].TextRange.StartLineOffset == 10 &&
                taintIssue.Flows[0].Locations[1].TextRange.EndLineOffset == 12 &&
                taintIssue.Flows[1].Locations[0].Message == "message4" &&
                taintIssue.Flows[1].Locations[0].FilePath == @"C:\root\file\path\4" &&
                taintIssue.Flows[1].Locations[0].TextRange.LineHash == "hash4" &&
                taintIssue.Flows[1].Locations[0].TextRange.StartLine == 13 &&
                taintIssue.Flows[1].Locations[0].TextRange.EndLine == 15 &&
                taintIssue.Flows[1].Locations[0].TextRange.StartLineOffset == 14 &&
                taintIssue.Flows[1].Locations[0].TextRange.EndLineOffset == 16
            ));
    }

    [DataTestMethod]
    [DataRow(IssueSeverity.INFO, AnalysisIssueSeverity.Info)]
    [DataRow(IssueSeverity.MINOR, AnalysisIssueSeverity.Minor)]
    [DataRow(IssueSeverity.CRITICAL, AnalysisIssueSeverity.Critical)]
    [DataRow(IssueSeverity.MAJOR, AnalysisIssueSeverity.Major)]
    [DataRow(IssueSeverity.BLOCKER, AnalysisIssueSeverity.Blocker)]
    public void StandardSeverity_Converts(IssueSeverity slCoreSeverity, AnalysisIssueSeverity expectedSeverity)
    {
        var taintVulnerabilityDto = CreateDefaultTaintDto(new StandardModeDetails(slCoreSeverity, default));

        testSubject.Convert(taintVulnerabilityDto, "C:\\root");

        issueVizConverter.Received()
            .Convert(Arg.Is<ITaintIssue>(x => x.HighestSoftwareQualitySeverity == null && x.Severity == expectedSeverity));
    }

    [TestMethod]
    public void StandardSeverity_InvalidSeverity_Converts()
    {
        var taintVulnerabilityDto = CreateDefaultTaintDto(new StandardModeDetails((IssueSeverity)999, default));

        var act = () => testSubject.Convert(taintVulnerabilityDto, "C:\\root");

        act.Should().Throw<ArgumentException>();
        issueVizConverter.DidNotReceiveWithAnyArgs().Convert(default, default);
    }

    [TestMethod]
    public void MqrSeverity_EmptySeverities_Throws()
    {
        var taintVulnerabilityDto = CreateDefaultTaintDto(new MQRModeDetails(default, []));

        var act = () => testSubject.Convert(taintVulnerabilityDto, "C:\\root");

        act.Should().Throw<ArgumentException>();
        issueVizConverter.DidNotReceiveWithAnyArgs().Convert(default, default);
    }

    [DataTestMethod]
    [DataRow(ImpactSeverity.BLOCKER, SoftwareQualitySeverity.Blocker)]
    [DataRow(ImpactSeverity.HIGH, SoftwareQualitySeverity.High)]
    [DataRow(ImpactSeverity.MEDIUM, SoftwareQualitySeverity.Medium)]
    [DataRow(ImpactSeverity.LOW, SoftwareQualitySeverity.Low)]
    [DataRow(ImpactSeverity.INFO, SoftwareQualitySeverity.Info)]
    public void MqrSeverity_SingleSeverity_ReturnsIt(
        ImpactSeverity slCoreSeverity,
        SoftwareQualitySeverity expectedSeverity)
    {
        var taintVulnerabilityDto = CreateDefaultTaintDto(new MQRModeDetails(default, [new ImpactDto(SoftwareQuality.SECURITY, slCoreSeverity)]));

        testSubject.Convert(taintVulnerabilityDto, "C:\\root");

        issueVizConverter.Received()
            .Convert(Arg.Is<ITaintIssue>(x => x.HighestSoftwareQualitySeverity == expectedSeverity && x.Severity == null));
    }

    [TestMethod]
    public void MqrSeverity_InvalidSeverity_Throws()
    {
        var taintVulnerabilityDto = CreateDefaultTaintDto(new MQRModeDetails(default, [new ImpactDto(SoftwareQuality.SECURITY, (ImpactSeverity)999)]));

        var act = () => testSubject.Convert(taintVulnerabilityDto, "C:\\root");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [DataTestMethod]
    [DataRow(new[] { ImpactSeverity.LOW, ImpactSeverity.LOW, ImpactSeverity.LOW }, SoftwareQualitySeverity.Low)]
    [DataRow(new[] { ImpactSeverity.LOW, ImpactSeverity.LOW, ImpactSeverity.INFO }, SoftwareQualitySeverity.Low)]
    [DataRow(new[] { ImpactSeverity.LOW, ImpactSeverity.MEDIUM, ImpactSeverity.LOW }, SoftwareQualitySeverity.Medium)]
    [DataRow(new[] { ImpactSeverity.HIGH, ImpactSeverity.MEDIUM, ImpactSeverity.LOW }, SoftwareQualitySeverity.High)]
    [DataRow(new[] { ImpactSeverity.MEDIUM, ImpactSeverity.BLOCKER, ImpactSeverity.HIGH }, SoftwareQualitySeverity.Blocker)]
    public void MqrSeverity_MultipleSeverities_ReturnsHighest(ImpactSeverity[] slCoreSeverities, SoftwareQualitySeverity expectedSeverity)
    {
        var qualities = Enum.GetValues(typeof(SoftwareQuality)).Cast<SoftwareQuality>().ToArray();

        if (slCoreSeverities.Length != qualities.Length)
        {
            Assert.Fail("Wrong length of the list");
        }

        var taintVulnerabilityDto = CreateDefaultTaintDto(new MQRModeDetails(default, qualities.Zip(slCoreSeverities, (x, y) => new ImpactDto(x, y)).ToList()));

        testSubject.Convert(taintVulnerabilityDto, "C:\\root");

        issueVizConverter.Received()
            .Convert(Arg.Is<ITaintIssue>(x => x.HighestSoftwareQualitySeverity == expectedSeverity && x.Severity == null));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Convert_IssueVizIsCorrectlyMarkedAsSuppressed(bool isIssueSuppressed)
    {
        var taintVulnerabilityDto = CreateDefaultTaintDto(resolved: isIssueSuppressed);
        var expectedConvertedIssueViz = CreateIssueViz();
        issueVizConverter.Convert(Arg.Any<IAnalysisIssueBase>(), null)
            .Returns(expectedConvertedIssueViz);

        testSubject.Convert(taintVulnerabilityDto, "C:\\root");

        expectedConvertedIssueViz.Received().IsSuppressed = isIssueSuppressed;
    }

    private static TaintVulnerabilityDto CreateDefaultTaintDto(Either<StandardModeDetails, MQRModeDetails> severity = null, bool resolved = true) =>
        new(
            Guid.Parse("efa697a2-9cfd-4faf-ba21-71b378667a81"),
            "serverkey",
            resolved,
            "rulekey:S123",
            "message1",
            "file\\path\\1",
            DateTimeOffset.Now,
            severity ?? new StandardModeDetails(IssueSeverity.MINOR, RuleType.VULNERABILITY),
            [
                new TaintFlowDto(
                [
                    new TaintLocationDto(
                        new TextRangeWithHashDto(5, 6, 7, 8, "hash2"),
                        "message2",
                        "file\\path\\2"),
                    new TaintLocationDto(
                        new TextRangeWithHashDto(9, 10, 11, 12, "hash3"),
                        "message3",
                        "file\\path\\3"),
                ]),
                new TaintFlowDto(
                [
                    new TaintLocationDto(
                        new TextRangeWithHashDto(13, 14, 15, 16, "hash4"),
                        "message4",
                        "file\\path\\4")
                ])
            ],
            new TextRangeWithHashDto(1, 2, 3, 4, "hash1"),
            "rulecontext",
            false);

    private static IAnalysisIssueVisualization CreateIssueViz(string serverFilePath = null, params IAnalysisIssueLocationVisualization[] locationVizs)
    {
        var issueViz = Substitute.For<IAnalysisIssueVisualization>();

        var flowViz = Substitute.For<IAnalysisIssueFlowVisualization>();
        flowViz.Locations.Returns(locationVizs);

        issueViz.Flows.Returns([flowViz]);

        var location = Substitute.For<IAnalysisIssueLocation>();
        issueViz.Location.Returns(location);
        location.FilePath.Returns(serverFilePath);

        return issueViz;
    }
}
