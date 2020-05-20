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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using FluentAssertions.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class SonarLintExceptionTests
    {
        [TestMethod]
        public void Ctor_DefaultConstructor()
        {
            var ex = new SonarLintException();

            ex.Message.Should().NotBeNull(); // default localized error message
            ex.Message.Should().Contain(nameof(SonarLintException)); // default localized error message
            ex.InnerException.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_MessageOnly()
        {
            var ex = new SonarLintException("xxx");

            ex.Message.Should().Be("xxx");
            ex.InnerException.Should().BeNull();
        }

        [TestMethod]
        public void Ctor_MessageAndInnerException()
        {
            var inner = new StackOverflowException();

            var ex = new SonarLintException("Binding failed", inner);

            ex.Message.Should().Be("Binding failed");
            ex.InnerException.Should().BeSameAs(inner);
        }

        [TestMethod]
        public void Ctor_Serialization()
        {
            var inner = new InvalidDataException();
            var serializer = new BinaryFormatter();

            var originalEx = new SonarLintException("yyy", inner);
            SonarLintException deserializedEx;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, originalEx);
                stream.Position = 0;
                deserializedEx = serializer.Deserialize(stream) as SonarLintException;
            }

            deserializedEx.Message.Should().Be("yyy");
            deserializedEx.InnerException.Should().BeOfType<InvalidDataException>();
        }
    }
}
