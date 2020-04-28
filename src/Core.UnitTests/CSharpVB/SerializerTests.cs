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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.CSharpVB;

namespace SonarLint.VisualStudio.Core.UnitTests.CSharpVB
{
    [TestClass]
    public class SerializerTests
    {
        [TestMethod]
        public void Serializer_NullArg_Throws()
        {
            // Arrange
            Action act = () => Serializer.ToString((MyDataClass)null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("model");
        }

        [TestMethod]
        public void Serializer_ToString_Succeeds()
        {
            // Arrange
            var inputData = new MyDataClass() { Value1 = "val1", Value2 = 22 };

            // Act
            var actual = Serializer.ToString(inputData);

            // Assert
            const string expected = @"<?xml version=""1.0"" encoding=""utf-8""?>
<MyDataClass xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Value1>val1</Value1>
  <Value2>22</Value2>
</MyDataClass>";

            actual.Should().Be(expected);
        }

        public class MyDataClass
        {
            public string Value1 { get; set; }
            public int Value2 { get; set; }
        }
    }
}
