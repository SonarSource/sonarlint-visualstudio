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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
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
        public void Ctor_UpdatesTags()
        {
            var locVizWithSpan = CreateLocViz(new SnapshotSpan());

            var storeMock = new Mock<IIssueLocationStore>();
            storeMock.Setup(x => x.GetLocations(ValidBufferDocName)).Returns(new[] { locVizWithSpan });

            var testSubject = new LocationTagger(ValidBuffer, storeMock.Object, ValidSpanCalculator, ValidLogger);

            testSubject.locationTagSpans.Should().NotBeNull();
            testSubject.locationTagSpans.Count.Should().Be(1);
            testSubject.locationTagSpans[0].Tag.Location.Should().BeSameAs(locVizWithSpan);
        }

        [TestMethod]
        public void OnIssuesChanged_NotTheTrackedFile_TagsChangedNotRaised()
        {
            var storeMock = new Mock<IIssueLocationStore>();
            var testSubject = new LocationTagger(ValidBuffer, storeMock.Object, ValidSpanCalculator, ValidLogger);

            SnapshotSpanEventArgs actualTagsChangedArgs = null;
            testSubject.TagsChanged += (senders, args) => actualTagsChangedArgs = args;

            storeMock.Raise(x => x.IssuesChanged += null, new IssuesChangedEventArgs(new string[] { "not the file being tracked.txt" }));

            actualTagsChangedArgs.Should().BeNull();
        }

        [TestMethod]
        public void OnIssuesChanged_IsTheTrackedFile_TagsChangedIsRaised()
        {
            var storeMock = new Mock<IIssueLocationStore>();
            var testSubject = new LocationTagger(ValidBuffer, storeMock.Object, ValidSpanCalculator, ValidLogger);

            SnapshotSpanEventArgs actualTagsChangedArgs = null;
            testSubject.TagsChanged += (senders, args) => actualTagsChangedArgs = args;

            storeMock.Raise(x => x.IssuesChanged += null, new IssuesChangedEventArgs(new string[] { ValidBufferDocName }));

            actualTagsChangedArgs.Should().NotBeNull();
            // Changed span should be the whole snapshot
            actualTagsChangedArgs.Span.Start.Position.Should().Be(0);
            actualTagsChangedArgs.Span.End.Position.Should().Be(ValidBuffer.CurrentSnapshot.Length);
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
            //TOOD
        }

        [TestMethod]
        public void GetTags_ReturnsMatchingTags()
        {
            //TOOD
        }

        private static Mock<ITextBuffer> CreateBufferMock(string filePath)
        {
            var snapshotMock = new Mock<ITextSnapshot>();
            snapshotMock.Setup(x => x.Length).Returns(999);

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
            return locVizMock.Object;
        }
    }
}
