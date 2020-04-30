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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ConfigurationProviderTests
    {
        private Mock<ISolutionBindingPathProvider> legacyPathProvider;
        private Mock<ISolutionBindingPathProvider> newPathProvider;
        private Mock<ISolutionBindingSerializer> solutionBindingSerializer;
        private Mock<ISolutionBindingPostSaveOperation> legacyPostSaveOperation;
        private ConfigurationProvider testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            legacyPathProvider = new Mock<ISolutionBindingPathProvider>();
            newPathProvider = new Mock<ISolutionBindingPathProvider>();
            solutionBindingSerializer = new Mock<ISolutionBindingSerializer>();
            legacyPostSaveOperation = new Mock<ISolutionBindingPostSaveOperation>();

            testSubject = new ConfigurationProvider(legacyPathProvider.Object,
                newPathProvider.Object,
                solutionBindingSerializer.Object,
                legacyPostSaveOperation.Object);
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullLegacySerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(null, newPathProvider.Object, null, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("legacyPathProvider");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullConnectedModeSerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(legacyPathProvider.Object, null, null,null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("connectedModePathProvider");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullSolutionBindingSerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(legacyPathProvider.Object, newPathProvider.Object, null, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingSerializer");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullPostSaveAction_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(legacyPathProvider.Object, newPathProvider.Object, solutionBindingSerializer.Object, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("legacyPostSaveOperation");
        }

        [TestMethod]
        public void GetConfig_NoConfig_ReturnsStandalone()
        {
            // Arrange
            legacyPathProvider.Setup(x => x.Get()).Returns(null as string);
            newPathProvider.Setup(x => x.Get()).Returns(null as string);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeNull();
            actual.Mode.Should().Be(SonarLintMode.Standalone);
        }


        [TestMethod]
        public void GetConfig_NewConfigOnly_ReturnsConnected()
        {
            // Arrange
            legacyPathProvider.Setup(x => x.Get()).Returns(null as string);
            newPathProvider.Setup(x => x.Get()).Returns("new");

            var expectedProject = new BoundSonarQubeProject();
            solutionBindingSerializer.Setup(x => x.Read("new")).Returns(expectedProject);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().NotBeNull();
            actual.Project.Should().BeSameAs(expectedProject);
            actual.Mode.Should().Be(SonarLintMode.Connected);
        }

        [TestMethod]
        public void GetConfig_LegacyConfigOnly_ReturnsLegacy()
        {
            // Arrange
            legacyPathProvider.Setup(x => x.Get()).Returns("old");

            var expectedProject = new BoundSonarQubeProject();
            solutionBindingSerializer.Setup(x => x.Read("old")).Returns(expectedProject);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().NotBeNull();
            actual.Project.Should().BeSameAs(expectedProject);
            actual.Mode.Should().Be(SonarLintMode.LegacyConnected);
        }

        [TestMethod]
        public void GetConfig_NoLegacyProjectAtFileLocation_NoConnectedProjectAtFileLocation_ReturnsStandalone()
        {
            // Arrange
            legacyPathProvider.Setup(x => x.Get()).Returns("legacy");
            solutionBindingSerializer.Setup(x => x.Read("legacy")).Returns(null as BoundSonarQubeProject);

            newPathProvider.Setup(x => x.Get()).Returns("new");
            solutionBindingSerializer.Setup(x => x.Read("new")).Returns(null as BoundSonarQubeProject);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeNull();
            actual.Mode.Should().Be(SonarLintMode.Standalone);
        }

        [TestMethod]
        public void GetConfig_NoLegacyProjectAtFileLocation_ConnectedProjectAtFileLocation_ReturnsConnected()
        {
            // Arrange
            legacyPathProvider.Setup(x => x.Get()).Returns("legacy");
            solutionBindingSerializer.Setup(x => x.Read("legacy")).Returns(null as BoundSonarQubeProject);

            var expectedProject = new BoundSonarQubeProject();
            newPathProvider.Setup(x => x.Get()).Returns("new");
            solutionBindingSerializer.Setup(x => x.Read("new")).Returns(expectedProject);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeSameAs(expectedProject);
            actual.Mode.Should().Be(SonarLintMode.Connected);
        }

        [TestMethod]
        public void GetConfig_LegacyProjectAtFileLocation_ConnectedProjectAtFileLocation_ReturnsLegacy()
        {
            // Note that this should not happen in practice - we only expect the legacys
            // or new bindings to present. However, the legacy should take priority.

            // Arrange
            legacyPathProvider.Setup(x => x.Get()).Returns("legacy");
            newPathProvider.Setup(x => x.Get()).Returns("new");

            var expectedProject = new BoundSonarQubeProject();
            solutionBindingSerializer.Setup(x => x.Read("legacy")).Returns(expectedProject);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeSameAs(expectedProject);
            actual.Mode.Should().Be(SonarLintMode.LegacyConnected);
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
            var config = BindingConfiguration.CreateBoundConfiguration(new BoundSonarQubeProject(), SonarLintMode.LegacyConnected);
            legacyPathProvider.Setup(x => x.Get()).Returns("old.txt");

            solutionBindingSerializer
                .Setup(x => x.Write("old.txt", config.Project, legacyPostSaveOperation.Object.OnSuccessfulSave))
                .Returns(true);

            // Act
            var actual = testSubject.WriteConfiguration(config);

            // Assert
            actual.Should().BeTrue();

            solutionBindingSerializer.Verify(x =>
                    x.Write("old.txt", config.Project, legacyPostSaveOperation.Object.OnSuccessfulSave),
                Times.Once);
        }

        [TestMethod]
        public void WriteConfig_NewConnectedModeConfig_SaveNewConfig()
        {
            var projectToWrite = new BoundSonarQubeProject();
            var config = BindingConfiguration.CreateBoundConfiguration(projectToWrite, SonarLintMode.Connected);
            newPathProvider.Setup(x => x.Get()).Returns("new.txt");

            solutionBindingSerializer
                .Setup(x => x.Write("new.txt", config.Project, It.Is<Predicate<string>>(s => true)))
                .Returns(true);

            // Act
            var actual = testSubject.WriteConfiguration(config);

            // Assert
            actual.Should().BeTrue();

            solutionBindingSerializer.Verify(x =>
                    x.Write("new.txt", config.Project, It.Is<Predicate<string>>(s => true)),
                Times.Once);
        }
    }
}
