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
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
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
        public void TryGetTelemetryId_False()
        {
            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object);

            testSubject.TryGetTelemetryId(out var guid).Should().BeFalse();
            guid.Should().BeEmpty();
        }

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
        public void OnTagsChanged_DismissLightBulbSession()
        {
            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();
            var lightBulbBroker = new Mock<ILightBulbBroker>();
            var textView = CreateWpfTextView();

            CreateTestSubject(selectedIssueLocationsTagAggregator.Object, 
                issueLocationsTagAggregator.Object, 
                lightBulbBroker: lightBulbBroker.Object, 
                textView: textView);

            lightBulbBroker.VerifyNoOtherCalls();

            selectedIssueLocationsTagAggregator.Raise(x => x.TagsChanged += null, new TagsChangedEventArgs(Mock.Of<IMappingSpan>()));
            lightBulbBroker.Verify(x=> x.DismissSession(textView), Times.Once);

            issueLocationsTagAggregator.Raise(x => x.TagsChanged += null, new TagsChangedEventArgs(Mock.Of<IMappingSpan>()));
            lightBulbBroker.Verify(x => x.DismissSession(textView), Times.Exactly(2));
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
            var actionSets = GetSuggestedActions(
                primaryIssues:Enumerable.Empty<IAnalysisIssueVisualization>(),
                secondaryLocations:Enumerable.Empty<IAnalysisIssueLocationVisualization>(),
                selectedIssue: null);

            actionSets.Should().BeEmpty();
        }

        [TestMethod]
        public void GetSuggestedActions_NoSelectionTags_NoIssueTagsWithSecondaryLocations_NoActions()
        {
            var issuesWithoutSecondaryLocations = new[]
            {
                CreateIssueViz(),
                CreateIssueViz()
            };

            var actionSets = GetSuggestedActions(
                primaryIssues: issuesWithoutSecondaryLocations,
                secondaryLocations: Enumerable.Empty<IAnalysisIssueLocationVisualization>(),
                selectedIssue: null);

            actionSets.Should().BeEmpty();
        }

        [TestMethod]
        public void GetSuggestedActions_NoSelectionTags_HasIssueTagsWithSecondaryLocations_SelectIssueActions()
        {
            var issues = new[]
            {
                CreateIssueViz(CreateFlowViz(CreateLocationViz())),
                CreateIssueViz(),
                CreateIssueViz(CreateFlowViz(CreateLocationViz()))
            };

            var actionSets = GetSuggestedActions(
                primaryIssues: issues,
                secondaryLocations: Enumerable.Empty<IAnalysisIssueLocationVisualization>(),
                selectedIssue: null);

            actionSets.Count.Should().Be(1);
            var suggestedActions = actionSets[0].Actions.ToArray();
            suggestedActions.Length.Should().Be(2);
            suggestedActions[0].Should().BeOfType<SelectIssueVisualizationAction>();
            (suggestedActions[0] as SelectIssueVisualizationAction).Issue.Should().Be(issues[0]);
            (suggestedActions[1] as SelectIssueVisualizationAction).Issue.Should().Be(issues[2]);
        }

        [TestMethod]
        public void GetSuggestedActions_NoSelectionTags_HasSelectedIssueTag_SelectAndDeselectIssueAction()
        {
            var issues = new[]
            {
                CreateIssueViz(CreateFlowViz(CreateLocationViz()))
            };

            var actionSets = GetSuggestedActions(
                primaryIssues: issues,
                secondaryLocations: Enumerable.Empty<IAnalysisIssueLocationVisualization>(),
                selectedIssue: issues[0]);

            actionSets.Count.Should().Be(1);
            var suggestedActions = actionSets[0].Actions.ToArray();
            suggestedActions.Length.Should().Be(2);
            suggestedActions[0].Should().BeOfType<SelectIssueVisualizationAction>();
            (suggestedActions[0] as SelectIssueVisualizationAction).Issue.Should().Be(issues[0]);
            suggestedActions[1].Should().BeOfType<DeselectIssueVisualizationAction>();
        }

        [TestMethod]
        public void GetSuggestedActions_HasSelectionTags_NoIssueTags_DeselectIssueAction()
        {
            var secondaryLocations = new[]
            {
                CreateLocationViz(),
                CreateLocationViz()
            };

            var actionSets = GetSuggestedActions(
                primaryIssues: Enumerable.Empty<IAnalysisIssueVisualization>(),
                secondaryLocations: secondaryLocations,
                selectedIssue: CreateIssueViz());

            actionSets.Count.Should().Be(1);
            var suggestedActions = actionSets[0].Actions.ToArray();
            suggestedActions.Length.Should().Be(1);
            suggestedActions[0].Should().BeOfType<DeselectIssueVisualizationAction>();
        }

        [TestMethod]
        public void GetSuggestedActions_HasSelectionTags_HasIssueTagsWithSecondaryLocations_SelectAndDeselectIssueActions()
        {
            var secondaryLocations = new[]
            {
                CreateLocationViz(),
                CreateLocationViz()
            };

            var issues = new[]
            {
                CreateIssueViz(CreateFlowViz(CreateLocationViz())),
                CreateIssueViz(CreateFlowViz(CreateLocationViz()))
            };

            var actionSets = GetSuggestedActions(
                primaryIssues: issues,
                secondaryLocations: secondaryLocations,
                selectedIssue: issues[1]);

            actionSets.Count.Should().Be(1);
            var suggestedActions = actionSets.First().Actions.ToArray();
            suggestedActions.Length.Should().Be(3);
            suggestedActions[0].Should().BeOfType<SelectIssueVisualizationAction>();
            suggestedActions[1].Should().BeOfType<SelectIssueVisualizationAction>();
            suggestedActions[2].Should().BeOfType<DeselectIssueVisualizationAction>();
            (suggestedActions[0] as SelectIssueVisualizationAction).Issue.Should().Be(issues[0]);
            (suggestedActions[1] as SelectIssueVisualizationAction).Issue.Should().Be(issues[1]);
        }

        private static IAnalysisIssueVisualization CreateIssueViz(params IAnalysisIssueFlowVisualization[] flows)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Flows).Returns(flows);

            return issueViz.Object;
        }

        private static IssueLocationActionsSource CreateTestSubject(ITagAggregator<ISelectedIssueLocationTag> selectedIssueLocationsTagAggregator, 
            ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator, 
            IAnalysisIssueSelectionService selectionService = null, 
            ILightBulbBroker lightBulbBroker = null, 
            ITextView textView = null)
        {
            textView = textView ?? CreateWpfTextView();
            var vsUiShell = Mock.Of<IVsUIShell>();
            var bufferTagAggregatorFactoryService = new Mock<IBufferTagAggregatorFactoryService>();

            bufferTagAggregatorFactoryService
                .Setup(x => x.CreateTagAggregator<ISelectedIssueLocationTag>(textView.TextBuffer))
                .Returns(selectedIssueLocationsTagAggregator);

            bufferTagAggregatorFactoryService
                .Setup(x => x.CreateTagAggregator<IIssueLocationTag>(textView.TextBuffer))
                .Returns(issueLocationsTagAggregator);

            var analysisIssueSelectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            analysisIssueSelectionServiceMock.Setup(x => x.SelectedIssue).Returns(Mock.Of<IAnalysisIssueVisualization>());

            selectionService = selectionService ?? analysisIssueSelectionServiceMock.Object;
            lightBulbBroker = lightBulbBroker ?? Mock.Of<ILightBulbBroker>();

            return new IssueLocationActionsSource(lightBulbBroker, vsUiShell, bufferTagAggregatorFactoryService.Object, textView, selectionService);
        }

        private IList<SuggestedActionSet> GetSuggestedActions(IEnumerable<IAnalysisIssueVisualization> primaryIssues,
            IEnumerable<IAnalysisIssueLocationVisualization> secondaryLocations,
            IAnalysisIssueVisualization selectedIssue)
        {
            var mockSpan = new SnapshotSpan();
            var snapshot = CreateSnapshot();

            var primaryTagSpans = primaryIssues.Select(x => CreateMappingTagSpan(snapshot, CreateIssueLocationTag(x), mockSpan));
            var secondaryTagSpans = secondaryLocations.Select(x => CreateMappingTagSpan(snapshot, CreateSelectedLocationTag(x), mockSpan));

            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();
            issueLocationsTagAggregator.Setup(x => x.GetTags(mockSpan)).Returns(primaryTagSpans);

            var selectedIssueLocationsTagAggregator = new Mock<ITagAggregator<ISelectedIssueLocationTag>>();
            selectedIssueLocationsTagAggregator.Setup(x => x.GetTags(mockSpan)).Returns(secondaryTagSpans);

            var selectionService = new Mock<IAnalysisIssueSelectionService>();
            selectionService.Setup(x => x.SelectedIssue).Returns(selectedIssue);

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator.Object, issueLocationsTagAggregator.Object, selectionService.Object);
            var actualActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

            return actualActionsSet.ToList();
        }
    }
}
