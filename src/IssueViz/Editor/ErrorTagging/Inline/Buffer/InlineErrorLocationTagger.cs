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

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging.Inline.Buffer
{
    internal class InlineErrorLocationTagger: TranslatingTaggerBase<IIssueLocationTag, IInlineErrorTag>
    {
        public InlineErrorLocationTagger(ITagAggregator<IIssueLocationTag> tagAggregator, ITextBuffer textBuffer)
            : base(tagAggregator, textBuffer)
        {
        }

        #region ITagger methods

        protected override IEnumerable<IMappingTagSpan<IInlineErrorTag>> Translate(IEnumerable<IMappingTagSpan<IIssueLocationTag>> trackedTagSpans)
        {
            var primaryLocations = trackedTagSpans.Where(x => IsValidPrimaryLocation(x.Tag.Location))
                .GroupBy(x => x.Tag.Location.Span.Value.Snapshot.GetLineNumberFromPosition(x.Tag.Location.Span.Value.End));


            var translated = new List<IMappingTagSpan<IInlineErrorTag>>();

            foreach (var primaryLocation in primaryLocations)
            {
                var lineNumber = primaryLocation.Key;
                var items = primaryLocation.ToArray();

                // Assume that all locations in the group are in the same snapshot
                var firstSpan = items[0].Tag.Location.Span.Value;
                var textLine = firstSpan.Snapshot.GetLineFromPosition(firstSpan.Start);
                var lineExtent = textLine.Extent;
                var endPosition = lineExtent.End.Position;

                translated.Add(new MappingTagSpan<IInlineErrorTag>(items[0].Span, new InlineErrorTag(lineExtent, items)));
            }

            return translated;
        }

        protected override TagSpan<IInlineErrorTag> CreateTagSpan(IInlineErrorTag trackedTag, NormalizedSnapshotSpanCollection spans)
        {
            return new TagSpan<IInlineErrorTag>(trackedTag.LineExtent, trackedTag);
        }

        #endregion

        private static bool IsValidPrimaryLocation(IAnalysisIssueLocationVisualization locViz) =>
            locViz is IAnalysisIssueVisualization && locViz.Span.IsNavigable();
    }
}
