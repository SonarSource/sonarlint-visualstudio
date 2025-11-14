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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor;

[TestClass]
public class TextViewEditorTests
{
    private readonly List<FixSuggestionChange> oneChange = [CreateChangesDto(1, 1, "var a=1;")];
    private readonly List<FixSuggestionChange> twoChanges = [CreateChangesDto(1, 1, "var a=1;"), CreateChangesDto(2, 2, "var b=0;")];
    private IIssueSpanCalculator issueSpanCalculator;
    private ILogger logger;
    private TextViewEditor testSubject;
    private ITextBuffer textBuffer;
    private ITextBufferFactoryService textBufferFactoryService;
    private ITextEdit textEdit;
    private ITextView textView;

    [TestInitialize]
    public void TestInitialize()
    {
        issueSpanCalculator = Substitute.For<IIssueSpanCalculator>();
        textBufferFactoryService = Substitute.For<ITextBufferFactoryService>();
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
        testSubject = new TextViewEditor(issueSpanCalculator, logger, textBufferFactoryService);

        MockCalculateSpan();
        MockIssueSpanCalculatorIsSameHash(true);
        MockTextBuffer();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<TextViewEditor, ITextViewEditor>(
            MefTestHelpers.CreateExport<IIssueSpanCalculator>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<ITextBufferFactoryService>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<TextViewEditor>();

    [TestMethod]
    public void Ctor_SetsContext() => logger.Received(1).ForContext(nameof(TextViewEditor));

    [TestMethod]
    public void ApplyChanges_OneChange_AppliesChange()
    {
        var suggestedChange = oneChange[0];

        var applied = testSubject.ApplyChanges(textBuffer, oneChange, false);

        applied.Should().BeTrue();
        Received.InOrder(() =>
        {
            textBuffer.CreateEdit();
            issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), suggestedChange.BeforeStartLine, suggestedChange.BeforeEndLine);
            textEdit.Replace(Arg.Any<Span>(), Arg.Any<string>());
            textEdit.Apply();
            textEdit.Dispose();
        });
    }

    [TestMethod]
    public void ApplyChanges_OneChange_OriginalTextChanged_DoesNotApplyChangeWhenAbortIsTrue()
    {
        MockIssueSpanCalculatorIsSameHash(false);

        var result = testSubject.ApplyChanges(textBuffer, oneChange, true);

        result.Should().BeFalse();
        textEdit.DidNotReceiveWithAnyArgs().Replace(default, default);
        textEdit.DidNotReceiveWithAnyArgs().Apply();
        textEdit.Received(1).Dispose();
    }

    [TestMethod]
    public void ApplyChanges_OneChange_OriginalTextChanged_ApplyChangesWhenAbortIsFalse()
    {
        MockIssueSpanCalculatorIsSameHash(false);

        var result = testSubject.ApplyChanges(textBuffer, oneChange, false);

        result.Should().BeTrue();
        textEdit.ReceivedWithAnyArgs(1).Replace(default, default);
        textEdit.Received(1).Apply();
        textEdit.Received(1).Dispose();
    }

    [TestMethod]
    public void ApplyChanges_TwoChanges_CallsTextEditApplyOnce()
    {
        testSubject.ApplyChanges(textBuffer, twoChanges, false);

        issueSpanCalculator.Received(2).CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>());
        textEdit.Received(2).Replace(Arg.Any<Span>(), Arg.Any<string>());
        textEdit.Received(1).Apply();
    }

    ///// <summary>
    ///// The changes are applied from bottom to top to avoid changing the line numbers
    ///// of the changes that are below the current change.
    ///// This is important when the change is more lines than the original line range.
    ///// </summary>
    [TestMethod]
    public void ApplyChanges_WhenMoreThanOneFixes_ApplyThemFromBottomToTop()
    {
        testSubject.ApplyChanges(textBuffer, twoChanges, false);

        Received.InOrder(() =>
        {
            issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), 2, 2);
            issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), 1, 1);
        });
    }

    [TestMethod]
    public void ApplyChanges_TwoChanges_OriginalTextChangedForOneOfTheChanges_DoesNotApplyChangeWhenAbortIsTrue()
    {
        issueSpanCalculator.IsSameHash(Arg.Any<SnapshotSpan>(), Arg.Any<string>()).Returns(true, false);

        var result = testSubject.ApplyChanges(textBuffer, twoChanges, true);

        result.Should().BeFalse();
        textEdit.Received(1).Replace(Arg.Any<Span>(), Arg.Any<string>());
        textEdit.DidNotReceiveWithAnyArgs().Apply();
        textEdit.Received(1).Dispose();
    }

    [TestMethod]
    public void ApplyChanges_WhenApplyingChangeAndExceptionIsThrown_ShouldDisposeEdit()
    {
        FailWhenApplyingEdit();

        var act = () => testSubject.ApplyChanges(textBuffer, oneChange, false);

        act.Should().Throw<Exception>();
        textEdit.DidNotReceiveWithAnyArgs().Replace(default, default);
        textEdit.Received().Dispose();
    }

    [TestMethod]
    public void FocusLine_MovesToCaretAndEnsuresVisible()
    {
        var line = MockLineView(2);

        testSubject.FocusLine(textView, 2);

        Received.InOrder(() =>
        {
            textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(2);
            textView.Caret.MoveTo(line.Start);
            textView.ViewScroller.EnsureSpanVisible(line.Extent);
        });
    }

    [TestMethod]
    public void FocusLine_WhenException_DoesNotThrowAndLogs()
    {
        var reason = "line does not exist";
        textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(2).Throws(new Exception(reason));

        var act = () => testSubject.FocusLine(textView, 2);

        act.Should().NotThrow();
        logger.Received(1).LogVerbose(Resources.FocusLineFailed, 2, reason);
    }

    [TestMethod]
    public void CreateTextBuffer_ShouldReturnTextBuffer()
    {
        var text = "some text";
        var contentType = Substitute.For<IContentType>();
        var expectedTextBuffer = Substitute.For<ITextBuffer>();
        textBufferFactoryService.CreateTextBuffer(text, contentType).Returns(expectedTextBuffer);

        var result = testSubject.CreateTextBuffer(text, contentType);

        textBufferFactoryService.Received(1).CreateTextBuffer(text, contentType);
        result.Should().Be(expectedTextBuffer);
    }

    private void MockIssueSpanCalculatorIsSameHash(bool isSameHash) => issueSpanCalculator.IsSameHash(Arg.Any<SnapshotSpan>(), Arg.Any<string>()).Returns(isSameHash);

    private void MockCalculateSpan() => issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>()).Returns(_ => CreateMockedSnapshotSpan("some text"));

    private static FixSuggestionChange CreateChangesDto(
        int startLine,
        int endLine,
        string before,
        string after = "") =>
        new(startLine, endLine, before, after);

    private void MockTextBuffer()
    {
        textBuffer = Substitute.For<ITextBuffer>();
        textEdit = Substitute.For<ITextEdit>();
        textBuffer.CreateEdit().Returns(textEdit);
        textView = Substitute.For<ITextView>();
    }

    private ITextSnapshotLine MockLineView(int lineNumber)
    {
        var line = Substitute.For<ITextSnapshotLine>();
        textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber).Returns(line);
        return line;
    }

    private static SnapshotSpan CreateMockedSnapshotSpan(string text)
    {
        var mockTextSnapshot = Substitute.For<ITextSnapshot>();
        mockTextSnapshot.Length.Returns(text.Length + 9999);

        return new SnapshotSpan(mockTextSnapshot, new Span(0, text.Length));
    }

    private void FailWhenApplyingEdit() =>
        issueSpanCalculator.CalculateSpan(Arg.Any<ITextSnapshot>(), Arg.Any<int>(), Arg.Any<int>())
            .Throws(new Exception());
}
