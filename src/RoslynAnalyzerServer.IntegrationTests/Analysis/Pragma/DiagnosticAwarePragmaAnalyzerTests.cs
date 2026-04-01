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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;
using static SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis.Pragma.PragmaTestHelper;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis.Pragma;

[TestClass]
public class DiagnosticAwarePragmaAnalyzerTests
{
    [TestMethod]
    public async Task AnalyzeSyntaxTree_NoPragmas_NoDiagnosticRaised()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            class Foo { }
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_PragmaWithMatchingDiagnostic_NoDiagnosticRaised()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234
            class Foo
            {
                //SimulateIssue:S1234
            }
            #pragma warning restore S1234
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_PragmaWithNoMatchingDiagnostic_DiagnosticRaised()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        AssertExpectedPragmaDiagnostics(results, ("S1234", 0), ("S1234", 2));
        AssertPairedPragmaProperties(results, 0, 2);
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_MultiIdPragmaWithPartialMatch_DiagnosticRaisedOnlyForUnmatchedId()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234, S5678
            class Foo
            {
                //SimulateIssue:S1234
            }
            #pragma warning restore S1234, S5678
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        AssertExpectedPragmaDiagnostics(results, ("S5678", 0), ("S5678", 5));
        AssertPairedPragmaProperties(results, 0, 5);
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_UnmatchDiagnosticNotSupported()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234, S5678
            class Foo
            {
                //SimulateIssue:S1234
            }
            #pragma warning restore S1234, S5678
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds.Remove("S5678"));

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_MultiPragmaWith_DiagnosticRaisedOnlyForUnmatchedId()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234
            #pragma warning disable S5678
            class Foo
            {
                //SimulateIssue:S1234
            }
            #pragma warning restore S1234
            #pragma warning restore S5678
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        AssertExpectedPragmaDiagnostics(results, ("S5678", 1), ("S5678", 7));
        AssertPairedPragmaProperties(results, 1, 7);
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_MultiIdPragmaWithFullMatch_DiagnosticRaisedOnlyForAll()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234, S5678
            class Foo { }
            #pragma warning restore S1234, S5678
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        AssertExpectedPragmaDiagnostics(results, ("S1234", 0), ("S5678", 0), ("S1234", 2), ("S5678", 2));
        AssertPairedPragmaProperties(results, 0, 2);
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_PragmaForUnsupportedId_NoDiagnosticRaised()
    {
        var tree = CSharpSyntaxTree.ParseText(
            """
            #pragma warning disable CS0168
            class Foo { }
            #pragma warning restore CS0168
            """);

        var results = await GetPragmaDiagnosticsAsync(
            tree,
            ImmutableArray<Diagnostic>.Empty,
            ImmutableHashSet<string>.Empty);

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_DisableWithoutRestore_DiagnosticRaised()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234
            #pragma warning disable S5678
            #pragma warning disable S1234
            class Foo { }
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        AssertExpectedPragmaDiagnostics(results, ("S1234", 0), ("S5678", 1), ("S1234", 2));
        AssertSinglePragmaProperties(results);
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_RestoreWithoutDisable_DiagnosticRaised()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            class Foo { }
            #pragma warning restore S1234
            #pragma warning restore S5678
            #pragma warning restore S1234
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        AssertExpectedPragmaDiagnostics(results, ("S1234", 1), ("S5678", 2), ("S1234", 3));
        AssertSinglePragmaProperties(results);
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_NestedPragmasSameId_InnerMatchedOuterReported()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234
            #pragma warning disable S1234
            class Foo
            {
                //SimulateIssue:S1234
            }
            #pragma warning restore S1234
            #pragma warning restore S1234
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        AssertExpectedPragmaDiagnostics(results, ("S1234", 0), ("S1234", 7));
        AssertPairedPragmaProperties(results, 0, 7);
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_DiagnosticOutsidePragmaRegion_DiagnosticRaised()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            class Bar
            {
                //SimulateIssue:S1234
            }
            #pragma warning disable S1234
            class Foo { }
            #pragma warning restore S1234
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        AssertExpectedPragmaDiagnostics(results, ("S1234", 4), ("S1234", 6));
        AssertPairedPragmaProperties(results, 4, 6);
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_MultipleDiagnosticsInRegion_NoDiagnosticRaised()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234
            class Foo
            {
                //SimulateIssue:S1234
            }
            class Bar
            {
                //SimulateIssue:S1234
            }
            #pragma warning restore S1234
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_NewRegionWithoutDiagnostic_DiagnosticRaised()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234
            class Foo
            {
                //SimulateIssue:S1234
            }
            #pragma warning restore S1234
            #pragma warning disable S1234
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        AssertExpectedPragmaDiagnostics(results, ("S1234", 6));
        AssertSinglePragmaProperties(results);
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_MoreRestoresThanDisables_DiagnosticRaised()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234
            class Foo
            {
                //SimulateIssue:S1234
            }
            #pragma warning restore S1234
            #pragma warning restore S1234
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        AssertExpectedPragmaDiagnostics(results, ("S1234", 6));
        AssertSinglePragmaProperties(results);
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_SyntaxErrors_NoDiagnosticRaised()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234
            class Foo {
            #pragma warning restore S1234
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        results.Should().BeEmpty();
    }

    private static void AssertExpectedPragmaDiagnostics(
        ImmutableArray<Diagnostic> results,
        params (string ruleId, int line)[] expected)
    {
        var actual = results.Select(d => (
            ruleId: d.Properties[DiagnosticAwarePragmaAnalyzer.ReportedDiagnosticId],
            line: d.Location.GetLineSpan().StartLinePosition.Line));

        actual.Should().BeEquivalentTo(expected);
    }

    private static void AssertPairedPragmaProperties(
        ImmutableArray<Diagnostic> results,
        int disableLine,
        int restoreLine)
    {
        foreach (var diagnostic in results)
        {
            diagnostic.AdditionalLocations.Should().HaveCount(2);
            diagnostic.AdditionalLocations[0].GetLineSpan().StartLinePosition.Line.Should().Be(disableLine);
            diagnostic.AdditionalLocations[1].GetLineSpan().StartLinePosition.Line.Should().Be(restoreLine);
            diagnostic.Properties["0"].Should().Be("#pragma warning disable");
            diagnostic.Properties["1"].Should().Be("#pragma warning restore");
        }
    }

    private static void AssertSinglePragmaProperties(ImmutableArray<Diagnostic> results)
    {
        foreach (var diagnostic in results)
        {
            diagnostic.AdditionalLocations.Should().BeEmpty();
            diagnostic.Properties.ContainsKey("0").Should().BeFalse();
            diagnostic.Properties.ContainsKey("1").Should().BeFalse();
        }
    }
}
