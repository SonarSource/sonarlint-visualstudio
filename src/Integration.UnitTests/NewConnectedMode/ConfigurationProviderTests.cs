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
    public class ConfigurationProviderTests
    {
        [TestMethod]
        public void Ctor_InvalidArgs_NullLegacySerializer_Throws()
        {
            // Arrange
            var serializer = new ConfigurableSolutionBindingSerializer();
            Action act = () => new ConfigurationProvider(null, serializer);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("legacySerializer");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullConnectedModeSerializer_Throws()
        {
            // Arrange
            var serializer = new ConfigurableSolutionBindingSerializer();
            Action act = () => new ConfigurationProvider(serializer, null);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("newConnectedModeSerializer");
        }

        [TestMethod]
        public void GetConfig_NoConfig_ReturnsStandalone()
        {
            // Arrange
            var legacySerializer = new ConfigurableSolutionBindingSerializer { CurrentBinding = null };
            var newSerializer = new ConfigurableSolutionBindingSerializer { CurrentBinding = null };
            var configProvider = new ConfigurationProvider(legacySerializer, newSerializer);

            // Act
            var actual = configProvider.GetConfiguration();

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
            var legacySerializer = new ConfigurableSolutionBindingSerializer { CurrentBinding = new BoundSonarQubeProject() };
            var newSerializer = new ConfigurableSolutionBindingSerializer { CurrentBinding = new BoundSonarQubeProject() };
            var configProvider = new ConfigurationProvider(legacySerializer, newSerializer);

            // Act
            var actual = configProvider.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().NotBeNull();
            actual.Project.Should().BeSameAs(legacySerializer.CurrentBinding);
            actual.Mode.Should().Be(SonarLintMode.LegacyConnected);
        }

        [TestMethod]
        public void GetConfig_NewConfigOnly_ReturnsLegacy()
        {
            // Arrange
            var legacySerializer = new ConfigurableSolutionBindingSerializer { CurrentBinding = null };
            var newSerializer = new ConfigurableSolutionBindingSerializer { CurrentBinding = new BoundSonarQubeProject() };
            var configProvider = new ConfigurationProvider(legacySerializer, newSerializer);

            // Act
            var actual = configProvider.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().NotBeNull();
            actual.Project.Should().BeSameAs(newSerializer.CurrentBinding);
            actual.Mode.Should().Be(SonarLintMode.Connected);
        }
    }
}
