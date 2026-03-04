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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class DiagnosticToAnalysisIssueConverterTests
{
    private readonly DiagnosticToAnalysisIssueConverter testSubject = new();

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<DiagnosticToAnalysisIssueConverter, IDiagnosticToAnalysisIssueConverter>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<DiagnosticToAnalysisIssueConverter>();

    [TestMethod]
    public void Convert_SetsRuleKeyWithCsharpsquidPrefix()
    {
        var diagnostic = CreateDiagnostic("S1234", "message", @"c:\test.cs", 0, 0, 0, 0);

        var result = testSubject.Convert(diagnostic, []);

        result.RuleKey.Should().Be("csharpsquid:S1234");
    }

    [TestMethod]
    public void Convert_PrimaryLocation_OneBasedLinesAndZeroBasedOffsets()
    {
        var diagnostic = CreateDiagnostic("S1", "msg", @"c:\test.cs", 5, 5, 3, 10);

        var result = testSubject.Convert(diagnostic, []);

        result.PrimaryLocation.TextRange.StartLine.Should().Be(6);
        result.PrimaryLocation.TextRange.EndLine.Should().Be(6);
        result.PrimaryLocation.TextRange.StartLineOffset.Should().Be(3);
        result.PrimaryLocation.TextRange.EndLineOffset.Should().Be(10);
    }

    [TestMethod]
    public void Convert_PrimaryLocation_HasCorrectFilePath()
    {
        var diagnostic = CreateDiagnostic("S1", "msg", @"c:\test\file.cs", 0, 0, 0, 0);

        var result = testSubject.Convert(diagnostic, []);

        result.PrimaryLocation.FilePath.Should().Be(@"c:\test\file.cs");
    }

    [TestMethod]
    public void Convert_PrimaryLocation_HasCorrectMessage()
    {
        var diagnostic = CreateDiagnostic("S1", "expected message", @"c:\test.cs", 0, 0, 0, 0);

        var result = testSubject.Convert(diagnostic, []);

        result.PrimaryLocation.Message.Should().Be("expected message");
    }

    [TestMethod]
    public void Convert_HighestImpact_IsMaintainabilityLow()
    {
        var diagnostic = CreateDiagnostic("S1", "msg", @"c:\test.cs", 0, 0, 0, 0);

        var result = testSubject.Convert(diagnostic, []);

        result.HighestImpact.Quality.Should().Be(SoftwareQuality.Maintainability);
        result.HighestImpact.Severity.Should().Be(SoftwareQualitySeverity.Low);
    }

    [TestMethod]
    public void Convert_NoAdditionalLocations_FlowsIsEmpty()
    {
        var diagnostic = CreateDiagnostic("S1", "msg", @"c:\test.cs", 0, 0, 0, 0);

        var result = testSubject.Convert(diagnostic, []);

        result.Flows.Should().BeEmpty();
    }

    [TestMethod]
    public void Convert_WithAdditionalLocations_CreatesSingleFlowWithLocations()
    {
        var primaryLocation = CreateLocation(@"c:\test.cs", 0, 0, 0, 0);
        var additionalLocations = new[]
        {
            CreateLocation(@"c:\test.cs", 10, 10, 5, 15),
            CreateLocation(@"c:\other.cs", 20, 20, 0, 10)
        };
        var diagnostic = CreateDiagnosticWithLocations("S1", "msg", primaryLocation, additionalLocations);

        var result = testSubject.Convert(diagnostic, []);

        result.Flows.Should().ContainSingle();
        result.Flows[0].Locations.Should().HaveCount(2);
    }

    [TestMethod]
    public void Convert_WithQuickFixes_FixesIncluded()
    {
        var diagnostic = CreateDiagnostic("S1", "msg", @"c:\test.cs", 0, 0, 0, 0);
        var quickFixes = new List<RoslynQuickFix> { new(Guid.NewGuid()), new(Guid.NewGuid()) };

        var result = testSubject.Convert(diagnostic, quickFixes);

        result.Fixes.Should().HaveCount(2);
    }

    [TestMethod]
    public void Convert_NoQuickFixes_FixesEmpty()
    {
        var diagnostic = CreateDiagnostic("S1", "msg", @"c:\test.cs", 0, 0, 0, 0);

        var result = testSubject.Convert(diagnostic, []);

        result.Fixes.Should().BeEmpty();
    }

    private static Diagnostic CreateDiagnostic(
        string id,
        string message,
        string filePath,
        int startLine,
        int endLine,
        int startChar,
        int endChar)
    {
        var location = CreateLocation(filePath, startLine, endLine, startChar, endChar);
        return CreateDiagnosticWithLocations(id, message, location);
    }

    private static Diagnostic CreateDiagnosticWithLocations(
        string id,
        string message,
        Location location,
        Location[] additionalLocations = null)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            "Any Title",
            message,
            "Any Category",
            default,
            default);

        return Diagnostic.Create(descriptor, location, additionalLocations: additionalLocations);
    }

    private static Location CreateLocation(
        string filePath,
        int startLine,
        int endLine,
        int startChar,
        int endChar) =>
        Location.Create(
            filePath,
            new TextSpan(0, 1),
            new LinePositionSpan(
                new LinePosition(startLine, startChar),
                new LinePosition(endLine, endChar)));
}
