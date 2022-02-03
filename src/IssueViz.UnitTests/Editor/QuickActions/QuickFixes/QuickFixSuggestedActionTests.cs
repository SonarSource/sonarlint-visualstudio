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

using System.Collections.Generic;
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
            var editVisualization = new Mock<IQuickFixEditVisualization>();
            editVisualization.Setup(e => e.Edit.Text).Returns("edit");
            
            var quickFixViz = new Mock<IQuickFixVisualization>();            
            quickFixViz.Setup(x => x.Fix.Message).Returns("some fix");
            quickFixViz.Setup(x => x.EditVisualizations).Returns(new List<IQuickFixEditVisualization> { editVisualization.Object });


            var textEdit = new Mock<ITextEdit>();

            var textBuffer = new Mock<ITextBuffer>();
            textBuffer.Setup(tb => tb.CreateEdit()).Returns(textEdit.Object);

            var testSubject = new QuickFixSuggestedAction(quickFixViz.Object, textBuffer.Object);
            testSubject.Invoke(new System.Threading.CancellationToken());

            testSubject.DisplayText.Should().Be("some fix");
            textBuffer.Verify(tb => tb.CreateEdit(), Times.Once(), "CreateEdit should be called once");
            textEdit.Verify(tb => tb.Replace(It.IsAny<Span>(), It.IsAny<string>()), Times.Exactly(1), "Replace should be called one time");
            textEdit.Verify(tb => tb.Apply(), Times.Once(), "Apply should be called once");

        }

        [TestMethod]
        public void Invoke_AppliesFixWithMultipleEdits()
        {
            var editVisualization1 = new Mock<IQuickFixEditVisualization>();
            editVisualization1.Setup(e => e.Edit.Text).Returns("edit1");
            var editVisualization2 = new Mock<IQuickFixEditVisualization>();
            editVisualization2.Setup(e => e.Edit.Text).Returns("edit2");
            var editVisualization3 = new Mock<IQuickFixEditVisualization>();
            editVisualization3.Setup(e => e.Edit.Text).Returns("edit3");

            var quickFixViz = new Mock<IQuickFixVisualization>();
            quickFixViz.Setup(x => x.Fix.Message).Returns("some fix");
            quickFixViz.Setup(x => x.EditVisualizations).Returns(new List<IQuickFixEditVisualization> { editVisualization1.Object, editVisualization2.Object, editVisualization3.Object, });


            var textEdit = new Mock<ITextEdit>();

            var textBuffer = new Mock<ITextBuffer>();
            textBuffer.Setup(tb => tb.CreateEdit()).Returns(textEdit.Object);

            var testSubject = new QuickFixSuggestedAction(quickFixViz.Object, textBuffer.Object);
            testSubject.Invoke(new System.Threading.CancellationToken());

            testSubject.DisplayText.Should().Be("some fix");
            textBuffer.Verify(tb => tb.CreateEdit(), Times.Once(), "CreateEdit should be called once");
            textEdit.Verify(tb => tb.Replace(It.IsAny<Span>(), It.IsAny<string>()), Times.Exactly(3), "Replace should be called three time");
            textEdit.Verify(tb => tb.Apply(), Times.Once(), "Apply should be called once");

        }
    }
}
