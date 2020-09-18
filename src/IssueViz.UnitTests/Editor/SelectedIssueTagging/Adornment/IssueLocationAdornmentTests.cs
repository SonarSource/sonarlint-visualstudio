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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.SelectedIssueTagging.Adornment
{
    [TestClass, Ignore]
    public class IssueLocationAdornmentTests
    {
        [TestMethod]
        public void Ctor_Initialization()
        {
            var fontSize = 14d;
            var locViz = CreateLocationViz(CreateSnapshot(), new Span(0, 1), stepNumber: 99);
            var formattedLineSource = CreateFormattedLineSource(fontSize, "Times New Roman");

            // Act
            var testSubject = new IssueLocationAdornment(locViz, formattedLineSource);
            var textContent = testSubject.Child as TextBlock;

            textContent.Should().NotBeNull();

            var expectedFontSize = fontSize - 2;
            textContent.FontSize.Should().Be(expectedFontSize);
            textContent.FontFamily.ToString().Should().Be("Times New Roman");
            textContent.Text.Should().Be("99");
        }

        [TestMethod]
        public void Update_SetsExpectedProperties()
        {
            var locViz = CreateLocationViz(CreateSnapshot(), new Span(0, 1), stepNumber: 99);
            var originalLineSource = CreateFormattedLineSource(14d, "Times New Roman");
            var testSubject = new IssueLocationAdornment(locViz, originalLineSource);

            // Act
            var newFontSize = 10d;
            var newLineSource = CreateFormattedLineSource(newFontSize, "Arial");
            testSubject.Update(newLineSource);

            var textContent = testSubject.Child as TextBlock;

            var expectedFontSize = newFontSize - 2;
            textContent.FontSize.Should().Be(expectedFontSize);
            textContent.FontFamily.ToString().Should().Be("Arial");
        }

        [TestMethod]
        public void Ctor_TextIsDark_CorrectLightThemeColors()
        {
            VerifyThemeColors(Colors.Black, ThemeColors.LightTheme);
        }

        [TestMethod]
        public void Ctor_TextIsLight_CorrectDarkThemeColors()
        {
            VerifyThemeColors(Colors.White, ThemeColors.DarkTheme);
        }

        private void VerifyThemeColors(Color textColor, IThemeColors expectedTheme)
        {
            var originalLineSource = CreateFormattedLineSource(14d, "Times New Roman", textColor);
            var locViz = CreateLocationViz(CreateSnapshot(), new Span(0, 1), stepNumber: 99);
            var testSubject = new IssueLocationAdornment(locViz, originalLineSource);

            testSubject.Background.Should().Be(expectedTheme.BackgroundBrush);
            testSubject.BorderBrush.Should().Be(expectedTheme.BorderBrush);
        }
    }
}
