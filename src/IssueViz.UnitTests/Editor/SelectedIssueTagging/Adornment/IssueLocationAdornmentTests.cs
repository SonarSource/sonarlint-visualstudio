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

using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.SelectedIssueTagging.Adornment
{
    [TestClass]
    public class IssueLocationAdornmentTests
    {
        [TestMethod]
        public void Ctor_Initialization()
        {
            var locViz = CreateLocationViz(CreateSnapshot(), new Span(0, 1), stepNumber: 99);
            var selectedTag = CreateSelectedLocationTag(locViz);
            var formattedLineSource = CreateFormattedLineSource(14d, "Times New Roman");

            // Act
            var testSubject = new IssueLocationAdornment(selectedTag, formattedLineSource);

            var textContent = testSubject.Content as TextBlock;
            textContent.Should().NotBeNull();

            textContent.Text.Should().Be("99");
            textContent.FontSize.Should().Be(14d);
            textContent.FontFamily.ToString().Should().Be("Times New Roman");
        }

        private static IFormattedLineSource CreateFormattedLineSource(double fontSize, string fontFamily)
        {
            var textRunPropertiesMock = new Mock<TextRunProperties>();
            textRunPropertiesMock.Setup(x => x.FontRenderingEmSize).Returns(fontSize);
            textRunPropertiesMock.Setup(x => x.Typeface).Returns(new Typeface(fontFamily));

            var lineSourceMock = new Mock<IFormattedLineSource>();
            lineSourceMock.Setup(x => x.DefaultTextProperties).Returns(textRunPropertiesMock.Object);

            return lineSourceMock.Object;
        }
    }
}
