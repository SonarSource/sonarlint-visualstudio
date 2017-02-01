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

using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class ProjectViewModelVisibilityConverterTests
    {
        [ExpectedException(typeof(NotSupportedException))]
        [TestMethod]
        public void ProjectViewModelVisibilityConverter_ConvertBack_ThrowsNotSupportedException()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();

            // Act & Assert
            converter.ConvertBack(null, null, null, null);
        }

        [TestMethod]
        public void ProjectViewModelVisibilityConverter_Convert_WhenGivenANullValue_ExpectsDependencyProperyUnsetValue()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();

            // Act
            var result = converter.Convert(null, null, null, null);

            // Assert
            DependencyProperty.UnsetValue.Should().Be(result);
        }

        [TestMethod]
        public void ProjectViewModelVisibilityConverter_Convert_WhenNotGivenFourValues_ExpectsDependencyProperyUnsetValue()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();

            // Act
            var result0 = converter.Convert(new object[0], null, null, null);
            var result1 = converter.Convert(new object[1], null, null, null);
            var result2 = converter.Convert(new object[2], null, null, null);
            var result3 = converter.Convert(new object[3], null, null, null);
            var result5 = converter.Convert(new object[5], null, null, null);

            // Assert
            result0.Should().Be(DependencyProperty.UnsetValue);
            result1.Should().Be(DependencyProperty.UnsetValue);
            result2.Should().Be(DependencyProperty.UnsetValue);
            result3.Should().Be(DependencyProperty.UnsetValue);
            result5.Should().Be(DependencyProperty.UnsetValue);
        }

        [TestMethod]
        public void ProjectViewModelVisibilityConverter_Convert_WhenGivenFourValuesButFirstValueIsNotStringOrIsNull_ExpectsDependencyProperyUnsetValue()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();
            var values = new object[4];

            // Act
            values[0] = 5;
            var result1 = converter.Convert(values, null, null, null);
            values[0] = null;
            var result2 = converter.Convert(values, null, null, null);

            // Assert
            result1.Should().Be(DependencyProperty.UnsetValue);
            result2.Should().Be(DependencyProperty.UnsetValue);
        }

        [TestMethod]
        public void ProjectViewModelVisibilityConverter_Convert_WhenGivenFourValuesButSecondValueIsNotBoolOrIsNull_ExpectsDependencyProperyUnsetValue()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();
            var values = new object[4];
            values[0] = "foo";

            // Act
            values[1] = 5;
            var result1 = converter.Convert(values, null, null, null);
            values[1] = null;
            var result2 = converter.Convert(values, null, null, null);

            // Assert
            result1.Should().Be(DependencyProperty.UnsetValue);
            result2.Should().Be(DependencyProperty.UnsetValue);
        }

        [TestMethod]
        public void ProjectViewModelVisibilityConverter_Convert_WhenGivenFourValuesButThirdValueIsNotBoolOrIsNull_ExpectsDependencyProperyUnsetValue()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();
            var values = new object[4];
            values[0] = "foo";
            values[1] = true;

            // Act
            values[2] = 5;
            var result1 = converter.Convert(values, null, null, null);
            values[2] = null;
            var result2 = converter.Convert(values, null, null, null);

            // Assert
            result1.Should().Be(DependencyProperty.UnsetValue);
            result2.Should().Be(DependencyProperty.UnsetValue);
        }

        [TestMethod]
        public void ProjectViewModelVisibilityConverter_Convert_WhenGivenFourValuesButFourthValueIsNotStringOrIsNull_ExpectsDependencyProperyUnsetValue()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();
            var values = new object[4];
            values[0] = "foo";
            values[1] = true;
            values[2] = "bar";

            // Act
            values[3] = 5;
            var result1 = converter.Convert(values, null, null, null);
            values[3] = null;
            var result2 = converter.Convert(values, null, null, null);

            // Assert
            result1.Should().Be(DependencyProperty.UnsetValue);
            result2.Should().Be(DependencyProperty.UnsetValue);
        }

        [TestMethod]
        public void ProjectViewModelVisibilityConverter_Convert_WhenShowAllProjectsIsTrueAndProjectNameContainsFilterText_ReturnsVisible()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();
            var projectName = "foobar";
            var isShowingAllProjects = true;
            var isBound = false;
            var filterText = "bar";
            var values = new object[] { projectName, isShowingAllProjects, isBound, filterText };

            // Act
            var result = converter.Convert(values, null, null, null);

            // Assert
            result.Should().BeAssignableTo<Visibility>();
            Visibility.Visible.Should().Be(result);
        }

        [TestMethod]
        public void ProjectViewModelVisibilityConverter_Convert_WhenShowAllProjectsIsTrueAndProjectNameDoesNotContainFilterText_ReturnsCollapsed()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();
            var projectName = "foobar";
            var isShowingAllProjects = true;
            var isBound = false;
            var filterText = "test";
            var values = new object[] { projectName, isShowingAllProjects, isBound, filterText };

            // Act
            var result = converter.Convert(values, null, null, null);

            // Assert
            result.Should().BeAssignableTo<Visibility>();
            Visibility.Collapsed.Should().Be(result);
        }

        [TestMethod]
        public void ProjectViewModelVisibilityConverter_Convert_WhenShowAllProjectsIsFalseAndIsBoundIsTrue_ReturnsVisible()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();
            var projectName = "";
            var isShowingAllProjects = false;
            var isBound = true;
            var filterText = "";
            var values = new object[] { projectName, isShowingAllProjects, isBound, filterText };

            // Act
            var result = converter.Convert(values, null, null, null);

            // Assert
            result.Should().BeAssignableTo<Visibility>();
            Visibility.Visible.Should().Be(result);
        }

        [TestMethod]
        public void ProjectViewModelVisibilityConverter_Convert_WhenShowAllProjectsIsFalseAndIsBoundIsFalse_ReturnsCollapsed()
        {
            // Arrange
            var converter = new ProjectViewModelVisibilityConverter();
            var projectName = "";
            var isShowingAllProjects = false;
            var isBound = false;
            var filterText = "";
            var values = new object[] { projectName, isShowingAllProjects, isBound, filterText };

            // Act
            var result = converter.Convert(values, null, null, null);

            // Assert
            result.Should().BeAssignableTo<Visibility>();
            Visibility.Collapsed.Should().Be(result);
        }
    }
}
