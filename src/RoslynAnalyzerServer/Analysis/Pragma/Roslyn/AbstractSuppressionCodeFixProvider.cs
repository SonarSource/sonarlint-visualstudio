// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma.Roslyn;

internal partial class AbstractSuppressionCodeFixProvider
{
    internal sealed class SuppressionTargetInfo
    {
        public SyntaxToken StartToken { get; set; }
        public SyntaxToken EndToken { get; set; }
        public SyntaxNode NodeWithTokens { get; set; }
    }

    // token position for pragmas is calculated based on logic from https://github.com/dotnet/roslyn/blob/f511fcb6e9a166980340a6dbcffef48a2a4caf47/src/Features/Core/Portable/CodeFixes/Suppression/AbstractSuppressionCodeFixProvider.cs#L261
    internal static async Task<Document> AddPragmaDirectivesAsync(
        Document document,
        SyntaxNode root,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var text = root.GetText();
        var lines = text.Lines;

        var indexOfLine = lines.IndexOf(diagnosticSpan.Start);
        var lineAtPos = lines[indexOfLine];
        var startToken = root.FindToken(lineAtPos.Start);
        startToken = PragmaHelpers.GetAdjustedTokenForPragmaDisable(startToken, root, lines);

        var spanEnd = Math.Max(startToken.Span.End, diagnosticSpan.End);
        indexOfLine = lines.IndexOf(spanEnd);
        lineAtPos = lines[indexOfLine];
        var endToken = root.FindToken(lineAtPos.End);
        endToken = PragmaHelpers.GetAdjustedTokenForPragmaRestore(endToken, root, lines, indexOfLine);

        var nodeWithTokens = PragmaHelpers.GetNodeWithTokens(startToken, endToken, root);

        var suppressionTargetInfo = new SuppressionTargetInfo
        {
            StartToken = startToken,
            EndToken = endToken,
            NodeWithTokens = nodeWithTokens
        };

        PragmaHelpers.NormalizeTriviaOnTokens(ref document, ref suppressionTargetInfo);

        var diagnosticId = diagnostic.Id;
        return await PragmaHelpers.GetChangeDocumentWithPragmaAdjustedAsync(
            document,
            diagnosticSpan,
            suppressionTargetInfo,
            (token, span) => PragmaHelpers.GetNewStartTokenWithAddedPragma(token, span, diagnosticId),
            (token, span) => PragmaHelpers.GetNewEndTokenWithAddedPragma(token, span, diagnosticId),
            cancellationToken).ConfigureAwait(false);
    }
}
