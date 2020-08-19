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

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.BufferTagger
{
    partial class IssueLocationTagger
    {
        internal interface IBufferTagCalculator
        {
            IList<ITagSpan<IssueLocationTag>> GetTagSpans(IAnalysisIssueFlowVisualization flow, ITextSnapshot textSnapshot);
        }

        internal class BufferTagCalculator : IBufferTagCalculator
        {
            private readonly IIssueSpanCalculator issueSpanCalculator;

            public BufferTagCalculator() : this(new IssueSpanCalculator()) { }

            internal BufferTagCalculator(IIssueSpanCalculator issueSpanCalculator)
            {
                this.issueSpanCalculator = issueSpanCalculator;
            }

            public IList<ITagSpan<IssueLocationTag>> GetTagSpans(IAnalysisIssueFlowVisualization flow, ITextSnapshot textSnapshot)
            {
                var newTagSpans = new List<ITagSpan<IssueLocationTag>>();

                if (flow == null)
                {
                    return newTagSpans;
                }

                var filePath = GetFileName(textSnapshot);

                // Create tags for the other locations
                foreach (var locationViz in flow.Locations)
                {
                    SnapshotSpan? newSpan = CalculateSpanForLocation(filePath, locationViz.Location, textSnapshot);
                    if (newSpan.HasValue)
                    {
                        newTagSpans.Add(new TagSpan<IssueLocationTag>(newSpan.Value, new IssueLocationTag(locationViz)));
                    }
                }

                return newTagSpans;
            }

            private string GetFileName(ITextSnapshot textSnapshot)
            {
                ITextDocument newTextDocument = null;
                textSnapshot?.TextBuffer?.Properties?.TryGetProperty(typeof(ITextDocument), out newTextDocument);
                return newTextDocument?.FilePath;
            }

            private SnapshotSpan? CalculateSpanForLocation(string filePath, IAnalysisIssueLocation location, ITextSnapshot textSnapshot)
            {
                SnapshotSpan? newSpan = null;
                if (IsMatchingPath(filePath, location.FilePath))
                {
                    newSpan = issueSpanCalculator.CalculateSpan(location, textSnapshot);
                }
                return newSpan;
            }

            private static bool IsMatchingPath(string filePath, string issuePath)
            {
                if (issuePath == null)
                {
                    return false;
                }

                return issuePath.Equals(filePath, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
