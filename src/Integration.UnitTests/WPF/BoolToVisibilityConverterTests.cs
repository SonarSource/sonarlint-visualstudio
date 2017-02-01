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
using System.Globalization;
using System.Windows;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class BoolToVisibilityConverterTests
    {
        [TestMethod]
        public void BoolToVisibilityConverter_DefaultValues()
        {
            var converter = new BoolToVisibilityConverter();

            Visibility.Visible.Should().Be(converter.TrueValue);
            Visibility.Collapsed.Should().Be(converter.FalseValue);
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_NonBoolInput_ThrowsArgumentException()
        {
            var converter = new BoolToVisibilityConverter();

            Exceptions.Expect<ArgumentException>(() =>
            {
                converter.Convert("NotABoolean", typeof(Visibility), null, CultureInfo.InvariantCulture);
            });
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_NonVisibilityOutput_ThrowsArgumentException()
        {
            var converter = new BoolToVisibilityConverter();
            var notVisibilityType = typeof(string);

            Exceptions.Expect<ArgumentException>(() =>
            {
                converter.Convert(true, notVisibilityType, null, CultureInfo.InvariantCulture);
            });
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_True_ReturnsTrueValue()
        {
            var converter = new BoolToVisibilityConverter
            {
                TrueValue = Visibility.Hidden,
                FalseValue = Visibility.Visible
            };
            object result = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);

            result.Should().BeAssignableTo<Visibility>();
            Visibility.Hidden.Should().Be(result);
        }

        [TestMethod]
        public void BoolToVisibilityConverter_Convert_False_ReturnsFalseValue()
        {
            var converter = new BoolToVisibilityConverter
            {
                TrueValue = Visibility.Hidden,
                FalseValue = Visibility.Visible
            };
            object result = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

            result.Should().BeAssignableTo<Visibility>();
            Visibility.Visible.Should().Be(result);
        }
    }
}