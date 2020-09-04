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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class FilteringTaggerBaseTests
    {
        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var aggregatorMock = new Mock<ITagAggregator<TrackedTag>>();
            aggregatorMock.SetupAdd(x => x.BatchedTagsChanged += (sender, args) => { });

            var testSubject = new TestableFilteringTagger(aggregatorMock.Object, ValidBuffer);

            aggregatorMock.VerifyAdd(x => x.BatchedTagsChanged += It.IsAny<EventHandler<BatchedTagsChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnsubscribesFromEvents()
        {
            var aggregatorMock = new Mock<ITagAggregator<TrackedTag>>();
            aggregatorMock.SetupAdd(x => x.BatchedTagsChanged += (sender, args) => { });

            var testSubject = new TestableFilteringTagger(aggregatorMock.Object, ValidBuffer);
            testSubject.Dispose();

            aggregatorMock.VerifyRemove(x => x.BatchedTagsChanged -= It.IsAny<EventHandler<BatchedTagsChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void GetSnapshot_CreatedWithBuffer_ReturnsExpectedValue()
        {
            var testSubject = new TestableFilteringTagger(Mock.Of<ITagAggregator<TrackedTag>>(), ValidBuffer);
            testSubject.GetSnapshotTestAccessor().Should().Be(ValidBuffer.CurrentSnapshot);
        }

        [TestMethod]
        public void GetSnapshot_CreatedWithTextView_ReturnsExpectedValue()
        {
            var textViewMock = new Mock<ITextView>();
            textViewMock.Setup(x => x.TextSnapshot).Returns(Mock.Of<ITextSnapshot>());

            var testSubject = new TestableFilteringTagger(Mock.Of<ITagAggregator<TrackedTag>>(), textViewMock.Object);
            testSubject.GetSnapshotTestAccessor().Should().Be(textViewMock.Object.TextSnapshot);
        }

        [TestMethod]
        public void OnAggregatorTagsChanged_NotifiesEditorOfChange()
        {
            var aggregatorMock = new Mock<ITagAggregator<TrackedTag>>();

            var testSubject = new TestableFilteringTagger(aggregatorMock.Object, ValidBuffer);

            SnapshotSpanEventArgs suppliedArgs = null;
            int eventCount = 0;
            Span expectedSpan = new Span(0, ValidBuffer.CurrentSnapshot.Length);
            SnapshotSpan expectedSnapshotSpan = new SnapshotSpan(ValidBuffer.CurrentSnapshot, new Span(0, ValidBuffer.CurrentSnapshot.Length));
            testSubject.TagsChanged += (sender, args) => { eventCount++; suppliedArgs = args; };

            // Act
            aggregatorMock.Raise(x => x.BatchedTagsChanged += null, new BatchedTagsChangedEventArgs(Array.Empty<IMappingSpan>()));

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.Span.Should().Be(expectedSnapshotSpan);
        }

        [TestMethod]
        public void GetTags_NoTags_ReturnsEmpty()
        {
            var validSpan = new Span(0, ValidBuffer.CurrentSnapshot.Length);
            var inputSpans = new NormalizedSnapshotSpanCollection(ValidBuffer.CurrentSnapshot, validSpan);

            var aggregatorMock = new Mock<ITagAggregator<TrackedTag>>();
            aggregatorMock.Setup(x => x.GetTags(inputSpans)).Returns(Array.Empty<IMappingTagSpan<TrackedTag>>());

            var testSubject = new TestableFilteringTagger(aggregatorMock.Object, ValidBuffer);

            testSubject.GetTags(inputSpans)
                .Should().BeEmpty();

            aggregatorMock.Verify(x => x.GetTags(inputSpans), Times.Once);
        }

        [TestMethod]
        public void GetTags_EmptyInputSpan_ReturnsEmpty()
        {
            var aggregatorMock = new Mock<ITagAggregator<TrackedTag>>();
            var inputSpans = new NormalizedSnapshotSpanCollection();

            var testSubject = new TestableFilteringTagger(aggregatorMock.Object, ValidBuffer);

            testSubject.GetTags(inputSpans)
                .Should().BeEmpty();

            aggregatorMock.Verify(x => x.GetTags(inputSpans), Times.Never);
        }

        [TestMethod]
        public void GetTags_HasTagsButFilterReturnsEmpty_ReturnsEmpty()
        {
            var buffer = ValidBuffer;
            var snapshot = buffer.CurrentSnapshot;

            // Using the same span for the input and the aggregator tag spans to make sure they overlap
            var validSpan = new Span(0, snapshot.Length);
            var inputSpans = new NormalizedSnapshotSpanCollection(snapshot, validSpan);

            var tagSpan1 = CreateMappingTagSpan(snapshot, new TrackedTag(), validSpan);
            var tagSpan2 = CreateMappingTagSpan(snapshot, new TrackedTag(), validSpan);
            var aggregator = CreateAggregator(tagSpan1, tagSpan2);

            var testSubject = new TestableFilteringTagger(aggregator, buffer);
            testSubject.SetFilterResult( /* empty */);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            testSubject.FilterCallCount.Should().Be(1);
            testSubject.LastFilterInput.Should().BeEquivalentTo(tagSpan1, tagSpan2);
            actual.Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_HasTagsAndFilterReturnsData_CreatesExpectedTagSpans()
        {
            var buffer = ValidBuffer;
            var snapshot = buffer.CurrentSnapshot;

            // Using the same span for the input and the aggregator tag spans to make sure they overlap
            var validSpan = new Span(0, snapshot.Length);
            var inputSpans = new NormalizedSnapshotSpanCollection(snapshot, validSpan);

            var tagSpan1 = CreateMappingTagSpan(snapshot, new TrackedTag(), validSpan);
            var tagSpan2 = CreateMappingTagSpan(snapshot, new TrackedTag(), validSpan);
            var tagSpan3 = CreateMappingTagSpan(snapshot, new TrackedTag(), validSpan);
            var aggregator = CreateAggregator(tagSpan1, tagSpan2, tagSpan3);

            var testSubject = new TestableFilteringTagger(aggregator, buffer);
            testSubject.SetFilterResult(tagSpan1, tagSpan3);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            testSubject.FilterCallCount.Should().Be(1);
            testSubject.LastFilterInput.Should().BeEquivalentTo(tagSpan1, tagSpan2, tagSpan3);
            actual.Should().NotBeEmpty();
            actual.Select(tagSpan => tagSpan.Tag.SuppliedTrackedTag).Should().BeEquivalentTo(tagSpan1.Tag, tagSpan3.Tag);
        }

        [TestMethod]
        public void GetTags_HasTagsButNoOverlappingSpans_ReturnsEmpty()
        {
            var buffer = CreateBufferWithSnapshot(length: 100);
            var snapshot = buffer.CurrentSnapshot;

            var inputSpan = new Span(20, 10);
            var inputSpans = new NormalizedSnapshotSpanCollection(snapshot, inputSpan);

            var tagSpan1 = CreateMappingTagSpan(snapshot, new TrackedTag(), new Span(0, 0));
            var tagSpan2 = CreateMappingTagSpan(snapshot, new TrackedTag(), new Span(1, 5));
            var tagSpan3 = CreateMappingTagSpan(snapshot, new TrackedTag(), new Span(31, 10));
            var aggregator = CreateAggregator(tagSpan1, tagSpan2, tagSpan3);

            var testSubject = new TestableFilteringTagger(aggregator, buffer);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            testSubject.FilterCallCount.Should().Be(1);
            testSubject.LastFilterInput.Should().BeEquivalentTo(tagSpan1, tagSpan2, tagSpan3);
            actual.Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_HasTagsWithOverlappingSpans_CreatesExpectedTags()
        {
            var buffer = CreateBufferWithSnapshot(length: 100);
            var snapshot = buffer.CurrentSnapshot;

            var inputSpan = new Span(20, 10);
            var inputSpans = new NormalizedSnapshotSpanCollection(snapshot, inputSpan);

            var tagSpan1 = CreateMappingTagSpan(snapshot, new TrackedTag(), new Span(0, 0));   // no overlap
            var tagSpan2 = CreateMappingTagSpan(snapshot, new TrackedTag(), new Span(1, 20));  // overlaps [20, 30]
            var tagSpan3 = CreateMappingTagSpan(snapshot, new TrackedTag(), new Span(30, 10)); // overlaps [20, 30]
            var tagSpan4 = CreateMappingTagSpan(snapshot, new TrackedTag(), new Span(31, 1));  // no overlap
            var aggregator = CreateAggregator(tagSpan1, tagSpan2, tagSpan3, tagSpan4);

            var testSubject = new TestableFilteringTagger(aggregator, buffer);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            testSubject.FilterCallCount.Should().Be(1);
            testSubject.LastFilterInput.Should().BeEquivalentTo(tagSpan1, tagSpan2, tagSpan3, tagSpan4);
            actual.Select(tagSpan => tagSpan.Tag.SuppliedTrackedTag).Should().BeEquivalentTo(tagSpan2.Tag, tagSpan3.Tag);
        }

        #region Supporting types

        public class TrackedTag : ITag // needs to be public to be mockable
        {
            private static int instanceCounter;
            public int Id { get; } = ++instanceCounter;
        }

        private class ProducedTag : ITag
        {
            public ProducedTag(TrackedTag trackedTag, NormalizedSnapshotSpanCollection spans)
            {
                SuppliedTrackedTag = trackedTag;
                SuppliedSpans = spans;
            }

            public TrackedTag SuppliedTrackedTag { get; }
            public NormalizedSnapshotSpanCollection SuppliedSpans { get; }
        }

        private class TestableFilteringTagger : FilteringTaggerBase<TrackedTag, ProducedTag>
        {
            #region Test helpers

            public int FilterCallCount { get; private set; }

            public IEnumerable<IMappingTagSpan<TrackedTag>> LastFilterInput { get; private set; }

            private IEnumerable<IMappingTagSpan<TrackedTag>> filterReturnValue;

            public void SetFilterResult(params IMappingTagSpan<TrackedTag>[] filteredValues)
            {
                this.filterReturnValue = filteredValues;
            }

            public ITextSnapshot GetSnapshotTestAccessor() => GetSnapshot();

            #endregion

            public readonly SnapshotSpan DummySnapshotSpan = new SnapshotSpan();

            public TestableFilteringTagger(ITagAggregator<TrackedTag> tagAggregator, ITextBuffer textBuffer)
                : base(tagAggregator, textBuffer)
            {
            }

            public TestableFilteringTagger(ITagAggregator<TrackedTag> tagAggregator, ITextView textView)
                : base(tagAggregator, textView)
            {
            }

            protected override TagSpan<ProducedTag> CreateTagSpan(TrackedTag trackedTag, NormalizedSnapshotSpanCollection spans)
            {
                return new TagSpan<ProducedTag>(DummySnapshotSpan, new ProducedTag(trackedTag, spans));
            }

            protected override IEnumerable<IMappingTagSpan<TrackedTag>> Filter(IEnumerable<IMappingTagSpan<TrackedTag>> trackedTagSpans)
            {
                LastFilterInput = trackedTagSpans;
                FilterCallCount++;

                return filterReturnValue ?? base.Filter(trackedTagSpans);
            }
        }

        #endregion
    }
}
