/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Text;
using SonarLint.VisualStudio.Core.Logging;

namespace SonarLint.VisualStudio.Core.UnitTests.Logging;

[TestClass]
public class StringBuilderLoggingExtensionsTests
{
    [TestMethod]
    [DataRow(null, "", "[] ")]
    [DataRow(null, null, "[] ")]
    [DataRow(null, "a", "[a] ")]
    [DataRow("", "a", "[a] ")]
    [DataRow("", "a {0}", "[a {0}] ")]
    [DataRow("abc", "def", "abc[def] ")]
    [DataRow("abc ", "def", "abc [def] ")]
    public void AppendProperty_AddsPlainPropertyValueToTheEnd(string original, string property, string expected) =>
        new StringBuilder(original).AppendProperty(property).ToString().Should().Be(expected);

    [DataRow("msg", "prefix msg")]
    [DataRow("msg {0}", "prefix msg {0}")]
    [DataRow("", "prefix ")]
    [TestMethod]
    public void AppendMessage_NoParameters_AppendsAsIs(string message, string expected) =>
        new StringBuilder("prefix ").AppendMessage(message, []).ToString().Should().Be(expected);

    [TestMethod]
    public void AppendMessage_WithParameters_AppendsWithFormat() =>
        new StringBuilder("prefix ").AppendMessage("msg {0}", ["param1"]).ToString().Should().Be("prefix msg param1");

    [TestMethod]
    public void AppendMessage_WithParametersMismatch_ThrowsFormatException()
    {
        var act = () => new StringBuilder("prefix ").AppendMessage("msg {0} {1}", ["param1"]);

        act.Should().Throw<FormatException>();
    }
}
