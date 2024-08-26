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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class LegacyBindingConfigurationTests
    {
        [TestMethod]
        public void Static_CheckStandaloneSingleton()
        {
            LegacyBindingConfiguration.Standalone.Project.Should().BeNull();
            LegacyBindingConfiguration.Standalone.Mode.Should().Be(SonarLintMode.Standalone);
        }

        [TestMethod]
        public void StaticCreator_InvalidArgs_NullProject_Throws()
        {
            // Arrange
            Action act = () => LegacyBindingConfiguration.CreateBoundConfiguration(null, SonarLintMode.Connected, "c:\\");

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void StaticCreator_InvalidArgs_NullDirectoryPath_Throws(string directoryPath)
        {
            // Arrange
            Action act = () => LegacyBindingConfiguration.CreateBoundConfiguration(new BoundSonarQubeProject(), SonarLintMode.Connected, directoryPath);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingConfigDirectory");
        }

        [TestMethod]
        public void StaticCreator_LegacyProject_ModeIsLegacy()
        {
            // Arrange & Act
            var actual = LegacyBindingConfiguration.CreateBoundConfiguration(new BoundSonarQubeProject(), SonarLintMode.LegacyConnected, "c:\\");

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().NotBeNull();
            actual.Mode.Should().Be(SonarLintMode.LegacyConnected);
        }

        [TestMethod]
        public void StaticCreator_LegacyProject_ModeIsNotLegacy()
        {
            // Arrange & Act
            var actual = LegacyBindingConfiguration.CreateBoundConfiguration(new BoundSonarQubeProject(), SonarLintMode.Connected, "c:\\");

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
            var projectAAA1 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", "projectName", organization: null);
            var projectAAA2 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", "projectName", organization: null);
            var projectAAA3 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", "projectName", organization: null);

            var config1 = LegacyBindingConfiguration.CreateBoundConfiguration(projectAAA1, SonarLintMode.LegacyConnected, "c:\\");
            var config2 = LegacyBindingConfiguration.CreateBoundConfiguration(projectAAA2, SonarLintMode.LegacyConnected, "c:\\");
            var config3 = LegacyBindingConfiguration.CreateBoundConfiguration(projectAAA3, SonarLintMode.LegacyConnected, "c:\\");

            // Action & Assert
            // Reflexive
            CheckAreEqual(config1, config2);
            CheckAreEqual(config2, config1);

            // Transitive
            CheckAreEqual(config1, config2);
            CheckAreEqual(config2, config3);
            CheckAreEqual(config3, config1);

            // Symmetric
            CheckAreEqual(config2, config3);
            CheckAreEqual(config3, config2);
        }

        [TestMethod]
        public void Equals_NonNullOrg_AreEqual()
        {
            // Arrange
            var projectAAA1 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", "projectName",
                organization: new SonarQubeOrganization("org1", "111"));
            var projectAAA2 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", "projectName",
                organization: new SonarQubeOrganization("org1", "222222222222222"));
            var projectAAA3 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", "projectName",
                organization: new SonarQubeOrganization("org1", "333333333333333333"));

            var config1 = LegacyBindingConfiguration.CreateBoundConfiguration(projectAAA1, SonarLintMode.Connected, "c:\\");
            var config2 = LegacyBindingConfiguration.CreateBoundConfiguration(projectAAA2, SonarLintMode.Connected, "c:\\");
            var config3 = LegacyBindingConfiguration.CreateBoundConfiguration(projectAAA3, SonarLintMode.Connected, "c:\\");

            // Act & Assert
            // Reflexive
            CheckAreEqual(config1, config1);
            CheckAreEqual(config2, config2);

            // Transitive
            CheckAreEqual(config1, config2);
            CheckAreEqual(config2, config3);
            CheckAreEqual(config3, config1);

            // Symmetric
            CheckAreEqual(config2, config3);
            CheckAreEqual(config3, config2);
        }

        [TestMethod]
        public void Equals_DifferentProjects_AreNotEqual()
        {
            // Arrange
            var project1 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", "projectName", organization: null);
            var project2 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectBBB", "projectName", organization: null);

            var standalone = LegacyBindingConfiguration.Standalone;
            var config1 = LegacyBindingConfiguration.CreateBoundConfiguration(project1, SonarLintMode.LegacyConnected, "c:\\");
            var config2 = LegacyBindingConfiguration.CreateBoundConfiguration(project2, SonarLintMode.LegacyConnected, "c:\\");

            // Act & Assert
            CheckAreNotEqual(config1, config2);
            CheckAreNotEqual(config2, config1);

            CheckAreNotEqual(standalone, config1);
            CheckAreNotEqual(config1, standalone);
        }

        [TestMethod]
        public void Equals_DifferentModes_AreNotEqual()
        {
            // Arrange
            var project1 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", "projectName", organization: null);

            var standalone = LegacyBindingConfiguration.Standalone;
            var config1 = LegacyBindingConfiguration.CreateBoundConfiguration(project1, SonarLintMode.LegacyConnected, "c:\\");
            var config2 = LegacyBindingConfiguration.CreateBoundConfiguration(project1, SonarLintMode.Connected, "c:\\");

            // Act & Assert
            CheckAreNotEqual(config1, config2);
            CheckAreNotEqual(config2, config1);

            CheckAreNotEqual(standalone, config1);
            CheckAreNotEqual(standalone, config2);
        }

        [TestMethod]
        public void Equals_DifferentOrganisation_AreNotEqual()
        {
            // Arrange
            var project1 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", "projectName",
                organization: new SonarQubeOrganization("org1", "any"));
            var project2 = new BoundSonarQubeProject(new Uri("http://localhost"), "projectAAA", "projectName",
                organization: new SonarQubeOrganization("ORG1", "any")); // different in case only

            var config1 = LegacyBindingConfiguration.CreateBoundConfiguration(project1, SonarLintMode.LegacyConnected, "c:\\");
            var config2 = LegacyBindingConfiguration.CreateBoundConfiguration(project2, SonarLintMode.LegacyConnected, "c:\\");

            // Act & Assert
            CheckAreNotEqual(config1, config2);
            CheckAreNotEqual(config2, config1);
        }

        [TestMethod]
        public void Equals_DifferentServer_AreNotEqual()
        {
            // Arrange
            var project1 = new BoundSonarQubeProject(new Uri("http://localhost1"), "projectAAA", "projectName",
                organization: new SonarQubeOrganization("org1", "any"));
            var project2 = new BoundSonarQubeProject(new Uri("http://localhost2"), "projectAAA", "projectName",
                organization: new SonarQubeOrganization("org1", "any"));

            var config1 = LegacyBindingConfiguration.CreateBoundConfiguration(project1, SonarLintMode.LegacyConnected, "c:\\");
            var config2 = LegacyBindingConfiguration.CreateBoundConfiguration(project2, SonarLintMode.LegacyConnected, "c:\\");

            // Act & Assert
            CheckAreNotEqual(config1, config2);
            CheckAreNotEqual(config2, config1);
        }

        [TestMethod]
        public void Equals_Null_AreNotEqual()
        {
            // Act & Assert
            object nullObject = null;
            LegacyBindingConfiguration.Standalone.Equals(nullObject).Should().BeFalse();

            LegacyBindingConfiguration nullConfig = null;
            LegacyBindingConfiguration.Standalone.Equals(nullConfig).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_DifferentType_AreNotEqual()
        {
            // Act & Assert
            LegacyBindingConfiguration.Standalone.Equals(new object()).Should().BeFalse();
        }

        private static void CheckAreEqual(LegacyBindingConfiguration left, LegacyBindingConfiguration right)
        {
            left.Equals(right).Should().BeTrue(); // strongly-typed Equals
            left.Equals((object)right).Should().BeTrue(); // untyped Equals

            left.GetHashCode().Should().Be(right.GetHashCode());
        }

        private static void CheckAreNotEqual(LegacyBindingConfiguration left, LegacyBindingConfiguration right)
        {
            left.Equals(right).Should().BeFalse();  // strongly-typed Equals
            left.Equals((object)right).Should().BeFalse(); // untyped Equals
        }

        #endregion

        [TestMethod]
        [DataRow("c:\\MY Directory", "file.SUFFIX", "c:\\MY Directory\\file.suffix")]
        [DataRow("c:\\", "NAME.txt", "c:\\name.txt")]
        [DataRow("c:\\", "N|a<m>e.txt", "c:\\n_a_m_e.txt")]
        public void BuildPathUnderConfigDirectory_GeneratesCorrectFilePath(string rootDirectory, string fileNameSuffixAndExtension, string expectedPath)
        {
            var project = new BoundSonarQubeProject(new Uri("http://localhost2"), "My<Key>", "projectName");
            var testSubject = LegacyBindingConfiguration.CreateBoundConfiguration(project, SonarLintMode.LegacyConnected, rootDirectory);

            var result = testSubject.BuildPathUnderConfigDirectory(fileNameSuffixAndExtension);

            result.Should().Be(expectedPath);
        }
    }
}
