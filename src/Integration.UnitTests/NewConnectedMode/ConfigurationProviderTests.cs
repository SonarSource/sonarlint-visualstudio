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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ConfigurationProviderTests
    {
        private Mock<ISolutionBindingPathProvider> legacySerializer;
        private Mock<ISolutionBindingPathProvider> newSerializer;
        private Mock<ISolutionBindingFile> solutionBindingSerializer;
        private ConfigurationProvider testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            legacySerializer = new Mock<ISolutionBindingPathProvider>();
            newSerializer = new Mock<ISolutionBindingPathProvider>();
            solutionBindingSerializer = new Mock<ISolutionBindingFile>();

            testSubject = new ConfigurationProvider(legacySerializer.Object,
                newSerializer.Object,
                solutionBindingSerializer.Object,
                null);
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullLegacySerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(null, newSerializer.Object, null, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("legacyPathProvider");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullConnectedModeSerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(legacySerializer.Object, null, null,null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("connectedModePathProvider");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullSolutionBindingSerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(legacySerializer.Object, newSerializer.Object, null, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingFile");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullPostSaveAction_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(legacySerializer.Object, newSerializer.Object, solutionBindingSerializer.Object, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("legacyPostSaveOperation");
        }

        [TestMethod]
        public void GetConfig_NoConfig_ReturnsStandalone()
        {
            // Arrange
            legacySerializer.Setup(x => x.Get()).Returns(null as string);
            newSerializer.Setup(x => x.Get()).Returns(null as string);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeNull();
            actual.Mode.Should().Be(SonarLintMode.Standalone);
        }

        [TestMethod]
        public void GetConfig_LegacyAndNewConfig_ReturnsLegacy()
        {
            // Note that this should not happen in practice - we only expect the legacys
            // or new bindings to present. However, the legacy should take priority.

            // Arrange
            legacySerializer.Setup(x => x.Get()).Returns("legacy");
            newSerializer.Setup(x => x.Get()).Returns("new");

            var expectedProject = new BoundSonarQubeProject();
            solutionBindingSerializer.Setup(x => x.ReadSolutionBinding("legacy")).Returns(expectedProject);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().NotBeNull();
            actual.Project.Should().BeSameAs(expectedProject);
            actual.Mode.Should().Be(SonarLintMode.LegacyConnected);
        }

        [TestMethod]
        public void GetConfig_NewConfigOnly_ReturnsLegacy()
        {
            // Arrange
            legacySerializer.Setup(x => x.Get()).Returns(null as string);
            newSerializer.Setup(x => x.Get()).Returns("new");

            var expectedProject = new BoundSonarQubeProject();
            solutionBindingSerializer.Setup(x => x.ReadSolutionBinding("new")).Returns(expectedProject);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().NotBeNull();
            actual.Project.Should().BeSameAs(expectedProject);
            actual.Mode.Should().Be(SonarLintMode.Connected);
        }

        [TestMethod]
        public void WriteConfig_InvalidArg_Throws()
        {
            // Act
            Action act = () => testSubject.WriteConfiguration(null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configuration");
        }

        [TestMethod]
        public void WriteConfig_StandaloneConfig_Throws()
        {
            // Act
            Action act = () => testSubject.WriteConfiguration(BindingConfiguration.Standalone);

            // Assert
            act.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public void WriteConfig_LegacyConfig_SavesLegacyConfig()
        {
            // Arrange
            var projectToWrite = new BoundSonarQubeProject();

            legacySerializer.Setup(x => x.Get()).Returns("old.txt");
            solutionBindingSerializer.Setup(x => x.WriteSolutionBinding("old.txt", projectToWrite, null)).Returns(true);

            var config = BindingConfiguration.CreateBoundConfiguration(projectToWrite, SonarLintMode.LegacyConnected);

            // Act
            var actual = testSubject.WriteConfiguration(config);

            // Assert
            actual.Should().BeTrue();

            solutionBindingSerializer.Verify(x => x.WriteSolutionBinding("old.txt", projectToWrite, null));
        }

        [TestMethod]
        public void WriteConfig_NewConnectedModeConfig_CallsNewSerializer()
        {
            var projectToWrite = new BoundSonarQubeProject();

            legacySerializer.Setup(x => x.Get()).Returns(null as string);
            newSerializer.Setup(x => x.Get()).Returns("new.txt");
            solutionBindingSerializer.Setup(x => x.WriteSolutionBinding("new.txt", projectToWrite, null)).Returns(true);

            var config = BindingConfiguration.CreateBoundConfiguration(projectToWrite, SonarLintMode.Connected);

            // Act
            var actual = testSubject.WriteConfiguration(config);

            // Assert
            actual.Should().BeTrue();

            solutionBindingSerializer.Verify(x => x.WriteSolutionBinding("new.txt", projectToWrite, null));
        }
    }
}
