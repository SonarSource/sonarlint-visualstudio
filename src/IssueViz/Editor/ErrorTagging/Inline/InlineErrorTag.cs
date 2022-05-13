using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging.Inline
{
    internal interface IInlineErrorTag : ITag
    {
        SnapshotSpan LineExtent { get; }
        IMappingTagSpan<ISonarErrorTag>[] LocationTagSpans { get; }
    }

    internal class InlineErrorTag : TextMarkerTag, IInlineErrorTag
    {
        public InlineErrorTag(
            SnapshotSpan lineExtent,
            IMappingTagSpan<ISonarErrorTag>[] locationTagSpans)
            : base("SLVS_INNOVATION_INLINE_TAG")
        {
            LineExtent = lineExtent;
            LocationTagSpans = locationTagSpans;
        }

        public SnapshotSpan LineExtent { get; }

        public IMappingTagSpan<ISonarErrorTag>[] LocationTagSpans { get; }
    }
}
