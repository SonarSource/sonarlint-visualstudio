/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.ErrorTagging;

[TestClass]
public class ErrorTaggerTests
{
    [TestMethod]
    public void Ctor_SubscribesToEvents()
    {
        var focusOnNewCodeService = Substitute.For<IFocusOnNewCodeService>();

        CreateTestSubject(focusOnNewCodeService: focusOnNewCodeService);

        focusOnNewCodeService.Received(1).Changed += Arg.Any<EventHandler<NewCodeStatusChangedEventArgs>>();
    }

    [TestMethod]
    public void GetTags_FilterIsApplied_ExpectedTagsCreated()
    {
        var snapshot = CreateSnapshot(length: 50);

        var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);

        var primary1 = CreateTagSpanWithPrimaryLocation(snapshot, new Span(1, 5), message: "error message 1", ruleKey: "cpp:S5350");
        var primary1Suppressed = CreateTagSpanWithPrimaryLocation(snapshot, new Span(2, 6), isResolved: true);
        var primary2 = CreateTagSpanWithPrimaryLocation(snapshot, new Span(10, 5), message: "error message 2", ruleKey: "cpp:emptyCompoundStatement");
        var primary2Suppressed = CreateTagSpanWithPrimaryLocation(snapshot, new Span(8, 15), isResolved: true);
        var secondary1 = CreateTagSpanWithSecondaryLocation(snapshot, new Span(20, 5));
        var secondary2 = CreateTagSpanWithSecondaryLocation(snapshot, new Span(30, 5));
        var aggregator = CreateAggregator(primary1, secondary1, primary1Suppressed, primary2, secondary2, primary2Suppressed);

        var tooltipProvider = Substitute.For<IErrorTagTooltipProvider>();
        var issue1 = (primary1.Tag.Location as IAnalysisIssueVisualization).Issue;
        var issue2 = (primary2.Tag.Location as IAnalysisIssueVisualization).Issue;

        tooltipProvider.Create(issue1).Returns("some tooltip1");
        tooltipProvider.Create(issue2).Returns("some tooltip2");

        var focusOnNewCodeService = Substitute.For<IFocusOnNewCodeService>();
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(false));

        var testSubject = new ErrorTagger(aggregator, snapshot.TextBuffer, tooltipProvider, focusOnNewCodeService);

        // Act
        var actual = testSubject.GetTags(inputSpans).ToArray();

        actual[0].Tag.ToolTipContent.Should().Be("some tooltip1");
        actual[0].Span.Span.Should().Be(primary1.Tag.Location.Span.Value.Span);

        actual[1].Tag.ToolTipContent.Should().Be("some tooltip2");
        actual[1].Span.Span.Should().Be(primary2.Tag.Location.Span.Value.Span);

        actual.Length.Should().Be(2);
    }

    [TestMethod]
    public void GetTags_EmptySpansShouldBeFiltered()
    {
        var snapshot = CreateSnapshot(length: 50);

        var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);

        var validSpan = new Span(1, 5);
        var invalidSpan = new Span(10, 0);

        var primary1 = CreateTagSpanWithPrimaryLocation(snapshot, validSpan, message: "error message 1", ruleKey: "cpp:S5350");
        var primary2 = CreateTagSpanWithPrimaryLocation(snapshot, invalidSpan, message: "error message 2", ruleKey: "cpp:emptyCompoundStatement");
        var aggregator = CreateAggregator(primary1, primary2);

        var tooltipProvider = Substitute.For<IErrorTagTooltipProvider>();
        var issue1 = (primary1.Tag.Location as IAnalysisIssueVisualization).Issue;
        var issue2 = (primary2.Tag.Location as IAnalysisIssueVisualization).Issue;
        tooltipProvider.Create(issue1).Returns("some tooltip1");
        tooltipProvider.Create(issue2).Returns("some tooltip2");

        var focusOnNewCodeService = Substitute.For<IFocusOnNewCodeService>();
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(false));

        var testSubject = new ErrorTagger(aggregator, snapshot.TextBuffer, tooltipProvider, focusOnNewCodeService);

        // Act
        var actual = testSubject.GetTags(inputSpans).ToArray();

        actual.Length.Should().Be(1);

        actual[0].Tag.ToolTipContent.Should().Be("some tooltip1");
        actual[0].Span.Span.Should().Be(validSpan);
    }

    [TestMethod]
    public void GetTags_WhenFocusOnNewCodeDisabled_ErrorTypeIsWarning()
    {
        var snapshot = CreateSnapshot(length: 50);
        var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);
        var newCodeTagSpan = CreateTagSpanWithPrimaryLocation(snapshot, new Span(1, 5), message: "error message 1", ruleKey: "cpp:S5350", isOnNewCode: true);
        var oldCodeTagSpan = CreateTagSpanWithPrimaryLocation(snapshot, new Span(2, 5), message: "error message 2", ruleKey: "cpp:S5350", isOnNewCode: false);
        var aggregator = CreateAggregator(newCodeTagSpan, oldCodeTagSpan);
        var focusOnNewCodeService = Substitute.For<IFocusOnNewCodeService>();
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(false));
        var testSubject = CreateTestSubject(aggregator, snapshot, focusOnNewCodeService);

        var actual = testSubject.GetTags(inputSpans).ToArray();

        actual.Length.Should().Be(2);
        actual[0].Tag.ErrorType.Should().Be(PredefinedErrorTypeNames.Warning);
        actual[1].Tag.ErrorType.Should().Be(PredefinedErrorTypeNames.Warning);
    }

    [TestMethod]
    public void GetTags_WhenFocusOnNewCodeEnabled_ErrorTypeIsWarningForNewAndSuggestionForOld()
    {
        var snapshot = CreateSnapshot(length: 50);
        var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);
        var newCodeTagSpan = CreateTagSpanWithPrimaryLocation(snapshot, new Span(1, 5), message: "error message 1", ruleKey: "cpp:S5350", isOnNewCode: true);
        var oldCodeTagSpan = CreateTagSpanWithPrimaryLocation(snapshot, new Span(2, 5), message: "error message 2", ruleKey: "cpp:S5350", isOnNewCode: false);
        var aggregator = CreateAggregator(newCodeTagSpan, oldCodeTagSpan);
        var focusOnNewCodeService = Substitute.For<IFocusOnNewCodeService>();
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(true));
        var testSubject = CreateTestSubject(aggregator, snapshot, focusOnNewCodeService);

        var actual = testSubject.GetTags(inputSpans).ToArray();

        actual.Length.Should().Be(2);
        actual[0].Tag.ErrorType.Should().Be(PredefinedErrorTypeNames.Warning);
        actual[1].Tag.ErrorType.Should().Be(PredefinedErrorTypeNames.HintedSuggestion);
    }

    [TestMethod]
    public void GetTags_WhenSeverityIsError_ErrorTypeIsSyntaxError()
    {
        var snapshot = CreateSnapshot(length: 50);
        var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);
        var tagSpan = CreateTagSpanWithPrimaryLocation(snapshot, new Span(1, 5), vsSeverity: __VSERRORCATEGORY.EC_ERROR);
        var aggregator = CreateAggregator(tagSpan);
        var focusOnNewCodeService = Substitute.For<IFocusOnNewCodeService>();
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(false));
        var testSubject = CreateTestSubject(aggregator, snapshot, focusOnNewCodeService);

        var actual = testSubject.GetTags(inputSpans).ToArray();

        actual.Length.Should().Be(1);
        actual[0].Tag.ErrorType.Should().Be(PredefinedErrorTypeNames.SyntaxError);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetTags_WhenSeverityIsError_FocusOnNewCodeDoesNotAffectErrorType(bool isOnNewCode)
    {
        var snapshot = CreateSnapshot(length: 50);
        var inputSpans = CreateSpanCollectionSpanningWholeSnapshot(snapshot);
        var tagSpan = CreateTagSpanWithPrimaryLocation(snapshot, new Span(1, 5), isOnNewCode: isOnNewCode, vsSeverity: __VSERRORCATEGORY.EC_ERROR);
        var aggregator = CreateAggregator(tagSpan);
        var focusOnNewCodeService = Substitute.For<IFocusOnNewCodeService>();
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(true));
        var testSubject = CreateTestSubject(aggregator, snapshot, focusOnNewCodeService);

        var actual = testSubject.GetTags(inputSpans).ToArray();

        actual.Length.Should().Be(1);
        actual[0].Tag.ErrorType.Should().Be(PredefinedErrorTypeNames.SyntaxError);
    }

    [TestMethod]
    public void GetTags_WhenFocusOnNewCodeChanged_NotifyTagger()
    {
        var focusOnNewCodeService = Substitute.For<IFocusOnNewCodeService>();
        var testSubject = CreateTestSubject(focusOnNewCodeService: focusOnNewCodeService);
        var tagsChangedRaised = false;
        testSubject.TagsChanged += (_, _) => tagsChangedRaised = true;

        focusOnNewCodeService.Changed += Raise.Event<EventHandler<NewCodeStatusChangedEventArgs>>(focusOnNewCodeService, new NewCodeStatusChangedEventArgs(new FocusOnNewCodeStatus(true)));

        tagsChangedRaised.Should().BeTrue();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFocusOnNewCode()
    {
        var focusOnNewCodeService = Substitute.For<IFocusOnNewCodeService>();
        var testSubject = CreateTestSubject(focusOnNewCodeService: focusOnNewCodeService);

        testSubject.Dispose();

        focusOnNewCodeService.Received().Changed -= Arg.Any<EventHandler<NewCodeStatusChangedEventArgs>>();
    }

    private static ErrorTagger CreateTestSubject(ITagAggregator<IIssueLocationTag> aggregator = null, ITextSnapshot snapshot = null, IFocusOnNewCodeService focusOnNewCodeService = null)
    {
        var textBuffer = snapshot == null ? Substitute.For<ITextSnapshot>().TextBuffer : snapshot.TextBuffer;
        var tooltipProvider = Substitute.For<IErrorTagTooltipProvider>();
        return new ErrorTagger(aggregator ?? Substitute.For<ITagAggregator<IIssueLocationTag>>(), textBuffer, tooltipProvider, focusOnNewCodeService ?? Substitute.For<IFocusOnNewCodeService>());
    }

    private static IMappingTagSpan<IIssueLocationTag> CreateTagSpanWithPrimaryLocation(
        ITextSnapshot snapshot,
        Span span,
        string message = "",
        string ruleKey = "",
        bool isResolved = false,
        bool isOnNewCode = false,
        __VSERRORCATEGORY vsSeverity = __VSERRORCATEGORY.EC_WARNING)
    {
        var viz = CreateIssueViz(snapshot, span, message, ruleKey, isResolved, isOnNewCode, vsSeverity);
        var tag = CreateIssueLocationTag(viz);
        return CreateMappingTagSpan(snapshot, tag, span);
    }

    private static IMappingTagSpan<IIssueLocationTag> CreateTagSpanWithSecondaryLocation(ITextSnapshot snapshot, Span span, string errorMessage = "")
    {
        var viz = CreateLocationViz(snapshot, span, errorMessage);
        var tag = CreateIssueLocationTag(viz);
        return CreateMappingTagSpan(snapshot, tag, span);
    }
}
