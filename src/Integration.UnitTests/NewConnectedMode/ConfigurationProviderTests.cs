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
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ConfigurationProviderTests
    {
        private ConfigurableSolutionBindingSerializer legacySerializer;
        private ConfigurableSolutionBindingSerializer newSerializer;
        private ConfigurationProvider testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            legacySerializer = new ConfigurableSolutionBindingSerializer { CurrentBinding = null };
            newSerializer = new ConfigurableSolutionBindingSerializer { CurrentBinding = null };
            testSubject = new ConfigurationProvider(legacySerializer, newSerializer);
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullLegacySerializer_Throws()
        {
            // Arrange
            var serializer = new ConfigurableSolutionBindingSerializer();
            Action act = () => new ConfigurationProvider(null, serializer);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("legacySerializer");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullConnectedModeSerializer_Throws()
        {
            // Arrange
            var serializer = new ConfigurableSolutionBindingSerializer();
            Action act = () => new ConfigurationProvider(serializer, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("newConnectedModeSerializer");
        }

        [TestMethod]
        public void GetConfig_NoConfig_ReturnsStandalone()
        {
            // Arrange
            legacySerializer.CurrentBinding = null;
            newSerializer.CurrentBinding = null;

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
            legacySerializer.CurrentBinding = new BoundSonarQubeProject();
            newSerializer.CurrentBinding = new BoundSonarQubeProject();

            // Act
            var actual = testSubject.GetConfiguration();

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
            legacySerializer.CurrentBinding = null;
            newSerializer.CurrentBinding = new BoundSonarQubeProject();

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().NotBeNull();
            actual.Project.Should().BeSameAs(newSerializer.CurrentBinding);
            actual.Mode.Should().Be(SonarLintMode.Connected);
        }

        [TestMethod]
        public void WriteConfig_InvalidArg_Throws()
        {
            // Arrange
            legacySerializer.CurrentBinding = null;
            newSerializer.CurrentBinding = null;

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
        public void WriteConfig_LegacyConfig_CallsLegacySerializer()
        {
            // Arrange
            legacySerializer.WriteSolutionBindingAction = p => "filename.txt";

            var config = BindingConfiguration.CreateBoundConfiguration(new BoundSonarQubeProject(), isLegacy: true);

            // Act
            var actual = testSubject.WriteConfiguration(config);

            // Assert
            actual.Should().BeTrue();
            legacySerializer.WrittenFilesCount.Should().Be(1);
            newSerializer.WrittenFilesCount.Should().Be(0);
        }

        [TestMethod]
        public void WriteConfig_NewConnectedModeConfig_CallsNewSerializer()
        {
            // Arrange
            newSerializer.WriteSolutionBindingAction = p => "filename.txt";

            var config = BindingConfiguration.CreateBoundConfiguration(new BoundSonarQubeProject(), isLegacy: false);

            // Act
            var actual = testSubject.WriteConfiguration(config);

            // Assert
            actual.Should().BeTrue();
            legacySerializer.WrittenFilesCount.Should().Be(0);
            newSerializer.WrittenFilesCount.Should().Be(1);
        }
    }
}
