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
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.LocationTagging
{
    [TestClass]
    public class LocationTagger_BufferChangeTests
    {
        [TestMethod]
        public void BufferChanged_FileIsRenamed_CurrentFileNameIsUsed()
        {
            const string originalName = "original file name.txt";
            const string changedName = "new file name.txt";

            var storeMock = new Mock<IIssueLocationStore>();
            var bufferMock = CreateBufferMock(filePath: originalName);

            CreateTestSubject(bufferMock.Object, storeMock.Object);

            // Check store is called with original name
            storeMock.Verify(x => x.GetLocations(originalName), Times.Once);

            RenameBufferFile(bufferMock, changedName);

            // Simulate an edit
            RaiseBufferChangedEvent(bufferMock, bufferMock.Object.CurrentSnapshot, bufferMock.Object.CurrentSnapshot);

            // Check the store was notified using the new name
            storeMock.Verify(x => x.Refresh(new[] { changedName }), Times.Once);
        }

        [TestMethod]
        public void BufferChanged_BufferChangedInsideTagSpan_TagSpanInvalidated()
        {
            var tagSpans = new[]
            {
                new Span(15, 10),
                new Span(30, 5)
            };

            var deletedSpan = new Span(17, 3);

            const string validBufferDocName = "a.cpp";
            const int bufferLength = 100;

            var buffer = CreateBufferMock(bufferLength, validBufferDocName);
            var (beforeSnapshot, afterSnapshot) = CreateSnapshotChange(buffer.Object, bufferLength, deletedSpan);

            var locations = tagSpans.Select(span => CreateLocationViz(beforeSnapshot, span)).ToArray();
            var store = CreateLocationStore(validBufferDocName, locations);

            var testSubject = CreateTestSubject(buffer.Object, store);
            RaiseBufferChangedEvent(buffer, beforeSnapshot, afterSnapshot);

            testSubject.TagSpans.Count.Should().Be(1);
            testSubject.TagSpans[0].Tag.Location.Should().Be(locations[1]);
            locations[0].Span.Value.IsEmpty.Should().BeTrue();

            var postDeletionTranslatedSpan = new Span(tagSpans[1].Start - deletedSpan.Length, tagSpans[1].Length);
            locations[1].Span.Value.Span.Should().Be(postDeletionTranslatedSpan);
        }

        [TestMethod]
        public void BufferChanged_BufferChangedAboveTagSpans_AffectedSpanIsFromChangeUntilEndOfTagSpans()
        {
            var tagSpans = new[]
            {
                new Span(15, 10),
                new Span(30, 5)
            };

            var deletedSpan = new Span(9, 4);

            var expectedAffectedSpan = Span.FromBounds(deletedSpan.Start, tagSpans[1].Start + tagSpans[1].Length - deletedSpan.Length);

            VerifyTagsRaisedWithCorrectAffectedSpan(tagSpans, deletedSpan, expectedAffectedSpan);
        }

        [TestMethod]
        public void BufferChanged_BufferChangedBetweenTagSpans_AffectedSpanIsFromChangeUntilEndOfTagSpans()
        {
            var tagSpans = new[]
            {
                new Span(15, 10),
                new Span(30, 5)
            };

            var deletedSpan = new Span(20, 3);

            var expectedAffectedSpan = Span.FromBounds(deletedSpan.Start, tagSpans[1].Start + tagSpans[1].Length - deletedSpan.Length);

            VerifyTagsRaisedWithCorrectAffectedSpan(tagSpans, deletedSpan, expectedAffectedSpan);
        }

        [TestMethod]
        public void BufferChanged_BufferChangedBelowTagSpans_AffectedSpanIsFromChangeUntilEndOfBuffer()
        {
            const int bufferLength = 100;

            var tagSpans = new[]
            {
                new Span(15, 10),
                new Span(30, 5)
            };

            var deletedSpan = new Span(50, 5);

            var expectedAffectedSpan = Span.FromBounds(deletedSpan.Start, bufferLength);

            VerifyTagsRaisedWithCorrectAffectedSpan(tagSpans, deletedSpan, expectedAffectedSpan, bufferLength);
        }

        [TestMethod]
        public void BufferChanged_NonCriticalException_IsSuppressed()
        {
            var bufferMock = CreateBufferMock();
            var snapshot = bufferMock.Object.CurrentSnapshot;
            var storeMock = new Mock<IIssueLocationStore>();

            CreateTestSubject(bufferMock.Object, storeMock.Object);

            storeMock.Invocations.Clear();
            storeMock.Setup(x => x.Refresh(It.IsAny<IEnumerable<string>>())).Throws(new InvalidOperationException("this is a test"));

            RaiseBufferChangedEvent(bufferMock, snapshot, snapshot);

            storeMock.Verify(x => x.Refresh(It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [TestMethod]
        public void BufferChanged_CriticalException_IsNotSuppressed()
        {
            var bufferMock = CreateBufferMock();
            var snapshot = bufferMock.Object.CurrentSnapshot;
            var storeMock = new Mock<IIssueLocationStore>();

            CreateTestSubject(bufferMock.Object, storeMock.Object);

            storeMock.Setup(x => x.Refresh(It.IsAny<IEnumerable<string>>())).Throws(new StackOverflowException("this is a test"));

            Action act = () => RaiseBufferChangedEvent(bufferMock, snapshot, snapshot);

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("this is a test");
        }

        [TestMethod]
        public void BufferChanged_SnapshotsAreTranslated()
        {
            const int length = 100;
            const string filePath = "test.cpp";

            var buffer = CreateBufferMock(length, filePath);
            var (beforeSnapshot, afterSnapshot) = CreateSnapshotChange(buffer.Object, length, new SnapshotSpan());

            var location = CreateLocationViz(beforeSnapshot, new Span(1, 10));
            var store = CreateLocationStore(filePath, location);
            var testSubject = CreateTestSubject(buffer.Object, store);

            // Sanity checks
            testSubject.TagSpans.Count.Should().Be(1);
            testSubject.TagSpans[0].Span.Snapshot.Should().Be(beforeSnapshot);
            testSubject.TagSpans[0].Tag.Location.Span.Value.Snapshot.Should().Be(beforeSnapshot);

            RaiseBufferChangedEvent(buffer, beforeSnapshot, afterSnapshot);

            testSubject.TagSpans.Count.Should().Be(1);
            testSubject.TagSpans[0].Span.Snapshot.Should().Be(afterSnapshot);
            testSubject.TagSpans[0].Tag.Location.Span.Value.Snapshot.Should().Be(afterSnapshot);
        }

        private static void CheckStoreRefreshWasCalled(IIssueLocationStore store, params string[] expectedFilePaths)
        {
            var storeMock = ((IMocked<IIssueLocationStore>)store).Mock;
            storeMock.Verify(x => x.Refresh(expectedFilePaths), Times.Once);
        }

        private static void RaiseBufferChangedEvent(Mock<ITextBuffer> bufferMock, ITextSnapshot before, ITextSnapshot after) =>
            bufferMock.Raise(x => x.ChangedLowPriority += null, new TextContentChangedEventArgs(before, after, EditOptions.DefaultMinimalChange, null));

        private static LocationTagger CreateTestSubject(ITextBuffer buffer, IIssueLocationStore store) =>
            new LocationTagger(buffer, store, Mock.Of<IIssueSpanCalculator>(), Mock.Of<ILogger>());

        private static IIssueLocationStore CreateLocationStore(string filePath, params IAnalysisIssueLocationVisualization[] locations)
        {
            var storeMock = new Mock<IIssueLocationStore>();

            storeMock.Setup(x => x.GetLocations(filePath)).Returns(locations);

            return storeMock.Object;
        }

        private static Tuple<ITextSnapshot, ITextSnapshot> CreateSnapshotChange(ITextBuffer buffer, int bufferLength, Span deletedSpan)
        {
            var textChanges = new[] {CreateTextChange(deletedSpan)};

            var versionMock2 = CreateTextVersion(buffer, 2);
            var versionMock1 = CreateTextVersion(buffer, 1, nextVersion: versionMock2, changeCollection: textChanges);

            var beforeSnapshot = CreateSnapshot(buffer, bufferLength, versionMock1);
            var afterSnapshot = CreateSnapshot(buffer, bufferLength, versionMock2);

            return new Tuple<ITextSnapshot, ITextSnapshot>(beforeSnapshot, afterSnapshot);
        }

        private static ITextChange CreateTextChange(Span oldSpan)
        {
            var newSpan = new Span(oldSpan.Start, 0); // simulate that the text in span was deleted
            var textChange = new Mock<ITextChange>();

            textChange.Setup(x => x.OldSpan).Returns(oldSpan);
            textChange.Setup(x => x.OldPosition).Returns(oldSpan.Start);
            textChange.Setup(x => x.OldEnd).Returns(oldSpan.End);
            textChange.Setup(x => x.OldLength).Returns(oldSpan.Length);

            textChange.Setup(x => x.NewSpan).Returns(newSpan);
            textChange.Setup(x => x.NewPosition).Returns(newSpan.Start);
            textChange.Setup(x => x.NewEnd).Returns(newSpan.End);
            textChange.Setup(x => x.NewLength).Returns(newSpan.Length);

            return textChange.Object;
        }

        private void VerifyTagsRaisedWithCorrectAffectedSpan(Span[] tagSpans, Span deletedSpans, Span expectedAffectedSpan, int bufferLength = 100)
        {
            const string validBufferDocName = "a.cpp";

            var buffer = CreateBufferMock(bufferLength, validBufferDocName);
            var (beforeSnapshot, afterSnapshot) = CreateSnapshotChange(buffer.Object, bufferLength, deletedSpans);

            var locations = tagSpans.Select(span => CreateLocationViz(beforeSnapshot, span));
            var store = CreateLocationStore(validBufferDocName, locations.ToArray());

            var testSubject = CreateTestSubject(buffer.Object, store);

            SnapshotSpanEventArgs actualTagsChangedArgs = null;
            testSubject.TagsChanged += (sender, args) => actualTagsChangedArgs = args;

            RaiseBufferChangedEvent(buffer, beforeSnapshot, afterSnapshot);

            // TagsChanged event should have been raised
            actualTagsChangedArgs.Should().NotBeNull();
            actualTagsChangedArgs.Span.Start.Position.Should().Be(expectedAffectedSpan.Start);
            actualTagsChangedArgs.Span.End.Position.Should().Be(expectedAffectedSpan.End);

            // Location store should have been notified
            CheckStoreRefreshWasCalled(store, validBufferDocName);
        }
    }
}
