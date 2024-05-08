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

using System.Windows;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.Core.UnitTests.WPF
{
    [TestClass]
    public class NullableSelectedItemConverterTests
    {
        [TestMethod]
        public void Convert_ValueIsNull_ReturnsUnsetValue()
        {
            var testSubject = new NullableSelectedItemConverter();

            var result = testSubject.Convert(null, null, null, null);

            result.Should().Be(DependencyProperty.UnsetValue);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow("")]
        [DataRow(false)]
        public void Convert_ValueIsNotNull_ReturnsValue(object value)
        {
            var testSubject = new NullableSelectedItemConverter();

            var result = testSubject.Convert(value, null, null, null);

            result.Should().Be(value);
        }

        [TestMethod]
        public void Convert_ValueIsNotNull_Object_ReturnsValue()
        {
            var value = new object();
            var testSubject = new NullableSelectedItemConverter();

            var result = testSubject.Convert(value, null, null, null);

            result.Should().Be(value);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("some value")]
        public void ConvertBack_ReturnsValue(object value)
        {
            var testSubject = new NullableSelectedItemConverter();

            var result = testSubject.ConvertBack(value, null, null, null);
            result.Should().Be(value);
        }
    }
}
