/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using NSubstitute;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using SoftwareQuality = SonarLint.VisualStudio.Core.Analysis.SoftwareQuality;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class RoslynIssueToAnalysisIssueConverterTests
{
    private readonly ISonarLintSettings sonarLintSettings = Substitute.For<ISonarLintSettings>();
    private RoslynIssueToAnalysisIssueConverter testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.Warn);
        testSubject = new RoslynIssueToAnalysisIssueConverter(sonarLintSettings);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynIssueToAnalysisIssueConverter, IDiagnosticToAnalysisIssueConverter>(
            MefTestHelpers.CreateExport<ISonarLintSettings>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<RoslynIssueToAnalysisIssueConverter>();

    [TestMethod]
    public void Convert_SetsRuleKeyFromRoslynIssue()
    {
        var roslynIssue = CreateRoslynIssue("csharpsquid:S1234", "message", @"C:\test.cs", 1, 1, 0, 0);

        var result = testSubject.Convert(roslynIssue);

        result.RuleKey.Should().Be("csharpsquid:S1234");
    }

    [TestMethod]
    public void Convert_PrimaryLocation_PreservesTextRange()
    {
        var roslynIssue = CreateRoslynIssue("csharpsquid:S1", "msg", @"C:\test.cs", 6, 6, 3, 10);

        var result = testSubject.Convert(roslynIssue);

        result.PrimaryLocation.TextRange.StartLine.Should().Be(6);
        result.PrimaryLocation.TextRange.EndLine.Should().Be(6);
        result.PrimaryLocation.TextRange.StartLineOffset.Should().Be(3);
        result.PrimaryLocation.TextRange.EndLineOffset.Should().Be(10);
    }

    [TestMethod]
    public void Convert_PrimaryLocation_HasCorrectFilePath()
    {
        var roslynIssue = CreateRoslynIssue("csharpsquid:S1", "msg", @"C:\test\file.cs", 1, 1, 0, 0);

        var result = testSubject.Convert(roslynIssue);

        result.PrimaryLocation.FilePath.Should().Be(@"C:\test\file.cs");
    }

    [TestMethod]
    public void Convert_PrimaryLocation_HasCorrectMessage()
    {
        var roslynIssue = CreateRoslynIssue("csharpsquid:S1", "expected message", @"C:\test.cs", 1, 1, 0, 0);

        var result = testSubject.Convert(roslynIssue);

        result.PrimaryLocation.Message.Should().Be("expected message");
    }

    [TestMethod]
    public void Convert_HighestImpact_QualityIsMaintainability()
    {
        var roslynIssue = CreateRoslynIssue("csharpsquid:S1", "msg", @"C:\test.cs", 1, 1, 0, 0);

        var result = testSubject.Convert(roslynIssue);

        result.HighestImpact.Quality.Should().Be(SoftwareQuality.Maintainability);
    }

    [TestMethod]
    public void Convert_PragmaRuleSeverityWarn_ImpactSeverityIsMedium()
    {
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.Warn);
        var roslynIssue = CreateRoslynIssue("csharpsquid:S1", "msg", @"C:\test.cs", 1, 1, 0, 0);

        var result = testSubject.Convert(roslynIssue);

        result.HighestImpact.Severity.Should().Be(SoftwareQualitySeverity.Medium);
    }

    [TestMethod]
    public void Convert_PragmaRuleSeverityInfo_ImpactSeverityIsLow()
    {
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.Info);
        var roslynIssue = CreateRoslynIssue("csharpsquid:S1", "msg", @"C:\test.cs", 1, 1, 0, 0);

        var result = testSubject.Convert(roslynIssue);

        result.HighestImpact.Severity.Should().Be(SoftwareQualitySeverity.Low);
    }

    [TestMethod]
    public void Convert_PragmaRuleSeverityNone_ImpactSeverityDefaultsToLow()
    {
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.None);
        var roslynIssue = CreateRoslynIssue("csharpsquid:S1", "msg", @"C:\test.cs", 1, 1, 0, 0);

        var result = testSubject.Convert(roslynIssue);

        result.HighestImpact.Severity.Should().Be(SoftwareQualitySeverity.Low);
    }

    [TestMethod]
    public void Convert_NoFlows_FlowsIsEmpty()
    {
        var roslynIssue = CreateRoslynIssue("csharpsquid:S1", "msg", @"C:\test.cs", 1, 1, 0, 0);

        var result = testSubject.Convert(roslynIssue);

        result.Flows.Should().BeEmpty();
    }

    [TestMethod]
    public void Convert_WithFlows_CreatesFlowsWithLocations()
    {
        var flowLocations = new List<RoslynIssueLocation>
        {
            new("secondary1", new FileUri(@"C:\test.cs"), new RoslynIssueTextRange(11, 11, 5, 15)),
            new("secondary2", new FileUri(@"C:\other.cs"), new RoslynIssueTextRange(21, 21, 0, 10))
        };
        var flow = new RoslynIssueFlow(flowLocations);
        var primaryLocation = new RoslynIssueLocation("msg", new FileUri(@"C:\test.cs"), new RoslynIssueTextRange(1, 1, 0, 0));
        var roslynIssue = new RoslynIssue("csharpsquid:S1", primaryLocation, [flow]);

        var result = testSubject.Convert(roslynIssue);

        result.Flows.Should().ContainSingle();
        result.Flows[0].Locations.Should().HaveCount(2);
        result.Flows[0].Locations[0].FilePath.Should().Be(@"C:\test.cs");
        result.Flows[0].Locations[0].TextRange.StartLine.Should().Be(11);
        result.Flows[0].Locations[1].FilePath.Should().Be(@"C:\other.cs");
        result.Flows[0].Locations[1].TextRange.StartLine.Should().Be(21);
    }

    [TestMethod]
    public void Convert_WithQuickFixes_ParsedViaRoundTrip()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var quickFixes = new List<RoslynIssueQuickFix>
        {
            new(new RoslynQuickFix(id1).GetStorageValue()),
            new(new RoslynQuickFix(id2).GetStorageValue())
        };
        var primaryLocation = new RoslynIssueLocation("msg", new FileUri(@"C:\test.cs"), new RoslynIssueTextRange(1, 1, 0, 0));
        var roslynIssue = new RoslynIssue("csharpsquid:S1", primaryLocation, quickFixes: quickFixes);

        var result = testSubject.Convert(roslynIssue);

        result.Fixes.Should().HaveCount(2);
        result.Fixes[0].Should().BeOfType<RoslynQuickFix>().Which.Id.Should().Be(id1);
        result.Fixes[1].Should().BeOfType<RoslynQuickFix>().Which.Id.Should().Be(id2);
    }

    [TestMethod]
    public void Convert_NoQuickFixes_FixesEmpty()
    {
        var roslynIssue = CreateRoslynIssue("csharpsquid:S1", "msg", @"C:\test.cs", 1, 1, 0, 0);

        var result = testSubject.Convert(roslynIssue);

        result.Fixes.Should().BeEmpty();
    }

    [TestMethod]
    public void Convert_QuickFixWithInvalidStorageValue_IsSkipped()
    {
        var validId = Guid.NewGuid();
        var quickFixes = new List<RoslynIssueQuickFix>
        {
            new("invalid-storage-value"),
            new(new RoslynQuickFix(validId).GetStorageValue())
        };
        var primaryLocation = new RoslynIssueLocation("msg", new FileUri(@"C:\test.cs"), new RoslynIssueTextRange(1, 1, 0, 0));
        var roslynIssue = new RoslynIssue("csharpsquid:S1", primaryLocation, quickFixes: quickFixes);

        var result = testSubject.Convert(roslynIssue);

        result.Fixes.Should().ContainSingle();
        result.Fixes[0].Should().BeOfType<RoslynQuickFix>().Which.Id.Should().Be(validId);
    }

    private static RoslynIssue CreateRoslynIssue(
        string ruleId,
        string message,
        string filePath,
        int startLine,
        int endLine,
        int startLineOffset,
        int endLineOffset)
    {
        var textRange = new RoslynIssueTextRange(startLine, endLine, startLineOffset, endLineOffset);
        var location = new RoslynIssueLocation(message, new FileUri(filePath), textRange);
        return new RoslynIssue(ruleId, location);
    }
}
