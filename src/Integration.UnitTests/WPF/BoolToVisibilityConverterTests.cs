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

using SonarLint.VisualStudio.Integration.WPF;
 using Xunit;
using System;
using System.Globalization;
using System.Windows;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{

    public class BoolToVisibilityConverterTests
    {
        [Fact]
        public void BoolToVisibilityConverter_DefaultValues()
        {
            var converter = new BoolToVisibilityConverter();

            converter.TrueValue.Should().Be( Visibility.Visible);
            converter.FalseValue.Should().Be( Visibility.Collapsed);
        }

        [Fact]
        public void Convert_NonBoolInput_ThrowsArgumentException()
        {
            // Arrange
            var converter = new BoolToVisibilityConverter();

            // Act
            Action act = () => converter.Convert("NotABoolean", typeof(Visibility), null, CultureInfo.InvariantCulture);

            // Assert
            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Convert_NonVisibilityOutput_ThrowsArgumentException()
        {
            // Arrange
            var converter = new BoolToVisibilityConverter();
            var notVisibilityType = typeof(string);

            // Act
            Action act = () => converter.Convert(true, notVisibilityType, null, CultureInfo.InvariantCulture);

            // Assert
            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void BoolToVisibilityConverter_Convert_True_ReturnsTrueValue()
        {
            var converter = new BoolToVisibilityConverter
            {
                TrueValue = Visibility.Hidden,
                FalseValue = Visibility.Visible
            };
            object result = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);

            result.Should().BeAssignableTo<Visibility>();
            result.Should().Be( Visibility.Hidden);
        }

        [Fact]
        public void BoolToVisibilityConverter_Convert_False_ReturnsFalseValue()
        {
            var converter = new BoolToVisibilityConverter
            {
                TrueValue = Visibility.Hidden,
                FalseValue = Visibility.Visible
            };
            object result = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);

            result.Should().BeAssignableTo<Visibility>();
            result.Should().Be( Visibility.Visible);
        }
    }
}
