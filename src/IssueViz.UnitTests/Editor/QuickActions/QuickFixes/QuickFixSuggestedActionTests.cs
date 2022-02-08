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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
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

            var testSubject = new QuickFixSuggestedAction(quickFixViz.Object, Mock.Of<ITextBuffer>());

            testSubject.DisplayText.Should().Be("some fix");
        }

        [TestMethod]
        public void Invoke_AppliesFixWithOneEdit()
        {
            Mock<ITextSnapshot> snapShot = CreateTextSnapshot();

            var span = new Span(1, 10);
            var snapshotSpan = new SnapshotSpan(snapShot.Object, span);
            var editVisualization = new Mock<IQuickFixEditVisualization>();
            editVisualization.Setup(e => e.Edit.Text).Returns("edit");
            editVisualization.Setup(e => e.Span).Returns(snapshotSpan);

            var quickFixViz = new Mock<IQuickFixVisualization>();
            quickFixViz.Setup(x => x.EditVisualizations).Returns(new List<IQuickFixEditVisualization> { editVisualization.Object });


            var textEdit = new Mock<ITextEdit>(MockBehavior.Strict);
            var textBuffer = new Mock<ITextBuffer>(MockBehavior.Strict);

            var sequence = new MockSequence();

            textBuffer.InSequence(sequence).Setup(t => t.CreateEdit()).Returns(textEdit.Object);
            textEdit.InSequence(sequence).Setup(t => t.Replace(span, "edit")).Returns(true);
            textEdit.InSequence(sequence).Setup(t => t.Apply()).Returns(Mock.Of<ITextSnapshot>());

            var testSubject = new QuickFixSuggestedAction(quickFixViz.Object, textBuffer.Object);
            testSubject.Invoke(CancellationToken.None);


            textBuffer.Verify(tb => tb.CreateEdit(), Times.Once(), "CreateEdit should be called once");
            textEdit.Verify(tb => tb.Replace(It.IsAny<Span>(), It.IsAny<string>()), Times.Exactly(1), "Replace should be called one time");
            textEdit.Verify(tb => tb.Apply(), Times.Once(), "Apply should be called once");

        }

        [TestMethod]
        public void Invoke_AppliesFixWithMultipleEdits()
        {
            Mock<ITextSnapshot> snapShot = CreateTextSnapshot();

            var span1 = new Span(1, 10);
            var snapshotSpan1 = new SnapshotSpan(snapShot.Object, span1);
            var editVisualization1 = new Mock<IQuickFixEditVisualization>();
            editVisualization1.Setup(e => e.Edit.Text).Returns("edit1");
            editVisualization1.Setup(e => e.Span).Returns(snapshotSpan1);


            var span2 = new Span(2, 20);
            var snapshotSpan2 = new SnapshotSpan(snapShot.Object, span2);
            var editVisualization2 = new Mock<IQuickFixEditVisualization>();
            editVisualization2.Setup(e => e.Edit.Text).Returns("edit2");
            editVisualization2.Setup(e => e.Span).Returns(snapshotSpan2);

            var span3 = new Span(3, 30);
            var snapshotSpan3 = new SnapshotSpan(snapShot.Object, span3);
            var editVisualization3 = new Mock<IQuickFixEditVisualization>();
            editVisualization3.Setup(e => e.Edit.Text).Returns("edit3");
            editVisualization3.Setup(e => e.Span).Returns(snapshotSpan3);

            var quickFixViz = new Mock<IQuickFixVisualization>();
            quickFixViz.Setup(x => x.EditVisualizations).Returns(new List<IQuickFixEditVisualization> { editVisualization1.Object, editVisualization2.Object, editVisualization3.Object, });


            var textEdit = new Mock<ITextEdit>(MockBehavior.Strict);
            var textBuffer = new Mock<ITextBuffer>(MockBehavior.Strict);

            var sequence = new MockSequence();

            textBuffer.InSequence(sequence).Setup(t => t.CreateEdit()).Returns(textEdit.Object);
            textEdit.InSequence(sequence).Setup(t => t.Replace(span1, "edit1")).Returns(true);
            textEdit.InSequence(sequence).Setup(t => t.Replace(span2, "edit2")).Returns(true);
            textEdit.InSequence(sequence).Setup(t => t.Replace(span3, "edit3")).Returns(true);
            textEdit.InSequence(sequence).Setup(t => t.Apply()).Returns(Mock.Of<ITextSnapshot>());

            var testSubject = new QuickFixSuggestedAction(quickFixViz.Object, textBuffer.Object);
            testSubject.Invoke(CancellationToken.None);

            textBuffer.Verify(tb => tb.CreateEdit(), Times.Once(), "CreateEdit should be called once");
            textEdit.Verify(tb => tb.Replace(It.IsAny<Span>(), It.IsAny<string>()), Times.Exactly(3), "Replace should be called three time");
            textEdit.Verify(tb => tb.Apply(), Times.Once(), "Apply should be called once");

        }

        [TestMethod]
        public void CancellationToken_Cancels()
        {
            var quickFixViz = new Mock<IQuickFixVisualization>();
            var textBuffer = new Mock<ITextBuffer>();
            var textEdit = new Mock<ITextEdit>();
            textBuffer.Setup(tb => tb.CreateEdit()).Returns(textEdit.Object);

            var testSubject = new QuickFixSuggestedAction(quickFixViz.Object, textBuffer.Object);
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            quickFixViz.SetupGet(x => x.EditVisualizations).Returns(Array.Empty<IQuickFixEditVisualization>());

            tokenSource.Cancel();

            testSubject.Invoke(tokenSource.Token);      

            quickFixViz.VerifyNoOtherCalls();
            textBuffer.VerifyNoOtherCalls();
            textEdit.VerifyNoOtherCalls();
        }

        private static Mock<ITextSnapshot> CreateTextSnapshot()
        {
            var snapShot = new Mock<ITextSnapshot>();
            snapShot.SetupGet(ss => ss.Length).Returns(int.MaxValue);
            return snapShot;
        }
    }
}
