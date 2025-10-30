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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class DiagnosticToRoslynIssueConverterTests
{
    private static readonly ImmutableDictionary<string, string?> SecondaryLocationMessages =
        new Dictionary<string, string?> { { "0", "First secondary message" }, { "1", "Second secondary message" } }.ToImmutableDictionary();
    private static readonly ImmutableDictionary<string, string?>? NoSecondaryLocationMessages = null;

    private readonly DiagnosticToRoslynIssueConverter testSubject = new();

    [TestMethod]
    public void MefCtor_CheckExports() => MefTestHelpers.CheckTypeCanBeImported<DiagnosticToRoslynIssueConverter, IDiagnosticToRoslynIssueConverter>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<DiagnosticToRoslynIssueConverter>();

    public static object[][] TestData =>
    [
        [Language.CSharp, "S1234", "test message", "c:\\test\\file.cs", 0, 0, 3, 4],
        [Language.CSharp, "S1234", "test message", "c:\\test\\file.cs", 1, 1, 3, 4],
        [Language.CSharp, "S5678", "multi-line issue", "c:\\test\\file2.cs", 5, 10, 15, 20],
        [Language.VBNET, "S1234", "test message", "c:\\test\\file.vb", 0, 0, 3, 4]
    ];

    [DataTestMethod]
    [DynamicData(nameof(TestData))]
    public void ConvertToSonarDiagnostic_ConvertsDiagnosticCorrectly(
        Language language,
        string ruleId,
        string message,
        string fileName,
        int startLine,
        int endLine,
        int startChar,
        int endChar)
    {
        var fileUri = new FileUri(fileName);
        var location = CreateLocation(fileUri, startLine, endLine, startChar, endChar);
        var diagnostic = CreateDiagnostic(ruleId, message, location);
        var expectedTextRange = new RoslynIssueTextRange(
            startLine + 1, // Convert to 1-based
            endLine + 1, // Convert to 1-based
            startChar,
            endChar);
        var expectedLocation = new RoslynIssueLocation(
            message,
            fileUri,
            expectedTextRange);
        var expectedRuleId = $"{language.RepoInfo.Key}:{ruleId}";
        var expectedDiagnostic = new RoslynIssue(
            expectedRuleId,
            expectedLocation);

        var result = testSubject.ConvertToSonarDiagnostic(diagnostic, [], language);

        result.Should().BeEquivalentTo(expectedDiagnostic);
    }

    public static object?[][] SecondaryLocationTestData =>
    [
        [
            SecondaryLocationMessages,
            SecondaryLocationMessages.OrderBy(x => x.Key).Select(x => x.Value).ToArray()
        ],
        [
            NoSecondaryLocationMessages,
            new[] { "Location 0", "Location 1" }
        ]
    ];

    [DataTestMethod]
    [DynamicData(nameof(SecondaryLocationTestData))]
    public void ConvertToSonarDiagnostic_WithSecondaryLocations_ConvertsCorrectly(
        ImmutableDictionary<string, string?>? properties,
        string[] expectedMessages)
    {
        const string fileCs = "c:\\test\\file.cs";
        const string file2Cs = "c:\\test\\file2.cs";
        var primaryLocation = CreateLocation(new FileUri(fileCs), 5, 5, 10, 15);
        var additionalLocations = new[] { CreateLocation(new FileUri(fileCs), 10, 10, 20, 25), CreateLocation(new FileUri(file2Cs), 15, 15, 30, 35) };
        var diagnostic = CreateDiagnostic("any", "any", primaryLocation, additionalLocations, properties);
        var expectedFlows = new[]
        {
            new RoslynIssueFlow(new List<RoslynIssueLocation>
            {
                new(
                    expectedMessages[0],
                    new FileUri(fileCs),
                    new RoslynIssueTextRange(11, 11, 20, 25)),
                new(
                    expectedMessages[1],
                    new FileUri(file2Cs),
                    new RoslynIssueTextRange(16, 16, 30, 35))
            })
        };

        var result = testSubject.ConvertToSonarDiagnostic(diagnostic, [], Language.CSharp);

        result.Flows.Should().BeEquivalentTo(expectedFlows);
    }

    [TestMethod]
    public void ConvertToSonarDiagnostic_WithQuickFixes_ConvertsCorrectly()
    {
        var diagnostic = CreateDiagnostic("any", "any", CreateLocation(new FileUri("file:///C:/any.cs"), 0, 0, 0, 0));
        var quickFix1 = new RoslynQuickFix(Guid.NewGuid());
        var quickFix2 = new RoslynQuickFix(Guid.NewGuid());

        var result = testSubject.ConvertToSonarDiagnostic(diagnostic, [quickFix1, quickFix2], Language.CSharp);

        result.QuickFixes.Should().BeEquivalentTo([new RoslynIssueQuickFix(quickFix1.GetStorageValue()), new RoslynIssueQuickFix(quickFix2.GetStorageValue())],
            options => options.ComparingByMembers<RoslynIssueQuickFix>());
    }

    [TestMethod]
    public void ConvertToSonarDiagnostic_WithNoQuickFixes_ReturnsEmptyQuickFixesList()
    {
        var diagnostic = CreateDiagnostic("any", "any", CreateLocation(new FileUri("file:///C:/any.cs"), 0, 0, 0, 0));

        var result = testSubject.ConvertToSonarDiagnostic(diagnostic, [], Language.CSharp);

        result.QuickFixes.Should().BeEmpty();
    }

    private static Location CreateLocation(
        FileUri fileUri,
        int startLine,
        int endLine,
        int startChar,
        int endChar)
    {
        var textSpan = new TextSpan(12, 34);
        var syntaxTree = Substitute.For<SyntaxTree>();
        var linePositionSpan = new LinePositionSpan(
            new LinePosition(startLine, startChar),
            new LinePosition(endLine, endChar));
        syntaxTree.GetMappedLineSpan(textSpan, CancellationToken.None).Returns(new FileLinePositionSpan(fileUri.LocalPath, linePositionSpan));

        return Location.Create(syntaxTree, textSpan);
    }

    private static Diagnostic CreateDiagnostic(
        string id,
        string message,
        Location location,
        Location[]? additionalLocations = null,
        ImmutableDictionary<string, string?>? properties = null)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            "Any Title",
            message,
            "Any Category",
            default,
            default);

        return Diagnostic.Create(descriptor, location, additionalLocations: additionalLocations, properties: properties ?? ImmutableDictionary<string, string?>.Empty);
    }
}
