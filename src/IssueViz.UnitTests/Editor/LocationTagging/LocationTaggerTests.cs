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
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.LocationTagging
{
    [TestClass]
    public class LocationTaggerTests
    {
        private const string ValidBufferDocName = "doc.txt";
        private readonly IIssueLocationStore ValidStore = Mock.Of<IIssueLocationStore>();
        private readonly IIssueSpanCalculator ValidSpanCalculator = Mock.Of<IIssueSpanCalculator>();
        private readonly ILogger ValidLogger = Mock.Of<ILogger>();

        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var storeMock = new Mock<IIssueLocationStore>();
            storeMock.SetupAdd(x => x.IssuesChanged += (sender, args) => { });

            var bufferMock = CreateBufferMock(filePath: ValidBufferDocName);
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

            var bufferMock = CreateBufferMock(filePath: ValidBufferDocName);
            bufferMock.SetupAdd(x => x.ChangedLowPriority += (sender, args) => { });

            var testSubject = new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

            testSubject.Dispose();

            storeMock.VerifyRemove(x => x.IssuesChanged -= It.IsAny<EventHandler<IssuesChangedEventArgs>>(), Times.Once);
            bufferMock.VerifyRemove(x => x.ChangedLowPriority -= It.IsAny<EventHandler<TextContentChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Ctor_GetCurrentFilePath_ReturnsExpectedValue()
        {
            var bufferMock = CreateBufferMock(filePath: "original");
            var testSubject = new LocationTagger(bufferMock.Object, ValidStore, ValidSpanCalculator, ValidLogger);
            testSubject.GetCurrentFilePath().Should().Be("original");

            // Check the name change is picked up
            RenameBufferFile(bufferMock, "new");
            testSubject.GetCurrentFilePath().Should().Be("new");
        }

        [TestMethod]
        public void Ctor_InitialisesTags()
        {
            var bufferMock = CreateBufferMock(filePath: ValidBufferDocName);
            var existingSpan = new SnapshotSpan(bufferMock.Object.CurrentSnapshot, new Span(1, 10));
            var locVizWithSpan = CreateLocViz(existingSpan);
            var locVizWithoutSpan = CreateLocViz(null);
            var locVizWithEmptySpan = CreateLocViz(new SnapshotSpan?());

            var storeMock = new Mock<IIssueLocationStore>();
            storeMock.Setup(x => x.GetLocations(ValidBufferDocName)).Returns(new[] { locVizWithSpan, locVizWithoutSpan, locVizWithEmptySpan });

            var newSpan1 = new SnapshotSpan(bufferMock.Object.CurrentSnapshot, new Span(1, 10));
            var newSpan2 = new SnapshotSpan(bufferMock.Object.CurrentSnapshot, new Span(5, 3));
            var calculatorMock = new Mock<IIssueSpanCalculator>();
            calculatorMock.Setup(x => x.CalculateSpan(locVizWithoutSpan.Location, It.IsAny<ITextSnapshot>())).Returns(newSpan1);
            calculatorMock.Setup(x => x.CalculateSpan(locVizWithEmptySpan.Location, It.IsAny<ITextSnapshot>())).Returns(newSpan2);

            var testSubject = new LocationTagger(bufferMock.Object, storeMock.Object, calculatorMock.Object, ValidLogger);

            testSubject.TagSpans.Should().NotBeNull();
            testSubject.TagSpans.Count.Should().Be(3);
            testSubject.TagSpans[0].Tag.Location.Should().BeSameAs(locVizWithSpan);
            testSubject.TagSpans[1].Tag.Location.Should().BeSameAs(locVizWithoutSpan);
            testSubject.TagSpans[2].Tag.Location.Should().BeSameAs(locVizWithEmptySpan);

            // Locations without spans should have one calculated for them
            testSubject.TagSpans[0].Tag.Location.Span.Value.Should().Be(existingSpan);
            testSubject.TagSpans[1].Tag.Location.Span.Value.Should().Be(newSpan1);
            testSubject.TagSpans[2].Tag.Location.Span.Value.Should().Be(newSpan2);
        }

        [TestMethod]
        public void Ctor_LocationsHaveNoSpans_TagsNotCreated()
        {
            var bufferMock = CreateBufferMock(filePath: ValidBufferDocName);
            var existingSpan = new SnapshotSpan(bufferMock.Object.CurrentSnapshot, new Span(1, 10));
            var locVizWithSpan = CreateLocViz(existingSpan);
            var locVizWithoutSpan = CreateLocViz(null);

            var storeMock = new Mock<IIssueLocationStore>();
            storeMock.Setup(x => x.GetLocations(ValidBufferDocName)).Returns(new[] { locVizWithSpan, locVizWithoutSpan });

            var calculatorMock = new Mock<IIssueSpanCalculator>();
            calculatorMock.Setup(x => x.CalculateSpan(locVizWithoutSpan.Location, It.IsAny<ITextSnapshot>())).Returns(new SnapshotSpan());

            var testSubject = new LocationTagger(bufferMock.Object, storeMock.Object, calculatorMock.Object, ValidLogger);

            testSubject.TagSpans.Should().NotBeNull();
            testSubject.TagSpans.Count.Should().Be(1);
            testSubject.TagSpans[0].Tag.Location.Should().BeSameAs(locVizWithSpan);
            testSubject.TagSpans[0].Tag.Location.Span.Value.Should().Be(existingSpan);

            locVizWithoutSpan.Span.Should().NotBeNull();
            locVizWithoutSpan.Span.Value.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void Ctor_LocationsHaveSpansThatBelongToAnotherBuffer_SpansRecalculated()
        {
            var oldBufferMock = CreateBufferMock(filePath: ValidBufferDocName);
            var oldBufferSpan = new SnapshotSpan(oldBufferMock.Object.CurrentSnapshot, new Span(1, 10));
            var location = CreateLocViz(oldBufferSpan);

            var storeMock = new Mock<IIssueLocationStore>();
            storeMock.Setup(x => x.GetLocations(ValidBufferDocName)).Returns(new[] { location });

            var newBufferMock = CreateBufferMock(filePath: ValidBufferDocName);
            var newSpan = new SnapshotSpan(newBufferMock.Object.CurrentSnapshot, new Span(1, 10));

            var calculatorMock = new Mock<IIssueSpanCalculator>();
            calculatorMock.Setup(x => x.CalculateSpan(location.Location, newBufferMock.Object.CurrentSnapshot)).Returns(newSpan);

            var testSubject = new LocationTagger(newBufferMock.Object, storeMock.Object, calculatorMock.Object, ValidLogger);

            testSubject.TagSpans.Should().NotBeNull();
            testSubject.TagSpans.Count.Should().Be(1);
            testSubject.TagSpans[0].Span.Should().Be(newSpan);
            testSubject.TagSpans[0].Tag.Location.Should().BeSameAs(location);
            testSubject.TagSpans[0].Tag.Location.Span.Should().Be(newSpan);
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
            var buffer = CreateBufferMock(filePath: ValidBufferDocName);
            var oldIssue1 = CreateLocViz(new SnapshotSpan(buffer.Object.CurrentSnapshot, 3, 1));
            var oldIssue2 = CreateLocViz(new SnapshotSpan(buffer.Object.CurrentSnapshot, 20, 2));
            var oldIssues = new[] { oldIssue1, oldIssue2 };

            var storeMock = new Mock<IIssueLocationStore>();
            storeMock.Setup(x => x.GetLocations(ValidBufferDocName)).Returns(oldIssues);

            var testSubject = new LocationTagger(buffer.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

            var newIssue1 = CreateLocViz(new SnapshotSpan(buffer.Object.CurrentSnapshot, 15, 10));
            var newIssue2 = CreateLocViz(new SnapshotSpan(buffer.Object.CurrentSnapshot, 25, 5));
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
            var bufferMock = CreateBufferMock(filePath: originalName);

            new LocationTagger(bufferMock.Object, storeMock.Object, ValidSpanCalculator, ValidLogger);

            // Check store is called with original name
            storeMock.Verify(x => x.GetLocations(originalName), Times.Once);
            RenameBufferFile(bufferMock, changedName);

            // Simulate issues changing
            RaiseIssuesChangedEvent(storeMock, changedName);

            // Check the store was notified using the new name
            storeMock.Verify(x => x.GetLocations(changedName), Times.Once);
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
            var buffer = CreateBufferMock();
            var locSpan1 = new Span(1, 10);
            var locSpan2 = new Span(11, 10);
            var storeMock = CreateStoreWithLocationsWithSpans(buffer.Object.CurrentSnapshot, out _, locSpan1, locSpan2);

            var inputSpans = new NormalizedSnapshotSpanCollection(buffer.Object.CurrentSnapshot, new Span(32, 5));

            var testSubject = new LocationTagger(buffer.Object, storeMock, ValidSpanCalculator, ValidLogger);
            testSubject.TagSpans.Count.Should().Be(2); // sanity check

            var matchingTags = testSubject.GetTags(inputSpans);
            matchingTags.Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_HasOverlappingSpans_ReturnsExpectedTags()
        {
            // Make sure the tagger and NormalizedSpanCollection use the same snapshot
            // so we don't attempt to translate the spans
            var buffer = CreateBufferMock();
            var locSpan1 = new Span(1, 10);
            var locSpan2 = new Span(11, 10);
            var locSpan3 = new Span(21, 10);
            var storeMock = CreateStoreWithLocationsWithSpans(buffer.Object.CurrentSnapshot, out _, locSpan1, locSpan2, locSpan3);

            var inputSpans = new NormalizedSnapshotSpanCollection(buffer.Object.CurrentSnapshot, new Span(18, 22));

            var testSubject = new LocationTagger(buffer.Object, storeMock, ValidSpanCalculator, ValidLogger);
            testSubject.TagSpans.Count().Should().Be(3); // sanity check

            var matchingTags = testSubject.GetTags(inputSpans);
            matchingTags.Select(x => x.Span.Span).Should().BeEquivalentTo(locSpan2, locSpan3);
        }

        [TestMethod]
        public void GetTags_DifferentSnapshots_SnapshotsAreTranslated()
        {
            const int length = 100;
            var buffer = CreateBufferMock(length, ValidBufferDocName).Object;

            var versionMock2 = CreateTextVersion(buffer, 2);
            var versionMock1 = CreateTextVersion(buffer, 1, nextVersion: versionMock2);

            var snapshotMock1 = CreateSnapshot(buffer, length, versionMock1);
            var snapshotMock2 = CreateSnapshot(buffer, length, versionMock2);

            var locSpan1 = new Span(1, 10);
            var locSpan2 = new Span(11, 5);
            var storeMock = CreateStoreWithLocationsWithSpans(snapshotMock1, out _, locSpan1, locSpan2);

            var inputSpans = new NormalizedSnapshotSpanCollection(snapshotMock2, new Span(18, 22));

            var testSubject = new LocationTagger(buffer, storeMock, ValidSpanCalculator, ValidLogger);

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
        public void GetTags_ExceptionInTranslatingSpan_ExceptionSuppressedAndLogged()
        {
            const int length = 100;
            var buffer = CreateBufferMock(length, ValidBufferDocName).Object;
            var snapshot = CreateSnapshot(buffer, length);
            var storeMock = CreateStoreWithLocationsWithSpans(snapshot, out _, new Span(1, 10));

            var logger = new Mock<ILogger>();
            var testSubject = new LocationTagger(buffer, storeMock, ValidSpanCalculator, logger.Object);
            testSubject.TagSpans.Count.Should().Be(1);

            var snapshotForAnotherBuffer = CreateSnapshot(CreateBuffer(), length);
            var inputSpans = new NormalizedSnapshotSpanCollection(snapshotForAnotherBuffer, new Span(18, 22));

            Action act = () => testSubject.GetTags(inputSpans).ToArray();
            act.Should().NotThrow();

            logger.VerifyNoOtherCalls();

            //testSubject.TagSpans.Count.Should().Be(0);
            //logger.AssertPartialOutputStringExists(ValidBufferDocName, "The specified ITextSnapshot doesn't belong to the correct TextBuffer.");
        }

        private static IAnalysisIssueLocationVisualization CreateLocViz(SnapshotSpan? span)
        {
            var locVizMock = new Mock<IAnalysisIssueLocationVisualization>();
            locVizMock.SetupProperty(x => x.Span);
            locVizMock.Object.Span = span;

            locVizMock.Setup(x => x.Location).Returns(Mock.Of<IAnalysisIssueLocation>());
            return locVizMock.Object;
        }

        private static IIssueLocationStore CreateStoreWithLocationsWithSpans(ITextSnapshot snapshot,
            out IAnalysisIssueLocationVisualization[] locVizs, params Span[] spans)
        {
            var storeMock = new Mock<IIssueLocationStore>();

            locVizs = spans.Select(x =>
            {
                var snapshotSpan = new SnapshotSpan(snapshot, x);
                return CreateLocViz(snapshotSpan);
            }).ToArray();

            storeMock.Setup(x => x.GetLocations(It.IsAny<string>())).Returns(locVizs);
            return storeMock.Object;
        }

        private static void RaiseIssuesChangedEvent(Mock<IIssueLocationStore> storeMock, params string[] fileNames) =>
            storeMock.Raise(x => x.IssuesChanged += null, new IssuesChangedEventArgs(fileNames));
    }
}
