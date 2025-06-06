﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.Core.UnitTests.WPF
{
    [TestClass]
    public class NullToBooleanConverterTests
    {
        [TestMethod]
        [DataRow(null, true)]
        [DataRow(1, false)]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow("some string", false)]
        public void Convert_ReturnsTrueIfValueIsNull(object value, bool expectedResult)
        {
            var testSubject = new NullToBooleanConverter();

            var result = testSubject.Convert(value, null, null, null);

            result.Should().Be(expectedResult);
        }

        [TestMethod]
        public void ConvertBack_NotImplementedException()
        {
            var testSubject = new NullToBooleanConverter();

            Action act = () => testSubject.ConvertBack(null, null, null, null);

            act.Should().Throw<NotImplementedException>("ConvertBack is not required");
        }
    }
}
