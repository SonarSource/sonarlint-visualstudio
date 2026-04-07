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
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DiagnosticAwarePragmaAnalyzer(Func<ImmutableArray<Diagnostic>> diagnostics, ImmutableHashSet<string> supportedIds) : DiagnosticAnalyzer
{
    private sealed class StackEntry(Location location)
    {
        public Location Location { get; } = location;
        public bool Unused { get; private set; } = true;
        public void SetAsUsed() => Unused = false;
    }

    public const string ReportedDiagnosticId = "ReportedRuleId";
    private static readonly DiagnosticDescriptor Rule = new(
        AdditionalRules.UnusedPragmaRuleKey,
        Resources.SQVSPRAGMATitle,
        Resources.SQVSPRAGMAMessageFormat,
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

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
        var treeDiagnostics = new Queue<Diagnostic>(allDiagnostics()
            .Where(d => d.Location.SourceTree == context.Tree)
            .OrderBy(d => d.Location.SourceSpan.Start));
        var stacks = new Dictionary<string, Stack<StackEntry>>(StringComparer.OrdinalIgnoreCase);

        var root = context.Tree.GetRoot(context.CancellationToken);

        if (root.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return;
        }

        foreach (var trivia in root.DescendantTrivia(descendIntoChildren: node => node.ContainsDirectives))
        {
            if (trivia.GetStructure() is not PragmaWarningDirectiveTriviaSyntax pragma)
            {
                continue;
            }

            ProcessPragma(context, treeDiagnostics, stacks, supportedIds, trivia, pragma);
        }

        FlushUnclosedPragmaDisables(context, treeDiagnostics, stacks);
    }

    private static void ProcessPragma(
        SyntaxTreeAnalysisContext context,
        Queue<Diagnostic> treeDiagnostics,
        Dictionary<string, Stack<StackEntry>> stacks,
        ImmutableHashSet<string> supportedIds,
        SyntaxTrivia trivia,
        PragmaWarningDirectiveTriviaSyntax pragma)
    {
        ProcessDiagnosticsUpToLocation(treeDiagnostics, trivia.SpanStart, stacks);
        if (pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword))
        {
            ProcessPragmaDisable(pragma, supportedIds, stacks);
        }
        else if (pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.RestoreKeyword))
        {
            ProcessPragmaRestore(pragma, context, supportedIds, stacks);
        }
    }

    private static void ProcessDiagnosticsUpToLocation(
        Queue<Diagnostic> treeDiagnostics,
        int beforePosition,
        Dictionary<string, Stack<StackEntry>> stacks)
    {
        while (treeDiagnostics.Count > 0 && treeDiagnostics.Peek().Location.SourceSpan.Start < beforePosition)
        {
            if (stacks.TryGetValue(treeDiagnostics.Dequeue().Id, out var stack) && stack.Count > 0)
            {
                stack.Peek().SetAsUsed();
            }
        }
    }

    private static void ProcessPragmaDisable(
        PragmaWarningDirectiveTriviaSyntax pragma,
        ImmutableHashSet<string> supportedRules,
        Dictionary<string, Stack<StackEntry>> stacks)
    {
        foreach (var errorCode in pragma.ErrorCodes)
        {
            if (!GetSuppressedDiagnosticId(supportedRules, errorCode, out var identifier, out var ruleId))
            {
                continue;
            }
            if (!stacks.ContainsKey(ruleId))
            {
                stacks[ruleId] = new();
            }
            stacks[ruleId].Push(new(identifier.GetLocation()));
        }
    }

    private static void ProcessPragmaRestore(
        PragmaWarningDirectiveTriviaSyntax pragma,
        SyntaxTreeAnalysisContext context,
        ImmutableHashSet<string> supportedRules,
        Dictionary<string, Stack<StackEntry>> stacks)
    {
        foreach (var errorCode in pragma.ErrorCodes)
        {
            if (!GetSuppressedDiagnosticId(supportedRules, errorCode, out var identifier, out var ruleId))
            {
                continue;
            }
            if (stacks.TryGetValue(ruleId, out var stack) && stack.Count != 0)
            {
                if (stack.Pop() is not { Unused: true, Location: var location })
                {
                    continue;
                }
                ReportPairedPragma(context, location, identifier.GetLocation(), ruleId);
            }
            else
            {
                ReportSinglePragma(context, identifier.GetLocation(), ruleId);
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

    private static void FlushUnclosedPragmaDisables(
        SyntaxTreeAnalysisContext context,
        Queue<Diagnostic> treeDiagnostics,
        Dictionary<string, Stack<StackEntry>> stacks)
    {
        if (!stacks.Values.Any(x => x.Any()))
        {
            return;
        }

        ProcessDiagnosticsUpToLocation(treeDiagnostics, int.MaxValue, stacks);

        foreach (var (ruleId, entry) in stacks
                     .SelectMany(kvp => kvp.Value.Where(y => y.Unused), (kvp, entry) => (ruleId: kvp.Key, entry)))
        {
            ReportSinglePragma(context, entry.Location, ruleId);
        }
    }

    private static void ReportPairedPragma(
        SyntaxTreeAnalysisContext context,
        Location disableLocation,
        Location restoreLocation,
        string ruleId)
    {
        var properties = CreateProperties(ruleId).SetItem("0", "#pragma warning disable").SetItem("1", "#pragma warning restore");
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            disableLocation,
            additionalLocations: [disableLocation, restoreLocation],
            properties: properties,
            messageArgs: [ruleId]));
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            restoreLocation,
            additionalLocations: [disableLocation, restoreLocation],
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
