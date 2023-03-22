/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class RuleHelpXamlTranslatorTests
    {
        [TestMethod]
        public void TranslateHtmlToXaml_InlineContent_AddsParagraph()
        {
            var testSubject = new RuleHelpXamlTranslator();

            var htmlText = "inline Text";

            var expectedText = "<Paragraph>inline Text</Paragraph>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Should().Be(expectedText);
        }

        [TestMethod]
        public void TranslateHtmlToXaml_BlockContent_Translates()
        {
            var testSubject = new RuleHelpXamlTranslator();

            var htmlText = @"<ul><li>some list item</li></ul>";

            var expectedText = @"<List Style=""{DynamicResource UnorderedList}"">
  <ListItem>
    <Paragraph>some list item</Paragraph>
  </ListItem>
</List>".Replace("\r\n", "\n").Replace("\n", "\r\n");

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Should().Be(expectedText);
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        public void TranslateHtmlToXaml_HTagsAreHandled(int headerSize)
        {
            var testSubject = new RuleHelpXamlTranslator();

            var htmlText = $"<h{headerSize}>Text</h{headerSize}>";

            var expectedText = $"<Paragraph Style=\"{{DynamicResource Heading{headerSize}_Paragraph}}\">Text</Paragraph>";

            var result = testSubject.TranslateHtmlToXaml(htmlText);

            result.Should().Be(expectedText);
        }
    }
}
