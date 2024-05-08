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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Models
{
    [TestClass]
    public class QuickFixVisualizationTests
    {
        [TestMethod]
        public void CanBeApplied_HasOneEditWithChangedText_False()
        {
            var snapshot = CreateSnapshot();
            var spanTranslator = new Mock<ISpanTranslator>();

            var edit1 = SetupEditVisualization(
                snapshot,
                spanTranslator,
                new Span(1, 2),
                isSameText: true);

            var edit2 = SetupEditVisualization(
                snapshot,
                spanTranslator,
                new Span(10, 3),
                isSameText: false);

            var testSubject = CreateTestSubject(spanTranslator.Object, edit1, edit2);

            testSubject.CanBeApplied(snapshot.Object).Should().BeFalse();
        }

        [TestMethod]
        public void CanBeApplied_NoEditsWithChangedText_True()
        {
            var snapshot = CreateSnapshot();
            var spanTranslator = new Mock<ISpanTranslator>();

            var edit1 = SetupEditVisualization(
                snapshot, 
                spanTranslator, 
                new Span(1, 2), 
                isSameText: true);

            var edit2 = SetupEditVisualization(
                snapshot, 
                spanTranslator, 
                new Span(10, 3), 
                isSameText: true);

            var testSubject = CreateTestSubject(spanTranslator.Object, edit1, edit2);

            testSubject.CanBeApplied(snapshot.Object).Should().BeTrue();
        }

        [TestMethod]
        public void CanBeApplied_SameTextButDifferentCasing_False()
        {
            var snapshot = CreateSnapshot();
            var spanTranslator = new Mock<ISpanTranslator>();

            var edit = SetupEditVisualization(
                snapshot,
                spanTranslator,
                originalText: "test",
                updatedText: "Test");

            var testSubject = CreateTestSubject(spanTranslator.Object, edit);

            testSubject.CanBeApplied(snapshot.Object).Should().BeFalse();
        }

        private IQuickFixEditVisualization SetupEditVisualization(Mock<ITextSnapshot> snapshot,
            Mock<ISpanTranslator> spanTranslator,
            string originalText,
            string updatedText) =>
            SetupEditVisualization(snapshot, spanTranslator, new Span(1, 2), originalText, updatedText);

        private IQuickFixEditVisualization SetupEditVisualization(Mock<ITextSnapshot> snapshot,
            Mock<ISpanTranslator> spanTranslator, 
            Span originalSpan,
            bool isSameText)
        {
            var originalText = Guid.NewGuid().ToString();
            var updatedText = isSameText ? originalText : Guid.NewGuid().ToString();

            return SetupEditVisualization(snapshot, spanTranslator, originalSpan, originalText, updatedText);
        }

        private IQuickFixEditVisualization SetupEditVisualization(Mock<ITextSnapshot> snapshot,
            Mock<ISpanTranslator> spanTranslator,
            Span originalSpan,
            string originalText,
            string updatedText)
        {
            var originalSnapshotSpan = new SnapshotSpan(snapshot.Object, originalSpan);

            var editViz = new Mock<IQuickFixEditVisualization>();
            editViz.Setup(x => x.Span).Returns(originalSnapshotSpan);

            var updatedSnapshotSpan = new SnapshotSpan(snapshot.Object, new Span(originalSpan.Start + 1, originalSpan.Length));

            snapshot.Setup(x => x.GetText(originalSnapshotSpan)).Returns(originalText);
            snapshot.Setup(x => x.GetText(updatedSnapshotSpan)).Returns(updatedText);

            spanTranslator
                .Setup(x => x.TranslateTo(originalSnapshotSpan, snapshot.Object, SpanTrackingMode.EdgeExclusive))
                .Returns(updatedSnapshotSpan);

            return editViz.Object;
        }

        private static Mock<ITextSnapshot> CreateSnapshot()
        {
            var snapshot = new Mock<ITextSnapshot>();
            snapshot.Setup(x => x.Length).Returns(20);

            return snapshot;
        }

        private IQuickFixVisualization CreateTestSubject(ISpanTranslator spanTranslator, params IQuickFixEditVisualization[] editVisualizations) => 
            new QuickFixVisualization(Mock.Of<IQuickFix>(), editVisualizations, spanTranslator);
    }
}
