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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis.Pragma;

[TestClass]
public class DiagnosticAwarePragmaAnalyzerTests
{
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

        results.Should().HaveCount(2);
        results.Select(x => x.Properties[DiagnosticAwarePragmaAnalyzer.ReportedDiagnosticId]).Should().OnlyContain(y => y == "S1234");
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_MultiIdPragmaWithPartialMatch_DiagnosticRaisedOnlyForUnmatchedId()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234, S5678
            class Foo { //SimulateIssue:S1234 }
            #pragma warning restore S1234, S5678
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        results.Should().HaveCount(2);
        results.Select(x => x.Properties[DiagnosticAwarePragmaAnalyzer.ReportedDiagnosticId]).Should().OnlyContain(y => y == "S5678");
    }

    [TestMethod]
    public async Task AnalyzeSyntaxTree_MultiPragmaWith_DiagnosticRaisedOnlyForUnmatchedId()
    {
        var (tree, testIssues, supportedIds) = GetPragmaDiagnosticsForMarkedSource(
            """
            #pragma warning disable S1234
            #pragma warning disable S5678
            class Foo { //SimulateIssue:S1234 }
            #pragma warning restore S1234
            #pragma warning restore S5678
            """);

        var results = await GetPragmaDiagnosticsAsync(tree, testIssues, supportedIds);

        results.Should().HaveCount(2);
        results.Select(x => x.Properties[DiagnosticAwarePragmaAnalyzer.ReportedDiagnosticId]).Should().OnlyContain(y => y == "S5678");
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

        results.Should().HaveCount(4);
        results.Select(x => x.Properties[DiagnosticAwarePragmaAnalyzer.ReportedDiagnosticId]).Should().OnlyContain(y =>
            y == "S1234" ||  y == "S5678");
    }

    private static ImmutableArray<Diagnostic> ExtractTestIssues(SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        foreach (var trivia in root.DescendantTrivia())
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                continue;
            }

            var text = trivia.ToString();
            const string prefix = "//SimulateIssue:";
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var ruleId = text.Substring(prefix.Length).Split(' ')[0];
                diagnostics.Add(CreateDiagnostic(ruleId, tree, trivia.Span));
            }
        }

        return diagnostics.ToImmutable();
    }

    private static ImmutableHashSet<string> ExtractSupportedIdsFromPragmas(SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var ids = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in root.DescendantNodes(descendIntoTrivia: true))
        {
            if (node is PragmaWarningDirectiveTriviaSyntax pragma)
            {
                foreach (var errorCode in pragma.ErrorCodes)
                {
                    if (errorCode is IdentifierNameSyntax identifier)
                    {
                        ids.Add(identifier.Identifier.Text);
                    }
                }
            }
        }

        return ids.ToImmutable();
    }

    private static (SyntaxTree tree, ImmutableArray<Diagnostic> testIssues, ImmutableHashSet<string> supportedIds) GetPragmaDiagnosticsForMarkedSource(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var testIssues = ExtractTestIssues(tree);
        var supportedIds = ExtractSupportedIdsFromPragmas(tree);
        return (tree, testIssues, supportedIds);
    }

    private static Diagnostic CreateDiagnostic(string ruleId, SyntaxTree tree, TextSpan span)
    {
        var descriptor = new DiagnosticDescriptor(ruleId, "Test", "Test", "Test", DiagnosticSeverity.Warning, true);
        return Diagnostic.Create(descriptor, Location.Create(tree, span));
    }

    private static async Task<ImmutableArray<Diagnostic>> GetPragmaDiagnosticsAsync(
        SyntaxTree tree,
        ImmutableArray<Diagnostic> knownDiagnostics,
        ImmutableHashSet<string> supportedIds)
    {
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new DiagnosticAwarePragmaAnalyzer(() => knownDiagnostics, supportedIds);
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        var allDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return allDiagnostics
            .Where(d => d.Id == DiagnosticAwarePragmaAnalyzer.DiagnosticId)
            .ToImmutableArray();
    }
}
