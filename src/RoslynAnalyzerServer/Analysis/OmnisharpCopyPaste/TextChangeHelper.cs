using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

namespace OmniSharp.Roslyn.Utilities
{
    internal static class TextChangesCopyPaste
    {
        public static async Task<IEnumerable<RoslynIssueQuickFixEdit>> GetAsync(Document document, Document oldDocument, CancellationToken token)
        {
            var changes = await document.GetTextChangesAsync(oldDocument, token);
            var oldText = await oldDocument.GetTextAsync(token);

            return Convert(oldText, changes);
        }

        private static RoslynIssueQuickFixEdit Convert(SourceText oldText, TextChange change)
        {
            var span = change.Span;
            var newText = change.NewText;
            var prefix = string.Empty;
            var postfix = string.Empty;

            if (newText.Length > 0)
            {
                // Roslyn computes text changes on character arrays. So it might happen that a
                // change starts inbetween \r\n which is OK when you are offset-based but a problem
                // when you are line,column-based. This code extends text edits which just overlap
                // a with a line break to its full line break

                if (span.Start > 0 && newText[0] == '\n' && oldText[span.Start - 1] == '\r')
                {
                    // text: foo\r\nbar\r\nfoo
                    // edit:      [----)
                    span = TextSpan.FromBounds(span.Start - 1, span.End);
                    prefix = "\r";
                }

                if (span.End < oldText.Length - 1 && newText[newText.Length - 1] == '\r' && oldText[span.End] == '\n')
                {
                    // text: foo\r\nbar\r\nfoo
                    // edit:        [----)
                    span = TextSpan.FromBounds(span.Start, span.End + 1);
                    postfix = "\n";
                }
            }

            var linePositionSpan = oldText.Lines.GetLinePositionSpan(span);

            return new RoslynIssueQuickFixEdit(
                prefix + newText + postfix,
                new RoslynIssueTextRange(
                    linePositionSpan.Start.Line + 1,
                    linePositionSpan.End.Line + 1,
                    linePositionSpan.Start.Character,
                    linePositionSpan.End.Character));
        }

        public static IEnumerable<RoslynIssueQuickFixEdit> Convert(SourceText oldText, IEnumerable<TextChange> changes)
        {
            return changes
                .OrderByDescending(change => change.Span)
                .Select(change => Convert(oldText, change));
        }
    }
}
