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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableManager;
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
        private Mock<ISolutionBindingDataReader> solutionBindingDataReader;
        private ConfigurationProvider testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            legacyPathProvider = new Mock<ISolutionBindingPathProvider>();
            newPathProvider = new Mock<ISolutionBindingPathProvider>();
            solutionBindingDataReader = new Mock<ISolutionBindingDataReader>();

            testSubject = new ConfigurationProvider(legacyPathProvider.Object,
                newPathProvider.Object,
                solutionBindingDataReader.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var exports = new[]
            {
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>()),
                MefTestHelpers.CreateExport<ICredentialStoreService>(Mock.Of<ICredentialStoreService>()),
                MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>()),
            };

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<ConfigurationProvider, IConfigurationProvider>(null, exports);
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullLegacySerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(null, newPathProvider.Object, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("legacyPathProvider");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullConnectedModeSerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(legacyPathProvider.Object, null, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("connectedModePathProvider");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullSolutionBindingSerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationProvider(legacyPathProvider.Object, newPathProvider.Object, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingDataReader");
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
            newPathProvider.Setup(x => x.Get()).Returns("c:\\new");

            var expectedProject = new BoundSonarQubeProject();
            solutionBindingDataReader.Setup(x => x.Read("c:\\new")).Returns(expectedProject);

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
            legacyPathProvider.Setup(x => x.Get()).Returns("c:\\old");

            var expectedProject = new BoundSonarQubeProject();
            solutionBindingDataReader.Setup(x => x.Read("c:\\old")).Returns(expectedProject);

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
            legacyPathProvider.Setup(x => x.Get()).Returns("c:\\legacy");
            solutionBindingDataReader.Setup(x => x.Read("c:\\legacy")).Returns(null as BoundSonarQubeProject);

            newPathProvider.Setup(x => x.Get()).Returns("c:\\new");
            solutionBindingDataReader.Setup(x => x.Read("c:\\new")).Returns(null as BoundSonarQubeProject);

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
            legacyPathProvider.Setup(x => x.Get()).Returns("c:\\legacy");
            solutionBindingDataReader.Setup(x => x.Read("c:\\legacy")).Returns(null as BoundSonarQubeProject);

            var expectedProject = new BoundSonarQubeProject();
            newPathProvider.Setup(x => x.Get()).Returns("c:\\new");
            solutionBindingDataReader.Setup(x => x.Read("c:\\new")).Returns(expectedProject);

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
            legacyPathProvider.Setup(x => x.Get()).Returns("c:\\legacy");
            newPathProvider.Setup(x => x.Get()).Returns("c:\\new");

            var expectedProject = new BoundSonarQubeProject();
            solutionBindingDataReader.Setup(x => x.Read("c:\\legacy")).Returns(expectedProject);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeSameAs(expectedProject);
            actual.Mode.Should().Be(SonarLintMode.LegacyConnected);
        }
    }
}
