using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Core.Analysis
{
    public interface IQuickFix
    {
        string Message { get; }
        IReadOnlyList<IEdit> Edits { get; }
    }

    public interface IEdit
    {
        int StartLine { get; }
        int StartColumn { get; }
        int EndLine { get; }
        int EndColumn { get; }
        string Text { get; }
    }
}
