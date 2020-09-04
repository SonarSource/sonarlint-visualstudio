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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.ErrorTagging
{
    [TestClass]
    public class ErrorTaggerTests
    {
        [TestMethod]
        public void GetTags_FilterIsApplied_ExpectedTagsCreated()
        {
            var buffer = CreateBufferWithSnapshot(length: 50);
            var snapshot = buffer.CurrentSnapshot;

            // Input span that will match everything in the snapshot so will overlap with all other spans
            var wholeSpan = new Span(0, snapshot.Length);
            var inputSpans = new NormalizedSnapshotSpanCollection(snapshot, wholeSpan);

            var primary1 = CreateTagSpanWithPrimaryLocation(snapshot, new Span(1, 5), "error message 1");
            var primary2 = CreateTagSpanWithPrimaryLocation(snapshot, new Span(10, 5), "error message 2");
            var secondary1 = CreateTagSpanWithSecondaryLocation(snapshot, new Span(20, 5));
            var secondary2 = CreateTagSpanWithSecondaryLocation(snapshot, new Span(30, 5));
            var aggregator = CreateAggregator(primary1, secondary1, primary2, secondary2);

            var testSubject = new ErrorTagger(aggregator, buffer);

            // Act
            var actual = testSubject.GetTags(inputSpans).ToArray();

            actual[0].Tag.ToolTipContent.Should().Be((object)"error message 1");
            actual[0].Span.Span.Should().Be(primary1.Tag.Location.Span.Value.Span);

            actual[1].Tag.ToolTipContent.Should().Be((object)"error message 2");
            actual[1].Span.Span.Should().Be(primary2.Tag.Location.Span.Value.Span);
        }

        private static IMappingTagSpan<IIssueLocationTag> CreateTagSpanWithPrimaryLocation(ITextSnapshot snapshot, Span span, string errorMessage = "")
        {
            var viz = CreateIssueViz(snapshot, span, errorMessage);
            var tag = CreateTag(viz);
            return CreateMappingTagSpan(snapshot, tag, span);
        }

        private static IMappingTagSpan<IIssueLocationTag> CreateTagSpanWithSecondaryLocation(ITextSnapshot snapshot, Span span, string errorMessage = "")
        {
            var viz = CreateLocationViz(snapshot, span, errorMessage);
            var tag = CreateTag(viz);
            return CreateMappingTagSpan(snapshot, tag, span);
        }

        private static IAnalysisIssueVisualization CreateIssueViz(ITextSnapshot snapshot, Span span, string errorMessage)
        {
            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
            var snapshotSpan = new SnapshotSpan(snapshot, span);
            issueVizMock.Setup(x => x.Span).Returns(snapshotSpan);
            issueVizMock.Setup(x => x.Location).Returns(new DummyAnalysisIssue { Message = errorMessage });
            return issueVizMock.Object;
        }

        private static IAnalysisIssueLocationVisualization CreateLocationViz(ITextSnapshot snapshot, Span span, string errorMessage)
        {
            var locVizMock = new Mock<IAnalysisIssueLocationVisualization>();
            var snapshotSpan = new SnapshotSpan(snapshot, span);
            locVizMock.Setup(x => x.Span).Returns(snapshotSpan);
            locVizMock.Setup(x => x.Location).Returns(new DummyAnalysisIssueLocation { Message = errorMessage });
            return locVizMock.Object;
        }

        private static IIssueLocationTag CreateTag(IAnalysisIssueLocationVisualization locViz)
        {
            var tagMock = new Mock<IIssueLocationTag>();
            tagMock.Setup(x => x.Location).Returns(locViz);
            return tagMock.Object;
        }
    }
}
