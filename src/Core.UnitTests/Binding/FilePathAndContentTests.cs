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
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Core.UnitTests.Binding
{
    [TestClass]
    public class FilePathAndContentTests
    {
        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void Ctor_NullFilePath_ArgumentNullException(string filePath)
        {
            Action act = () => new FilePathAndContent<string>(filePath, "content");

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("path");
        }

        [TestMethod]
        public void Ctor_NullContent_ArgumentNullException()
        {
            Action act = () => new FilePathAndContent<string>("filepath", null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("content");
        }

        [TestMethod]
        public void Ctor_ValidProperties_AreSetCorrectly()
        {
            var testSubject = new FilePathAndContent<int>("foo", 123);

            testSubject.Path.Should().Be("foo");
            testSubject.Content.Should().Be(123);
        }
    }
}
