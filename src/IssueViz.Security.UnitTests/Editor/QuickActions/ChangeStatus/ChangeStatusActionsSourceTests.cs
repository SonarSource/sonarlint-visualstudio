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

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Security.Editor.QuickActions.ChangeStatus;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Editor.QuickActions.ChangeStatus;

[TestClass]
public class ChangeStatusActionsSourceTests
{
    private SnapshotSpan mockSpan;
    private ITextBuffer textBuffer = null!;
    private ITextView textView = null!;
    private IMuteIssuesService muteIssuesService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        mockSpan = new SnapshotSpan();
        textView = CreateTextView();
        textBuffer = CreateTextBuffer();
        muteIssuesService = Substitute.For<IMuteIssuesService>();
    }

    [TestMethod]
    public void IsSubclassOfIssueActionsSourceBase()
    {
        typeof(ChangeStatusActionsSource).Should().BeAssignableTo<IssueActionsSourceBase>();
    }

    [TestMethod]
    [DataRow(null, false)]
    [DataRow("key1", true)]
    public async Task HasSuggestedActionsAsync_NonServerOrResolvedIssue_False(string issueServerKey, bool isResolved)
    {
        var issues = new[] { CreateIssueViz(issueServerKey: issueServerKey, isResolved: isResolved) };
        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        hasSuggestedActions.Should().BeFalse();
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_UnresolvedServerIssue_True()
    {
        var issues = new[] { CreateIssueViz(issueServerKey: "key1", isResolved: false) };
        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        hasSuggestedActions.Should().BeTrue();
    }

    [TestMethod]
    public void GetSuggestedActions_MixedIssues_OneActionPerUnresolvedServerIssue()
    {
        var issues = new[]
        {
            CreateIssueViz(issueServerKey: "key1", isResolved: false),
            CreateIssueViz(issueServerKey: null, isResolved: false),
            CreateIssueViz(issueServerKey: "key2", isResolved: true),
            CreateIssueViz(issueServerKey: "key3", isResolved: false)
        };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        var suggestedActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);
        suggestedActionsSet.Count().Should().Be(1);

        var changeStatusActions = suggestedActionsSet.Single().Actions.OfType<ChangeStatusSuggestedAction>().ToList();
        changeStatusActions.Count.Should().Be(2);
        changeStatusActions.Select(x => x.DisplayText).Should().OnlyContain(x => x == Resources.ChangeStatusActionText);
    }

    private ChangeStatusActionsSource CreateTestSubject(
        ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator = null)
    {
        issueLocationsTagAggregator ??= Substitute.For<ITagAggregator<IIssueLocationTag>>();
        var logger = Substitute.For<ILogger>();
        logger.ForVerboseContext(Arg.Any<string[]>()).Returns(logger);

        var bufferTagAggregatorFactoryService = Substitute.For<IBufferTagAggregatorFactoryService>();
        bufferTagAggregatorFactoryService
            .CreateTagAggregator<IIssueLocationTag>(textBuffer)
            .Returns(issueLocationsTagAggregator);

        return new ChangeStatusActionsSource(
            Substitute.For<ILightBulbBroker>(),
            bufferTagAggregatorFactoryService,
            textView,
            textBuffer,
            muteIssuesService,
            logger,
            new NoOpThreadHandler());
    }

    private static IAnalysisIssueVisualization CreateIssueViz(string issueServerKey, bool isResolved)
    {
        var issueViz = Substitute.For<IAnalysisIssueVisualization>();
        issueViz.IssueServerKey.Returns(issueServerKey);
        issueViz.IsResolved.Returns(isResolved);
        return issueViz;
    }

    private ITagAggregator<IIssueLocationTag> CreateTagAggregatorForIssues(IAnalysisIssueVisualization[] issues)
    {
        var issueTags = issues.Select(CreateMappingTagSpan).ToArray();

        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
        issueLocationsTagAggregator.GetTags(mockSpan).Returns(issueTags);

        return issueLocationsTagAggregator;
    }

    private static IMappingTagSpan<IIssueLocationTag> CreateMappingTagSpan(IAnalysisIssueVisualization issueViz)
    {
        var tag = Substitute.For<IIssueLocationTag>();
        tag.Location.Returns(issueViz);

        var tagSpan = Substitute.For<IMappingTagSpan<IIssueLocationTag>>();
        tagSpan.Tag.Returns(tag);

        return tagSpan;
    }

    private static ITextView CreateTextView()
    {
        var snapshot = Substitute.For<ITextSnapshot>();
        snapshot.Length.Returns(999);

        var buffer = Substitute.For<ITextBuffer>();
        buffer.CurrentSnapshot.Returns(snapshot);
        snapshot.TextBuffer.Returns(buffer);

        var view = Substitute.For<ITextView>();
        view.TextSnapshot.Returns(snapshot);
        view.TextBuffer.Returns(buffer);
        return view;
    }

    private static ITextBuffer CreateTextBuffer()
    {
        var snapshot = Substitute.For<ITextSnapshot>();
        var buffer = Substitute.For<ITextBuffer>();

        buffer.CurrentSnapshot.Returns(snapshot);
        snapshot.TextBuffer.Returns(buffer);
        snapshot.Length.Returns(999);

        return buffer;
    }
}
