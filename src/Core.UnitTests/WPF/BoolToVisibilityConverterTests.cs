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

using System;
using System.Globalization;
using System.Windows;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.Core.UnitTests.WPF
{
    [TestClass]
    public class BoolToVisibilityConverterTests
    {
        [TestMethod]
        public void BoolToVisibilityConverter_DefaultValues()
        {
            var converter = new BoolToVisibilityConverter();

            Visibility.Visible.Should().Be(converter.TrueValue);
            Visibility.Collapsed.Should().Be(converter.FalseValue);
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_NonBoolInput_ThrowsArgumentException()
        {
            var converter = new BoolToVisibilityConverter();

            Action act = () => converter.Convert("NotABoolean", typeof(Visibility), null, CultureInfo.InvariantCulture);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_NonVisibilityOutput_ThrowsArgumentException()
        {
            var converter = new BoolToVisibilityConverter();
            var notVisibilityType = typeof(string);

            Action act = () => converter.Convert(true, notVisibilityType, null, CultureInfo.InvariantCulture);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_True_ReturnsTrueValue()
        {
            var converter = new BoolToVisibilityConverter
            {
                TrueValue = Visibility.Hidden,
                FalseValue = Visibility.Visible
            };
            object result = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);

            result.Should().BeAssignableTo<Visibility>();
            Visibility.Hidden.Should().Be(result);
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_False_ReturnsFalseValue()
        {
            var converter = new BoolToVisibilityConverter
            {
                TrueValue = Visibility.Hidden,
                FalseValue = Visibility.Visible
            };
            object result = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

            result.Should().BeAssignableTo<Visibility>();
            Visibility.Visible.Should().Be(result);
        }

        [TestMethod]
        public void ConvertBack_NotImplementedException()
        {
            var converter = new BoolToVisibilityConverter
            {
                TrueValue = Visibility.Hidden,
                FalseValue = Visibility.Visible
            };

            Action act = () => converter.ConvertBack(null, null, null, null);

            act.Should().Throw<NotImplementedException>("ConvertBack is not required");
        }
    }
}
