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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

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

            var testSubject = new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

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
        public void Ctor_SetsFilePath()
        {
            var testSubject = new LocationTagger(ValidBuffer, ValidStore, ValidSpanCalculator, ValidLogger);
            testSubject.FilePath.Should().Be(ValidBufferDocName);
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
        public void OnIssuesChanged_BufferFileIsChanged_TagsChangedIsRaised()
        {
            var storeMock = new Mock<IIssueLocationStore>();
            var testSubject = new LocationTagger(ValidBuffer, storeMock.Object, ValidSpanCalculator, ValidLogger);
            var initialTags = testSubject.TagSpans;

            SnapshotSpanEventArgs actualTagsChangedArgs = null;
            testSubject.TagsChanged += (senders, args) => actualTagsChangedArgs = args;

            RaiseIssuesChangedEvent(storeMock, ValidBufferDocName);

            actualTagsChangedArgs.Should().NotBeNull();
            // Changed span should be the whole snapshot
            actualTagsChangedArgs.Span.Start.Position.Should().Be(0);
            actualTagsChangedArgs.Span.End.Position.Should().Be(ValidBuffer.CurrentSnapshot.Length);

            testSubject.TagSpans.Should().NotBeSameAs(initialTags);
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
        public void BufferChanged_TagsAreUpdatedAndStoreRefreshed()
        {
            const int snapshotLength = 40;
            var bufferMock = CreateBufferMock(ValidBufferDocName);

            var snapshot = CreateSnapshot(length: snapshotLength, bufferMock.Object);
            var locSpan1 = new Span(1, 5);
            var locSpan2 = new Span(2, 5);
            var storeMock = CreateStoreWithLocationsWithSpans(snapshot, locSpan1, locSpan2);

            var testSubject = new LocationTagger(bufferMock.Object, storeMock, ValidSpanCalculator, ValidLogger);
            var initialTags = testSubject.TagSpans;

            SnapshotSpanEventArgs actualTagsChangedArgs = null;
            testSubject.TagsChanged += (senders, args) => actualTagsChangedArgs = args;

            RaiseBufferChangedEvent(bufferMock, snapshot, snapshot);

            // TagsChanged event should have been raised
            actualTagsChangedArgs.Should().NotBeNull();
            actualTagsChangedArgs.Span.Start.Position.Should().Be(0);
            actualTagsChangedArgs.Span.End.Position.Should().Be(snapshotLength);

            // Location store should have been notified
            CheckStoreRefreshWasCalled(storeMock, ValidBufferDocName);

            testSubject.TagSpans.Should().NotBeSameAs(initialTags);
        }

        [TestMethod]
        public void BufferChanged_NonCriticalException_IsSuppressed()
        {
            var bufferMock = CreateBufferMock(ValidBufferDocName);
            var snapshot = bufferMock.Object.CurrentSnapshot;
            var storeMock = new Mock<IIssueLocationStore>();

            var testSubject = new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

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

            var testSubject = new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

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

        private static Mock<ITextBuffer> CreateBufferMock(string filePath, int length = 999)
        {
            var snapshotMock = new Mock<ITextSnapshot>();
            snapshotMock.Setup(x => x.Length).Returns(length);

            var bufferMock = new Mock<ITextBuffer>();
            bufferMock.Setup(x => x.CurrentSnapshot).Returns(snapshotMock.Object);

            var properties = new Microsoft.VisualStudio.Utilities.PropertyCollection();
            bufferMock.Setup(x => x.Properties).Returns(properties);

            var docMock = new Mock<ITextDocument>();
            docMock.Setup(x => x.FilePath).Returns(filePath);
            properties[typeof(ITextDocument)] = docMock.Object;

            return bufferMock;
        }

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

        private static ITextVersion CreateTextVersion(ITextBuffer buffer, int versionNumber, ITextVersion nextVersion = null)
        {
            // Create an empty changes collection
            var changesMock = new Mock<INormalizedTextChangeCollection>();
            changesMock.Setup(x => x.Count).Returns(0);

            var versionMock = new Mock<ITextVersion>();
            versionMock.Setup(x => x.VersionNumber).Returns(versionNumber);
            versionMock.Setup(x => x.Length).Returns(buffer.CurrentSnapshot.Length);
            versionMock.Setup(x => x.TextBuffer).Returns(buffer);
            versionMock.Setup(x => x.Changes).Returns(changesMock.Object);
            versionMock.Setup(x => x.Next).Returns(nextVersion);

            return versionMock.Object;
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
    }
}
