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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Models
{
    [TestClass]
    public class QuickFixVisualizationTests
    {
        [TestMethod]
        public void IsApplicable_HasOneEditWithChangedText_False()
        {
            var span1 = new Span(1, 2);
            var span2 = new Span(3, 4);

            var snapshot = new Mock<ITextSnapshot>();
            snapshot.Setup(x => x.Length).Returns(20);

            var edit1 = CreateEditVisualization("edit 1", new SnapshotSpan(snapshot.Object, span1));
            var edit2 = CreateEditVisualization("edit 2", new SnapshotSpan(snapshot.Object, span2));

            snapshot.Setup(x => x.GetText(span1)).Returns("edit 1");
            snapshot.Setup(x => x.GetText(span2)).Returns("this is some new text");

            var testSubject = CreateTestSubject(edit1, edit2);

            testSubject.IsApplicable(snapshot.Object).Should().BeFalse();
        }

        [TestMethod]
        public void IsApplicable_NoEditsWithChangedText_True()
        {
            var span1 = new Span(1, 2);
            var span2 = new Span(3, 4);

            var snapshot = new Mock<ITextSnapshot>();
            snapshot.Setup(x => x.Length).Returns(20);

            var edit1 = CreateEditVisualization("edit 1", new SnapshotSpan(snapshot.Object, span1));
            var edit2 = CreateEditVisualization("edit 2", new SnapshotSpan(snapshot.Object, span2));

            snapshot.Setup(x => x.GetText(span1)).Returns("edit 1");
            snapshot.Setup(x => x.GetText(span2)).Returns("edit 2");

            var testSubject = CreateTestSubject(edit1, edit2);

            testSubject.IsApplicable(snapshot.Object).Should().BeTrue();
        }

        private IQuickFixEditVisualization CreateEditVisualization(string text, SnapshotSpan snapshotSpan)
        {
            var editViz = new Mock<IQuickFixEditVisualization>();
            editViz.Setup(x => x.Span).Returns(snapshotSpan);
            editViz.Setup(x => x.OriginalText).Returns(text);

            return editViz.Object;
        }

        private IQuickFixVisualization CreateTestSubject(params IQuickFixEditVisualization[] editVisualizations) => 
            new QuickFixVisualization(Mock.Of<IQuickFix>(), editVisualizations, GetDoNothingSpanTranslator());

        private ISpanTranslator GetDoNothingSpanTranslator()
        {
            var doNothingSpanTranslator = new Mock<ISpanTranslator>();

            var originalSnapshotSpan = new SnapshotSpan();

            doNothingSpanTranslator.Setup(x => x.TranslateTo(
                    It.IsAny<SnapshotSpan>(),
                    It.IsAny<ITextSnapshot>(),
                    It.IsAny<SpanTrackingMode>()))
                .Callback((SnapshotSpan snapshotSpan, ITextSnapshot textSnapshot, SpanTrackingMode mode)
                    => originalSnapshotSpan = snapshotSpan)
                .Returns(() => originalSnapshotSpan);

            return doNothingSpanTranslator.Object;
        }
    }
}
