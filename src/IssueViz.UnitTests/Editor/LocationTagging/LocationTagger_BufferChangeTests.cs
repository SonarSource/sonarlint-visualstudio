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
        public void BufferChanged_BufferChangedAboveTagSpans_AffectedSpanIsFromChangeUntilEndOfTagSpans()
        {
            const int bufferLength = 100;

            var tagSpans = new[]
            {
                new Span(15, 10),
                new Span(30, 5)
            };

            var changeSpans = new[]
            {
                new Span(3, 5),
                new Span(9, 1)
            };

            var expectedAffectedSpan = Span.FromBounds(3, 35);

            VerifyTagsRaisedWithCorrectAffectedSpan(bufferLength, tagSpans, changeSpans, expectedAffectedSpan);
        }

        [TestMethod]
        public void BufferChanged_BufferChangedBetweenTagSpans_AffectedSpanIsFromChangeUntilEndOfTagSpans()
        {
            const int bufferLength = 100;

            var tagSpans = new[]
            {
                new Span(15, 10),
                new Span(30, 5)
            };

            var changeSpans = new[]
            {
                new Span(20, 5),
                new Span(27, 1)
            };

            var expectedAffectedSpan = Span.FromBounds(20, 35);

            VerifyTagsRaisedWithCorrectAffectedSpan(bufferLength, tagSpans, changeSpans, expectedAffectedSpan);
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

            var changeSpans = new[]
            {
                new Span(50, 5),
                new Span(60, 1)
            };

            var expectedAffectedSpan = Span.FromBounds(50, bufferLength);

            VerifyTagsRaisedWithCorrectAffectedSpan(bufferLength, tagSpans, changeSpans, expectedAffectedSpan);
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
            var filePath = "test.cpp";
            var bufferMock = CreateBufferMock(length, filePath);

            var versionMock2 = CreateTextVersion(bufferMock.Object, 2);
            var versionMock1 = CreateTextVersion(bufferMock.Object, 1, nextVersion: versionMock2);

            var snapshotMock1 = CreateSnapshot(bufferMock.Object, length, versionMock1);
            var snapshotMock2 = CreateSnapshot(bufferMock.Object, length, versionMock2);

            var location = CreateLocationViz(snapshotMock1, new Span(1, 10));
            var storeMock = CreateLocationStore(filePath, location);

            var testSubject = CreateTestSubject(bufferMock.Object, storeMock);

            // Sanity checks
            testSubject.TagSpans.Count.Should().Be(1);
            testSubject.TagSpans[0].Span.Snapshot.Should().Be(snapshotMock1);
            testSubject.TagSpans[0].Tag.Location.Span.Value.Snapshot.Should().Be(snapshotMock1);

            RaiseBufferChangedEvent(bufferMock, snapshotMock1, snapshotMock2);

            testSubject.TagSpans.Count.Should().Be(1);
            testSubject.TagSpans[0].Span.Snapshot.Should().Be(snapshotMock2);
            testSubject.TagSpans[0].Tag.Location.Span.Value.Snapshot.Should().Be(snapshotMock2);
        }

        private IIssueLocationStore CreateLocationStore(string filePath, params IAnalysisIssueLocationVisualization[] locations)
        {
            var storeMock = new Mock<IIssueLocationStore>();

            storeMock.Setup(x => x.GetLocations(filePath)).Returns(locations);

            return storeMock.Object;
        }

        private void VerifyTagsRaisedWithCorrectAffectedSpan(int bufferLength, Span[] tagSpans, Span[] changeSpans, Span expectedAffectedSpan)
        {
            const string validBufferDocName = "test.cpp";
            var bufferMock = CreateBufferMock(filePath: validBufferDocName);

            var textChanges = changeSpans.Select(s =>
            {
                var textChange = new Mock<ITextChange>();
                textChange.Setup(x => x.NewSpan).Returns(s);
                return textChange.Object;
            }).ToArray();

            var versionMock2 = CreateTextVersion(bufferMock.Object, 2);
            var versionMock1 = CreateTextVersion(bufferMock.Object, 1, nextVersion: versionMock2, changeCollection: textChanges);

            var snapshotMock1 = CreateSnapshot(bufferMock.Object, bufferLength, versionMock1);
            var snapshotMock2 = CreateSnapshot(bufferMock.Object, bufferLength, versionMock2);

            var locations = tagSpans.Select(x => CreateLocationViz(snapshotMock1, x)).ToArray();
            var storeMock = CreateLocationStore(validBufferDocName, locations);

            var testSubject = CreateTestSubject(bufferMock.Object, storeMock);

            SnapshotSpanEventArgs actualTagsChangedArgs = null;
            testSubject.TagsChanged += (senders, args) => actualTagsChangedArgs = args;

            RaiseBufferChangedEvent(bufferMock, snapshotMock1, snapshotMock2);

            // TagsChanged event should have been raised
            actualTagsChangedArgs.Should().NotBeNull();
            actualTagsChangedArgs.Span.Start.Position.Should().Be(expectedAffectedSpan.Start);
            actualTagsChangedArgs.Span.End.Position.Should().Be(expectedAffectedSpan.End);

            // Location store should have been notified
            CheckStoreRefreshWasCalled(storeMock, validBufferDocName);
        }

        private static void CheckStoreRefreshWasCalled(IIssueLocationStore store, params string[] expectedFilePaths)
        {
            var storeMock = ((IMocked<IIssueLocationStore>)store).Mock;
            storeMock.Verify(x => x.Refresh(expectedFilePaths), Times.Once);
        }

        private static void RaiseBufferChangedEvent(Mock<ITextBuffer> bufferMock, ITextSnapshot before, ITextSnapshot after) =>
            bufferMock.Raise(x => x.ChangedLowPriority += null, new TextContentChangedEventArgs(before, after, EditOptions.DefaultMinimalChange, null));

        private static LocationTagger CreateTestSubject(ITextBuffer buffer = null, IIssueLocationStore store = null, IIssueSpanCalculator spanCalculator = null)
        {
            buffer = buffer ?? CreateBufferMock().Object;
            store = store ?? Mock.Of<IIssueLocationStore>();
            spanCalculator = spanCalculator ?? Mock.Of<IIssueSpanCalculator>();
            var logger = Mock.Of<ILogger>();

            return new LocationTagger(buffer, store, spanCalculator, logger);
        }
    }
}
