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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Buffer;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.SelectedIssueTagging.Buffer
{
    [TestClass, Ignore]
    public class SelectedIssueLocationTaggerTests
    {
        private readonly ITagAggregator<IIssueLocationTag> ValidAggregator = new Mock<ITagAggregator<IIssueLocationTag>>().Object;

        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var aggregatorMock = new Mock<ITagAggregator<IIssueLocationTag>>();
            aggregatorMock.SetupAdd(x => x.BatchedTagsChanged += (sender, args) => { });

            var bufferMock = CreateBufferMock();

            var selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            selectionServiceMock.SetupAdd(x => x.SelectionChanged += (sender, args) => { });

            // Act
            var testSubject = new SelectedIssueLocationTagger(aggregatorMock.Object, bufferMock.Object, selectionServiceMock.Object);

            aggregatorMock.VerifyAdd(x => x.BatchedTagsChanged += It.IsAny<EventHandler<BatchedTagsChangedEventArgs>>(), Times.Once);
            selectionServiceMock.VerifyAdd(x => x.SelectionChanged += It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once);

            // Should not listen to any buffer events
            bufferMock.Invocations.Count.Should().Be(0);
            bufferMock.VerifyAdd(x => x.ChangedLowPriority += It.IsAny<EventHandler<TextContentChangedEventArgs>>(), Times.Never);
        }

        [TestMethod]
        public void Dispose_UnsubscribesFromEvents()
        {
            var aggregatorMock = new Mock<ITagAggregator<IIssueLocationTag>>();
            aggregatorMock.SetupRemove(x => x.BatchedTagsChanged -= (sender, args) => { });

            var bufferMock = CreateBufferMock();

            var selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            selectionServiceMock.SetupRemove(x => x.SelectionChanged -= (sender, args) => { });

            var testSubject = new SelectedIssueLocationTagger(aggregatorMock.Object, bufferMock.Object, selectionServiceMock.Object);

            // Act
            testSubject.Dispose();

            aggregatorMock.VerifyRemove(x => x.BatchedTagsChanged -= It.IsAny<EventHandler<BatchedTagsChangedEventArgs>>(), Times.Once);
            selectionServiceMock.VerifyRemove(x => x.SelectionChanged -= It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once);
            bufferMock.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void OnSelectionChanged_ChangeLevelIsLocation_EditorIsNotNotified()
        {
            var selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            var testSubject = new SelectedIssueLocationTagger(ValidAggregator, ValidBuffer, selectionServiceMock.Object);

            var tagsChangedEventCount = 0;
            testSubject.TagsChanged += (sender, args) => tagsChangedEventCount++;

            // Act
            selectionServiceMock.Raise(x => x.SelectionChanged += null, new SelectionChangedEventArgs(SelectionChangeLevel.Location, null, null, null));

            tagsChangedEventCount.Should().Be(0);
        }

        [TestMethod]
        [DataRow(SelectionChangeLevel.Flow)]
        [DataRow(SelectionChangeLevel.Issue)]
        public void OnSelectionChanged_ChangeLevelIsNotLocation_EditorIsNotified(SelectionChangeLevel changeLevel)
        {
            var selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            var testSubject = new SelectedIssueLocationTagger(ValidAggregator, ValidBuffer, selectionServiceMock.Object);

            var tagsChangedEventCount = 0;
            SnapshotSpanEventArgs actualEventArgs = null;
            testSubject.TagsChanged += (sender, args) => { tagsChangedEventCount++; actualEventArgs = args; };

            // Act
            selectionServiceMock.Raise(x => x.SelectionChanged += null, new SelectionChangedEventArgs(changeLevel, null, null, null));

            tagsChangedEventCount.Should().Be(1);
            actualEventArgs.Should().NotBeNull();
            actualEventArgs.Span.Start.Position.Should().Be(0);
            actualEventArgs.Span.End.Position.Should().Be(ValidBuffer.CurrentSnapshot.Length);
            actualEventArgs.Span.Snapshot.Should().Be(ValidBuffer.CurrentSnapshot);
        }

        [TestMethod]
        public void GetTags_NoSelectedFlow_ReturnsEmpty()
        {
            var snapshot = CreateSnapshot(length: 50);
            var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);

            var selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            selectionServiceMock.Setup(x => x.SelectedFlow).Returns((IAnalysisIssueFlowVisualization)null); // no selected flow

            var locViz = CreateLocationViz(snapshot, new Span(1, 1), "tagged location");
            var aggregator = CreateLocationAggregator(locViz);

            var testSubject = new SelectedIssueLocationTagger(aggregator, snapshot.TextBuffer, selectionServiceMock.Object);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            actual.Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_NoMatchingLocations_ReturnsEmpty()
        {
            var snapshot = CreateSnapshot(length: 50);
            var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);

            var selectedLoc1 = CreateLocationViz(snapshot, new Span(1, 5), "selection 1");
            var selectedLoc2 = CreateLocationViz(snapshot, new Span(20, 25), "selection 2");
            var selectionService = CreateSelectionService(selectedLoc1, selectedLoc2);

            var taggedLoc1 = CreateLocationViz(snapshot, new Span(10, 5), "tag 1");
            var taggedLoc2 = CreateLocationViz(snapshot, new Span(12, 5), "tag 1 ");
            var aggregator = CreateLocationAggregator(taggedLoc1, taggedLoc2);

            var testSubject = new SelectedIssueLocationTagger(aggregator, snapshot.TextBuffer, selectionService);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            actual.Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_TagsMatchSelectedIssuesLocations_CreatesExpectedTags()
        {
            var snapshot = CreateSnapshot(length: 50);
            var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);

            var matchingLoc1 = CreateLocationViz(snapshot, new Span(1, 5), "match 1");
            var matchingLoc2 = CreateLocationViz(snapshot, new Span(20, 25), "match 2");
            var noMatch1 = CreateLocationViz(snapshot, new Span(10, 5), "no match 1");
            var noMatch2 = CreateLocationViz(snapshot, new Span(12, 5), "no match 2");

            var selectionService = CreateSelectionService(matchingLoc1, matchingLoc2, noMatch1);
            var aggregator = CreateLocationAggregator(noMatch2, matchingLoc1, matchingLoc2);

            var testSubject = new SelectedIssueLocationTagger(aggregator, snapshot.TextBuffer, selectionService);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            actual.Length.Should().Be(2);
            actual[0].Tag.Location.Should().Be(matchingLoc1);
            actual[0].Span.Should().Be(matchingLoc1.Span.Value);

            actual[1].Tag.Location.Should().Be(matchingLoc2);
            actual[1].Tag.Location.Should().Be(matchingLoc2);
            actual[1].Span.Should().Be(matchingLoc2.Span.Value);
        }

        private static IAnalysisIssueSelectionService CreateSelectionService(params IAnalysisIssueLocationVisualization[] selectedLocations)
        {
            var flow = CreateFlowViz(selectedLocations);
            var selectionService = new Mock<IAnalysisIssueSelectionService>();
            selectionService.Setup(x => x.SelectedFlow).Returns(flow);

            return selectionService.Object;
        }

        private static ITagAggregator<IIssueLocationTag> CreateLocationAggregator(params IAnalysisIssueLocationVisualization[] locVizs)
        {
            var tagSpans = locVizs
                .Select(CreateTagSpanWithLocViz)
                .ToArray();

            return CreateAggregator(tagSpans);
        }

        private static IMappingTagSpan<IIssueLocationTag> CreateTagSpanWithLocViz(IAnalysisIssueLocationVisualization locViz)
        {
            var tag = CreateIssueLocationTag(locViz);
            return CreateMappingTagSpan(locViz.Span.Value.Snapshot, tag, locViz.Span.Value.Span);
        }
    }
}
