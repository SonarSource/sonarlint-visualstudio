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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Highlight;
using SonarLint.VisualStudio.IssueVisualization.Models;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.SelectedIssueTagging.Highlighting
{
    [TestClass]
    public class IssueHighlightViewTaggerTests
    {
        [TestMethod]
        public void GetTags_NoSelectedIssueLocationTags_ReturnsEmpty()
        {
            var snapshot = CreateSnapshot(length: 50);
            var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);

            var aggregator = CreateSelectedIssueAggregator();
            var viewMock = CreateValidTextView(snapshot);

            var testSubject = new IssueHighlightTagger(aggregator, viewMock);

            // Act
            testSubject.GetTags(inputSpans)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_HasSelectedIssueLocationTags_ReturnsExpectedHighlightTags()
        {
            var snapshot = CreateSnapshot(length: 50);
            var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);

            var selectedLoc1 = CreateLocationViz(snapshot, new Span(1, 5), "selection 1");
            var selectedLoc2 = CreateLocationViz(snapshot, new Span(20, 25), "selection 2");
            var aggregator = CreateSelectedIssueAggregator(selectedLoc1, selectedLoc2);

            var viewMock = CreateValidTextView(snapshot);

            var testSubject = new IssueHighlightTagger(aggregator, viewMock);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            actual.Length.Should().Be(2);
            actual[0].Span.Span.Should().Be(selectedLoc1.Span.Value.Span);
            actual[1].Span.Span.Should().Be(selectedLoc2.Span.Value.Span);
        }

        private static IWpfTextView CreateValidTextView(ITextSnapshot snapshot)
        {
            var viewMock = new Mock<IWpfTextView>();
            viewMock.Setup(x => x.TextSnapshot).Returns(snapshot);
            return viewMock.Object;
        }

        private static ITagAggregator<ISelectedIssueLocationTag> CreateSelectedIssueAggregator(params IAnalysisIssueLocationVisualization[] locVizs)
        {
            var tagSpans = locVizs
                .Select(CreateTagSpanWithLocViz)
                .ToArray();

            return CreateAggregator(tagSpans);
        }

        private static IMappingTagSpan<ISelectedIssueLocationTag> CreateTagSpanWithLocViz(IAnalysisIssueLocationVisualization locViz)
        {
            var tag = CreateSelectedLocationTag(locViz);
            return CreateMappingTagSpan(locViz.Span.Value.Snapshot, tag, locViz.Span.Value.Span);
        }
    }
}
