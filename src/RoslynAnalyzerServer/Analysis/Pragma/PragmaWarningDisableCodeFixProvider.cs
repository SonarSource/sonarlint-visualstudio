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
            var node = root.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
            if (node is not IdentifierNameSyntax identifier)
            {
                continue;
            }

            if (identifier.Parent is not PragmaWarningDirectiveTriviaSyntax)
            {
                continue;
            }

            var title = Resources.PD0001CodeFixTitle;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
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
        var locations = new List<Location> { diagnostic.Location };
        locations.AddRange(diagnostic.AdditionalLocations);
        locations.Sort((a, b) => b.SourceSpan.Start.CompareTo(a.SourceSpan.Start));

        var currentRoot = root;
        foreach (var loc in locations)
        {
            var node = currentRoot.FindNode(loc.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
            if (node is not IdentifierNameSyntax identifier)
            {
                continue;
            }
            if (identifier.Parent is not PragmaWarningDirectiveTriviaSyntax pragma)
            {
                continue;
            }

            currentRoot = pragma.ErrorCodes.Count == 1
                ? RemoveEntirePragmaDirective(currentRoot, pragma)
                : RemoveIdentifierFromList(currentRoot, pragma, identifier);
        }

        return Task.FromResult(document.WithSyntaxRoot(currentRoot));
    }

    private static SyntaxNode RemoveEntirePragmaDirective(SyntaxNode root, PragmaWarningDirectiveTriviaSyntax pragma)
    {
        var parentTrivia = pragma.ParentTrivia;
        var token = parentTrivia.Token;
        var leadingTrivia = token.LeadingTrivia;

        var index = leadingTrivia.IndexOf(parentTrivia);
        if (index < 0)
        {
            return root;
        }

        var newTriviaList = new SyntaxTriviaList();
        for (var i = 0; i < leadingTrivia.Count; i++)
        {
            if (i == index)
            {
                // Skip the pragma trivia and its trailing EndOfLine
                if (i + 1 < leadingTrivia.Count && leadingTrivia[i + 1].IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    i++; // skip the EOL too
                }
                continue;
            }

            newTriviaList = newTriviaList.Add(leadingTrivia[i]);
        }

        return root.ReplaceToken(token, token.WithLeadingTrivia(newTriviaList));
    }

    private static SyntaxNode RemoveIdentifierFromList(
        SyntaxNode root,
        PragmaWarningDirectiveTriviaSyntax pragma,
        IdentifierNameSyntax identifier)
    {
        var errorCodes = pragma.ErrorCodes;
        var index = -1;
        for (var i = 0; i < errorCodes.Count; i++)
        {
            if (errorCodes[i] == identifier)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return root;
        }

        var newErrorCodes = SyntaxFactory.SeparatedList<ExpressionSyntax>(
            errorCodes.Where((_, i) => i != index),
            errorCodes.GetSeparators().Where((_, i) => i != (index == 0 ? 0 : index - 1)));

        // Preserve trailing trivia on the last item
        if (newErrorCodes.Count > 0)
        {
            var lastItem = newErrorCodes.Last();
            var originalLastItem = errorCodes[errorCodes.Count - 1];
            if (lastItem != originalLastItem)
            {
                // The last item changed, transfer trailing trivia from the original last item
                newErrorCodes = newErrorCodes.Replace(
                    newErrorCodes.Last(),
                    newErrorCodes.Last().WithTrailingTrivia(originalLastItem.GetTrailingTrivia()));
            }
        }

        var newPragma = pragma.WithErrorCodes(newErrorCodes);
        var oldTrivia = pragma.ParentTrivia;
        var newTrivia = SyntaxFactory.Trivia(newPragma);

        return root.ReplaceTrivia(oldTrivia, newTrivia);
    }
}
