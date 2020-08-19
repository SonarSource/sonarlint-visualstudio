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
using SonarLint.VisualStudio.IssueVisualization.Editor.BufferTagger;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SelectionChangedEventArgs = SonarLint.VisualStudio.IssueVisualization.Selection.SelectionChangedEventArgs;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.BufferTagger
{
    [TestClass]
    public class IssueLocationTaggerTests
    {
        private readonly ITextBuffer ValidBuffer = CreateBuffer();
        private readonly IssueLocationTagger.IBufferTagCalculator ValidTagCalculator = Mock.Of<IssueLocationTagger.IBufferTagCalculator>();
        private readonly IAnalysisIssueSelectionService ValidSelectionService = Mock.Of<IAnalysisIssueSelectionService>();

        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var mockSelectionService = new Mock<IAnalysisIssueSelectionService>();
            mockSelectionService.SetupAdd(x => x.SelectionChanged += (sender, args) => { });

            var testSubject = new IssueLocationTagger(ValidBuffer, mockSelectionService.Object, ValidTagCalculator);

            mockSelectionService.VerifyAdd(x => x.SelectionChanged += It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnsubscribesFromEvents()
        {
            var mockSelectionService = new Mock<IAnalysisIssueSelectionService>();
            mockSelectionService.SetupRemove(x => x.SelectionChanged -= (sender, args) => { });

            var testSubject = new IssueLocationTagger(ValidBuffer, mockSelectionService.Object, ValidTagCalculator);

            testSubject.Dispose();
            testSubject.Dispose(); // should only unsubscribe once

            mockSelectionService.VerifyRemove(x => x.SelectionChanged -= It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Ctor_UpdatesTags()
        {
            var mockTagCalculator = new Mock<IssueLocationTagger.IBufferTagCalculator>();

            var testSubject = new IssueLocationTagger(ValidBuffer, ValidSelectionService, mockTagCalculator.Object);

            mockTagCalculator.Verify(x => x.GetTagSpans(It.IsAny<IAnalysisIssueFlowVisualization>(), ValidBuffer.CurrentSnapshot), Times.Once);
        }

        [TestMethod]
        [DataRow("Issue")]
        [DataRow("Flow")]
        public void OnSelectionChanged_IssueOrFlow_UpdatesTagsAndNotifiesListeners(string changeLevelAsText)
        {
            // SelectionChangeLevel is internal so we can't use as an parameter type in a test
            var selectionChangeLevel = (SelectionChangeLevel)Enum.Parse(typeof(SelectionChangeLevel), changeLevelAsText);
            var mockSelectionService = new Mock<IAnalysisIssueSelectionService>();
            var mockTagCalculator = new Mock<IssueLocationTagger.IBufferTagCalculator>();

            var testSubject = new IssueLocationTagger(ValidBuffer, mockSelectionService.Object, mockTagCalculator.Object);
            SnapshotSpanEventArgs actualTagsChangedArgs = null;
            testSubject.TagsChanged += (senders, args) => actualTagsChangedArgs = args;
            mockTagCalculator.Invocations.Clear();

            mockSelectionService.Raise(x => x.SelectionChanged += null, new SelectionChangedEventArgs(selectionChangeLevel, null, null, null));

            mockTagCalculator.Verify(x => x.GetTagSpans(It.IsAny<IAnalysisIssueFlowVisualization>(), ValidBuffer.CurrentSnapshot), Times.Once);
            actualTagsChangedArgs.Should().NotBeNull();
            // Changed span should be the whole snapshot
            actualTagsChangedArgs.Span.Start.Position.Should().Be(0);
            actualTagsChangedArgs.Span.End.Position.Should().Be(ValidBuffer.CurrentSnapshot.Length);
        }

        [TestMethod]
        public void OnSelectionChanged_LocationLevel_ListenersAreNotRaised()
        {
            var mockSelectionService = new Mock<IAnalysisIssueSelectionService>();
            var mockTagCalculator = new Mock<IssueLocationTagger.IBufferTagCalculator>();

            var testSubject = new IssueLocationTagger(ValidBuffer, mockSelectionService.Object, mockTagCalculator.Object);
            bool tagsChangedInvoked = false;
            testSubject.TagsChanged += (senders, args) => tagsChangedInvoked = true;

            mockTagCalculator.Invocations.Clear();
            mockSelectionService.Raise(x => x.SelectionChanged += null, new SelectionChangedEventArgs(SelectionChangeLevel.Location, null, null, null));

            tagsChangedInvoked.Should().BeFalse();
            mockTagCalculator.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void GetTags_NoTags_ReturnsEmpty()
        {
            var testSubject = new IssueLocationTagger(ValidBuffer, ValidSelectionService, ValidTagCalculator);

            var validSpan = new Span(0, ValidBuffer.CurrentSnapshot.Length);
            var inputSpans = new NormalizedSnapshotSpanCollection(ValidBuffer.CurrentSnapshot, validSpan);

            testSubject.GetTags(inputSpans)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void GetTags_EmptyInputSpan_ReturnsEmpty()
        {
            var testSubject = new IssueLocationTagger(ValidBuffer, ValidSelectionService, ValidTagCalculator);

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

        private static ITextBuffer CreateBuffer()
        {
            var mockSnapshot = new Mock<ITextSnapshot>();
            mockSnapshot.Setup(x => x.Length).Returns(999);

            var mockBuffer = new Mock<ITextBuffer>();
            mockBuffer.Setup(x => x.CurrentSnapshot).Returns(mockSnapshot.Object);

            return mockBuffer.Object;
        }
    }
}
