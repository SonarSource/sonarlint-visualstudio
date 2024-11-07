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

using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint;

[TestClass]
public class TaintIssueToIssueVisualizationConverterTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<TaintIssueToIssueVisualizationConverter, ITaintIssueToIssueVisualizationConverter>(
            MefTestHelpers.CreateExport<IAnalysisIssueVisualizationConverter>());
    }

    [TestMethod]
    public void Convert_FromSonarQubeIssue_IssueVizConverterCalledWithCorrectParameters_ReturnsConvertedIssueVizWithReversedLocations()
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
        var issueVizConverter = Substitute.For<IAnalysisIssueVisualizationConverter>();
        issueVizConverter.Convert(Arg.Any<IAnalysisIssueBase>(), null)
            .Returns(expectedConvertedIssueViz);

        var testSubject = CreateTestSubject(issueVizConverter);
        var result = testSubject.Convert(taintDto, "C:\\root");

        result.Should().BeSameAs(expectedConvertedIssueViz);

        expectedConvertedIssueViz.Received().IsSuppressed = true;
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

    // [TestMethod]
    // [DataRow(SonarQubeIssueSeverity.Blocker, AnalysisIssueSeverity.Blocker)]
    // [DataRow(SonarQubeIssueSeverity.Critical, AnalysisIssueSeverity.Critical)]
    // [DataRow(SonarQubeIssueSeverity.Info, AnalysisIssueSeverity.Info)]
    // [DataRow(SonarQubeIssueSeverity.Major, AnalysisIssueSeverity.Major)]
    // [DataRow(SonarQubeIssueSeverity.Minor, AnalysisIssueSeverity.Minor)]
    // public void Convert_KnownSeverity_ConvertedToAnalysisIssueSeverity(SonarQubeIssueSeverity sqSeverity, AnalysisIssueSeverity expectedSeverity)
    // {
    //     var result = TaintIssueToIssueVisualizationConverter.Convert(sqSeverity);
    //
    //     result.Should().Be(expectedSeverity);
    // }
    //
    // [TestMethod]
    // public void ConvertToHighestSeverity_NullSeverities_ReturnsNull()
    // {
    //     var result = TaintIssueToIssueVisualizationConverter.ConvertToHighestSeverity(null);
    //
    //     result.Should().Be(null);
    // }
    //
    // [TestMethod]
    // public void ConvertToHighestSeverity_EmptySeverities_ReturnsNull()
    // {
    //     var result = TaintIssueToIssueVisualizationConverter.ConvertToHighestSeverity(new Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity>());
    //
    //     result.Should().Be(null);
    // }
    //
    // [DataTestMethod]
    // [DataRow(SonarQubeSoftwareQualitySeverity.High, SoftwareQualitySeverity.High)]
    // [DataRow(SonarQubeSoftwareQualitySeverity.Medium, SoftwareQualitySeverity.Medium)]
    // [DataRow(SonarQubeSoftwareQualitySeverity.Low, SoftwareQualitySeverity.Low)]
    // public void ConvertToHighestSeverity_SingleSeverity_ReturnsIt(
    //     SonarQubeSoftwareQualitySeverity sqSeverity,
    //     SoftwareQualitySeverity expectedSeverity)
    // {
    //     var impacts = new Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity> { { SonarQubeSoftwareQuality.Maintainability, sqSeverity } };
    //
    //     var result = TaintIssueToIssueVisualizationConverter.ConvertToHighestSeverity(impacts);
    //
    //     result.Should().Be(expectedSeverity);
    // }
    //
    // [TestMethod]
    // public void ConvertToHighestSeverity_InvalidSeverity_Throws()
    // {
    //     var impacts = new Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity> { { SonarQubeSoftwareQuality.Maintainability, (SonarQubeSoftwareQualitySeverity)999 } };
    //
    //     var act = () => TaintIssueToIssueVisualizationConverter.ConvertToHighestSeverity(impacts);
    //
    //     act.Should().Throw<ArgumentOutOfRangeException>();
    // }
    //
    // [DataTestMethod]
    // [DataRow(new[] { SonarQubeSoftwareQualitySeverity.Low, SonarQubeSoftwareQualitySeverity.Low, SonarQubeSoftwareQualitySeverity.Low }, SoftwareQualitySeverity.Low)]
    // [DataRow(new[] { SonarQubeSoftwareQualitySeverity.Low, SonarQubeSoftwareQualitySeverity.Medium, SonarQubeSoftwareQualitySeverity.Low }, SoftwareQualitySeverity.Medium)]
    // [DataRow(new[] { SonarQubeSoftwareQualitySeverity.High, SonarQubeSoftwareQualitySeverity.Medium, SonarQubeSoftwareQualitySeverity.Low }, SoftwareQualitySeverity.High)]
    // public void ConvertToHighestSeverity_MultipleSeverities_ReturnsHighest(SonarQubeSoftwareQualitySeverity[] sqSeverities, SoftwareQualitySeverity expectedSeverity)
    // {
    //     if (sqSeverities.Length != 3)
    //     {
    //         Assert.Fail("Wrong length of the list");
    //     }
    //
    //     var impacts = new Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity>
    //     {
    //         { SonarQubeSoftwareQuality.Maintainability, sqSeverities[0] }, { SonarQubeSoftwareQuality.Reliability, sqSeverities[1] }, { SonarQubeSoftwareQuality.Security, sqSeverities[2] },
    //     };
    //
    //     var result = TaintIssueToIssueVisualizationConverter.ConvertToHighestSeverity(impacts);
    //
    //     result.Should().Be(expectedSeverity);
    // }
    //
    // [TestMethod]
    // [DataRow(SonarQubeIssueSeverity.Unknown)]
    // [DataRow((SonarQubeIssueSeverity)1234)]
    // public void Convert_UnknownSeverity_ArgumentOutOfRangeException(SonarQubeIssueSeverity sqSeverity)
    // {
    //     Action act = () => TaintIssueToIssueVisualizationConverter.Convert(sqSeverity);
    //
    //     act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueSeverity");
    // }
    //
    // public enum OriginalIssueType
    // {
    //     SonarQubeIssue,
    //     TaintSonarQubeIssue
    // }
    //
    // [TestMethod]
    // [DataRow(OriginalIssueType.SonarQubeIssue)]
    // [DataRow(OriginalIssueType.TaintSonarQubeIssue)]
    // public void Convert_CalculatesLocalFilePaths(OriginalIssueType originalIssueType)
    // {
    //     var locationViz1 = CreateLocationViz("server-path1");
    //     var locationViz2 = CreateLocationViz("server-path2");
    //     var locationViz3 = CreateLocationViz("server-path3");
    //     var expectedIssueViz = CreateIssueViz("server-path4", locationViz1, locationViz2, locationViz3);
    //
    //     var issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();
    //     issueVizConverter
    //         .Setup(x => x.Convert(It.IsAny<IAnalysisIssueBase>(), null))
    //         .Returns(expectedIssueViz);
    //
    //     var absoluteFilePathLocator = new Mock<IAbsoluteFilePathLocator>();
    //     absoluteFilePathLocator.Setup(x => x.Locate("server-path1")).Returns("local1");
    //     absoluteFilePathLocator.Setup(x => x.Locate("server-path2")).Returns((string)null);
    //     absoluteFilePathLocator.Setup(x => x.Locate("server-path3")).Returns("local3");
    //     absoluteFilePathLocator.Setup(x => x.Locate("server-path4")).Returns("local4");
    //
    //     var testSubject = CreateTestSubject(issueVizConverter.Object, absoluteFilePathLocator.Object);
    //
    //     var result = originalIssueType == OriginalIssueType.SonarQubeIssue
    //         ? testSubject.Convert(CreateDummySonarQubeIssue())
    //         : testSubject.Convert(CreateDummyTaintSonarQubeIssue());
    //
    //     result.Should().Be(expectedIssueViz);
    //
    //     expectedIssueViz.CurrentFilePath.Should().Be("local4");
    //
    //     var secondaryLocations = expectedIssueViz.GetSecondaryLocations().ToList();
    //     secondaryLocations[0].CurrentFilePath.Should().Be("local1");
    //     secondaryLocations[1].CurrentFilePath.Should().Be(null);
    //     secondaryLocations[2].CurrentFilePath.Should().Be("local3");
    // }
    //
    // [TestMethod]
    // public void Convert_FromTaintIssue_IssueVizConverterCalledWithCorrectParameters_ReturnsConvertedIssueVizWithReversedLocations()
    // {
    //     var location1 = CreateTaintServerLocation("path1", "message1", CreateTaintTextRange(1, 2, 3, 4, "hash1"));
    //     var location2 = CreateTaintServerLocation("path2", "message2", CreateTaintTextRange(5, 6, 7, 8, "hash2"));
    //     var flow1 = CreateTaintServerFlow(location1, location2);
    //
    //     var location3 = CreateTaintServerLocation("path3", "message3", CreateTaintTextRange(9, 10, 11, 12, "hash3"));
    //     var flow2 = CreateTaintServerFlow(location3);
    //
    //     var mainLocation = CreateTaintServerLocation("path4", "message4", CreateTaintTextRange(13, 14, 15, 16, "hash4"));
    //     var creationDate = DateTimeOffset.UtcNow;
    //     var issue = CreateTaintServerIssue("issue key",
    //         "rule",
    //         creationDate,
    //         SonarQubeIssueSeverity.Major,
    //         new Dictionary<SonarQubeSoftwareQuality, SonarQubeSoftwareQualitySeverity>
    //         {
    //             { SonarQubeSoftwareQuality.Security, SonarQubeSoftwareQualitySeverity.Low }, { SonarQubeSoftwareQuality.Maintainability, SonarQubeSoftwareQualitySeverity.Medium },
    //         },
    //         mainLocation,
    //         flow1,
    //         flow2);
    //
    //     var expectedConvertedIssueViz = CreateIssueViz();
    //     var issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();
    //     issueVizConverter
    //         .Setup(x => x.Convert(It.IsAny<IAnalysisIssueBase>(), null))
    //         .Returns(expectedConvertedIssueViz);
    //
    //     var testSubject = CreateTestSubject(issueVizConverter.Object);
    //     var result = testSubject.Convert(issue);
    //
    //     result.Should().BeSameAs(expectedConvertedIssueViz);
    //
    //     issueVizConverter.Verify(x => x.Convert(
    //             It.Is((TaintIssue taintIssue) =>
    //                 taintIssue.IssueKey == "issue key" &&
    //                 taintIssue.RuleKey == "rule" &&
    //                 taintIssue.Severity == AnalysisIssueSeverity.Major &&
    //                 taintIssue.HighestSoftwareQualitySeverity == SoftwareQualitySeverity.Medium &&
    //                 taintIssue.PrimaryLocation.FilePath == "path4" &&
    //                 taintIssue.PrimaryLocation.Message == "message4" &&
    //                 taintIssue.PrimaryLocation.TextRange.LineHash == "hash4" &&
    //                 taintIssue.PrimaryLocation.TextRange.StartLine == 13 &&
    //                 taintIssue.PrimaryLocation.TextRange.EndLine == 14 &&
    //                 taintIssue.PrimaryLocation.TextRange.StartLineOffset == 15 &&
    //                 taintIssue.PrimaryLocation.TextRange.EndLineOffset == 16 &&
    //                 taintIssue.CreationTimestamp == creationDate &&
    //                 // taintIssue.LastUpdateTimestamp == default &&
    //                 taintIssue.Flows.Count == 2 &&
    //                 taintIssue.Flows[0].Locations.Count == 2 &&
    //                 taintIssue.Flows[1].Locations.Count == 1 &&
    //                 taintIssue.Flows[0].Locations[0].Message == "message2" &&
    //                 taintIssue.Flows[0].Locations[0].FilePath == "path2" &&
    //                 taintIssue.Flows[0].Locations[0].TextRange.LineHash == "hash2" &&
    //                 taintIssue.Flows[0].Locations[0].TextRange.StartLine == 5 &&
    //                 taintIssue.Flows[0].Locations[0].TextRange.EndLine == 6 &&
    //                 taintIssue.Flows[0].Locations[0].TextRange.StartLineOffset == 7 &&
    //                 taintIssue.Flows[0].Locations[0].TextRange.EndLineOffset == 8 &&
    //                 taintIssue.Flows[0].Locations[1].Message == "message1" &&
    //                 taintIssue.Flows[0].Locations[1].FilePath == "path1" &&
    //                 taintIssue.Flows[0].Locations[1].TextRange.LineHash == "hash1" &&
    //                 taintIssue.Flows[0].Locations[1].TextRange.StartLine == 1 &&
    //                 taintIssue.Flows[0].Locations[1].TextRange.EndLine == 2 &&
    //                 taintIssue.Flows[0].Locations[1].TextRange.StartLineOffset == 3 &&
    //                 taintIssue.Flows[0].Locations[1].TextRange.EndLineOffset == 4 &&
    //                 taintIssue.Flows[1].Locations[0].Message == "message3" &&
    //                 taintIssue.Flows[1].Locations[0].FilePath == "path3" &&
    //                 taintIssue.Flows[1].Locations[0].TextRange.LineHash == "hash3" &&
    //                 taintIssue.Flows[1].Locations[0].TextRange.StartLine == 9 &&
    //                 taintIssue.Flows[1].Locations[0].TextRange.EndLine == 10 &&
    //                 taintIssue.Flows[1].Locations[0].TextRange.StartLineOffset == 11 &&
    //                 taintIssue.Flows[1].Locations[0].TextRange.EndLineOffset == 12
    //             ),
    //             It.IsAny<ITextSnapshot>()),
    //         Times.Once);
    // }
    //
    // [TestMethod]
    // [DataRow(true)]
    // [DataRow(false)]
    // public void Convert_FromSonarQubeIssue_IssueVizIsCorrectlyMarkedAsSuppressed(bool isIssueSuppressed)
    // {
    //     var issue = CreateServerIssue(resolved: isIssueSuppressed, textRange: new IssueTextRange(1, 2, 3, 4));
    //
    //     var expectedConvertedIssueViz = CreateIssueViz();
    //
    //     var issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();
    //     issueVizConverter
    //         .Setup(x => x.Convert(It.IsAny<IAnalysisIssueBase>(), null))
    //         .Returns(expectedConvertedIssueViz);
    //
    //     var testSubject = CreateTestSubject(issueVizConverter.Object);
    //     var result = testSubject.Convert(issue);
    //
    //     result.Should().Be(expectedConvertedIssueViz);
    //     result.IsSuppressed.Should().Be(isIssueSuppressed);
    // }
    //
    // [TestMethod]
    // public void Convert_FromTaintIssue_IssueVizIsNotSuppressed()
    // {
    //     var taintIssue = CreateTaintServerIssue(
    //         mainLocation: CreateTaintServerLocation(
    //             textRange: CreateTaintTextRange(1, 2, 3, 4, null)));
    //
    //     var expectedConvertedIssueViz = CreateIssueViz();
    //
    //     var issueVizConverter = new Mock<IAnalysisIssueVisualizationConverter>();
    //     issueVizConverter
    //         .Setup(x => x.Convert(It.IsAny<IAnalysisIssueBase>(), null))
    //         .Returns(expectedConvertedIssueViz);
    //
    //     var testSubject = CreateTestSubject(issueVizConverter.Object);
    //     var result = testSubject.Convert(taintIssue);
    //
    //     result.Should().Be(expectedConvertedIssueViz);
    //     result.IsSuppressed.Should().BeFalse();
    // }

    private static TaintIssueToIssueVisualizationConverter CreateTestSubject(IAnalysisIssueVisualizationConverter issueVizConverter = null)
    {
        issueVizConverter ??= Substitute.For<IAnalysisIssueVisualizationConverter>();

        return new TaintIssueToIssueVisualizationConverter(issueVizConverter);
    }

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

    private static IAnalysisIssueLocationVisualization CreateLocationViz(string serverFilePath)
    {
        var locationViz = Substitute.For<IAnalysisIssueLocationVisualization>();

        var location = Substitute.For<IAnalysisIssueLocation>();
        locationViz.Location.Returns(location);
        location.FilePath.Returns(serverFilePath);

        return locationViz;
    }
}
