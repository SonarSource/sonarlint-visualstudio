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
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment;
using static SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment.IssueLocationAdornmentTagger;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.SelectedIssueTagging.Adornment
{
    [TestClass]
    public class IssueLocationAdornmentTaggerTests
    {
        private static readonly IWpfTextView ValidTextView = CreateWpfTextView();

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var aggregatorMock = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            aggregatorMock.SetupAdd(x => x.BatchedTagsChanged += (sender, args) => { });

            // Act
            var testSubject = new IssueLocationAdornmentTagger(aggregatorMock.Object, ValidTextView);

            var expectedCallCount = Times.Exactly(2); // once for our class, once for the base class
            aggregatorMock.VerifyAdd(x => x.BatchedTagsChanged += It.IsAny<EventHandler<BatchedTagsChangedEventArgs>>(), expectedCallCount);
        }

        [TestMethod]
        public void Dispose_UnsubscribesFromEvents()
        {
            var aggregatorMock = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            aggregatorMock.SetupRemove(x => x.BatchedTagsChanged -= (sender, args) => { });

            var testSubject = new IssueLocationAdornmentTagger(aggregatorMock.Object, ValidTextView);

            // Act
            testSubject.Dispose();

            var expectedCallCount = Times.Exactly(2); // once for our class, once for the base class
            aggregatorMock.VerifyRemove(x => x.BatchedTagsChanged -= It.IsAny<EventHandler<BatchedTagsChangedEventArgs>>(), expectedCallCount);
        }

        [TestMethod]
        public void OnBatchedTagsChanged_CacheCleanupIsCalled()
        {
            var cacheMock = new Mock<ICachingAdornmentFactory>();

            var snapshot = CreateSnapshot(length: 123);
            var textView = CreateWpfTextView(snapshot);
            var locViz1 = CreateLocationViz();
            var locViz2 = CreateLocationViz();

            // The setup for the aggregator is different for this test since we're using a different
            // overload of "GetTags" from all of the other tests.
            // Here, we're expecting a SnapshotSpan covering the whole snapshot to be passed in
            var wholeSnapshotSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);

            var mappingSpan1 = CreateMappingTagSpan(snapshot, new SelectedIssueLocationTag(locViz1));
            var mappingSpan2 = CreateMappingTagSpan(snapshot, new SelectedIssueLocationTag(locViz2));
            var aggregatorMock = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            aggregatorMock.Setup(x => x.GetTags(wholeSnapshotSpan))
                .Returns(new[] { mappingSpan1, mappingSpan2 });

            var testSubject = new IssueLocationAdornmentTagger(aggregatorMock.Object, textView, cacheMock.Object);
            cacheMock.Invocations.Count.Should().Be(0);

            // Act
            RaiseBatchedTagsChanged(aggregatorMock);
            aggregatorMock.Verify(x => x.GetTags(wholeSnapshotSpan), Times.Once);
            cacheMock.Verify(x => x.RemoveUnused(new[] { locViz1, locViz2 }), Times.Once);
        }

        [TestMethod]
        public void GetTags_NoSelectedIssueLocationTags_ReturnsEmpty()
        {
            var snapshot = CreateSnapshot(length: 50);
            var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);

            var aggregator = CreateSelectedIssueAggregator();
            var viewMock = CreateWpfTextView(snapshot);

            var testSubject = new IssueLocationAdornmentTagger(aggregator, viewMock);

            // Act
            testSubject.GetTags(inputSpans)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_HasSelectedIssueLocationTags_ReturnsExpectedAdornmentTags()
        {
            var snapshot = CreateSnapshot(length: 50);
            var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);

            var selectedLoc1 = CreateLocationViz(snapshot, new Span(1, 5), "selection 1");
            var selectedLoc2 = CreateLocationViz(snapshot, new Span(20, 25), "selection 2");
            var aggregator = CreateSelectedIssueAggregator(selectedLoc1, selectedLoc2);

            var viewMock = CreateWpfTextView(snapshot);

            var testSubject = new IssueLocationAdornmentTagger(aggregator, viewMock);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            actual.Length.Should().Be(2);
            actual[0].Span.Span.Start.Should().Be(selectedLoc1.Span.Value.Span.Start);
            actual[1].Span.Span.Start.Should().Be(selectedLoc2.Span.Value.Span.Start);

            actual[0].Span.Span.Length.Should().Be(0);
            actual[1].Span.Span.Length.Should().Be(0);

            var adornment1 = actual[0].Tag.Adornment as IssueLocationAdornment;
            adornment1.Should().NotBeNull();
            adornment1.LocationViz.Should().Be(selectedLoc1);

            var adornment2 = actual[1].Tag.Adornment as IssueLocationAdornment;
            adornment2.Should().NotBeNull();
            adornment2.LocationViz.Should().Be(selectedLoc2);
        }
    }
}
