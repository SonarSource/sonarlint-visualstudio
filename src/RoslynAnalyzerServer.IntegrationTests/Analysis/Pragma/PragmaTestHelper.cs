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
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis.Pragma;

internal static class PragmaTestHelper
{
    internal static (SyntaxTree tree, ImmutableArray<Diagnostic> testIssues, ImmutableHashSet<string> supportedIds)
        GetPragmaDiagnosticsForMarkedSource(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var testIssues = ExtractTestIssues(tree);
        var supportedIds = ExtractSupportedIdsFromPragmas(tree);
        return (tree, testIssues, supportedIds);
    }

    internal static async Task<ImmutableArray<Diagnostic>> GetPragmaDiagnosticsAsync(
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
            .Where(d => d.Id == AdditionalRules.UnusedPragmaRuleKey)
            .ToImmutableArray();
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

    internal static Diagnostic CreateDiagnostic(string ruleId, SyntaxTree tree, TextSpan span)
    {
        var descriptor = new DiagnosticDescriptor(ruleId, "Test", "Test", "Test", DiagnosticSeverity.Warning, true);
        return Diagnostic.Create(descriptor, Location.Create(tree, span));
    }

    internal static Microsoft.CodeAnalysis.Document CreateDocument(AdhocWorkspace workspace, string source)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Default,
            "TestProject",
            "TestProject",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        workspace.AddProject(projectInfo);

        var documentInfo = DocumentInfo.Create(
            documentId,
            "Test.cs",
            loader: TextLoader.From(SourceText.From(source).Container, VersionStamp.Default));
        workspace.AddDocument(documentInfo);

        return workspace.CurrentSolution.GetDocument(documentId)!;
    }

    internal static async Task<string> ApplyCodeFixAsync(string source, Diagnostic diagnostic, CodeFixProvider codeFixProvider)
    {
        var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, source);
        var actions = new List<CodeAction>();

        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await codeFixProvider.RegisterCodeFixesAsync(context);
        actions.Should().ContainSingle();

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        foreach (var operation in operations)
        {
            operation.Apply(workspace, CancellationToken.None);
        }

        var changedDocument = workspace.CurrentSolution.GetDocument(document.Id)!;
        var text = await changedDocument.GetTextAsync();
        return text.ToString();
    }

    internal static string Normalize(string text) => text.Replace("\r\n", "\n").Trim();
}
