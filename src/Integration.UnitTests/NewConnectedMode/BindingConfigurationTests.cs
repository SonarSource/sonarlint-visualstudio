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
using SonarQube.Client.Models;

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
            var actual = BindingConfiguration.CreateBoundConfiguration(new BoundSonarQubeProject(), isLegacy: true);

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

        #region Equality tests

        [TestMethod]
        public void Equals_NullOrg_AreEqual()
        {
            // Arrange
            var projectAAA1 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", organization: null);
            var projectAAA2 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", organization: null);
            var projectAAA3 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", organization: null);

            var config1 = BindingConfiguration.CreateBoundConfiguration(projectAAA1, isLegacy: true);
            var config2 = BindingConfiguration.CreateBoundConfiguration(projectAAA2, isLegacy: true);
            var config3 = BindingConfiguration.CreateBoundConfiguration(projectAAA3, isLegacy: true);

            // Action & Assert
            // Reflexive
            config1.Equals(config1).Should().BeTrue();
            config2.Equals(config2).Should().BeTrue();

            // Transitive
            config1.Equals(config2).Should().BeTrue();
            config2.Equals(config3).Should().BeTrue();
            config3.Equals(config1).Should().BeTrue();

            // Symmetric
            config2.Equals(config3).Should().BeTrue();
            config3.Equals(config2).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_NonNullOrg_AreEqual()
        {
            // Arrange
            var projectAAA1 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA",
                organization: new SonarQubeOrganization("org1", "111"));
            var projectAAA2 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA",
                organization: new SonarQubeOrganization("org1", "222222222222222"));
            var projectAAA3 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA",
                organization: new SonarQubeOrganization("org1", "333333333333333333"));

            var config1 = BindingConfiguration.CreateBoundConfiguration(projectAAA1, isLegacy: false);
            var config2 = BindingConfiguration.CreateBoundConfiguration(projectAAA2, isLegacy: false);
            var config3 = BindingConfiguration.CreateBoundConfiguration(projectAAA3, isLegacy: false);

            // Act & Assert
            // Reflexive
            config1.Equals(config1).Should().BeTrue();
            config2.Equals(config2).Should().BeTrue();

            // Transitive
            config1.Equals(config2).Should().BeTrue();
            config2.Equals(config3).Should().BeTrue();
            config3.Equals(config1).Should().BeTrue();

            // Symmetric
            config2.Equals(config3).Should().BeTrue();
            config3.Equals(config2).Should().BeTrue();

            // Hash codes
            config1.GetHashCode().Should().Be(config2.GetHashCode());
            config1.GetHashCode().Should().Be(config3.GetHashCode());
        }

        [TestMethod]
        public void Equals_DifferentProjects_AreNotEqual()
        {
            // Arrange
            var project1 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", organization: null);
            var project2 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectBBB", organization: null);

            var standalone = BindingConfiguration.Standalone;
            var config1 = BindingConfiguration.CreateBoundConfiguration(project1, isLegacy: true);
            var config2 = BindingConfiguration.CreateBoundConfiguration(project2, isLegacy: true);

            // Act & Assert
            config1.Equals(config2).Should().BeFalse();
            config2.Equals(config1).Should().BeFalse();

            standalone.Equals(config1).Should().BeFalse();
            config1.Equals(standalone).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_DifferentModes_AreNotEqual()
        {
            // Arrange
            var project1 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", organization: null);

            var standalone = BindingConfiguration.Standalone;
            var config1 = BindingConfiguration.CreateBoundConfiguration(project1, isLegacy: true);
            var config2 = BindingConfiguration.CreateBoundConfiguration(project1, isLegacy: false);

            // Act & Assert
            config1.Equals(config2).Should().BeFalse();
            config2.Equals(config1).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_DifferentOrganisation_AreNotEqual()
        {
            // Arrange
            var project1 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA",
                organization: new SonarQubeOrganization("org1", "any"));
            var project2 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA",
                organization: new SonarQubeOrganization("ORG1", "any")); // different in case only

            var config1 = BindingConfiguration.CreateBoundConfiguration(project1, isLegacy: true);
            var config2 = BindingConfiguration.CreateBoundConfiguration(project2, isLegacy: true);

            // Act & Assert
            config1.Equals(config2).Should().BeFalse();
            config2.Equals(config1).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_DifferentServer_AreNotEqual()
        {
            // Arrange
            var project1 = new BoundSonarQubeProject(new Uri("http://localhost1"), "projectAAA",
                organization: new SonarQubeOrganization("org1", "any"));
            var project2 = new BoundSonarQubeProject(new Uri("http://localhost2"), "projectAAA",
                organization: new SonarQubeOrganization("org1", "any"));

            var config1 = BindingConfiguration.CreateBoundConfiguration(project1, isLegacy: true);
            var config2 = BindingConfiguration.CreateBoundConfiguration(project2, isLegacy: true);

            // Act & Assert
            config1.Equals(config2).Should().BeFalse();
            config2.Equals(config1).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_Null_AreNotEqual()
        {
            // Act & Assert
            BindingConfiguration.Standalone.Equals(null).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_DifferentType_AreNotEqual()
        {
            // Act & Assert
            BindingConfiguration.Standalone.Equals(new object()).Should().BeFalse();
        }

        #endregion
    }
}
