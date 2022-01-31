/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;
using SonarLint.VisualStudio.IssueVisualization.Models;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions.QuickFixes
{
    [TestClass]
    public class QuickFixActionsSourceTests
    {
        private readonly SnapshotSpan mockSpan = new();
        private readonly ITextSnapshot snapshot = CreateSnapshot();

        [TestMethod]
        public void TryGetTelemetryId_False()
        {
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

            testSubject.TryGetTelemetryId(out var guid).Should().BeFalse();
            guid.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_RegisterToTagAggregatorEvents()
        {
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();
            issueLocationsTagAggregator.SetupAdd(x => x.TagsChanged += null);

            CreateTestSubject(issueLocationsTagAggregator.Object);

            issueLocationsTagAggregator.VerifyAdd(x => x.TagsChanged += It.IsAny<EventHandler<TagsChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromTagAggregatorEvents()
        {
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();
            issueLocationsTagAggregator.SetupRemove(x => x.TagsChanged -= null);

            var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);
            testSubject.Dispose();

            issueLocationsTagAggregator.VerifyRemove(x => x.TagsChanged -= It.IsAny<EventHandler<TagsChangedEventArgs>>(), Times.Once);
            issueLocationsTagAggregator.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void OnTagsChanged_DismissLightBulbSession()
        {
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();
            var lightBulbBroker = new Mock<ILightBulbBroker>();
            var textView = Mock.Of<ITextView>();

            CreateTestSubject(issueLocationsTagAggregator.Object,
                lightBulbBroker: lightBulbBroker.Object,
                textView: textView);

            lightBulbBroker.VerifyNoOtherCalls();

            issueLocationsTagAggregator.Raise(x => x.TagsChanged += null, new TagsChangedEventArgs(Mock.Of<IMappingSpan>()));
            lightBulbBroker.Verify(x => x.DismissSession(textView), Times.Once);
        }

        [TestMethod]
        public void OnTagsChanged_NoSubscribersToSuggestedActionsChanged_NoException()
        {
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            CreateTestSubject(issueLocationsTagAggregator.Object);

            Action act = () => issueLocationsTagAggregator.Raise(x => x.TagsChanged += null, new TagsChangedEventArgs(Mock.Of<IMappingSpan>()));
            act.Should().NotThrow();
        }

        [TestMethod]
        public void OnTagsChanged_HasSubscribersToSuggestedActionsChanged_RaisesSuggestedActionsChanged()
        {
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            var eventHandler = new Mock<EventHandler<EventArgs>>();

            var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);
            testSubject.SuggestedActionsChanged += eventHandler.Object;

            issueLocationsTagAggregator.Raise(x => x.TagsChanged += null, new TagsChangedEventArgs(Mock.Of<IMappingSpan>()));
            
            eventHandler.Verify(x => x(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Once);
        }

        [TestMethod]
        public async Task HasSuggestedActionsAsync_NoIssueTags_False()
        {
            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

            var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, new SnapshotSpan(), CancellationToken.None);

            hasSuggestedActions.Should().Be(false);
        }

        [TestMethod]
        public async Task HasSuggestedActionsAsync_NoIssuesWithQuickFixes_False()
        {
            var issues = new[] { CreateIssueViz() };

            var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

            var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

            var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

            hasSuggestedActions.Should().Be(false);
        }

        [TestMethod]
        public async Task HasSuggestedActionsAsync_HasIssuesWithQuickFixes_True()
        {
            var issues = new[] { CreateIssueViz(Mock.Of<IQuickFix>()) };

            var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

            var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

            var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

            hasSuggestedActions.Should().Be(true);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetSuggestedActions_NoIssuesWithQuickFixes_NoActions(bool hasIssues)
        {
            var issues = hasIssues
                ? new[] { CreateIssueViz() }
                : Array.Empty<IAnalysisIssueVisualization>();

            var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

            var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

            var hasSuggestedActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

            hasSuggestedActionsSet.Should().BeEmpty();
        }

        [TestMethod]
        public void GetSuggestedActions_HasIssuesWithQuickFixes_OneActionForEveryFix()
        {
            var issues = new[]
            {
                CreateIssueViz(Mock.Of<IQuickFix>()),
                CreateIssueViz(Mock.Of<IQuickFix>(), Mock.Of<IQuickFix>()),
                CreateIssueViz()
            };

            var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

            var testSubject = CreateTestSubject(issueLocationsTagAggregator.Object);

            var hasSuggestedActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

            hasSuggestedActionsSet.Count().Should().Be(1);
            hasSuggestedActionsSet.Single().Actions.Count().Should().Be(3);
        }

        private QuickFixActionsSource CreateTestSubject(ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator,
            ILightBulbBroker lightBulbBroker = null,
            ITextView textView = null)
        {
            textView ??= CreateWpfTextView();
            lightBulbBroker ??= Mock.Of<ILightBulbBroker>();

            var bufferTagAggregatorFactoryService = new Mock<IBufferTagAggregatorFactoryService>();

            bufferTagAggregatorFactoryService
                .Setup(x => x.CreateTagAggregator<IIssueLocationTag>(textView.TextBuffer))
                .Returns(issueLocationsTagAggregator);

            var threadHandling = new NoOpThreadHandler();

            return new QuickFixActionsSource(lightBulbBroker, 
                bufferTagAggregatorFactoryService.Object, 
                textView,
                threadHandling);
        }

        private IAnalysisIssueVisualization CreateIssueViz(params IQuickFix[] fixes)
        {
            var baseIssue = new Mock<IAnalysisIssue>();
            baseIssue.Setup(x => x.Fixes).Returns(fixes);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(baseIssue.Object);

            return issueViz.Object;
        }

        private Mock<ITagAggregator<IIssueLocationTag>> CreateTagAggregatorForIssues(IAnalysisIssueVisualization[] issues)
        {
            var issueTags = issues.Select(x => CreateMappingTagSpan(snapshot, CreateIssueLocationTag(x), mockSpan)).ToArray();

            var issueLocationsTagAggregator = new Mock<ITagAggregator<IIssueLocationTag>>();

            issueLocationsTagAggregator
                .Setup(x => x.GetTags(mockSpan))
                .Returns(issueTags);

            return issueLocationsTagAggregator;
        }
    }
}
