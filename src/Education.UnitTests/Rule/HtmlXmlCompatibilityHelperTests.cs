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

using SonarLint.VisualStudio.Education.Rule;

namespace SonarLint.VisualStudio.Education.UnitTests.Rule
{
    [TestClass]
    public class HtmlXmlCompatibilityHelperTests
    {
        [TestMethod]
        public void EnsureHtmlIsXml_ClosesBr()
        {
            HtmlXmlCompatibilityHelper.EnsureHtmlIsXml("Lalala<br> < br >Hi").Should().BeEquivalentTo("Lalala<br/> < br />Hi");
        }

        [TestMethod]
        public void EnsureHtmlIsXml_ShouldNotMatchBrInText()
        {
            HtmlXmlCompatibilityHelper.EnsureHtmlIsXml("<p>some text with br in it <em>SASL</em>")
                .Should()
                .BeEquivalentTo("<p>some text with br in it <em>SASL</em>");
        }

        [TestMethod]
        public void EnsureHtmlIsXml_ClosesCol()
        {
            HtmlXmlCompatibilityHelper.EnsureHtmlIsXml("Lalala<col span=\"2\" style=\"background-color:red\">Hi <  col > hello")
                .Should()
                .BeEquivalentTo("Lalala<col span=\"2\" style=\"background-color:red\"/>Hi <  col /> hello");
        }

        [TestMethod]
        public void EnsureHtmlIsXml_ShouldNotMatchColInText()
        {
            HtmlXmlCompatibilityHelper.EnsureHtmlIsXml("<p>Lightweight Directory Access Protocol (LDAP) servers provide two main authentication methods: the <em>SASL</em>")
                .Should()
                .BeEquivalentTo("<p>Lightweight Directory Access Protocol (LDAP) servers provide two main authentication methods: the <em>SASL</em>");
        }

        [TestMethod]
        public void EnsureHtmlIsXml_Null_ReturnsNull()
        {
            HtmlXmlCompatibilityHelper.EnsureHtmlIsXml(null).Should().BeNull();
        }
    }
}
