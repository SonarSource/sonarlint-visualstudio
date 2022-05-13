/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging.Inline
{
    internal sealed class InlineErrorTagger : FilteringTaggerBase<IInlineErrorTag, IntraTextAdornmentTag>
    {
        private readonly IWpfTextView wpfTextView;

        public InlineErrorTagger(ITagAggregator<IInlineErrorTag> tagAggregator, IWpfTextView wpfTextView)
            : base(tagAggregator, wpfTextView)
        {
            this.wpfTextView = wpfTextView;
        }

        protected override TagSpan<IntraTextAdornmentTag> CreateTagSpan(IInlineErrorTag trackedTag, NormalizedSnapshotSpanCollection spans)
        {
            // To produce adornments that don't obscure the text, the adornment tags
            // should have zero length spans. Overriding this method allows control
            // over the tag spans.
            var translatedPosition = trackedTag.LineExtent.TranslateTo(wpfTextView.TextSnapshot, SpanTrackingMode.EdgeInclusive);
            var vsLine = wpfTextView.GetTextViewLineContainingBufferPosition(translatedPosition.End);

            var adornmentSpan = new SnapshotSpan(vsLine.End, 0);
            
            var adornment = new InlineErrorAdornment(trackedTag, wpfTextView.FormattedLineSource);

            // If we don't call Measure here the tag is positioned incorrectly
            adornment.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));

            return new TagSpan<IntraTextAdornmentTag>(adornmentSpan, new IntraTextAdornmentTag(adornment, null, PositionAffinity.Predecessor));
        }
    }
}
