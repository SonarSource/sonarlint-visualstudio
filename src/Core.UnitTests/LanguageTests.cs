/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class LanguageTests
    {
        [TestMethod]
        public void Language_Ctor_ArgChecks()
        {
            // Arrange
            var key = "k";
            var name = "MyName";
            var fileSuffix = "suffix";

            // Act + Assert
            // Nulls
            Action act = () => new Language(name, null, fileSuffix);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("name");

            act = () => new Language(null, key, fileSuffix);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("id");

            act = () => new Language(name, key, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSuffix");
        }

        [TestMethod]
        public void Language_UnknownLanguage()
        {
            Language.Unknown.Id.Should().BeEmpty();
            Language.Unknown.Name.Should().Be(CoreStrings.UnknownLanguageName);
        }

        [TestMethod]
        public void Language_IsSupported_SupportedLanguage_IsTrue()
        {
            // Act + Assert
            Language.CSharp.IsSupported.Should().BeTrue();
            Language.VBNET.IsSupported.Should().BeTrue();
            Language.Cpp.IsSupported.Should().BeTrue();
            Language.C.IsSupported.Should().BeTrue();
        }

        [TestMethod]
        public void Language_ISupported_UnsupportedLanguage_IsFalse()
        {
            var other = new Language("foo", "Foo language", "file_suffix");
            other.IsSupported.Should().BeFalse();
        }

        [TestMethod]
        public void Language_Equality()
        {
            // Arrange
            var lang1a = new Language("Language 1", "lang1", "file_suffix");
            var lang1b = new Language("Language 1", "lang1", "file_suffix");
            var lang2 = new Language("Language 2", "lang2", "file_suffix");

            // Act + Assert
            lang1b.Should().Be(lang1a, "Languages with the same keys and GUIDs should be equal");
            lang2.Should().NotBe(lang1a, "Languages with different keys and GUIDs should NOT be equal");
        }
    }
}
