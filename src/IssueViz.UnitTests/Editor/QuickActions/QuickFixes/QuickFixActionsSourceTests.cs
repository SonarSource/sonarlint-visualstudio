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
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions.QuickFixes;

[TestClass]
public class QuickFixActionsSourceTests
{
    private SnapshotSpan mockSpan;
    private ITextBuffer textBuffer;
    private ITextView textView;

    [TestInitialize]
    public void TestInitialize()
    {
        mockSpan = new SnapshotSpan();
        textView = CreateWpfTextView();
        textBuffer = CreateBuffer();
    }

    [TestMethod]
    public void TryGetTelemetryId_False()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        testSubject.TryGetTelemetryId(out var guid).Should().BeFalse();
        guid.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_RegisterToTagAggregatorEvents()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        CreateTestSubject(issueLocationsTagAggregator);

        issueLocationsTagAggregator.Received(1).TagsChanged += Arg.Any<EventHandler<TagsChangedEventArgs>>();
    }

    [TestMethod]
    public void Dispose_UnregisterFromTagAggregatorEvents()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);
        testSubject.Dispose();

        issueLocationsTagAggregator.Received(1).TagsChanged -= Arg.Any<EventHandler<TagsChangedEventArgs>>();
        issueLocationsTagAggregator.Received(1).Dispose();
    }

    [TestMethod]
    public void OnTagsChanged_NonCriticalException_ExceptionIsCaught()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);
        testSubject.SuggestedActionsChanged += (_, _) => throw new NotImplementedException("this is a test");

        var act = () => testSubject.HandleTagsChanged();
        act.Should().NotThrow();
    }

    [TestMethod]
    public void OnTagsChanged_CriticalException_ExceptionIsNotCaught()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);
        testSubject.SuggestedActionsChanged += (_, _) => throw new StackOverflowException("this is a test");

        var act = () => testSubject.HandleTagsChanged();
        act.Should().ThrowExactly<StackOverflowException>().WithMessage("this is a test");
    }

    [TestMethod]
    public void OnTagsChanged_NoSubscribersToSuggestedActionsChanged_NoException()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        CreateTestSubject(issueLocationsTagAggregator);

        var changedSpan = CreateMappingSpan(textView.TextSnapshot, new Span(0, 1));

        var act = () => issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(changedSpan));
        act.Should().NotThrow();
    }

    [TestMethod]
    public void OnTagsChanged_HasSubscribersToSuggestedActionsChanged_RaisesSuggestedActionsChanged()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        var eventHandler = Substitute.For<EventHandler<EventArgs>>();

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);
        testSubject.SuggestedActionsChanged += eventHandler;

        var changedSpan = CreateMappingSpan(textView.TextSnapshot, new Span(500, 1));

        issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(changedSpan));

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Any<EventArgs>());
    }

    [TestMethod]
    public void OnTagsChanged_LightBulbSessionNeverDismissed()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
        var lightBulbBroker = Substitute.For<ILightBulbBroker>();

        CreateTestSubject(issueLocationsTagAggregator, lightBulbBroker);

        var changedSpan = CreateMappingSpan(textView.TextSnapshot, new Span(0, 1));

        issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(changedSpan));

        lightBulbBroker.DidNotReceiveWithAnyArgs().DismissSession(default);
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_NoIssueTags_False()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, new SnapshotSpan(), CancellationToken.None);

        hasSuggestedActions.Should().Be(false);
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_NoIssuesWithQuickFixes_False()
    {
        var issues = new[] { CreateIssueViz() };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        hasSuggestedActions.Should().Be(false);
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_NoIssuesWithApplicableQuickFixes_False()
    {
        var issues = new[] { CreateIssueViz(CreateQuickFixViz(canBeApplied: false)) };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        hasSuggestedActions.Should().Be(false);
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_HasIssuesWithApplicableQuickFixes_True()
    {
        var issues = new[] { CreateIssueViz(CreateQuickFixViz(canBeApplied: true)) };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        hasSuggestedActions.Should().Be(true);
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_NonCriticalException_Suppressed()
    {
        // Regression test for #3122: Goldbar thrown when opening and quickly closing a .ts file
        // https://github.com/SonarSource/sonarlint-visualstudio/issues/3122

        var logger = new TestLogger();
        var tagAggregator = CreateThrowingAggregator(new InvalidOperationException("this is a test"));

        var testSubject = CreateTestSubject(tagAggregator, logger: logger);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        hasSuggestedActions.Should().Be(false);
        tagAggregator.Received().GetTags(Arg.Any<SnapshotSpan>());
        logger.AssertPartialOutputStringExists("this is a test");
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_CriticalException_IsNotSuppressed()
    {
        var logger = new TestLogger();
        var tagAggregator = CreateThrowingAggregator(new StackOverflowException("this is a test"));

        var testSubject = CreateTestSubject(tagAggregator, logger: logger);

        var func = async () => await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        func.Should().ThrowExactly<StackOverflowException>().And
            .Message.Should().Be("this is a test");

        logger.AssertPartialOutputStringDoesNotExist("this is a test");
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

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        var hasSuggestedActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

        hasSuggestedActionsSet.Should().BeEmpty();
    }

    [TestMethod]
    public void GetSuggestedActions_NoIssuesWithApplicableQuickFixes_NoActions()
    {
        var issues = new[] { CreateIssueViz(CreateQuickFixViz(canBeApplied: false)) };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        var hasSuggestedActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

        hasSuggestedActionsSet.Should().BeEmpty();
    }

    [TestMethod]
    public void GetSuggestedActions_HasIssuesWithQuickFixes_OneActionForEveryApplicableFix()
    {
        var issues = new[]
        {
            CreateIssueViz(
                CreateQuickFixViz(canBeApplied: false, message: "fix1"),
                CreateQuickFixViz(canBeApplied: true, message: "fix2")),
            CreateIssueViz(
                CreateQuickFixViz(canBeApplied: true, message: "fix3"),
                CreateQuickFixViz(canBeApplied: false, message: "fix4")),
            CreateIssueViz(),
            CreateIssueViz(CreateQuickFixViz(canBeApplied: false, message: "fix5")),
            CreateIssueViz(CreateQuickFixViz(canBeApplied: true, message: "fix6"))
        };

        var issueLocationsTagAggregator = CreateTagAggregatorForIssues(issues);

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);

        var hasSuggestedActionsSet = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);
        hasSuggestedActionsSet.Count().Should().Be(1);

        var quickFixSuggestedActions = hasSuggestedActionsSet.Single().Actions.OfType<QuickFixSuggestedAction>().ToList();
        quickFixSuggestedActions.Count.Should().Be(3);
        quickFixSuggestedActions.Select(x => x.DisplayText).Should().BeEquivalentTo(Resources.ProductNameCommandPrefix + "fix2", Resources.ProductNameCommandPrefix + "fix3",
            Resources.ProductNameCommandPrefix + "fix6");
    }

    [TestMethod]
    public async Task GetSuggestedActionsAsync_NonCriticalException_Suppressed()
    {
        var logger = new TestLogger();
        var tagAggregator = CreateThrowingAggregator(new InvalidOperationException("this is a test"));

        var testSubject = CreateTestSubject(tagAggregator, logger: logger);

        var actual = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
        tagAggregator.Received().GetTags(Arg.Any<SnapshotSpan>());
        logger.AssertPartialOutputStringExists("this is a test");
    }

    [TestMethod]
    public async Task GetSuggestedActionsAsync_CriticalException_IsNotSuppressed()
    {
        var logger = new TestLogger();
        var tagAggregator = CreateThrowingAggregator(new StackOverflowException("this is a test"));

        var testSubject = CreateTestSubject(tagAggregator, logger: logger);

        Action act = () => testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

        act.Should().ThrowExactly<StackOverflowException>().And
            .Message.Should().Be("this is a test");
        logger.AssertPartialOutputStringDoesNotExist("this is a test");
    }

    private QuickFixActionsSource CreateTestSubject(
        ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator = null,
        ILightBulbBroker lightBulbBroker = null,
        ILogger logger = null,
        IThreadHandling threadHandling = null)
    {
        issueLocationsTagAggregator ??= Substitute.For<ITagAggregator<IIssueLocationTag>>();
        lightBulbBroker ??= Substitute.For<ILightBulbBroker>();
        logger ??= Substitute.For<ILogger>();

        var bufferTagAggregatorFactoryService = Substitute.For<IBufferTagAggregatorFactoryService>();

        bufferTagAggregatorFactoryService
            .CreateTagAggregator<IIssueLocationTag>(textBuffer)
            .Returns(issueLocationsTagAggregator);

        threadHandling ??= new NoOpThreadHandler();

        return new QuickFixActionsSource(lightBulbBroker,
            bufferTagAggregatorFactoryService,
            textView,
            textBuffer,
            Substitute.For<IQuickFixApplicationLogic>(),
            logger,
            threadHandling);
    }

    private IAnalysisIssueVisualization CreateIssueViz(params IQuickFixApplication[] fixes)
    {
        var issueViz = Substitute.For<IAnalysisIssueVisualization>();
        issueViz.QuickFixes.Returns(fixes);

        return issueViz;
    }

    private ITagAggregator<IIssueLocationTag> CreateTagAggregatorForIssues(IAnalysisIssueVisualization[] issues)
    {
        var issueTags = issues.Select(x => CreateMappingTagSpan(textBuffer.CurrentSnapshot, CreateIssueLocationTag(x), mockSpan)).ToArray();

        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        issueLocationsTagAggregator
            .GetTags(mockSpan)
            .Returns(issueTags);

        return issueLocationsTagAggregator;
    }

    private IQuickFixApplication CreateQuickFixViz(bool canBeApplied, string message = null)
    {
        var quickFixApplication = Substitute.For<IQuickFixApplication>();
        quickFixApplication.Message.Returns(message);
        quickFixApplication.CanBeApplied(textBuffer.CurrentSnapshot).Returns(canBeApplied);

        return quickFixApplication;
    }

    private static ITagAggregator<IIssueLocationTag> CreateThrowingAggregator(Exception ex)
    {
        var throwingAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
        throwingAggregator.GetTags(Arg.Any<SnapshotSpan>()).Throws(ex);
        return throwingAggregator;
    }
}
