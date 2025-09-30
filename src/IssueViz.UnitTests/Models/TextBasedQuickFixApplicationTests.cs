/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Models;

[TestClass]
public class TextBasedQuickFixApplicationTests
{
    private ITextSnapshot snapshot;
    private ITextBasedQuickFixVisualization quickFixVisualization;
    private ISpanTranslator spanTranslator;
    private ITextBuffer textBuffer;
    private ITextEdit textEdit;
    private TextBasedQuickFixApplication testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        snapshot = Substitute.For<ITextSnapshot>();
        snapshot.Length.Returns(int.MaxValue);
        quickFixVisualization = Substitute.For<ITextBasedQuickFixVisualization>();
        spanTranslator = Substitute.For<ISpanTranslator>();

        testSubject = new TextBasedQuickFixApplication(quickFixVisualization, spanTranslator);

        SetupTextBufferAndEdit();
    }

    [TestMethod]
    public void Message_ReturnsFixMessage()
    {
        const string expectedMessage = "test message";
        var quickFix = Substitute.For<ITextBasedQuickFix>();
        quickFix.Message.Returns(expectedMessage);
        quickFixVisualization.Fix.Returns(quickFix);

        testSubject.Message.Should().Be(expectedMessage);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CanBeApplied_DelegatesToVisualization(bool expectedResult)
    {
        quickFixVisualization.CanBeApplied(snapshot).Returns(expectedResult);

        testSubject.CanBeApplied(snapshot).Should().Be(expectedResult);
        quickFixVisualization.Received(1).CanBeApplied(snapshot);
    }

    [TestMethod]
    public async Task ApplyAsync_CreatesEditAndAppliesAllChanges()
    {
        var span1 = new SnapshotSpan(snapshot, new Span(1, 10));
        var span2 = new SnapshotSpan(snapshot, new Span(20, 5));
        var translatedSpan1 = new SnapshotSpan(snapshot, new Span(2, 10));
        var translatedSpan2 = new SnapshotSpan(snapshot, new Span(25, 5));
        SetupEditVisualizations(
            (span1, "new text 1", translatedSpan1),
            (span2, "new text 2", translatedSpan2));

        await testSubject.ApplyAsync(snapshot, CancellationToken.None);

        textBuffer.Received(1).CreateEdit();
        spanTranslator.Received(1).TranslateTo(span1, snapshot, SpanTrackingMode.EdgeExclusive);
        spanTranslator.Received(1).TranslateTo(span2, snapshot, SpanTrackingMode.EdgeExclusive);
        textEdit.Received(1).Replace(translatedSpan1.Span, "new text 1");
        textEdit.Received(1).Replace(translatedSpan2.Span, "new text 2");
        textEdit.Received(1).Apply();
    }

    [TestMethod]
    public async Task ApplyAsync_CancellationRequested_DoesNotApplyChanges()
    {
        var cancellationToken = new CancellationToken(true);
        var span = new SnapshotSpan(snapshot, new Span(1, 10));
        SetupEditVisualizations((span, "new text", span));

        var act = () => testSubject.ApplyAsync(snapshot, cancellationToken);
        await act.Should().ThrowAsync<OperationCanceledException>();

        textBuffer.Received(1).CreateEdit();
        textEdit.DidNotReceiveWithAnyArgs().Apply();
    }

    private void SetupTextBufferAndEdit()
    {
        textEdit = Substitute.For<ITextEdit>();
        textBuffer = Substitute.For<ITextBuffer>();
        textBuffer.CreateEdit().Returns(textEdit);
        textBuffer.CurrentSnapshot.Returns(snapshot);
        snapshot.TextBuffer.Returns(textBuffer);
    }

    private void SetupEditVisualizations(params (SnapshotSpan span, string newText, SnapshotSpan translatedSpan)[] edits)
    {
        var editVisualizations = new ITextBasedQuickFixEditVisualization[edits.Length];

        for (int i = 0; i < edits.Length; i++)
        {
            var edit = Substitute.For<ITextBasedQuickFixEditVisualization>();
            edit.Span.Returns(edits[i].span);
            edit.Edit.NewText.Returns(edits[i].newText);
            editVisualizations[i] = edit;

            spanTranslator
                .TranslateTo(edits[i].span, snapshot, SpanTrackingMode.EdgeExclusive)
                .Returns(edits[i].translatedSpan);
        }

        quickFixVisualization.EditVisualizations.Returns(editVisualizations);
    }
}
