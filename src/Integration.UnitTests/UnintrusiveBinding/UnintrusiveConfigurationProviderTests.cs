/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.UnintrusiveBinding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.UnintrusiveBinding
{
    [TestClass]
    public class UnintrusiveConfigurationProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<UnintrusiveConfigurationProvider, IConfigurationProvider>(
                MefTestHelpers.CreateExport<IUnintrusiveBindingPathProvider>(),
                MefTestHelpers.CreateExport<ISolutionBindingDataReader>());
        }

        [TestMethod]
        public void GetConfig_NoConfig_ReturnsStandalone()
        {
            // Arrange
            var pathProvider = CreatePathProvider(null);
            var configReader = new Mock<ISolutionBindingDataReader>();
            var testSubject = CreateTestSubject(pathProvider, configReader.Object);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeNull();
            actual.Mode.Should().Be(SonarLintMode.Standalone);
            configReader.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetConfig_Bound_ReturnsExpectedConfig()
        {
            // Arrange
            var expectedProject = new BoundSonarQubeProject();

            var pathProvider = CreatePathProvider("c:\\users\\foo\\bindings\\xxx.config");
            var configReader = CreateReader(expectedProject);
            var testSubject = CreateTestSubject(pathProvider, configReader.Object);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            CheckExpectedFileRead(configReader, "c:\\users\\foo\\bindings\\xxx.config");
            actual.Should().NotBeNull();
            actual.Project.Should().BeSameAs(expectedProject);
            actual.Mode.Should().Be(SonarLintMode.Connected);
            actual.BindingConfigDirectory.Should().Be("c:\\users\\foo\\bindings");
        }

        [TestMethod]
        public void GetConfig_ConfigReaderReturnsNull_ReturnsStandalone()
        {
            // Arrange
            var pathProvider = CreatePathProvider("c:\\users\\foo\\bindings\\xxx.config");
            var configReader = CreateReader(null);
            var testSubject = CreateTestSubject(pathProvider, configReader.Object);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            CheckExpectedFileRead(configReader, "c:\\users\\foo\\bindings\\xxx.config");
            actual.Should().BeSameAs(BindingConfiguration.Standalone);
        }

        private static UnintrusiveConfigurationProvider CreateTestSubject(IUnintrusiveBindingPathProvider pathProvider,
            ISolutionBindingDataReader configReader = null)
        {
            configReader ??= Mock.Of<ISolutionBindingDataReader>();
            return new UnintrusiveConfigurationProvider(pathProvider, configReader);
        }

        private static IUnintrusiveBindingPathProvider CreatePathProvider(string pathToReturn)
        {
            var pathProvider = new Mock<IUnintrusiveBindingPathProvider>();
            pathProvider.Setup(x => x.Get()).Returns(() => pathToReturn);
            return pathProvider.Object;
        }

        private static Mock<ISolutionBindingDataReader> CreateReader(BoundSonarQubeProject projectToReturn)
        {
            var reader = new Mock<ISolutionBindingDataReader>();
            reader.Setup(x => x.Read(It.IsAny<string>())).Returns(projectToReturn);
            return reader;
        }

        private static void CheckExpectedFileRead(Mock<ISolutionBindingDataReader> configReader, string expectedFilePath)
            => configReader.Verify(x => x.Read(expectedFilePath), Times.Once);
    }
}
