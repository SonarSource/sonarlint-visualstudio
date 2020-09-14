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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.LocationTagging
{
    [TestClass]
    public class LocationTaggerTests
    {
        private const string ValidBufferDocName = "doc.txt";
        private readonly ITextBuffer ValidBuffer = CreateBufferMock(ValidBufferDocName).Object;
        private readonly IIssueLocationStore ValidStore = Mock.Of<IIssueLocationStore>();
        private readonly IIssueSpanCalculator ValidSpanCalculator = Mock.Of<IIssueSpanCalculator>();
        private readonly ILogger ValidLogger = Mock.Of<ILogger>();

        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var storeMock = new Mock<IIssueLocationStore>();
            storeMock.SetupAdd(x => x.IssuesChanged += (sender, args) => { });

            var bufferMock = CreateBufferMock(ValidBufferDocName);
            bufferMock.SetupAdd(x => x.ChangedLowPriority += (sender, args) => { });

            new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

            storeMock.VerifyAdd(x => x.IssuesChanged += It.IsAny<EventHandler<IssuesChangedEventArgs>>(), Times.Once);
            bufferMock.VerifyAdd(x => x.ChangedLowPriority += It.IsAny<EventHandler<TextContentChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnsubscribesFromEvents()
        {
            var storeMock = new Mock<IIssueLocationStore>();
            storeMock.SetupAdd(x => x.IssuesChanged += (sender, args) => { });

            var bufferMock = CreateBufferMock(ValidBufferDocName);
            bufferMock.SetupAdd(x => x.ChangedLowPriority += (sender, args) => { });

            var testSubject = new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

            testSubject.Dispose();

            storeMock.VerifyRemove(x => x.IssuesChanged -= It.IsAny<EventHandler<IssuesChangedEventArgs>>(), Times.Once);
            bufferMock.VerifyRemove(x => x.ChangedLowPriority -= It.IsAny<EventHandler<TextContentChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Ctor_GetCurrentFilePath_ReturnsExpectedValue()
        {
            var bufferMock = CreateBufferMock("original");
            var testSubject = new LocationTagger(bufferMock.Object, ValidStore, ValidSpanCalculator, ValidLogger);
            testSubject.GetCurrentFilePath().Should().Be("original");

            // Check the name change is picked up
            ChangeMockedFileName(bufferMock, "new");
            testSubject.GetCurrentFilePath().Should().Be("new");
        }

        [TestMethod]
        public void Ctor_InitialisesTags()
        {
            var existingSpan = CreateUniqueSpan();
            var locVizWithSpan = CreateLocViz(existingSpan);
            var locVizWithoutSpan = CreateLocViz(null);

            var storeMock = new Mock<IIssueLocationStore>();
            storeMock.Setup(x => x.GetLocations(ValidBufferDocName)).Returns(new[] { locVizWithSpan, locVizWithoutSpan });

            var newSpan = CreateUniqueSpan();
            var calculatorMock = new Mock<IIssueSpanCalculator>();
            calculatorMock.Setup(x => x.CalculateSpan(locVizWithoutSpan.Location, It.IsAny<ITextSnapshot>())).Returns(newSpan);

            var testSubject = new LocationTagger(ValidBuffer, storeMock.Object, calculatorMock.Object, ValidLogger);

            testSubject.TagSpans.Should().NotBeNull();
            testSubject.TagSpans.Count.Should().Be(2);
            testSubject.TagSpans[0].Tag.Location.Should().BeSameAs(locVizWithSpan);
            testSubject.TagSpans[1].Tag.Location.Should().BeSameAs(locVizWithoutSpan);

            // Locations without spans should have one calculated for them
            testSubject.TagSpans[0].Tag.Location.Span.Value.Should().Be(existingSpan);
            testSubject.TagSpans[1].Tag.Location.Span.Value.Should().Be(newSpan);
        }

        [TestMethod]
        public void OnIssuesChanged_BufferFileNotChanged_TagsChangedNotRaised()
        {
            var storeMock = new Mock<IIssueLocationStore>();
            var testSubject = new LocationTagger(ValidBuffer, storeMock.Object, ValidSpanCalculator, ValidLogger);
            var initialTags = testSubject.TagSpans;
            initialTags.Should().NotBeNull(); // sanity check

            var eventCount = 0;
            testSubject.TagsChanged += (senders, args) => eventCount++;

            RaiseIssuesChangedEvent(storeMock, "not the file being tracked.txt");

            eventCount.Should().Be(0);
            testSubject.TagSpans.Should().BeSameAs(initialTags);
        }

        [TestMethod]
        public void OnIssuesChanged_BufferFileIsChanged_TagsChangedIsRaisedWithAffectedSpan()
        {
            var snapshot = CreateSnapshot(length: 100);
            var oldIssue1 = CreateLocViz(new SnapshotSpan(snapshot, 3, 1));
            var oldIssue2 = CreateLocViz(new SnapshotSpan(snapshot, 20, 2));
            var oldIssues = new[] {oldIssue1, oldIssue2};

            var storeMock = new Mock<IIssueLocationStore>();
            storeMock.Setup(x => x.GetLocations(ValidBufferDocName)).Returns(oldIssues);

            var testSubject = new LocationTagger(ValidBuffer, storeMock.Object, ValidSpanCalculator, ValidLogger);

            var newIssue1 = CreateLocViz(new SnapshotSpan(snapshot, 15, 10));
            var newIssue2 = CreateLocViz(new SnapshotSpan(snapshot, 25, 5));
            var newIssues = new[] { newIssue1, newIssue2 };

            storeMock.Setup(x => x.GetLocations(ValidBufferDocName)).Returns(newIssues);

            SnapshotSpanEventArgs actualTagsChangedArgs = null;
            testSubject.TagsChanged += (senders, args) => actualTagsChangedArgs = args;

            RaiseIssuesChangedEvent(storeMock, ValidBufferDocName);

            actualTagsChangedArgs.Should().NotBeNull();
            actualTagsChangedArgs.Span.Start.Position.Should().Be(3); // oldIssue1.Start
            actualTagsChangedArgs.Span.End.Position.Should().Be(30); // newIssue2.Start + newIssue2.Length
        }

        [TestMethod]
        public void OnIssuesChanged_FileIsRenamed_CurrentFileNameIsUsed()
        {
            const string originalName = "original file name.txt";
            const string changedName = "new file name.txt";

            var storeMock = new Mock<IIssueLocationStore>();
            var bufferMock = CreateBufferMock(originalName);
            
            new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

            // Check store is called with original name
            storeMock.Verify(x => x.GetLocations(originalName), Times.Once);
            ChangeMockedFileName(bufferMock, changedName);

            // Simulate issues changing
            RaiseIssuesChangedEvent(storeMock, changedName);

            // Check the store was notified using the new name
            storeMock.Verify(x => x.GetLocations(changedName), Times.Once);
        }

        [TestMethod]
        public void BufferChanged_FileIsRenamed_CurrentFileNameIsUsed()
        {
            const string originalName = "original file name.txt";
            const string changedName = "new file name.txt";

            var storeMock = new Mock<IIssueLocationStore>();
            var bufferMock = CreateBufferMock(originalName);
            
            new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

            // Check store is called with original name
            storeMock.Verify(x => x.GetLocations(originalName), Times.Once);

            ChangeMockedFileName(bufferMock, changedName);

            // Simulate an edit
            RaiseBufferChangedEvent(bufferMock, bufferMock.Object.CurrentSnapshot, bufferMock.Object.CurrentSnapshot);

            // Check the store was notified using the new name
            storeMock.Verify(x => x.Refresh(new[] { changedName }), Times.Once);
        }

        [TestMethod]
        public void GetTags_NoTags_ReturnsEmpty()
        {
            var testSubject = new LocationTagger(ValidBuffer, ValidStore, ValidSpanCalculator, ValidLogger);

            var validSpan = new Span(0, ValidBuffer.CurrentSnapshot.Length);
            var inputSpans = new NormalizedSnapshotSpanCollection(ValidBuffer.CurrentSnapshot, validSpan);

            testSubject.GetTags(inputSpans)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_EmptyInputSpan_ReturnsEmpty()
        {
            var testSubject = new LocationTagger(ValidBuffer, ValidStore, ValidSpanCalculator, ValidLogger);

            var inputSpans = new NormalizedSnapshotSpanCollection();

            testSubject.GetTags(inputSpans)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_NoOverlappingSpans_ReturnsEmpty()
        {
            // Make sure the tagger and NormalizedSpanCollection use the same snapshot
            // so we don't attempt to translate the spans
            var snapshot = CreateSnapshot(length: 40);
            var locSpan1 = new Span(1, 10);
            var locSpan2 = new Span(11, 10);
            var storeMock = CreateStoreWithLocationsWithSpans(snapshot, locSpan1, locSpan2);

            var inputSpans = new NormalizedSnapshotSpanCollection(snapshot, new Span(32, 5));

            var testSubject = new LocationTagger(ValidBuffer, storeMock, ValidSpanCalculator, ValidLogger);
            testSubject.TagSpans.Count().Should().Be(2); // sanity check

            var matchingTags = testSubject.GetTags(inputSpans);
            matchingTags.Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_HasOverlappingSpans_ReturnsExpectedTags()
        {
            // Make sure the tagger and NormalizedSpanCollection use the same snapshot
            // so we don't attempt to translate the spans
            var snapshot = CreateSnapshot(length: 40);
            var locSpan1 = new Span(1, 10);
            var locSpan2 = new Span(11, 10);
            var locSpan3 = new Span(21, 10);
            var storeMock = CreateStoreWithLocationsWithSpans(snapshot, locSpan1, locSpan2, locSpan3);

            var inputSpans = new NormalizedSnapshotSpanCollection(snapshot, new Span(18, 22));

            var testSubject = new LocationTagger(ValidBuffer, storeMock, ValidSpanCalculator, ValidLogger);
            testSubject.TagSpans.Count().Should().Be(3); // sanity check

            var matchingTags = testSubject.GetTags(inputSpans);
            matchingTags.Select(x => x.Span.Span).Should().BeEquivalentTo(locSpan2, locSpan3);
        }

        [TestMethod]
        public void GetTags_DifferentSnapshots_SnapshotsAreTranslated()
        {
            const int length = 100;
            var buffer = CreateBufferMock(ValidBufferDocName, length).Object;

            var versionMock2 = CreateTextVersion(buffer, 2);
            var versionMock1 = CreateTextVersion(buffer, 1, nextVersion: versionMock2);

            var snapshotMock1 = CreateSnapshot(length, buffer, versionMock1);
            var snapshotMock2 = CreateSnapshot(length, buffer, versionMock2);

            var locSpan1 = new Span(1, 10);
            var locSpan2 = new Span(11, 5);
            var storeMock = CreateStoreWithLocationsWithSpans(snapshotMock1, locSpan1, locSpan2);

            var inputSpans = new NormalizedSnapshotSpanCollection(snapshotMock2, new Span(18, 22));

            var testSubject = new LocationTagger(ValidBuffer, storeMock, ValidSpanCalculator, ValidLogger);

            // Sanity checks
            testSubject.TagSpans.Count().Should().Be(2);
            testSubject.TagSpans[0].Span.Snapshot.Should().Be(snapshotMock1);
            testSubject.TagSpans[1].Span.Snapshot.Should().Be(snapshotMock1);
            testSubject.TagSpans[0].Tag.Location.Span.Value.Snapshot.Should().Be(snapshotMock1);
            testSubject.TagSpans[1].Tag.Location.Span.Value.Snapshot.Should().Be(snapshotMock1);

            testSubject.GetTags(inputSpans).ToArray(); // GetTags is lazy so we need to reify the result to force evaluation

            testSubject.TagSpans.Count().Should().Be(2);
            testSubject.TagSpans[0].Span.Snapshot.Should().Be(snapshotMock2);
            testSubject.TagSpans[1].Span.Snapshot.Should().Be(snapshotMock2);

            testSubject.TagSpans[0].Tag.Location.Span.Value.Snapshot.Should().Be(snapshotMock2);
            testSubject.TagSpans[1].Tag.Location.Span.Value.Snapshot.Should().Be(snapshotMock2);
        }

        [TestMethod]
        public void BufferChanged_BufferChangedAboveTagSpans_AffectedSpanIsFromChangeUntilEndOfTagSpans()
        {
            const int bufferLength = 100;

            var tagSpans = new []
            {
                new Span(15, 10),
                new Span(30, 5)
            };

            var changeSpans = new []
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

            var tagSpans = new []
            {
                new Span(15, 10),
                new Span(30, 5)
            };

            var changeSpans = new []
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

            var tagSpans = new []
            {
                new Span(15, 10),
                new Span(30, 5)
            };

            var changeSpans = new []
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
            var bufferMock = CreateBufferMock(ValidBufferDocName);
            var snapshot = bufferMock.Object.CurrentSnapshot;
            var storeMock = new Mock<IIssueLocationStore>();

            new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

            storeMock.Invocations.Clear();
            storeMock.Setup(x => x.Refresh(It.IsAny<IEnumerable<string>>())).Throws(new InvalidOperationException("this is a test"));

            RaiseBufferChangedEvent(bufferMock, snapshot, snapshot);

            storeMock.Verify(x => x.Refresh(It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [TestMethod]
        public void BufferChanged_CriticalException_IsNotSuppressed()
        {
            var bufferMock = CreateBufferMock(ValidBufferDocName);
            var snapshot = bufferMock.Object.CurrentSnapshot;
            var storeMock = new Mock<IIssueLocationStore>();

            new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

            storeMock.Setup(x => x.Refresh(It.IsAny<IEnumerable<string>>())).Throws(new StackOverflowException("this is a test"));

            Action act = () => RaiseBufferChangedEvent(bufferMock, snapshot, snapshot);

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("this is a test");
        }

        [TestMethod]
        public void BufferChanged_SnapshotsAreTranslated()
        {
            const int length = 100;
            var bufferMock = CreateBufferMock(ValidBufferDocName, length);

            var versionMock2 = CreateTextVersion(bufferMock.Object, 2);
            var versionMock1 = CreateTextVersion(bufferMock.Object, 1, nextVersion: versionMock2);

            var snapshotMock1 = CreateSnapshot(length, bufferMock.Object, versionMock1);
            var snapshotMock2 = CreateSnapshot(length, bufferMock.Object, versionMock2);

            var locSpan1 = new Span(1, 10);
            var storeMock = CreateStoreWithLocationsWithSpans(snapshotMock1, locSpan1);

            var testSubject = new LocationTagger(bufferMock.Object, storeMock, ValidSpanCalculator, ValidLogger);

            // Sanity checks
            testSubject.TagSpans.Count().Should().Be(1);
            testSubject.TagSpans[0].Span.Snapshot.Should().Be(snapshotMock1);
            testSubject.TagSpans[0].Tag.Location.Span.Value.Snapshot.Should().Be(snapshotMock1);

            RaiseBufferChangedEvent(bufferMock, snapshotMock1, snapshotMock2);

            testSubject.TagSpans.Count().Should().Be(1);
            testSubject.TagSpans[0].Span.Snapshot.Should().Be(snapshotMock2);
            testSubject.TagSpans[0].Tag.Location.Span.Value.Snapshot.Should().Be(snapshotMock2);
        }

        private static Mock<ITextBuffer> CreateBufferMock(string validBufferDocName, int length = 999) =>
            TaggerTestHelper.CreateBufferMock(length, validBufferDocName);

        private static IAnalysisIssueLocationVisualization CreateLocViz(SnapshotSpan? span)
        {
            var locVizMock = new Mock<IAnalysisIssueLocationVisualization>();
            locVizMock.SetupProperty(x => x.Span);
            locVizMock.Object.Span = span;

            locVizMock.Setup(x => x.Location).Returns(Mock.Of<IAnalysisIssueLocation>());
            return locVizMock.Object;
        }

        private static int spanCounter;

        private static SnapshotSpan CreateUniqueSpan()
        {
            // NB SnapshotSpans are value types, so any "new SpanshotSpan() == new SpanshotSpan()"
            // If we want to differentiate between spans we can do so by creating
            // them with different lengths.
            var length = ++spanCounter;
            var snapshotMock = new Mock<ITextSnapshot>();
            snapshotMock.Setup(x => x.Length).Returns(length);

            return new SnapshotSpan(snapshotMock.Object, 0, length);
        }

        private static ITextSnapshot CreateSnapshot(int length, ITextBuffer buffer = null, ITextVersion version = null)
        {
            var snapshotMock = new Mock<ITextSnapshot>();
            snapshotMock.Setup(x => x.Length).Returns(length);

            snapshotMock.Setup(x => x.TextBuffer).Returns(buffer);
            snapshotMock.Setup(x => x.Version).Returns(version);
            return snapshotMock.Object;
        }

        private static IIssueLocationStore CreateStoreWithLocationsWithSpans(ITextSnapshot snapshot, params Span[] spans)
        {
            var storeMock = new Mock<IIssueLocationStore>();

            var locVizs = spans.Select(x =>
            {
                var snapshotSpan = new SnapshotSpan(snapshot, x);
                return CreateLocViz(snapshotSpan);
            }).ToArray();

            storeMock.Setup(x => x.GetLocations(It.IsAny<string>())).Returns(locVizs);
            return storeMock.Object;
        }

        private static ITextVersion CreateTextVersion(ITextBuffer buffer, int versionNumber, ITextVersion nextVersion = null, ITextChange[] changeCollection = null)
        {
            var versionMock = new Mock<ITextVersion>();
            versionMock.Setup(x => x.VersionNumber).Returns(versionNumber);
            versionMock.Setup(x => x.Length).Returns(buffer.CurrentSnapshot.Length);
            versionMock.Setup(x => x.TextBuffer).Returns(buffer);
            versionMock.Setup(x => x.Next).Returns(nextVersion);

            if (changeCollection != null)
            {
                var normalizedTextChangeCollection = new TestableNormalizedTextChangeCollection(changeCollection);
                versionMock.Setup(x => x.Changes).Returns(normalizedTextChangeCollection);
            }
            else
            {
                // Create an empty changes collection
                var changesMock = new Mock<INormalizedTextChangeCollection>();
                changesMock.Setup(x => x.Count).Returns(0);
                versionMock.Setup(x => x.Changes).Returns(changesMock.Object);
            }

            return versionMock.Object;
        }

        private static void ChangeMockedFileName(Mock<ITextBuffer> bufferMock, string newName)
        {
            var docMocked = (IMocked<ITextDocument>)bufferMock.Object.Properties[typeof(ITextDocument)];
            docMocked.Mock.Setup(x => x.FilePath).Returns(newName);
        }

        private static void RaiseIssuesChangedEvent(Mock<IIssueLocationStore> storeMock, params string[] fileNames) =>
            storeMock.Raise(x => x.IssuesChanged += null, new IssuesChangedEventArgs(fileNames));

        private static void RaiseBufferChangedEvent(Mock<ITextBuffer> bufferMock, ITextSnapshot before, ITextSnapshot after) =>
            bufferMock.Raise(x => x.ChangedLowPriority += null, new TextContentChangedEventArgs(before, after, EditOptions.DefaultMinimalChange, null));

        private static void CheckStoreRefreshWasCalled(IIssueLocationStore store, params string[] expectedFilePaths)
        {
            var storeMock = ((IMocked<IIssueLocationStore>)store).Mock;
            storeMock.Verify(x => x.Refresh(expectedFilePaths), Times.Once);
        }

        private void VerifyTagsRaisedWithCorrectAffectedSpan(int bufferLength, Span[] tagSpans, Span[] changeSpans, Span expectedAffectedSpan)
        {
            var bufferMock = CreateBufferMock(ValidBufferDocName);

            var textChanges = changeSpans.Select(s =>
            {
                var textChange = new Mock<ITextChange>();
                textChange.Setup(x => x.NewSpan).Returns(s);
                return textChange.Object;
            }).ToArray();

            var versionMock2 = CreateTextVersion(bufferMock.Object, 2);
            var versionMock1 = CreateTextVersion(bufferMock.Object, 1, nextVersion: versionMock2, changeCollection: textChanges);

            var snapshotMock1 = CreateSnapshot(bufferLength, bufferMock.Object, versionMock1);
            var snapshotMock2 = CreateSnapshot(bufferLength, bufferMock.Object, versionMock2);

            var storeMock = CreateStoreWithLocationsWithSpans(snapshotMock1, spans: tagSpans);

            var testSubject = new LocationTagger(bufferMock.Object, storeMock, ValidSpanCalculator, ValidLogger);

            SnapshotSpanEventArgs actualTagsChangedArgs = null;
            testSubject.TagsChanged += (senders, args) => actualTagsChangedArgs = args;

            RaiseBufferChangedEvent(bufferMock, snapshotMock1, snapshotMock2);

            // TagsChanged event should have been raised
            actualTagsChangedArgs.Should().NotBeNull();
            actualTagsChangedArgs.Span.Start.Position.Should().Be(expectedAffectedSpan.Start);
            actualTagsChangedArgs.Span.End.Position.Should().Be(expectedAffectedSpan.End);

            // Location store should have been notified
            CheckStoreRefreshWasCalled(storeMock, ValidBufferDocName);
        }

        private class TestableNormalizedTextChangeCollection : INormalizedTextChangeCollection
        {
            private readonly IList<ITextChange> changeCollection;

            public TestableNormalizedTextChangeCollection(IList<ITextChange> changeCollection)
            {
                this.changeCollection = changeCollection;
            }

            public IEnumerator<ITextChange> GetEnumerator() => changeCollection.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) changeCollection).GetEnumerator();
            public void Add(ITextChange item) => changeCollection.Add(item);
            public void Clear() => changeCollection.Clear();
            public bool Contains(ITextChange item) => changeCollection.Contains(item);
            public void CopyTo(ITextChange[] array, int arrayIndex) => changeCollection.CopyTo(array, arrayIndex);
            public bool Remove(ITextChange item) => changeCollection.Remove(item);
            public int Count => changeCollection.Count;
            public bool IsReadOnly => changeCollection.IsReadOnly;
            public int IndexOf(ITextChange item) => changeCollection.IndexOf(item);
            public void Insert(int index, ITextChange item) => changeCollection.Insert(index, item);
            public void RemoveAt(int index) => changeCollection.RemoveAt(index);

            public ITextChange this[int index]
            {
                get => changeCollection[index];
                set => changeCollection[index] = value;
            }

            public bool IncludesLineChanges { get; }
        }
    }
}
