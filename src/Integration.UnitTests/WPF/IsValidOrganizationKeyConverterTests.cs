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
using SonarLint.VisualStudio.Integration.Connection.UI;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class IsValidOrganizationKeyConverterTests
    {
        [TestMethod]
        public void Convert_NonBoolTargetType_ThrowsArgumentException()
        {
            var converter = new IsValidOrganisationKeyConverter();

            Action act = () => converter.Convert("valid input", typeof(object), null, CultureInfo.CurrentCulture);

            act.Should().ThrowExactly<ArgumentException>().And.ParamName.Should().Be("targetType");
        }

        [TestMethod]
        public void Convert_NonTextInput_ThrowsArgumentException()
        {
            var converter = new IsValidOrganisationKeyConverter();

            var notAString = new object();
            Action act = () => converter.Convert(notAString, typeof(bool), null, CultureInfo.CurrentCulture);

            act.Should().ThrowExactly<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [TestMethod]
        public void Convert_NullInputString_ReturnsFalse()
        {
            var converter = new IsValidOrganisationKeyConverter();

            var result = converter.Convert((string)null, typeof(bool), null, CultureInfo.CurrentUICulture);

            result.Should().Be(false);
        }

        [TestMethod]
        public void Convert_WhitespaceInput_ReturnsFalse()
        {
            var converter = new IsValidOrganisationKeyConverter();

            var result = converter.Convert("\t\r\n ", typeof(bool), null, CultureInfo.CurrentUICulture);

            result.Should().Be(false);
        }

        [TestMethod]
        public void Convert_ValidOrgKey_ReturnsTrue()
        {
            var converter = new IsValidOrganisationKeyConverter();

            var result = converter.Convert(" is valid key ", typeof(bool), null, null);

            result.Should().Be(true);
        }

        [TestMethod]
        public void ConvertBack_NotImplemented()
        {
            var converter = new IsValidOrganisationKeyConverter();
            Action act = () => converter.ConvertBack(null, null, null, null);

            act.Should().ThrowExactly<NotImplementedException>();
        }

        [TestMethod]
        public void GetTrimmedKey_ReturnsExpectedValues()
        {
            IsValidOrganisationKeyConverter.GetTrimmedKey(null).Should().Be(null);
            IsValidOrganisationKeyConverter.GetTrimmedKey("\r\t\n").Should().Be("");
            IsValidOrganisationKeyConverter.GetTrimmedKey(" is a valid key\r\n").Should().Be("is a valid key");
        }
    }
}
