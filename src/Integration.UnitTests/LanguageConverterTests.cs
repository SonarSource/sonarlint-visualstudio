/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
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
        public void LanguageConverter_ConvertFrom_KnownLanguageId_ReturnsKnownLanguage()
        {
            // Act
            object result = this.testSubject.ConvertFrom(Language.VBNET.Id);

            // Assert
            result.Should().BeAssignableTo<Language>();
            result.Should().Be(Language.VBNET);
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