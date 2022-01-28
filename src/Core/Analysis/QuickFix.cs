using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Core.Analysis
{
    public class QuickFix : IQuickFix
    {
        private static readonly IReadOnlyList<IEdit> emptyEdits = new List<IEdit>();

        public QuickFix(string message, IReadOnlyList<IEdit> edits)
        {
            Message = message;
            Edits = edits ?? emptyEdits;
        }

        public string Message { get; }

        public IReadOnlyList<IEdit> Edits { get; }
    }

    public class Edit : IEdit
    {

        public Edit( int startLine,
            int startColumn,
            int endLine, 
            int endColumn,
            string text)
        {
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
            Text = text;
        }

        public int StartLine { get; }

        public int StartColumn { get; }

        public int EndLine { get; }

        public int EndColumn { get; }

        public string Text { get; }
    }
}
