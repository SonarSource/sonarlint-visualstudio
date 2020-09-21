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
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions
{
    [TestClass]
    public class IssueLocationActionsSourceTests
    {
        [TestMethod]
        public void Ctor_RegisterToTagAggregatorEvents()
        {
            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            selectedIssueLocationsTagAggregator.SetupAdd(x => x.TagsChanged += null);

            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();
            issueLocationsTagAggregator.SetupAdd(x => x.TagsChanged += null);

            CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object);

            selectedIssueLocationsTagAggregator.VerifyAdd(x => x.TagsChanged += It.IsAny<EventHandler<TagsChangedEventArgs>>(), Times.Once);
            issueLocationsTagAggregator.VerifyAdd(x => x.TagsChanged += It.IsAny<EventHandler<TagsChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromTagAggregatorEvents()
        {
            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            selectedIssueLocationsTagAggregator.SetupRemove(x => x.TagsChanged -= null);

            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();
            issueLocationsTagAggregator.SetupRemove(x => x.TagsChanged -= null);

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object);
            testSubject.Dispose();

            selectedIssueLocationsTagAggregator.VerifyRemove(x => x.TagsChanged -= It.IsAny<EventHandler<TagsChangedEventArgs>>(), Times.Once);
            issueLocationsTagAggregator.VerifyRemove(x => x.TagsChanged -= It.IsAny<EventHandler<TagsChangedEventArgs>>(), Times.Once);

            selectedIssueLocationsTagAggregator.Verify(x=> x.Dispose(), Times.Once);
            issueLocationsTagAggregator.Verify(x=> x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void OnTagsChanged_NoSubscribersToSuggestedActionsChanged_NoException()
        {
            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object);
            
            Action act = () => selectedIssueLocationsTagAggregator.Raise(x => x.TagsChanged += null, new TagsChangedEventArgs(Mock.Of<IMappingSpan>()));
            act.Should().NotThrow();

            act = () => issueLocationsTagAggregator.Raise(x => x.TagsChanged += null, new TagsChangedEventArgs(Mock.Of<IMappingSpan>()));
            act.Should().NotThrow();
        }

        [TestMethod]
        public void OnTagsChanged_HasSubscribersToSuggestedActionsChanged_RaisesSuggestedActionsChanged()
        {
            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            var eventHandler = new Mock<EventHandler<EventArgs>>();

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object);
            testSubject.SuggestedActionsChanged += eventHandler.Object;

            selectedIssueLocationsTagAggregator.Raise(x => x.TagsChanged += null, new TagsChangedEventArgs(Mock.Of<IMappingSpan>()));
            eventHandler.Verify(x=> x(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Once);

            eventHandler.Reset();

            issueLocationsTagAggregator.Raise(x => x.TagsChanged += null, new TagsChangedEventArgs(Mock.Of<IMappingSpan>()));
            eventHandler.Verify(x => x(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Once);
        }

        [TestMethod]
        public void GetSuggestedActions_NoSelectionTags_NoIssueTags_NoActions()
        {
            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object);
            var suggestedActionSets = testSubject.GetSuggestedActions(null, new SnapshotSpan(), CancellationToken.None);

            suggestedActionSets.Should().BeEmpty();
        }

        [TestMethod]
        public void GetSuggestedActions_NoSelectionTags_NoIssueTagsWithSecondaryLocations_NoActions()
        {
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            var mockSpan = new SnapshotSpan();

            var locations = new[]
            {
                CreateIssueViz(),
                CreateIssueViz()
            };

            SetupIssueLocationTags(locations, issueLocationsTagAggregator, mockSpan);

            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object);
            var suggestedActionSets = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

            suggestedActionSets.Should().BeEmpty();
        }

        [TestMethod]
        public void GetSuggestedActions_NoSelectionTags_HasIssueTagsWithSecondaryLocations_SelectIssueActions()
        {
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            var mockSpan = new SnapshotSpan();

            var issueVizWithoutFlows = CreateIssueViz();
            var firstIssueVizWithFlows = CreateIssueViz(CreateFlowViz(CreateLocationViz()));
            var secondIssueVizWithFlows = CreateIssueViz(CreateFlowViz(CreateLocationViz()));

            var locations = new[]
            {
                firstIssueVizWithFlows,
                issueVizWithoutFlows,
                secondIssueVizWithFlows
            };

            SetupIssueLocationTags(locations, issueLocationsTagAggregator, mockSpan);

            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object);
            var suggestedActionSets = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);
            
            suggestedActionSets.Count().Should().Be(1);
            var suggestedActions = suggestedActionSets.First().Actions.ToArray();
            suggestedActions.Length.Should().Be(2);
            suggestedActions[0].Should().BeOfType<SelectIssueVisualizationAction>();
            (suggestedActions[0] as SelectIssueVisualizationAction).Issue.Should().Be(firstIssueVizWithFlows);
            (suggestedActions[1] as SelectIssueVisualizationAction).Issue.Should().Be(secondIssueVizWithFlows);
        }

        [TestMethod]
        public void GetSuggestedActions_HasSelectionTags_NoIssueTags_DeselectIssueAction()
        {
            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();

            var mockSpan = new SnapshotSpan();

            var locations = new[]
            {
                Mock.Of<IAnalysisIssueLocationVisualization>(),
                Mock.Of<IAnalysisIssueLocationVisualization>()
            };

            SetupSelectedLocationTags(locations, selectedIssueLocationsTagAggregator, mockSpan);

            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();
            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object);
            var suggestedActionSets = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

            suggestedActionSets.Count().Should().Be(1);
            var suggestedActions = suggestedActionSets.First().Actions.ToArray();
            suggestedActions.Length.Should().Be(1);
            suggestedActions[0].Should().BeOfType<DeselectIssueVisualizationAction>();
        }

        [TestMethod]
        public void GetSuggestedActions_HasSelectionTags_HasIssueTagsWithSecondaryLocations_DeselectAndSelectIssueActions()
        {
            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();

            var mockSpan = new SnapshotSpan();

            var selectedLocations = new[]
            {
                Mock.Of<IAnalysisIssueLocationVisualization>(),
                Mock.Of<IAnalysisIssueLocationVisualization>()
            };

            SetupSelectedLocationTags(selectedLocations, selectedIssueLocationsTagAggregator, mockSpan);

            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            var issueLocations = new[]
            {
                CreateIssueViz(CreateFlowViz(CreateLocationViz())),
                CreateIssueViz(CreateFlowViz(CreateLocationViz()))
            };

            SetupIssueLocationTags(issueLocations, issueLocationsTagAggregator, mockSpan);

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object);
            var suggestedActionSets = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

            suggestedActionSets.Count().Should().Be(1);
            var suggestedActions = suggestedActionSets.First().Actions.ToArray();
            suggestedActions.Length.Should().Be(3);
            suggestedActions[0].Should().BeOfType<SelectIssueVisualizationAction>();
            suggestedActions[1].Should().BeOfType<SelectIssueVisualizationAction>();
            suggestedActions[2].Should().BeOfType<DeselectIssueVisualizationAction>();
            (suggestedActions[0] as SelectIssueVisualizationAction).Issue.Should().Be(issueLocations[0]);
            (suggestedActions[1] as SelectIssueVisualizationAction).Issue.Should().Be(issueLocations[1]);
        }

        private static IAnalysisIssueVisualization CreateIssueViz(params IAnalysisIssueFlowVisualization[] flows)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Flows).Returns(flows);

            return issueViz.Object;
        }

        private static void SetupIssueLocationTags(IAnalysisIssueLocationVisualization[] locations, Mock<ITagAggregator<IIssueLocationTag>> issueLocationsTagAggregator, SnapshotSpan mockSpan)
        {
            var snapshot = CreateSnapshot();
            var mappingTagSpans = locations.Select(x => CreateMappingTagSpan(snapshot, CreateIssueLocationTag(x)));

            issueLocationsTagAggregator.Setup(x => x.GetTags(mockSpan)).Returns(mappingTagSpans);
        }

        private void SetupSelectedLocationTags(IAnalysisIssueLocationVisualization[] locations, Mock<ITagAggregator<ISelectedIssueLocationTag>> selectedIssueLocationsTagAggregator, SnapshotSpan mockSpan)
        {
            var snapshot = CreateSnapshot();
            var mappingTagSpans = locations.Select(x => CreateMappingTagSpan(snapshot, CreateSelectedLocationTag(x)));

            selectedIssueLocationsTagAggregator.Setup(x => x.GetTags(mockSpan)).Returns(mappingTagSpans);
        }

        private static IssueLocationActionsSource CreateTestSubject(ITagAggregator<ISelectedIssueLocationTag> selectedIssueLocationsTagAggregator, ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator)
        {
            var vsUiShell = Mock.Of<IVsUIShell>();
            var buffer = Mock.Of<ITextBuffer>();
            var bufferTagAggregatorFactoryService = new Mock<IBufferTagAggregatorFactoryService>();

            bufferTagAggregatorFactoryService
                .Setup(x => x.CreateTagAggregator<ISelectedIssueLocationTag>(buffer))
                .Returns(selectedIssueLocationsTagAggregator);

            bufferTagAggregatorFactoryService
                .Setup(x => x.CreateTagAggregator<IIssueLocationTag>(buffer))
                .Returns(issueLocationsTagAggregator);

            var analysisIssueSelectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            analysisIssueSelectionServiceMock.Setup(x => x.SelectedIssue).Returns(Mock.Of<IAnalysisIssueVisualization>());

            return new IssueLocationActionsSource(vsUiShell, bufferTagAggregatorFactoryService.Object, buffer, analysisIssueSelectionServiceMock.Object);
        }
    }
}
