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

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DiagnosticAwarePragmaAnalyzer(Func<ImmutableArray<Diagnostic>> diagnostics, ImmutableHashSet<string> supportedIds) : DiagnosticAnalyzer
{
    private struct StackEntry
    {
        public Location Location;
        public bool Marked;
    }

    public const string DiagnosticId = "SQVSPRAGMA";
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

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, Func<ImmutableArray<Diagnostic>> allDiagnostics,
        ImmutableHashSet<string> disallowedIds)
    {

        var treeDiagnostics = allDiagnostics()
            .Where(d => d.Location.SourceTree == context.Tree)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToList();

        var root = context.Tree.GetRoot(context.CancellationToken);
        var stacks = new Dictionary<string, Stack<StackEntry>>(StringComparer.OrdinalIgnoreCase);
        var diagIndex = 0;

        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true)) // todo check for large files
        {
            var triviaStart = trivia.SpanStart;
            diagIndex = ConsumeDiagnostics(treeDiagnostics, diagIndex, triviaStart, stacks);

            if (trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia)
                && trivia.GetStructure() is PragmaWarningDirectiveTriviaSyntax pragma)
            {
                if (pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword))
                {
                    HandlePragmaDisable(pragma, disallowedIds, stacks);
                }
                else if (pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.RestoreKeyword))
                {
                    HandlePragmaRestore(pragma, context, disallowedIds, stacks);
                }
            }
        }

        ConsumeDiagnostics(treeDiagnostics, diagIndex, int.MaxValue, stacks);

        foreach (var id in disallowedIds)
        {
            FlushStack(context, stacks, id);
        }
    }

    private static int ConsumeDiagnostics(
        List<Diagnostic> diagnostics,
        int startIndex,
        int beforePosition,
        Dictionary<string, Stack<StackEntry>> stacks)
    {
        var i = startIndex;
        while (i < diagnostics.Count && diagnostics[i].Location.SourceSpan.Start < beforePosition)
        {
            var diag = diagnostics[i];
            var ruleId = diag.Id;
            if (stacks.TryGetValue(ruleId, out var stack) && stack.Count > 0)
            {
                var top = stack.Pop();
                top.Marked = false;
                stack.Push(top);
            }

            i++;
        }

        return i;
    }

    private static void HandlePragmaDisable(
        PragmaWarningDirectiveTriviaSyntax pragma,
        ImmutableHashSet<string> disallowedIds,
        Dictionary<string, Stack<StackEntry>> stacks)
    {
        foreach (var errorCode in pragma.ErrorCodes)
        {
            if (errorCode is not IdentifierNameSyntax identifier)
            {
                continue;
            }

            var ruleId = identifier.Identifier.ValueText;
            if (!disallowedIds.Contains(ruleId))
            {
                continue;
            }

            if (!stacks.TryGetValue(ruleId, out var stack))
            {
                stack = new Stack<StackEntry>();
                stacks[ruleId] = stack;
            }

            stack.Push(new StackEntry
            {
                Location = identifier.GetLocation(),
                Marked = true
            });
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
            if (errorCode is not IdentifierNameSyntax identifier)
            {
                continue;
            }

            var ruleId = identifier.Identifier.ValueText;
            if (!disallowedIds.Contains(ruleId))
            {
                continue;
            }

            if (!stacks.TryGetValue(ruleId, out var stack) || stack.Count == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation(), ruleId));
            }
            else
            {
                var entry = stack.Pop();
                if (entry.Marked)
                {
                    ReportPairedPragma(context, entry.Location, identifier.GetLocation(), ruleId);
                }
            }
        }
    }

    private static void ReportPairedPragma(
        SyntaxTreeAnalysisContext context,
        Location disableLocation,
        Location restoreLocation,
        string ruleId)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Rule, disableLocation,
            additionalLocations: [restoreLocation],
            properties: null,
            messageArgs: [ruleId]));
        context.ReportDiagnostic(Diagnostic.Create(
            Rule, restoreLocation,
            additionalLocations: [disableLocation],
            properties: null,
            messageArgs: [ruleId]));
    }

    private static void FlushStack(SyntaxTreeAnalysisContext context,
        Dictionary<string, Stack<StackEntry>> stacks, string ruleId)
    {
        if (!stacks.TryGetValue(ruleId, out var stack) || stack.Count == 0)
        {
            return;
        }

        foreach (var entry in stack.Where(entry => entry.Marked))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, entry.Location, ruleId));
        }

        stack.Clear();
    }
}
