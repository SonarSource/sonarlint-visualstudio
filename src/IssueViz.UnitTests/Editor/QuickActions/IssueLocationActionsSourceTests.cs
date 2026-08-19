/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
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
            var selectedIssueLocationsTagAggregator = Substitute.For<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator, issueLocationsTagAggregator);

            testSubject.TryGetTelemetryId(out var guid).Should().BeFalse();
            guid.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_RegisterToTagAggregatorEvents()
        {
            var selectedIssueLocationsTagAggregator = Substitute.For<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

            CreateTestSubject(selectedIssueLocationsTagAggregator, issueLocationsTagAggregator);

            selectedIssueLocationsTagAggregator.Received(1).TagsChanged += Arg.Any<EventHandler<TagsChangedEventArgs>>();
            issueLocationsTagAggregator.Received(1).TagsChanged += Arg.Any<EventHandler<TagsChangedEventArgs>>();
        }

        [TestMethod]
        public void Ctor_RegisterToSelectedIssueChangedEvent()
        {
            var selectedIssueLocationsTagAggregator = Substitute.For<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
            var selectionService = Substitute.For<IIssueSelectionService>();

            CreateTestSubject(selectedIssueLocationsTagAggregator, issueLocationsTagAggregator, selectionService);

            selectionService.Received(1).SelectedIssueChanged += Arg.Any<EventHandler>();
        }

        [TestMethod]
        public void Dispose_UnregisterFromTagAggregatorEvents()
        {
            var selectedIssueLocationsTagAggregator = Substitute.For<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator, issueLocationsTagAggregator);
            testSubject.Dispose();

            selectedIssueLocationsTagAggregator.Received(1).TagsChanged -= Arg.Any<EventHandler<TagsChangedEventArgs>>();
            issueLocationsTagAggregator.Received(1).TagsChanged -= Arg.Any<EventHandler<TagsChangedEventArgs>>();

            selectedIssueLocationsTagAggregator.Received(1).Dispose();
            issueLocationsTagAggregator.Received(1).Dispose();
        }

        [TestMethod]
        public void Dispose_UnregisterFromSelectedIssueChangedEvent()
        {
            var selectedIssueLocationsTagAggregator = Substitute.For<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
            var selectionService = Substitute.For<IIssueSelectionService>();

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator, issueLocationsTagAggregator, selectionService);
            testSubject.Dispose();

            selectionService.Received(1).SelectedIssueChanged -= Arg.Any<EventHandler>();
        }

        [TestMethod]
        public void OnTagsChanged_LightBulbSessionNeverDismissed()
        {
            var selectedIssueLocationsTagAggregator = Substitute.For<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
            var lightBulbBroker = Substitute.For<ILightBulbBroker>();
            var textView = CreateWpfTextView();

            CreateTestSubject(selectedIssueLocationsTagAggregator,
                issueLocationsTagAggregator,
                lightBulbBroker: lightBulbBroker,
                textView: textView);

            var changedSpan = CreateMappingSpan(textView.TextSnapshot, new Span(0, 1));

            selectedIssueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(changedSpan));
            issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(changedSpan));

            lightBulbBroker.DidNotReceiveWithAnyArgs().DismissSession(default);
        }

        [TestMethod]
        public void OnSelectedIssueChanged_DismissesLightBulbSessionAndRaisesSuggestedActionsChanged()
        {
            var selectedIssueLocationsTagAggregator = Substitute.For<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
            var selectionService = Substitute.For<IIssueSelectionService>();
            var lightBulbBroker = Substitute.For<ILightBulbBroker>();
            var textView = CreateWpfTextView();
            var eventHandler = Substitute.For<EventHandler<EventArgs>>();

            TestInfrastructure.ThreadHelper.SetCurrentThreadAsUIThread();

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator,
                issueLocationsTagAggregator,
                selectionService,
                lightBulbBroker: lightBulbBroker,
                textView: textView);
            testSubject.SuggestedActionsChanged += eventHandler;

            lightBulbBroker.DidNotReceiveWithAnyArgs().DismissSession(default);

            selectionService.SelectedIssueChanged += Raise.EventWith(EventArgs.Empty);

            lightBulbBroker.Received(1).DismissSession(textView);
            eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
        }

        [TestMethod]
        public void OnTagsChanged_NoSubscribersToSuggestedActionsChanged_NoException()
        {
            var selectedIssueLocationsTagAggregator = Substitute.For<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
            var textView = CreateWpfTextView();

            CreateTestSubject(selectedIssueLocationsTagAggregator, issueLocationsTagAggregator, textView: textView);

            var changedSpan = CreateMappingSpan(textView.TextSnapshot, new Span(0, 1));

            Action act = () => selectedIssueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(changedSpan));
            act.Should().NotThrow();

            act = () => issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(changedSpan));
            act.Should().NotThrow();
        }

        [TestMethod]
        public void OnTagsChanged_HasSubscribersToSuggestedActionsChanged_RaisesSuggestedActionsChanged()
        {
            var selectedIssueLocationsTagAggregator = Substitute.For<ITagAggregator<ISelectedIssueLocationTag>>();
            var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
            var textView = CreateWpfTextView();

            var eventHandler = Substitute.For<EventHandler<EventArgs>>();

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator, issueLocationsTagAggregator, textView: textView);
            testSubject.SuggestedActionsChanged += eventHandler;

            var changedSpan = CreateMappingSpan(textView.TextSnapshot, new Span(500, 1));

            selectedIssueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(changedSpan));
            eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());

            eventHandler.ClearReceivedCalls();

            issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(changedSpan));
            eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
        }

        [TestMethod]
        public void Get_Has_SuggestedActions_NoSelectionTags_NoIssueTags_NoActions()
        {
            var actionSets = GetSuggestedActions(
                primaryIssues:Enumerable.Empty<IAnalysisIssueVisualization>(),
                secondaryLocations:Enumerable.Empty<IAnalysisIssueLocationVisualization>(),
                selectedIssue: null);

            actionSets.actionList.Should().BeEmpty();
            actionSets.hasAction.Should().Be(false);
        }

        [TestMethod]
        public void Get_Has_SuggestedActions_NoSelectionTags_NoIssueTagsWithSecondaryLocations_NoActions()
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

            actionSets.actionList.Should().BeEmpty();
            actionSets.hasAction.Should().Be(false);
        }

        [TestMethod]
        public void Get_Has_SuggestedActions_NoSelectionTags_HasIssueTagsWithSecondaryLocations_SelectIssueActions()
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

            actionSets.actionList.Count.Should().Be(1);
            actionSets.hasAction.Should().Be(true);
            var suggestedActions = actionSets.actionList[0].Actions.ToArray();
            suggestedActions.Length.Should().Be(2);
            suggestedActions[0].Should().BeOfType<SelectIssueVisualizationAction>();
            (suggestedActions[0] as SelectIssueVisualizationAction).Issue.Should().Be(issues[0]);
            (suggestedActions[1] as SelectIssueVisualizationAction).Issue.Should().Be(issues[2]);
        }

        [TestMethod]
        public void Get_Has_SuggestedActions_NoSelectionTags_HasSelectedIssueTag_SelectAndDeselectIssueAction()
        {
            var issues = new[]
            {
                CreateIssueViz(CreateFlowViz(CreateLocationViz()))
            };

            var actionSets = GetSuggestedActions(
                primaryIssues: issues,
                secondaryLocations: Enumerable.Empty<IAnalysisIssueLocationVisualization>(),
                selectedIssue: issues[0]);

            actionSets.actionList.Count.Should().Be(1);
            actionSets.hasAction.Should().Be(true);
            var suggestedActions = actionSets.actionList[0].Actions.ToArray();
            suggestedActions.Length.Should().Be(2);
            suggestedActions[0].Should().BeOfType<SelectIssueVisualizationAction>();
            (suggestedActions[0] as SelectIssueVisualizationAction).Issue.Should().Be(issues[0]);
            suggestedActions[1].Should().BeOfType<DeselectIssueVisualizationAction>();
        }

        [TestMethod]
        public void Get_Has_SuggestedActions_HasSelectionTags_NoIssueTags_DeselectIssueAction()
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

            actionSets.actionList.Count.Should().Be(1);
            actionSets.hasAction.Should().Be(true);
            var suggestedActions = actionSets.actionList[0].Actions.ToArray();
            suggestedActions.Length.Should().Be(1);
            suggestedActions[0].Should().BeOfType<DeselectIssueVisualizationAction>();
        }

        [TestMethod]
        public void Get_Has_SuggestedActions_HasSelectionTags_HasIssueTagsWithSecondaryLocations_SelectAndDeselectIssueActions()
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

            actionSets.actionList.Count.Should().Be(1);
            actionSets.hasAction.Should().Be(true);
            var suggestedActions = actionSets.actionList.First().Actions.ToArray();
            suggestedActions.Length.Should().Be(3);
            suggestedActions[0].Should().BeOfType<SelectIssueVisualizationAction>();
            suggestedActions[1].Should().BeOfType<SelectIssueVisualizationAction>();
            suggestedActions[2].Should().BeOfType<DeselectIssueVisualizationAction>();
            (suggestedActions[0] as SelectIssueVisualizationAction).Issue.Should().Be(issues[0]);
            (suggestedActions[1] as SelectIssueVisualizationAction).Issue.Should().Be(issues[1]);
        }

        [TestMethod]
        public void Get_Has_SuggestedActions_HasSelecttionTags_NoIssueTagsWithSecondaryLocations_NoActions()
        {
            var issuesWithoutSecondaryLocations = new[]
            {
                CreateIssueViz(),
                CreateIssueViz()
            };

            var actionSets = GetSuggestedActions(
                primaryIssues: issuesWithoutSecondaryLocations,
                secondaryLocations: Enumerable.Empty<IAnalysisIssueLocationVisualization>(),
                selectedIssue: issuesWithoutSecondaryLocations[0]);

            actionSets.actionList.Should().BeEmpty();
            actionSets.hasAction.Should().Be(false);

        }

        private static IAnalysisIssueVisualization CreateIssueViz(params IAnalysisIssueFlowVisualization[] flows)
        {
            var issueViz = Substitute.For<IAnalysisIssueVisualization>();
            issueViz.Flows.Returns(flows);
            issueViz.SonarRuleId.Returns(new SonarCompositeRuleId("repo", "rule"));

            return issueViz;
        }

        private static IssueLocationActionsSource CreateTestSubject(ITagAggregator<ISelectedIssueLocationTag> selectedIssueLocationsTagAggregator,
            ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator,
            IIssueSelectionService selectionService = null,
            ILightBulbBroker lightBulbBroker = null,
            ITextView textView = null)
        {
            textView ??= CreateWpfTextView();
            var vsUiShell = Substitute.For<IVsUIShell>();
            var bufferTagAggregatorFactoryService = Substitute.For<IBufferTagAggregatorFactoryService>();

            bufferTagAggregatorFactoryService
                .CreateTagAggregator<ISelectedIssueLocationTag>(textView.TextBuffer)
                .Returns(selectedIssueLocationsTagAggregator);

            bufferTagAggregatorFactoryService
                .CreateTagAggregator<IIssueLocationTag>(textView.TextBuffer)
                .Returns(issueLocationsTagAggregator);

            var defaultSelectedIssue = Substitute.For<IAnalysisIssueVisualization>();
            var analysisIssueSelectionService = Substitute.For<IIssueSelectionService>();
            analysisIssueSelectionService.SelectedIssue.Returns(defaultSelectedIssue);

            selectionService ??= analysisIssueSelectionService;
            lightBulbBroker ??= Substitute.For<ILightBulbBroker>();

            return new IssueLocationActionsSource(lightBulbBroker, vsUiShell, bufferTagAggregatorFactoryService, textView, selectionService);
        }

        private (IList<SuggestedActionSet> actionList, bool hasAction) GetSuggestedActions(IEnumerable<IAnalysisIssueVisualization> primaryIssues,
            IEnumerable<IAnalysisIssueLocationVisualization> secondaryLocations,
            IAnalysisIssueVisualization selectedIssue)
        {
            var mockSpan = new SnapshotSpan();
            var snapshot = CreateSnapshot();

            var primaryTagSpans = primaryIssues.Select(x => CreateMappingTagSpan(snapshot, CreateIssueLocationTag(x), mockSpan));
            var secondaryTagSpans = secondaryLocations.Select(x => CreateMappingTagSpan(snapshot, CreateSelectedLocationTag(x), mockSpan));

            var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
            issueLocationsTagAggregator.GetTags(mockSpan).Returns(primaryTagSpans);

            var selectedIssueLocationsTagAggregator = Substitute.For<ITagAggregator<ISelectedIssueLocationTag>>();
            selectedIssueLocationsTagAggregator.GetTags(mockSpan).Returns(secondaryTagSpans);

            var selectionService = Substitute.For<IIssueSelectionService>();
            selectionService.SelectedIssue.Returns(selectedIssue);

            var testSubject = CreateTestSubject(selectedIssueLocationsTagAggregator, issueLocationsTagAggregator, selectionService);

            //We are testing these two methods together because their logic is coupled. These methods should not act indepedently of each other.
            var actualActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);
            var hasActions = testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None).Result;

            return (actualActionsSet.ToList(), hasActions);
        }
    }
}
