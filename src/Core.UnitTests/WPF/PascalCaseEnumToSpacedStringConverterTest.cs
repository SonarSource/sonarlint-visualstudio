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
 * This program is distributed in the hope that it will be useful;
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.Core.UnitTests.WPF;

[TestClass]
public class PascalCaseEnumToSpacedStringConverterTest
{
    private PascalCaseEnumToSpacedStringConverter testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new PascalCaseEnumToSpacedStringConverter();

    [DataTestMethod]
    [DataRow(TestEnum.Value1, "Value1")]
    [DataRow(TestEnum.TwoWords, "Two Words")]
    [DataRow(TestEnum.ThreeWordsHere, "Three Words Here")]
    [DataRow(TestEnum.MULTIPLEWORDS, "MULTIPLEWORDS")]
    public void Convert_TypeProvided_ConvertsAsExpected(TestEnum type, string expected)
    {
        var result = testSubject.Convert(type, null, null, null);

        result.Should().Be(expected);
    }

    [TestMethod]
    public void Convert_InvalidType_ReturnsReceivedValue()
    {
        var value = "test";
        var result = testSubject.Convert(value, null, null, null);

        result.Should().Be(value);
    }

    [TestMethod]
    public void Convert_NoEnumProvided_ReturnsReceivedValue()
    {
        var result = testSubject.Convert(null, null, null, null);

        result.Should().BeNull();
    }

    [TestMethod]
    public void ConvertBack_NotImplementedException()
    {
        Action act = () => testSubject.ConvertBack(null, null, null, null);

        act.Should().Throw<NotImplementedException>();
    }

    public enum TestEnum
    {
        Value1,
        TwoWords,
        ThreeWordsHere,
        MULTIPLEWORDS
    }
}
