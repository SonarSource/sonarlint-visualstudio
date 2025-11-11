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
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

internal interface ITypeReferenceFinder
{
    Task<HashSet<IRoslynDocumentWrapper>> GetCrossFileReferencesInScopeAsync(
        IRoslynDocumentWrapper sourceDocument,
        IEnumerable<IRoslynDocumentWrapper> documentsToSearch,
        IRoslynSolutionWrapper solution,
        CancellationToken cancellationToken);
}

[Export(typeof(ITypeReferenceFinder))]
[PartCreationPolicy(CreationPolicy.Shared)]
[ExcludeFromCodeCoverage]
internal class TypeReferenceFinder : ITypeReferenceFinder
{
    public async Task<HashSet<IRoslynDocumentWrapper>> GetCrossFileReferencesInScopeAsync(
        IRoslynDocumentWrapper sourceDocument,
        IEnumerable<IRoslynDocumentWrapper> documentsToSearch,
        IRoslynSolutionWrapper solution,
        CancellationToken cancellationToken)
    {
        var (model, root) = await GetModelAndRootAsync(sourceDocument, cancellationToken);
        if (model is null || root is null)
        {
            return [];
        }

        var targetSymbols = GetDeclaredTypeSymbols(model, root, cancellationToken);
        if (targetSymbols.Count == 0)
        {
            return [];
        }

        var referencedDocumentIds = new HashSet<IRoslynDocumentWrapper>();

        foreach (var document in documentsToSearch.Where(doc => doc.RoslynDocument.Id != sourceDocument.RoslynDocument.Id))
        {
            if (await DocumentContainsReferenceAsync(document, solution, targetSymbols, cancellationToken))
            {
                referencedDocumentIds.Add(document);
            }
        }

        return referencedDocumentIds;
    }

    private static ImmutableHashSet<ISymbol> GetDeclaredTypeSymbols(SemanticModel model, SyntaxNode root, CancellationToken token) =>
        root.DescendantNodes(node =>
                node is not // ignoring nodes that definitely can't have children of type/enum to save time
                    (CSSyntax.BaseFieldDeclarationSyntax
                    or CSSyntax.BaseMethodDeclarationSyntax
                    or CSSyntax.BasePropertyDeclarationSyntax
                    or CSSyntax.DelegateDeclarationSyntax
                    or CSSyntax.EnumMemberDeclarationSyntax
                    or CSSyntax.ExpressionOrPatternSyntax
                    or CSSyntax.StatementSyntax
                    or CSSyntax.IncompleteMemberSyntax
                    or VBSyntax.AttributesStatementSyntax
                    or VBSyntax.EndBlockStatementSyntax
                    or VBSyntax.EnumMemberDeclarationSyntax
                    or VBSyntax.EventBlockSyntax
                    or VBSyntax.FieldDeclarationSyntax
                    or VBSyntax.ImportsStatementSyntax
                    or VBSyntax.InheritsOrImplementsStatementSyntax
                    or VBSyntax.MethodBaseSyntax
                    or VBSyntax.MethodBlockBaseSyntax
                    or VBSyntax.OptionStatementSyntax
                    or VBSyntax.PropertyBlockSyntax
                    or VBSyntax.IncompleteMemberSyntax))
            .Where(node =>
                node is CSSyntax.TypeDeclarationSyntax
                    or CSSyntax.EnumDeclarationSyntax
                    or VBSyntax.TypeBlockSyntax
                    or VBSyntax.EnumBlockSyntax)
            .Select(node => model.GetDeclaredSymbol(node, token))
            .Where(symbol => symbol is not null)
            .ToImmutableHashSet(SymbolEqualityComparer.Default)!;

    private static async Task<bool> DocumentContainsReferenceAsync(
        IRoslynDocumentWrapper document,
        IRoslynSolutionWrapper solution,
        ISet<ISymbol> targetSymbols,
        CancellationToken token)
    {
        var (model, root) = await GetModelAndRootAsync(document, token);
        if (model is null || root is null)
        {
            return false;
        }

        foreach (var identifier in GetIdentifiers(root))
        {
            if (model.GetSymbolInfo(identifier, token).Symbol is not { } foundSymbol)
            {
                continue;
            }

            if (await IsReferringToTargetSymbolAsync(foundSymbol, targetSymbols!, solution, token)
                || await IsReferringToTargetSymbolAsync(foundSymbol.ContainingType, targetSymbols!, solution, token))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<SyntaxNode> GetIdentifiers(SyntaxNode root) =>
        root
            .DescendantNodes()
            .Where(x => x is CSSyntax.IdentifierNameSyntax  or CSSyntax.ImplicitObjectCreationExpressionSyntax or VBSyntax.IdentifierNameSyntax);

    private static async Task<bool> IsReferringToTargetSymbolAsync(
        ISymbol? symbolToCheck,
        ISet<ISymbol?> targetSymbols,
        IRoslynSolutionWrapper solution,
        CancellationToken token) =>
        targetSymbols.Contains(symbolToCheck?.OriginalDefinition)
        || targetSymbols.Contains(await SymbolFinder.FindSourceDefinitionAsync(symbolToCheck, solution.RoslynSolution, token));

    private static async Task<(SemanticModel?, SyntaxNode?)> GetModelAndRootAsync(IRoslynDocumentWrapper document, CancellationToken token)
    {
        var model = await document.RoslynDocument.GetSemanticModelAsync(token);
        var root = model != null ? await model.SyntaxTree.GetRootAsync(token) : null;
        return (model, root);
    }
}
