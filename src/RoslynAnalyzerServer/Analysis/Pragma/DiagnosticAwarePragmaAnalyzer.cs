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
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DiagnosticAwarePragmaAnalyzer(Func<ImmutableArray<Diagnostic>> diagnostics, ImmutableHashSet<string> supportedIds) : DiagnosticAnalyzer
{
    private sealed class StackEntry
    {
        public Location Location { get; init; }
        public bool Unused { get; set; }
    }

    public const string DiagnosticId = "SQVSPRAGMA";
    public const string ReportedDiagnosticId = "ReportedRuleId";
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        new LocalizableResourceString(nameof(Resources.SQVSPRAGMATitle), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.SQVSPRAGMAMessageFormat), Resources.ResourceManager,
            typeof(Resources)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.SQVSPRAGMADescription), Resources.ResourceManager,
            typeof(Resources)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(ctx => AnalyzeSyntaxTree(ctx, diagnostics, supportedIds));
    }

    private static void AnalyzeSyntaxTree(
        SyntaxTreeAnalysisContext context,
        Func<ImmutableArray<Diagnostic>> allDiagnostics,
        ImmutableHashSet<string> supportedIds)
    {
        var treeDiagnostics = allDiagnostics()
            .Where(d => d.Location.SourceTree == context.Tree)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToList();

        var root = context.Tree.GetRoot(context.CancellationToken);
        var stacks = new Dictionary<string, Stack<StackEntry>>(StringComparer.OrdinalIgnoreCase);
        var diagIndex = 0;

        foreach (var trivia in root.DescendantTrivia(descendIntoChildren: node => node.ContainsDirectives))
        {
            diagIndex = ProcessDiagnosticsUpToCurrentLocation(treeDiagnostics, diagIndex, trivia.SpanStart, stacks);

            if (trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia)
                && trivia.GetStructure() is PragmaWarningDirectiveTriviaSyntax pragma)
            {
                if (pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword))
                {
                    HandlePragmaDisable(pragma, supportedIds, stacks);
                }
                else if (pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.RestoreKeyword))
                {
                    HandlePragmaRestore(pragma, context, supportedIds, stacks);
                }
            }
        }

        FlushRemaining(context, supportedIds, treeDiagnostics, diagIndex, stacks);
    }

    private static void FlushRemaining(
        SyntaxTreeAnalysisContext context,
        ImmutableHashSet<string> supportedIds,
        List<Diagnostic> treeDiagnostics,
        int diagIndex,
        Dictionary<string, Stack<StackEntry>> stacks)
    {
        ProcessDiagnosticsUpToCurrentLocation(treeDiagnostics, diagIndex, int.MaxValue, stacks);

        foreach (var id in supportedIds)
        {
            if (!stacks.TryGetValue(id, out var stack) || stack.Count == 0)
            {
                return;
            }

            foreach (var entry in stack.Where(entry => entry.Unused))
            {
                ReportSinglePragma(context, entry.Location, id);
            }
        }
    }

    private static int ProcessDiagnosticsUpToCurrentLocation(
        List<Diagnostic> treeDiagnostics,
        int startIndex,
        int beforePosition,
        Dictionary<string, Stack<StackEntry>> stacks)
    {
        var currentIndex = startIndex;
        while (currentIndex < treeDiagnostics.Count && treeDiagnostics[currentIndex].Location.SourceSpan.Start < beforePosition)
        {
            if (stacks.TryGetValue(treeDiagnostics[currentIndex].Id, out var stack) && stack.Count > 0)
            {
                stack.Peek().Unused = false;
            }
            currentIndex++;
        }

        return currentIndex;
    }

    private static void HandlePragmaDisable(
        PragmaWarningDirectiveTriviaSyntax pragma,
        ImmutableHashSet<string> disallowedIds,
        Dictionary<string, Stack<StackEntry>> stacks)
    {
        foreach (var errorCode in pragma.ErrorCodes)
        {
            if (!GetSuppressedDiagnosticId(disallowedIds, errorCode, out var identifier, out var ruleId))
            {
                continue;
            }
            if (!stacks.TryGetValue(ruleId, out var stack))
            {
                stack = new Stack<StackEntry>();
                stacks[ruleId] = stack;
            }
            stack.Push(new StackEntry { Location = identifier.GetLocation(), Unused = true });
        }
    }

    private static void HandlePragmaRestore(
        PragmaWarningDirectiveTriviaSyntax pragma,
        SyntaxTreeAnalysisContext context,
        ImmutableHashSet<string> disallowedIds,
        Dictionary<string, Stack<StackEntry>> stacks)
    {
        foreach (var errorCode in pragma.ErrorCodes)
        {
            if (!GetSuppressedDiagnosticId(disallowedIds, errorCode, out var identifier, out var ruleId))
            {
                continue;
            }
            if (!stacks.TryGetValue(ruleId, out var stack) || stack.Count == 0)
            {
                ReportSinglePragma(context, identifier.GetLocation(), ruleId);
            }
            else if (stack.Pop() is { Unused: true, Location: var location })
            {
                ReportPairedPragma(context, location, identifier.GetLocation(), ruleId);
            }
        }
    }

    private static bool GetSuppressedDiagnosticId(
        ImmutableHashSet<string> supportedIds,
        ExpressionSyntax errorCode,
        [NotNullWhen(true)] out IdentifierNameSyntax? identifierNameSyntax,
        [NotNullWhen(true)] out string? ruleId)
    {
        if (errorCode is not IdentifierNameSyntax identifier)
        {
            identifierNameSyntax = null;
            ruleId = null;
            return false;
        }

        identifierNameSyntax = identifier;
        ruleId = identifierNameSyntax.Identifier.ValueText;
        return supportedIds.Contains(ruleId);
    }

    private static void ReportPairedPragma(
        SyntaxTreeAnalysisContext context,
        Location disableLocation,
        Location restoreLocation,
        string ruleId)
    {
        var properties = CreateProperties(ruleId);
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            disableLocation,
            additionalLocations: [restoreLocation],
            properties: properties,
            messageArgs: [ruleId]));
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            restoreLocation,
            additionalLocations: [disableLocation],
            properties: properties,
            messageArgs: [ruleId]));
    }

    private static void ReportSinglePragma(SyntaxTreeAnalysisContext context, Location location, string ruleId) =>
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            location,
            CreateProperties(ruleId),
            ruleId));

    private static ImmutableDictionary<string, string?> CreateProperties(string ruleId) =>
        ImmutableDictionary<string, string?>.Empty
            .SetItem(ReportedDiagnosticId, ruleId);
}
