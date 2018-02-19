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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class InMemoryConfigurationProviderTests
    {
        [TestMethod]
        public void ReadAndWrite()
        {
            // This test changes a static variable.
            // Save and reset the value to avoid unintended side-effects.
            var previous = InMemoryConfigurationProvider.Instance.GetConfiguration();

            try
            {
                // Arrange
                var testSubject = InMemoryConfigurationProvider.Instance;
                var config = BindingConfiguration.CreateBoundConfiguration(new BoundSonarQubeProject(), isLegacy: false);

                // 1. Write then read
                testSubject.WriteConfiguration(config);
                var read = testSubject.GetConfiguration();

                // Assert
                read.Should().NotBeNull();
                read.Project.Should().Be(config.Project);
                read.Mode.Should().Be(SonarLintMode.Connected);

                // 2. Delete then read
                testSubject.DeleteConfiguration();
                read = testSubject.GetConfiguration();

                // Assert
                read.Should().NotBeNull();
                read.Project.Should().BeNull();
                read.Mode.Should().Be(SonarLintMode.Standalone);
            }

            finally
            {
                InMemoryConfigurationProvider.Instance.WriteConfiguration(previous);
            }
        }

        [TestMethod]
        public void Singleton()
        {
            // Arrange
            var instance1 = InMemoryConfigurationProvider.Instance;
            var instance2 = InMemoryConfigurationProvider.Instance;

            // Act & Assert
            object.ReferenceEquals(instance1, instance2).Should().BeTrue();
        }
    }
}
