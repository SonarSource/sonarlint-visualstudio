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


using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class RuleHelpXamlTranslatorTests
    {
        [TestMethod]
        public void TranslateHtmlToXaml_DoesNotSupportInline_Inline_AddsParagraph()
        {
            var testSubject = new RuleHelpXamlTranslator();

            var htmlText = @"<code>inline Text</code>";

            var expectedText =
@"<Paragraph>
  <Span Style=""{DynamicResource Code_Span}"">inline Text</Span>
</Paragraph>";

            var result = testSubject.TranslateHtmlToXaml(htmlText, false);

            result.Should().Be(expectedText);
        }

        [TestMethod]
        public void TranslateHtmlToXaml_SupportInline_Inline_DoesNotAddParagraph()
        {
            var testSubject = new RuleHelpXamlTranslator();

            var htmlText = @"<code>inline Text</code>";

            var expectedText = @"<Span Style=""{DynamicResource Code_Span}"">inline Text</Span>";

            var result = testSubject.TranslateHtmlToXaml(htmlText, true);

            result.Should().Be(expectedText);
        }
        [TestMethod]
        public void TranslateHtmlToXaml_DoesNotSupportInline_Block_Translates()
        {
            var testSubject = new RuleHelpXamlTranslator();

            var htmlText = @"<ul><li>some list item</li></ul>";

            var expectedText = @"<List Style=""{DynamicResource UnorderedList}"">
  <ListItem>
    <Paragraph>some list item</Paragraph>
  </ListItem>
</List>";

            var result = testSubject.TranslateHtmlToXaml(htmlText, false);

            result.Should().Be(expectedText);
        }


        [TestMethod]
        public void TranslateHtmlToXaml_SupportInline_Block_Throws()
        {
            var testSubject = new RuleHelpXamlTranslator();

            var htmlText = @" <ul>
        <li>some list item</li>
            </ul>";

            Action act = () => testSubject.TranslateHtmlToXaml(htmlText, true);

            act.Should().Throw<InvalidOperationException>().WithMessage("Invalid state: can't find an element that supports blocks");
        }

    }
}
