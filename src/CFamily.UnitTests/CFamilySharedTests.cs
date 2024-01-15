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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.CFamily.UnitTests
{
    [TestClass]
    public class CFamilySharedTests
    {
        [TestMethod]
        [DataRow("c:\\aaa.cpp", SonarLanguageKeys.CPlusPlus)]
        [DataRow("c:\\AAA.CPP", SonarLanguageKeys.CPlusPlus)]
        [DataRow("c:\\aaa.cxx", SonarLanguageKeys.CPlusPlus)]
        [DataRow("d:\\xxx.cc", SonarLanguageKeys.CPlusPlus)]
        [DataRow("c:\\aaa\\bbb.c", SonarLanguageKeys.C)]
        [DataRow("c:\\aaa.cpp.x", null)]
        [DataRow("c:\\aaa.cs", null)]
        [DataRow("c:\\aaa.js", null)]
        [DataRow("c:\\aaa.x", null)]
        public void FindLanguage_ReturnsExpectedValue(string filePath, string expected)
        {
            CFamilyShared.FindLanguageFromExtension(filePath)
                .Should().Be(expected);
        }

        [TestMethod]
        [DataRow("c:\\test.h", true)]
        [DataRow("c:\\test.hpp", true)]
        [DataRow("c:\\test.hh", true)]
        [DataRow("c:\\test.hxx", true)]
        [DataRow("c:\\test.H", true)]
        [DataRow("c:\\test.HPP", true)]
        [DataRow("c:\\test.HH", true)]
        [DataRow("c:\\test.HXX", true)]
        [DataRow("c:\\test.Hxx", true)]
        [DataRow("c:\\test.h.ha", false)]
        [DataRow("c:\\test.cpp", false)]
        [DataRow("c:\\test.cpp.h", true)]
        public void IsHeaderFileExtension_ReturnsExpectedValue(string filePath, bool expected)
        {
            CFamilyShared.IsHeaderFileExtension(filePath)
                .Should().Be(expected);
        }
    }
}
