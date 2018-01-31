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
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BindingConfigurationTests
    {
        [TestMethod]
        public void Static_CheckStandaloneSingleton()
        {
            BindingConfiguration.Standalone.Project.Should().BeNull();
            BindingConfiguration.Standalone.Mode.Should().Be(SonarLintMode.Standalone);
        }

        [TestMethod]
        public void StaticCreator_InvalidArgs_NullProject_Throws()
        {
            // Arrange
            Action act = () => BindingConfiguration.CreateBoundConfiguration(null, false);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [TestMethod]
        public void StaticCreator_LegacyProject_ModeIsLegacy()
        {
            // Arrange & Act
            var actual =  BindingConfiguration.CreateBoundConfiguration(new BoundSonarQubeProject(), isLegacy: true);

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().NotBeNull();
            actual.Mode.Should().Be(SonarLintMode.LegacyConnected);
        }

        [TestMethod]
        public void StaticCreator_LegacyProject_ModeIsNotLegacy()
        {
            // Arrange & Act
            var actual = BindingConfiguration.CreateBoundConfiguration(new BoundSonarQubeProject(), isLegacy: false);

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().NotBeNull();
            actual.Mode.Should().Be(SonarLintMode.Connected);
        }
    }
}
