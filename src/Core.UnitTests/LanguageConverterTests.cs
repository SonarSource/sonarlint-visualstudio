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
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class LanguageConverterTests
    {
        #region Test boilerplate

        private LanguageConverter testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testSubject = new LanguageConverter();
        }

        #endregion Test boilerplate

        #region Tests

        [TestMethod]
        public void LanguageConverter_CanConvertFrom_StringType_IsTrue()
        {
            // Act
            var canConvertFrom = this.testSubject.CanConvertFrom(typeof(string));

            // Assert
            canConvertFrom.Should().BeTrue("Expected to be able to convert from string");
        }

        [TestMethod]
        public void LanguageConverter_CanConvertFrom_NonStringType_IsFalse()
        {
            // Arrange
            var types = new Type[] { typeof(int), typeof(bool), typeof(object) };

            foreach (Type type in types)
            {
                // Act
                var canConvertFrom = this.testSubject.CanConvertFrom(type);

                // Assert
                canConvertFrom.Should().BeFalse($"Expected NOT to be able to convert from {type.Name}");
            }
        }

        [TestMethod]
        public void LanguageConverter_ConvertFrom_AllKnownLanguages_AreRecognized()
        {
            CheckConvertFromRecognizesLanguage(Language.VBNET.Id, Language.VBNET);
            CheckConvertFromRecognizesLanguage(Language.C.Id, Language.C);
            CheckConvertFromRecognizesLanguage(Language.Cpp.Id, Language.Cpp);
            CheckConvertFromRecognizesLanguage(Language.CSharp.Id, Language.CSharp);
        }

        private void CheckConvertFromRecognizesLanguage(string id, Language expectedLanguage)
        {
            // Act
            object result = this.testSubject.ConvertFrom(id);

            // Assert
            result.Should().BeAssignableTo<Language>();
            result.Should().Be(expectedLanguage);
        }

        [TestMethod]
        public void LanguageConverter_ConvertFrom_UnknownLanguageId_ReturnsUnknownLanguage()
        {
            // Act
            object result = this.testSubject.ConvertFrom("WhoAmI?");

            // Assert
            result.Should().BeAssignableTo<Language>();
            result.Should().Be(Language.Unknown);
        }

        [TestMethod]
        public void LanguageConverter_ConvertFrom_Null_ReturnsUnknownLanguage()
        {
            // Act
            object result;
            using (new AssertIgnoreScope()) // null input
            {
                result = this.testSubject.ConvertFrom(null);
            }

            // Assert
            result.Should().BeAssignableTo<Language>();
            result.Should().Be(Language.Unknown);
        }

        [TestMethod]
        public void LanguageConverter_CanConvertTo_StringType_IsTrue()
        {
            // Act
            var canConvertTo = this.testSubject.CanConvertTo(typeof(string));

            // Assert
            canConvertTo.Should().BeTrue("Expected to be able to convert to string");
        }

        [TestMethod]
        public void LanguageConverter_CanConvertTo_NonStringType_IsFalse()
        {
            // Arrange
            var types = new Type[] { typeof(int), typeof(bool), typeof(object) };

            foreach (Type type in types)
            {
                // Act
                var canConvertTo = this.testSubject.CanConvertTo(type);

                // Assert
                canConvertTo.Should().BeFalse($"Expected NOT to be able to convert to {type.Name}");
            }
        }

        [TestMethod]
        public void LanguageConverter_ConvertTo_Language_ReturnsLanguageId()
        {
            // Act
            object result = this.testSubject.ConvertTo(Language.VBNET, typeof(string));

            // Assert
            result.Should().BeAssignableTo<string>();
            result.Should().Be(Language.VBNET.Id);
        }

        [TestMethod]
        public void LanguageConverter_ConvertTo_Null_ReturnsNull()
        {
            // Act
            object result;
            using (new AssertIgnoreScope()) // null input
            {
                result = this.testSubject.ConvertTo(null, typeof(string));
            }

            // Assert
            result.Should().BeNull();
        }

        #endregion Tests
    }
}
