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
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.ErrorTagging
{
    [TestClass]
    public class ErrorTaggerTests
    {
        private readonly ITextBuffer ValidBuffer = CreateBufferWithSnapshot();

        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var aggregatorMock = new Mock<ITagAggregator<IIssueLocationTag>>();
            aggregatorMock.SetupAdd(x => x.BatchedTagsChanged += (sender, args) => { });

            var testSubject = new ErrorTagger(ValidBuffer, aggregatorMock.Object);

            aggregatorMock.VerifyAdd(x => x.BatchedTagsChanged += It.IsAny<EventHandler<BatchedTagsChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnsubscribesFromEvents()
        {
            var aggregatorMock = new Mock<ITagAggregator<IIssueLocationTag>>();
            aggregatorMock.SetupAdd(x => x.BatchedTagsChanged += (sender, args) => { });

            var testSubject = new ErrorTagger(ValidBuffer, aggregatorMock.Object);
            testSubject.Dispose();

            aggregatorMock.VerifyRemove(x => x.BatchedTagsChanged -= It.IsAny<EventHandler<BatchedTagsChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void GetTags_NoTags_ReturnsEmpty()
        {
            var validSpan = new Span(0, ValidBuffer.CurrentSnapshot.Length);
            var inputSpans = new NormalizedSnapshotSpanCollection(ValidBuffer.CurrentSnapshot, validSpan);

            var aggregatorMock = new Mock<ITagAggregator<IIssueLocationTag>>();
            aggregatorMock.Setup(x => x.GetTags(inputSpans)).Returns(Array.Empty<IMappingTagSpan<IIssueLocationTag>>());

            var testSubject = new ErrorTagger(ValidBuffer, aggregatorMock.Object);

            testSubject.GetTags(inputSpans)
                .Should().BeEmpty();

            aggregatorMock.Verify(x => x.GetTags(inputSpans), Times.Once);
        }

        [TestMethod]
        public void GetTags_EmptyInputSpan_ReturnsEmpty()
        {
            var aggregatorMock = new Mock<ITagAggregator<IIssueLocationTag>>();
            var inputSpans = new NormalizedSnapshotSpanCollection();

            var testSubject = new ErrorTagger(ValidBuffer, aggregatorMock.Object);

            testSubject.GetTags(inputSpans)
                .Should().BeEmpty();

            aggregatorMock.Verify(x => x.GetTags(inputSpans), Times.Never);
        }

        private static ITextBuffer CreateBufferWithSnapshot()
        {
            var snapshotMock = new Mock<ITextSnapshot>();
            snapshotMock.Setup(x => x.Length).Returns(999);

            var bufferMock = new Mock<ITextBuffer>();
            bufferMock.Setup(x => x.CurrentSnapshot).Returns(snapshotMock.Object);

            return bufferMock.Object;
        }

    }
}
