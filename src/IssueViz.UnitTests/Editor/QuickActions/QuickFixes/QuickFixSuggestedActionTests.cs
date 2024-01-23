/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions.QuickFixes
{
    [TestClass]
    public class QuickFixSuggestedActionTests
    {
        [TestMethod]
        public void DisplayName_ReturnsFixMessage()
        {
            var quickFixViz = new Mock<IQuickFixVisualization>();
            quickFixViz.Setup(x => x.Fix.Message).Returns("some fix");

            var testSubject = CreateTestSubject(quickFixViz.Object);

            testSubject.DisplayText.Should().Be(QuickFixSuggestedAction.sonarLintPrefix + "some fix");
        }

        [TestMethod]
        public void Invoke_QuickFixCanBeApplied_TelemetryIsSent()
        {
            var snapshot = CreateTextSnapshot();
            var quickFixViz = CreateQuickFixViz(snapshot.Object);
            var textBuffer = CreateTextBuffer(snapshot.Object);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.RuleId).Returns("some rule");

            var telemetryManager = new Mock<IQuickFixesTelemetryManager>();

            var testSubject = CreateTestSubject(quickFixViz.Object,
                textBuffer.Object,
                issueViz: issueViz.Object,
                telemetryManager: telemetryManager.Object);

            testSubject.Invoke(CancellationToken.None);

            telemetryManager.Verify(x=> x.QuickFixApplied("some rule"), Times.Once);
            telemetryManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Invoke_QuickFixCannotBeApplied_TelemetryNotSent()
        {
            var snapshot = CreateTextSnapshot();
            var quickFixViz = CreateNonApplicableQuickFixViz(snapshot.Object);
            var textBuffer = CreateTextBuffer(snapshot.Object);

            var telemetryManager = new Mock<IQuickFixesTelemetryManager>();

            var testSubject = CreateTestSubject(quickFixViz.Object, textBuffer.Object, telemetryManager: telemetryManager.Object);
            testSubject.Invoke(CancellationToken.None);

            telemetryManager.Verify(x => x.QuickFixApplied(It.IsAny<string>()), Times.Never);
            telemetryManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Invoke_AppliesFixWithOneEdit()
        {
            var snapshot = CreateTextSnapshot();

            var span = new Span(1, 10);
            var editVisualization = CreateEditVisualization(new SnapshotSpan(snapshot.Object, span));
            var quickFixViz = CreateQuickFixViz(snapshot.Object, editVisualization.Object);
            var textEdit = new Mock<ITextEdit>(MockBehavior.Strict);
            var textBuffer = CreateTextBuffer(snapshot.Object, textEdit.Object);

            var sequence = new MockSequence();

            textBuffer.InSequence(sequence).Setup(t => t.CreateEdit()).Returns(textEdit.Object);
            textEdit.InSequence(sequence).Setup(t => t.Replace(span, "edit")).Returns(true);
            textEdit.InSequence(sequence).Setup(t => t.Apply()).Returns(Mock.Of<ITextSnapshot>());

            var testSubject = CreateTestSubject(quickFixViz.Object, textBuffer.Object);
            testSubject.Invoke(CancellationToken.None);

            textBuffer.Verify(tb => tb.CreateEdit(), Times.Once(), "CreateEdit should be called once");
            textEdit.Verify(tb => tb.Replace(It.IsAny<Span>(), It.IsAny<string>()), Times.Exactly(1), "Replace should be called one time");
            textEdit.Verify(tb => tb.Apply(), Times.Once(), "Apply should be called once");
        }

        [TestMethod]
        public void Invoke_AppliesFixWithMultipleEdits()
        {
            var snapshot = CreateTextSnapshot();

            var span1 = new Span(1, 10);
            var editVisualization1 = CreateEditVisualization(new SnapshotSpan(snapshot.Object, span1), "edit1");

            var span2 = new Span(2, 20);
            var editVisualization2 = CreateEditVisualization(new SnapshotSpan(snapshot.Object, span2), "edit2");

            var span3 = new Span(3, 30);
            var editVisualization3 = CreateEditVisualization(new SnapshotSpan(snapshot.Object, span3), "edit3");

            var quickFixViz = CreateQuickFixViz(snapshot.Object,
                editVisualization1.Object,
                editVisualization2.Object,
                editVisualization3.Object);

            var textEdit = new Mock<ITextEdit>(MockBehavior.Strict);
            var textBuffer = CreateTextBuffer(snapshot.Object, textEdit.Object);

            var sequence = new MockSequence();

            textBuffer.InSequence(sequence).Setup(t => t.CreateEdit()).Returns(textEdit.Object);
            textEdit.InSequence(sequence).Setup(t => t.Replace(span1, "edit1")).Returns(true);
            textEdit.InSequence(sequence).Setup(t => t.Replace(span2, "edit2")).Returns(true);
            textEdit.InSequence(sequence).Setup(t => t.Replace(span3, "edit3")).Returns(true);
            textEdit.InSequence(sequence).Setup(t => t.Apply()).Returns(Mock.Of<ITextSnapshot>());

            var testSubject = CreateTestSubject(quickFixViz.Object, textBuffer.Object);
            testSubject.Invoke(CancellationToken.None);

            textBuffer.Verify(tb => tb.CreateEdit(), Times.Once(), "CreateEdit should be called once");
            textEdit.Verify(tb => tb.Replace(It.IsAny<Span>(), It.IsAny<string>()), Times.Exactly(3), "Replace should be called three time");
            textEdit.Verify(tb => tb.Apply(), Times.Once(), "Apply should be called once");
        }

        [TestMethod]
        public void Invoke_CancellationTokenIsCancelled_NoChanges()
        {
            var quickFixViz = new Mock<IQuickFixVisualization>();
            var textBuffer = new Mock<ITextBuffer>();
            var issueViz = new Mock<IAnalysisIssueVisualization>();

            var testSubject = CreateTestSubject(quickFixViz.Object, textBuffer.Object, issueViz: issueViz.Object);

            testSubject.Invoke(new CancellationToken(canceled: true));

            quickFixViz.VerifyNoOtherCalls();
            textBuffer.VerifyNoOtherCalls();
            issueViz.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Invoke_QuickFixIsNotApplicable_NoChanges()
        {
            var snapshot = Mock.Of<ITextSnapshot>();
            var quickFixViz = CreateNonApplicableQuickFixViz(snapshot);
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            var textBuffer = CreateTextBuffer(snapshot);

            var testSubject = CreateTestSubject(quickFixViz.Object, textBuffer.Object, issueViz: issueViz.Object);

            testSubject.Invoke(CancellationToken.None);

            quickFixViz.Verify(x => x.CanBeApplied(snapshot), Times.Once);
            quickFixViz.VerifyNoOtherCalls();

            textBuffer.VerifyGet(x => x.CurrentSnapshot, Times.Once);
            textBuffer.VerifyNoOtherCalls();

            issueViz.VerifyGet(x => x.RuleId, Times.Once);
            issueViz.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Invoke_SpansAreTranslatedCorrectly()
        {
            var snapshot = CreateTextSnapshot();
            var span1 = new Span(1, 10);
            var span2 = new Span(20, 30);

            var originalSnapshotSpan = new SnapshotSpan(snapshot.Object, span1);
            var modifiedSnapshotSpan = new SnapshotSpan(snapshot.Object, span2);

            var spanTranslator = new Mock<ISpanTranslator>();
            spanTranslator
                .Setup(x => x.TranslateTo(originalSnapshotSpan, snapshot.Object, SpanTrackingMode.EdgeExclusive))
                .Returns(modifiedSnapshotSpan);

            var editVisualization = CreateEditVisualization(originalSnapshotSpan, text: "some edit");
            var quickFixViz = CreateQuickFixViz(snapshot.Object, editVisualization.Object);

            var textEdit = new Mock<ITextEdit>();
            var textBuffer = CreateTextBuffer(snapshot.Object, textEdit.Object);

            var testSubject = CreateTestSubject(quickFixViz.Object, textBuffer.Object, spanTranslator.Object);
            testSubject.Invoke(CancellationToken.None);

            textEdit.Verify(t => t.Replace(span2, "some edit"), Times.Once);
        }

        [TestMethod]
        public void Invoke_SpanInvalidatedCorrectly()
        {
            var snapshot = CreateTextSnapshot();
            var editVisualization = CreateEditVisualization(new SnapshotSpan(snapshot.Object, new Span(1, 10)));
            var quickFixViz = CreateQuickFixViz(snapshot.Object, editVisualization.Object);
            var textBuffer = CreateTextBuffer(snapshot.Object);

            var issueViz = new Mock<IAnalysisIssueVisualization>();

            var testSubject = CreateTestSubject(quickFixViz.Object, textBuffer.Object, issueViz: issueViz.Object);
            testSubject.Invoke(CancellationToken.None);

            issueViz.VerifySet(iv => iv.Span = new SnapshotSpan());
        }

        private static Mock<ITextSnapshot> CreateTextSnapshot()
        {
            var snapShot = new Mock<ITextSnapshot>();
            snapShot.SetupGet(ss => ss.Length).Returns(int.MaxValue);

            return snapShot;
        }

        private static QuickFixSuggestedAction CreateTestSubject(IQuickFixVisualization quickFixViz,
            ITextBuffer textBuffer = null,
            ISpanTranslator spanTranslator = null,
            IAnalysisIssueVisualization issueViz = null,
            IQuickFixesTelemetryManager telemetryManager = null)
        {
            if (spanTranslator == null)
            {
                SnapshotSpan originalSnapshotSpan = new();

                var doNothingSpanTranslator = new Mock<ISpanTranslator>();
                doNothingSpanTranslator.Setup(x => x.TranslateTo(
                        It.IsAny<SnapshotSpan>(),
                        It.IsAny<ITextSnapshot>(),
                        It.IsAny<SpanTrackingMode>()))
                    .Callback((SnapshotSpan snapshotSpan, ITextSnapshot textSnapshot, SpanTrackingMode mode)
                        => originalSnapshotSpan = snapshotSpan)
                    .Returns(() => originalSnapshotSpan);

                spanTranslator = doNothingSpanTranslator.Object;
            }

            issueViz ??= Mock.Of<IAnalysisIssueVisualization>();
            telemetryManager ??= Mock.Of<IQuickFixesTelemetryManager>();

            return new QuickFixSuggestedAction(quickFixViz,
                textBuffer,
                issueViz,
                telemetryManager,
                Mock.Of<ILogger>(),
                spanTranslator);
        }

        private static Mock<IQuickFixVisualization> CreateQuickFixViz(ITextSnapshot snapShot, params IQuickFixEditVisualization[] editVisualizations) =>
            CreateQuickFixViz(snapShot, true, editVisualizations);

        private static Mock<IQuickFixVisualization> CreateNonApplicableQuickFixViz(ITextSnapshot snapShot) =>
            CreateQuickFixViz(snapShot, false);

        private static Mock<IQuickFixVisualization> CreateQuickFixViz(ITextSnapshot snapShot,
            bool canBeApplied = true,
            params IQuickFixEditVisualization[] editVisualizations)
        {
            var quickFixViz = new Mock<IQuickFixVisualization>();

            quickFixViz
                .Setup(x => x.EditVisualizations)
                .Returns(editVisualizations);

            quickFixViz
                .Setup(x => x.CanBeApplied(snapShot))
                .Returns(canBeApplied);

            return quickFixViz;
        }

        private static Mock<IQuickFixEditVisualization> CreateEditVisualization(SnapshotSpan snapshotSpan, string text = "edit")
        {
            var editVisualization = new Mock<IQuickFixEditVisualization>();

            editVisualization.Setup(e => e.Edit.NewText).Returns(text);
            editVisualization.Setup(e => e.Span).Returns(snapshotSpan);

            return editVisualization;
        }

        private static Mock<ITextBuffer> CreateTextBuffer(ITextSnapshot snapShot, ITextEdit textEdit = null)
        {
            textEdit ??= Mock.Of<ITextEdit>();

            var textBuffer = new Mock<ITextBuffer>(MockBehavior.Strict);
            textBuffer.Setup(x => x.CurrentSnapshot).Returns(snapShot);
            textBuffer.Setup(t => t.CreateEdit()).Returns(textEdit);

            return textBuffer;
        }
    }
}
