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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions;

[TestClass]
public class IssueActionsSourceBaseTests
{
    private SnapshotSpan mockSpan;
    private ITextBuffer textBuffer = null!;
    private ITextView textView = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        mockSpan = new SnapshotSpan();
        textView = CreateTextView();
        textBuffer = CreateTextBuffer();
    }

    [TestMethod]
    public void TryGetTelemetryId_False()
    {
        var testSubject = CreateTestSubject();

        testSubject.TryGetTelemetryId(out var guid).Should().BeFalse();
        guid.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_RegisterToTagAggregatorEvents()
    {
        var lightBulbBroker = Substitute.For<ILightBulbBroker>();
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        CreateTestSubject(issueLocationsTagAggregator, lightBulbBroker);

        issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(Substitute.For<IMappingSpan>()));
        lightBulbBroker.Received(1).DismissSession(textView);
    }

    [TestMethod]
    public void Dispose_UnregisterFromTagAggregatorEvents()
    {
        var lightBulbBroker = Substitute.For<ILightBulbBroker>();
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        var testSubject = CreateTestSubject(issueLocationsTagAggregator, lightBulbBroker);
        testSubject.Dispose();

        issueLocationsTagAggregator.Received(1).Dispose();
        issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(Substitute.For<IMappingSpan>()));
        lightBulbBroker.DidNotReceive().DismissSession(Arg.Any<ITextView>());
    }

    [TestMethod]
    public void OnTagsChanged_DismissLightBulbSession()
    {
        var lightBulbBroker = Substitute.For<ILightBulbBroker>();
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        CreateTestSubject(issueLocationsTagAggregator, lightBulbBroker);

        lightBulbBroker.DidNotReceive().DismissSession(Arg.Any<ITextView>());

        issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(Substitute.For<IMappingSpan>()));
        lightBulbBroker.Received(1).DismissSession(textView);
    }

    [TestMethod]
    public void OnTagsChanged_NonCriticalException_ExceptionIsCaught()
    {
        var lightBulbBroker = Substitute.For<ILightBulbBroker>();
        lightBulbBroker
            .When(x => x.DismissSession(textView))
            .Throw(new NotImplementedException("this is a test"));

        var testSubject = CreateTestSubject(lightBulbBroker: lightBulbBroker);

        var act = async () => await testSubject.HandleTagsChangedAsync();
        act.Should().NotThrow();

        lightBulbBroker.Received(1).DismissSession(textView);
    }

    [TestMethod]
    public void OnTagsChanged_CriticalException_ExceptionIsNotCaught()
    {
        var lightBulbBroker = Substitute.For<ILightBulbBroker>();
        lightBulbBroker
            .When(x => x.DismissSession(textView))
            .Throw(new StackOverflowException("this is a test"));

        var testSubject = CreateTestSubject(lightBulbBroker: lightBulbBroker);

        var act = async () => await testSubject.HandleTagsChangedAsync();
        act.Should().ThrowExactly<StackOverflowException>().WithMessage("this is a test");

        lightBulbBroker.Received(1).DismissSession(textView);
    }

    [TestMethod]
    public void OnTagsChanged_NoSubscribersToSuggestedActionsChanged_NoException()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        CreateTestSubject(issueLocationsTagAggregator);

        var act = () => issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(Substitute.For<IMappingSpan>()));
        act.Should().NotThrow();
    }

    [TestMethod]
    public void OnTagsChanged_HasSubscribersToSuggestedActionsChanged_RaisesSuggestedActionsChanged()
    {
        var issueLocationsTagAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();

        var raised = false;

        var testSubject = CreateTestSubject(issueLocationsTagAggregator);
        testSubject.SuggestedActionsChanged += (_, _) => raised = true;

        issueLocationsTagAggregator.TagsChanged += Raise.EventWith(new TagsChangedEventArgs(Substitute.For<IMappingSpan>()));

        raised.Should().BeTrue();
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_NoIssueTags_False()
    {
        var testSubject = CreateTestSubject();

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, new SnapshotSpan(), CancellationToken.None);

        hasSuggestedActions.Should().BeFalse();
    }

    [TestMethod]
    public async Task HasSuggestedActionsAsync_NonCriticalException_Suppressed()
    {
        var tagAggregator = CreateThrowingAggregator(new InvalidOperationException("this is a test"));

        var testSubject = CreateTestSubject(tagAggregator);

        var hasSuggestedActions = await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        hasSuggestedActions.Should().BeFalse();
        tagAggregator.Received().GetTags(Arg.Any<SnapshotSpan>());
    }

    [TestMethod]
    public void HasSuggestedActionsAsync_CriticalException_IsNotSuppressed()
    {
        var tagAggregator = CreateThrowingAggregator(new StackOverflowException("this is a test"));

        var testSubject = CreateTestSubject(tagAggregator);

        var func = async () => await testSubject.HasSuggestedActionsAsync(null, mockSpan, CancellationToken.None);

        func.Should().ThrowExactly<StackOverflowException>().And
            .Message.Should().Be("this is a test");
    }

    [TestMethod]
    public void GetSuggestedActions_NonCriticalException_Suppressed()
    {
        var tagAggregator = CreateThrowingAggregator(new InvalidOperationException("this is a test"));

        var testSubject = CreateTestSubject(tagAggregator);

        var actual = testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
        tagAggregator.Received().GetTags(Arg.Any<SnapshotSpan>());
    }

    [TestMethod]
    public void GetSuggestedActions_CriticalException_IsNotSuppressed()
    {
        var tagAggregator = CreateThrowingAggregator(new StackOverflowException("this is a test"));

        var testSubject = CreateTestSubject(tagAggregator);

        Action act = () => testSubject.GetSuggestedActions(null, mockSpan, CancellationToken.None);

        act.Should().ThrowExactly<StackOverflowException>().And
            .Message.Should().Be("this is a test");
    }

    private TestableIssueActionsSource CreateTestSubject(
        ITagAggregator<IIssueLocationTag> issueLocationsTagAggregator = null,
        ILightBulbBroker lightBulbBroker = null,
        IThreadHandling threadHandling = null)
    {
        issueLocationsTagAggregator ??= Substitute.For<ITagAggregator<IIssueLocationTag>>();
        lightBulbBroker ??= Substitute.For<ILightBulbBroker>();
        var logger = Substitute.For<ILogger>();
        logger.ForVerboseContext(Arg.Any<string[]>()).Returns(logger);

        var bufferTagAggregatorFactoryService = Substitute.For<IBufferTagAggregatorFactoryService>();
        bufferTagAggregatorFactoryService
            .CreateTagAggregator<IIssueLocationTag>(textBuffer)
            .Returns(issueLocationsTagAggregator);

        threadHandling ??= new NoOpThreadHandler();

        return new TestableIssueActionsSource(
            lightBulbBroker,
            bufferTagAggregatorFactoryService,
            textView,
            textBuffer,
            logger,
            threadHandling);
    }

    private static ITagAggregator<IIssueLocationTag> CreateThrowingAggregator(Exception ex)
    {
        var throwingAggregator = Substitute.For<ITagAggregator<IIssueLocationTag>>();
        throwingAggregator.GetTags(Arg.Any<SnapshotSpan>()).Returns(_ => throw ex);
        return throwingAggregator;
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

    private sealed class TestableIssueActionsSource : IssueActionsSourceBase
    {
        public TestableIssueActionsSource(
            ILightBulbBroker lightBulbBroker,
            IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService,
            ITextView textView,
            ITextBuffer textBuffer,
            ILogger logger,
            IThreadHandling threadHandling)
            : base(lightBulbBroker, bufferTagAggregatorFactoryService, textView, textBuffer, logger, threadHandling)
        {
        }

        protected override SuggestedActionSetPriority Priority => SuggestedActionSetPriority.Medium;

        protected override bool TryGetMatchingIssues(IEnumerable<IAnalysisIssueVisualization> issueVisualizations, out IEnumerable<IAnalysisIssueVisualization> matchingIssues)
        {
            matchingIssues = issueVisualizations;
            return matchingIssues.Any();
        }

        protected override IEnumerable<ISuggestedAction> CreateActions(IEnumerable<IAnalysisIssueVisualization> matchingIssues)
        {
            return matchingIssues.Select(_ => Substitute.For<ISuggestedAction>());
        }
    }
}
