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
using System.Windows;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.WPF;

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