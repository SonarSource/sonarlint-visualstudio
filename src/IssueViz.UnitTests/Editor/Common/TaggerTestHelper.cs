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
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common
{
    internal static class TaggerTestHelper
    {
        public static readonly ITextBuffer ValidBuffer = CreateBufferMock().Object;

        public static ITextSnapshot CreateSnapshot(int length = 999) =>
            CreateBufferMock(length).Object.CurrentSnapshot;

        public static ITextBuffer CreateBuffer(int length = 999) =>
            CreateBufferMock(length).Object;

        public static Mock<ITextBuffer> CreateBufferMock(int length = 999, string filePath = "test.cpp")
        {
            var snapshotMock = new Mock<ITextSnapshot>();
            var bufferMock = new Mock<ITextBuffer>();

            bufferMock.Setup(x => x.CurrentSnapshot).Returns(snapshotMock.Object);
            snapshotMock.Setup(x => x.TextBuffer).Returns(bufferMock.Object);
            snapshotMock.Setup(x => x.Length).Returns(length);

            var properties = new Microsoft.VisualStudio.Utilities.PropertyCollection();
            bufferMock.Setup(x => x.Properties).Returns(properties);

            var docMock = new Mock<ITextDocument>();
            docMock.Setup(x => x.FilePath).Returns(filePath);
            properties[typeof(ITextDocument)] = docMock.Object;

            return bufferMock;
        }

        public static ITagAggregator<T> CreateAggregator<T>(params IMappingTagSpan<T>[] tagSpans) where T: ITag
        {
            var aggregatorMock = new Mock<ITagAggregator<T>>();
            aggregatorMock.Setup(x => x.GetTags(It.IsAny<NormalizedSnapshotSpanCollection>()))
                .Returns(tagSpans);
            return aggregatorMock.Object;
        }

        public static IMappingTagSpan<T> CreateMappingTagSpan<T>(ITextSnapshot snapshot, T tag, params Span[] spans) where T: ITag
        {
            var mappingSpanMock = new Mock<IMappingSpan>();
            var normalizedSpanCollection = new NormalizedSnapshotSpanCollection(snapshot, spans);
            mappingSpanMock.Setup(x => x.GetSpans(snapshot)).Returns(normalizedSpanCollection);

            var tagSpanMock = new Mock<IMappingTagSpan<T>>();
            tagSpanMock.Setup(x => x.Tag).Returns(tag);
            tagSpanMock.Setup(x => x.Span).Returns(mappingSpanMock.Object);

            return tagSpanMock.Object;
        }

        public static IAnalysisIssueVisualization CreateIssueViz(ITextSnapshot snapshot, Span span, string locationMessage)
        {
            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
            var snapshotSpan = new SnapshotSpan(snapshot, span);
            issueVizMock.Setup(x => x.Span).Returns(snapshotSpan);
            issueVizMock.Setup(x => x.Location).Returns(new DummyAnalysisIssue { Message = locationMessage });
            return issueVizMock.Object;
        }

        public static IAnalysisIssueFlowVisualization CreateFlowViz(params IAnalysisIssueLocationVisualization[] locVizs)
        {
            var flowVizMock = new Mock<IAnalysisIssueFlowVisualization>();
            flowVizMock.Setup(x => x.Locations).Returns(locVizs);
            return flowVizMock.Object;
        }

        public static IAnalysisIssueLocationVisualization CreateLocationViz(ITextSnapshot snapshot = null, Span? span = null, string locationMessage = null, int stepNumber = -1)
        {
            if (snapshot == null)
            {
                snapshot = CreateSnapshot();
            }
            if (!span.HasValue)
            {
                span = new Span(0, snapshot.Length);
            }
            if (locationMessage == null)
            {
                locationMessage = System.Guid.NewGuid().ToString();
            }

            var locVizMock = new Mock<IAnalysisIssueLocationVisualization>();
            var snapshotSpan = new SnapshotSpan(snapshot, span.Value);
            locVizMock.Setup(x => x.Span).Returns(snapshotSpan);
            locVizMock.Setup(x => x.Location).Returns(new DummyAnalysisIssueLocation { Message = locationMessage });
            locVizMock.Setup(x => x.StepNumber).Returns(stepNumber);
            return locVizMock.Object;
        }

        public static NormalizedSnapshotSpanCollection CreateSpanCollectionSpanningWholeSnapshot(ITextSnapshot snapshot)
        {
            // Span that will match everything in the snapshot so will overlap with all other spans
            var wholeSpan = new Span(0, snapshot.Length);
            return new NormalizedSnapshotSpanCollection(snapshot, wholeSpan);
        }

        public static IIssueLocationTag CreateIssueLocationTag(IAnalysisIssueLocationVisualization locViz)
        {
            var tagMock = new Mock<IIssueLocationTag>();
            tagMock.Setup(x => x.Location).Returns(locViz);
            return tagMock.Object;
        }

        public static ISelectedIssueLocationTag CreateSelectedLocationTag(IAnalysisIssueLocationVisualization locViz)
        {
            var tagMock = new Mock<ISelectedIssueLocationTag>();
            tagMock.Setup(x => x.Location).Returns(locViz);
            return tagMock.Object;
        }

        public static ITagAggregator<ISelectedIssueLocationTag> CreateSelectedIssueAggregator(params IAnalysisIssueLocationVisualization[] locVizs)
        {
            var tagSpans = locVizs
                .Select(CreateSelectedIssueTagSpan)
                .ToArray();

            return CreateAggregator(tagSpans);
        }

        private static IMappingTagSpan<ISelectedIssueLocationTag> CreateSelectedIssueTagSpan(IAnalysisIssueLocationVisualization locViz)
        {
            var tag = CreateSelectedLocationTag(locViz);
            return CreateMappingTagSpan(locViz.Span.Value.Snapshot, tag, locViz.Span.Value.Span);
        }

        public static ITaggableBufferIndicator CreateTaggableBufferIndicator(bool isTaggable = true)
        {
            var taggableBufferIndicator = new Mock<ITaggableBufferIndicator>();

            taggableBufferIndicator
                .Setup(x => x.IsTaggable(It.IsAny<ITextBuffer>()))
                .Returns(isTaggable);

            return taggableBufferIndicator.Object;
        }

        public static IWpfTextView CreateWpfTextView(ITextSnapshot snapshot = null, IFormattedLineSource formattedLineSource = null)
        {
            snapshot = snapshot ?? CreateSnapshot();
            var lineSource = formattedLineSource ?? CreateFormattedLineSource();

            var viewMock = new Mock<IWpfTextView>();
            viewMock.Setup(x => x.TextSnapshot).Returns(snapshot);
            viewMock.Setup(x => x.FormattedLineSource).Returns(lineSource);
            return viewMock.Object;
        }

        public static IFormattedLineSource CreateFormattedLineSource(double fontSize = 12d, string fontFamily = "Arial")
        {
            var textRunPropertiesMock = new Mock<TextRunProperties>();
            textRunPropertiesMock.Setup(x => x.FontRenderingEmSize).Returns(fontSize);
            textRunPropertiesMock.Setup(x => x.Typeface).Returns(new Typeface(fontFamily));

            var lineSourceMock = new Mock<IFormattedLineSource>();
            lineSourceMock.Setup(x => x.DefaultTextProperties).Returns(textRunPropertiesMock.Object);

            return lineSourceMock.Object;
        }

        public static void RaiseBatchedTagsChanged(Mock<ITagAggregator<ISelectedIssueLocationTag>> selectedIssuesAggregatorMock) =>
            selectedIssuesAggregatorMock.Raise(x => x.BatchedTagsChanged += null, new BatchedTagsChangedEventArgs(Array.Empty<IMappingSpan>()));
    }
}
