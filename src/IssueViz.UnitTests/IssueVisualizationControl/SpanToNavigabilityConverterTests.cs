/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl
{
    [TestClass]
    public class SpanToNavigabilityConverterTests
    {
        [TestMethod]
        public void ConvertBack_NotImplementedException()
        {
            Action act = () => new SpanToNavigabilityConverter().ConvertBack(null, null, null, null);

            act.Should().Throw<NotImplementedException>("ConvertBack is not required");
        }

        [TestMethod]
        public void Convert_NullSpan_True()
        {
            var testSubject = new SpanToNavigabilityConverter();
            var result = testSubject.Convert(null, null, null, null);

            result.Should().Be(true);
        }

        [TestMethod]
        public void Convert_EmptySpan_False()
        {
            var testSubject = new SpanToNavigabilityConverter();
            var result = testSubject.Convert(new SnapshotSpan(), null, null, null);

            result.Should().Be(false);
        }

        [TestMethod]
        public void Convert_NonEmptySpan_True()
        {
            var textSnapshotMock = new Mock<ITextSnapshot>();
            textSnapshotMock.SetupGet(x => x.Length).Returns(20);
            var span = new SnapshotSpan(textSnapshotMock.Object, new Span(0, 10));

            var testSubject = new SpanToNavigabilityConverter();
            var result = testSubject.Convert(span, null, null, null);

            result.Should().Be(true);
        }
    }
}
