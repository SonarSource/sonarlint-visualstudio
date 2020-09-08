/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment
{
    internal sealed class IssueLocationAdornmentTagger : FilteringTaggerBase<ISelectedIssueLocationTag, IntraTextAdornmentTag>
    {
        private readonly IWpfTextView wpfView;

        public IssueLocationAdornmentTagger(ITagAggregator<ISelectedIssueLocationTag> tagAggregator, IWpfTextView textView)
            : base(tagAggregator, textView)
        {
            wpfView = textView;
        }

        protected override TagSpan<IntraTextAdornmentTag> CreateTagSpan(ISelectedIssueLocationTag trackedTag, NormalizedSnapshotSpanCollection spans)
        {
            // To produce adornments that don't obscure the text, the adornment tags
            // should have zero length spans. Overriding this method allows control
            // over the tag spans.
            var adornmentSpan = new SnapshotSpan(trackedTag.Location.Span.Value.Start, 0);
            var adornment = new IssueLocationAdornment(trackedTag, wpfView.FormattedLineSource);

            // If we don't call Measure here the tag is positioned incorrectly
            adornment.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            return new TagSpan<IntraTextAdornmentTag>(adornmentSpan, new IntraTextAdornmentTag(adornment, null, PositionAffinity.Predecessor));
        }
    }
}
