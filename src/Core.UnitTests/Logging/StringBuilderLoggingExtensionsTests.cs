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

using System.Text;
using SonarLint.VisualStudio.Core.Logging;

namespace SonarLint.VisualStudio.Core.UnitTests.Logging;

[TestClass]
public class StringBuilderLoggingExtensionsTests
{
    [DataTestMethod]
    [DataRow(null, "", "[] ")]
    [DataRow(null, null, "[] ")]
    [DataRow(null, "a", "[a] ")]
    [DataRow("", "a", "[a] ")]
    [DataRow("", "a {0}", "[a {0}] ")]
    [DataRow("abc", "def", "abc[def] ")]
    [DataRow("abc ", "def", "abc [def] ")]
    public void AppendProperty_AddsPlainPropertyValueToTheEnd(string original, string property, string expected) =>
        new StringBuilder(original).AppendProperty(property).ToString().Should().Be(expected);

    [DataTestMethod]
    [DataRow(null, "", "[] ")]
    [DataRow(null, "a", "[a] ")]
    [DataRow("", "a", "[a] ")]
    [DataRow("abc", "def", "abc[def] ")]
    [DataRow("abc ", "def", "abc [def] ")]
    public void AppendPropertyFormat_NonFormattedProperty_AddsPlainValueToTheEnd(string original, string property, string expected) =>
        new StringBuilder(original).AppendPropertyFormat(property).ToString().Should().Be(expected);

    [TestMethod]
    public void AppendPropertyFormat_FormattedString_CorrectlyAppliesStringFormat() =>
        new StringBuilder().AppendPropertyFormat("for{0}ted", "mat").ToString().Should().Be("[formatted] ");

    [TestMethod]
    public void AppendPropertyFormat_IncorrectNumberOfParameters_Throws()
    {
        var act =()=> new StringBuilder().AppendPropertyFormat("for{0}t{1}", "mat");

        act.Should().Throw<FormatException>();
    }
}
