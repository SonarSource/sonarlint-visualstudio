// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the THIRD-PARTY-NOTICES file in the project root for more information.

// Adapted from Roslyn's AbstractSuppressionCodeFixProvider.PragmaHelpers.cs,
// AbstractSuppressionCodeFixProvider.cs, and CSharpSuppressionCodeFixProvider.cs.
// Original source:
// https://github.com/dotnet/roslyn/blob/79038dba2ed6a766f09f12fcef902122aec8ffdb/src/Features/Core/Portable/CodeFixes/Suppression/AbstractSuppressionCodeFixProvider.PragmaHelpers.cs#L22

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma.Roslyn;

internal static partial class AbstractSuppressionCodeFixProvider
{
    private static class PragmaHelpers
    {
        internal static async Task<Document> GetChangeDocumentWithPragmaAdjustedAsync(
            Document document,
            TextSpan diagnosticSpan,
            SuppressionTargetInfo suppressionTargetInfo,
            Func<SyntaxToken, TextSpan, SyntaxToken> getNewStartToken,
            Func<SyntaxToken, TextSpan, SyntaxToken> getNewEndToken,
            CancellationToken cancellationToken)
        {
            var startToken = suppressionTargetInfo.StartToken;
            var endToken = suppressionTargetInfo.EndToken;
            var nodeWithTokens = suppressionTargetInfo.NodeWithTokens;
            var root = await nodeWithTokens.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var startAndEndTokenAreTheSame = startToken == endToken;
            var newStartToken = getNewStartToken(startToken, diagnosticSpan);

            var newEndToken = endToken;
            if (startAndEndTokenAreTheSame)
            {
                var annotation = new SyntaxAnnotation();
                newEndToken = root.ReplaceToken(startToken, newStartToken.WithAdditionalAnnotations(annotation))
                    .GetAnnotatedTokens(annotation).Single();
                var spanChange = newStartToken.LeadingTrivia.FullSpan.Length - startToken.LeadingTrivia.FullSpan.Length;
                diagnosticSpan = new TextSpan(diagnosticSpan.Start + spanChange, diagnosticSpan.Length);
            }

            newEndToken = getNewEndToken(newEndToken, diagnosticSpan);

            SyntaxNode newNode;
            if (startAndEndTokenAreTheSame)
            {
                newNode = nodeWithTokens.ReplaceToken(startToken, newEndToken);
            }
            else
            {
                newNode = nodeWithTokens.ReplaceTokens(
                    new[] { startToken, endToken },
                    (o, _) => o == startToken ? newStartToken : newEndToken);
            }

            var newRoot = root.ReplaceNode(nodeWithTokens, newNode);
            return document.WithSyntaxRoot(newRoot);
        }

        private static int GetPositionForPragmaInsertion(
            ImmutableArray<SyntaxTrivia> triviaList,
            TextSpan currentDiagnosticSpan,
            bool isStartToken,
            out SyntaxTrivia triviaAtIndex)
        {
            int getNextIndex(int cur) => isStartToken ? cur - 1 : cur + 1;

            bool shouldConsiderTrivia(SyntaxTrivia trivia) =>
                isStartToken
                    ? trivia.FullSpan.End <= currentDiagnosticSpan.Start
                    : trivia.FullSpan.Start >= currentDiagnosticSpan.End;

            var walkedPastDiagnosticSpan = false;
            var seenEndOfLineTrivia = false;
            var index = isStartToken ? triviaList.Length - 1 : 0;
            while (index >= 0 && index < triviaList.Length)
            {
                var trivia = triviaList[index];

                walkedPastDiagnosticSpan = walkedPastDiagnosticSpan || shouldConsiderTrivia(trivia);
                seenEndOfLineTrivia = seenEndOfLineTrivia || IsEndOfLineOrContainsEndOfLine(trivia);

                if (walkedPastDiagnosticSpan && seenEndOfLineTrivia)
                {
                    break;
                }

                index = getNextIndex(index);
            }

            triviaAtIndex = index >= 0 && index < triviaList.Length
                ? triviaList[index]
                : default;

            return index;
        }

        internal static SyntaxToken GetNewStartTokenWithAddedPragma(
            SyntaxToken startToken,
            TextSpan currentDiagnosticSpan,
            string diagnosticId)
        {
            var trivia = startToken.LeadingTrivia.ToImmutableArray();
            var index = GetPositionForPragmaInsertion(trivia, currentDiagnosticSpan, isStartToken: true, triviaAtIndex: out var insertAfterTrivia);
            index++;

            bool needsLeadingEOL;
            if (index > 0)
            {
                needsLeadingEOL = !IsEndOfLineOrHasTrailingEndOfLine(insertAfterTrivia);
            }
            else if (startToken.FullSpan.Start == 0)
            {
                needsLeadingEOL = false;
            }
            else
            {
                needsLeadingEOL = true;
            }

            var pragmaTrivia = CreatePragmaDirectiveTrivia(
                SyntaxKind.DisableKeyword, diagnosticId, needsLeadingEOL, needsTrailingEndOfLine: true);

            return startToken.WithLeadingTrivia(trivia.InsertRange(index, pragmaTrivia));
        }

        internal static SyntaxToken GetNewEndTokenWithAddedPragma(
            SyntaxToken endToken,
            TextSpan currentDiagnosticSpan,
            string diagnosticId)
        {
            ImmutableArray<SyntaxTrivia> trivia;
            var isEOF = endToken.IsKind(SyntaxKind.EndOfFileToken);
            if (isEOF)
            {
                trivia = endToken.LeadingTrivia.ToImmutableArray();
            }
            else
            {
                trivia = endToken.TrailingTrivia.ToImmutableArray();
            }

            var index = GetPositionForPragmaInsertion(trivia, currentDiagnosticSpan, isStartToken: false, triviaAtIndex: out var insertBeforeTrivia);

            bool needsTrailingEOL;
            if (index < trivia.Length)
            {
                needsTrailingEOL = !IsEndOfLineOrHasLeadingEndOfLine(insertBeforeTrivia);
            }
            else if (isEOF)
            {
                needsTrailingEOL = false;
            }
            else
            {
                needsTrailingEOL = true;
            }

            var pragmaTrivia = CreatePragmaDirectiveTrivia(
                SyntaxKind.RestoreKeyword, diagnosticId, needsLeadingEndOfLine: true, needsTrailingEndOfLine: needsTrailingEOL);

            if (isEOF)
            {
                return endToken.WithLeadingTrivia(trivia.InsertRange(index, pragmaTrivia));
            }
            else
            {
                return endToken.WithTrailingTrivia(trivia.InsertRange(index, pragmaTrivia));
            }
        }

        internal static void NormalizeTriviaOnTokens(
            ref Document document,
            ref SuppressionTargetInfo suppressionTargetInfo)
        {
            var startToken = suppressionTargetInfo.StartToken;
            var endToken = suppressionTargetInfo.EndToken;
            var nodeWithTokens = suppressionTargetInfo.NodeWithTokens;
            var startAndEndTokensAreSame = startToken == endToken;
            var isEndTokenEOF = endToken.IsKind(SyntaxKind.EndOfFileToken);

            var previousOfStart = startToken.GetPreviousToken(includeZeroWidth: true);
            var nextOfEnd = !isEndTokenEOF ? endToken.GetNextToken(includeZeroWidth: true) : default;
            if (!previousOfStart.HasTrailingTrivia && !nextOfEnd.HasLeadingTrivia)
            {
                return;
            }

            var root = nodeWithTokens.SyntaxTree.GetRoot();
            var spanEnd = !isEndTokenEOF ? nextOfEnd.FullSpan.End : endToken.FullSpan.End;
            var subtreeRoot = root.FindNode(new TextSpan(previousOfStart.FullSpan.Start, spanEnd - previousOfStart.FullSpan.Start));

            var currentStartToken = startToken;
            var currentEndToken = endToken;
            var newStartToken = startToken.WithLeadingTrivia(previousOfStart.TrailingTrivia.Concat(startToken.LeadingTrivia));

            var newEndToken = currentEndToken;
            if (startAndEndTokensAreSame)
            {
                newEndToken = newStartToken;
            }

            newEndToken = newEndToken.WithTrailingTrivia(endToken.TrailingTrivia.Concat(nextOfEnd.LeadingTrivia));

            var newPreviousOfStart = previousOfStart.WithTrailingTrivia();
            var newNextOfEnd = nextOfEnd.WithLeadingTrivia();

            var tokensToReplace = new HashSet<SyntaxToken>(new[] { startToken, previousOfStart, endToken, nextOfEnd }
                .Where(t => t != default));

            var newSubtreeRoot = subtreeRoot.ReplaceTokens(
                tokensToReplace,
                (o, _) =>
                {
                    if (o == currentStartToken)
                    {
                        return startAndEndTokensAreSame ? newEndToken : newStartToken;
                    }
                    else if (o == previousOfStart)
                    {
                        return newPreviousOfStart;
                    }
                    else if (o == currentEndToken)
                    {
                        return newEndToken;
                    }
                    else if (o == nextOfEnd)
                    {
                        return newNextOfEnd;
                    }
                    else
                    {
                        return o;
                    }
                });

            root = root.ReplaceNode(subtreeRoot, newSubtreeRoot);
            document = document.WithSyntaxRoot(root);
            suppressionTargetInfo.StartToken = root.FindToken(startToken.SpanStart);
            suppressionTargetInfo.EndToken = root.FindToken(endToken.SpanStart);
            suppressionTargetInfo.NodeWithTokens = GetNodeWithTokens(suppressionTargetInfo.StartToken, suppressionTargetInfo.EndToken, root);
        }

        internal static SyntaxToken GetAdjustedTokenForPragmaDisable(SyntaxToken token, SyntaxNode root, TextLineCollection lines)
        {
            var containingStatement = GetContainingStatement(token);

            if (containingStatement is not null && containingStatement.GetFirstToken() != token)
            {
                var indexOfLine = lines.IndexOf(containingStatement.GetFirstToken().SpanStart);
                var line = lines[indexOfLine];
                token = root.FindToken(line.Start);
            }

            return token;
        }

        internal static SyntaxToken GetAdjustedTokenForPragmaRestore(
            SyntaxToken token,
            SyntaxNode root,
            TextLineCollection lines,
            int indexOfLine)
        {
            var containingStatement = GetContainingStatement(token);

            if (containingStatement is not null && containingStatement.GetLastToken() != token)
            {
                indexOfLine = lines.IndexOf(containingStatement.GetLastToken().SpanStart);
            }

            var line = lines[indexOfLine];
            token = root.FindToken(line.End);

            return token;
        }

        internal static SyntaxNode GetNodeWithTokens(SyntaxToken startToken, SyntaxToken endToken, SyntaxNode root)
        {
            if (endToken.IsKind(SyntaxKind.EndOfFileToken))
            {
                return root;
            }

            return root.FindNode(TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End));
        }

        private static SyntaxNode GetContainingStatement(SyntaxToken token)
        {
            return (SyntaxNode)token.Parent?.FirstAncestorOrSelf<StatementSyntax>()
                   ?? token.Parent?.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>();
        }

        private static SyntaxTriviaList CreatePragmaDirectiveTrivia(
            SyntaxKind disableOrRestoreKeyword,
            string diagnosticId,
            bool needsLeadingEndOfLine,
            bool needsTrailingEndOfLine)
        {
            var id = SyntaxFactory.IdentifierName(diagnosticId);
            var ids = new SeparatedSyntaxList<ExpressionSyntax>().Add(id);
            var pragmaDirective = SyntaxFactory.PragmaWarningDirectiveTrivia(
                    SyntaxFactory.Token(disableOrRestoreKeyword), ids, true)
                .NormalizeWhitespace()
                .WithEndOfDirectiveToken(SyntaxFactory.Token(SyntaxKind.EndOfDirectiveToken));
            var pragmaDirectiveTrivia = SyntaxFactory.Trivia(pragmaDirective);
            var endOfLineTrivia = SyntaxFactory.CarriageReturnLineFeed;
            var triviaList = SyntaxFactory.TriviaList(pragmaDirectiveTrivia);

            if (needsLeadingEndOfLine)
            {
                triviaList = triviaList.Insert(0, endOfLineTrivia);
            }

            if (needsTrailingEndOfLine)
            {
                triviaList = triviaList.Add(endOfLineTrivia);
            }

            return triviaList;
        }

        private static bool IsEndOfLine(SyntaxTrivia trivia) => trivia.IsKind(SyntaxKind.EndOfLineTrivia) || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia);

        private static bool IsEndOfLineOrHasLeadingEndOfLine(SyntaxTrivia trivia) =>
            IsEndOfLine(trivia) ||
            (trivia.HasStructure && IsEndOfLine(trivia.GetStructure()!.DescendantTrivia().FirstOrDefault()));

        private static bool IsEndOfLineOrHasTrailingEndOfLine(SyntaxTrivia trivia) =>
            IsEndOfLine(trivia) ||
            (trivia.HasStructure && IsEndOfLine(trivia.GetStructure()!.DescendantTrivia().LastOrDefault()));

        private static bool IsEndOfLineOrContainsEndOfLine(SyntaxTrivia trivia) =>
            IsEndOfLine(trivia) ||
            (trivia.HasStructure && trivia.GetStructure()!.DescendantTrivia().Any(IsEndOfLine));
    }
}
