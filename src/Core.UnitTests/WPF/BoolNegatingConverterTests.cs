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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.Core.UnitTests.WPF;

[TestClass]
public class BoolNegatingConverterTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Convert_BooleanToNegative(bool value)
    {
        var testSubject = new BoolNegatingConverter();

        var result = testSubject.Convert(value, null, null, null);

        result.Should().Be(!value);
    }

    [TestMethod]
    public void Convert_NotBool_Throws()
    {
        var testSubject = new BoolNegatingConverter();

        Action act = () => testSubject.Convert("not bool", null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void ConvertBack_ThrowsNotSupported()
    {
        var testSubject = new BoolNegatingConverter();

        Action act = () => testSubject.ConvertBack(null, null, null, null);

        act.Should().Throw<NotSupportedException>();
    }
}
