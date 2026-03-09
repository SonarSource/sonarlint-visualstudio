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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PragmaWarningDisableCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticAwarePragmaAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (FindPragmaIdentifier(root, diagnostic.Location.SourceSpan) == null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    Resources.SQVSPRAGMACodeFixTitle,
                    _ => RemoveIdentifierFromPragmaAsync(context.Document, root, diagnostic),
                    equivalenceKey: DiagnosticAwarePragmaAnalyzer.DiagnosticId),
                diagnostic);
        }
    }

    private static Task<Document> RemoveIdentifierFromPragmaAsync(
        Document document,
        SyntaxNode root,
        Diagnostic diagnostic)
    {
        var allLocations = new List<Location> { diagnostic.Location };
        allLocations.AddRange(diagnostic.AdditionalLocations);

        var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

        foreach (var loc in allLocations)
        {
            var found = FindPragmaIdentifier(root, loc.SourceSpan);
            if (found == null)
            {
                continue;
            }
            var (identifier, pragma) = found.Value;

            if (pragma.ErrorCodes.Count == 1)
            {
                editor.RemoveNode(pragma);
            }
            else
            {
                editor.RemoveNode(identifier);
            }
        }

        return Task.FromResult(document.WithSyntaxRoot(editor.GetChangedRoot()));
    }

    private static (IdentifierNameSyntax, PragmaWarningDirectiveTriviaSyntax)? FindPragmaIdentifier(
        SyntaxNode root, TextSpan span)
    {
        var node = root.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true);
        if (node is not IdentifierNameSyntax identifier)
        {
            return null;
        }

        if (identifier.Parent is not PragmaWarningDirectiveTriviaSyntax pragma)
        {
            return null;
        }

        return (identifier, pragma);
    }
}
