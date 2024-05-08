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

using System.Globalization;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.Core.UnitTests.WPF
{
    [TestClass]
    public class DateTimeConverterTests
    {
        [TestMethod]
        public void Convert_UsesUICulture()
        {
            var userCulture = CultureInfo.GetCultureInfo("fr-fr");
            var wpfCulture = CultureInfo.GetCultureInfo("en-US");

            var date = new DateTimeOffset(2020, 01, 25, 16, 50, 04, 0, TimeSpan.Zero);
            var expectedDate = date.ToString("G", userCulture);

            var testSubject = new DateTimeConverter(userCulture);
            var result = testSubject.Convert(date, typeof(string), null, wpfCulture);

            result.Should().Be(expectedDate);
        }

        [TestMethod]
        public void ConvertBack_NotImplementedException()
        {
            var testSubject = new DateTimeConverter();

            Action act = () => testSubject.ConvertBack(null, null, null, null);

            act.Should().Throw<NotImplementedException>("ConvertBack is not required");
        }
    }
}
