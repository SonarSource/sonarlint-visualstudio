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

using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class ObsoleteConfigurationProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<ObsoleteConfigurationProvider, IObsoleteConfigurationProvider>(
                MefTestHelpers.CreateExport<ISolutionBindingDataReader>(),
                MefTestHelpers.CreateExport<ISolutionInfoProvider>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<ObsoleteConfigurationProvider>();

        [TestMethod]
        public void Ctor_DoesNotCallServices()
        {
            // The constructor should be free-threaded i.e. run entirely on the calling thread
            // -> should not call services that swtich threads
            var legacyProvider = new Mock<ISolutionBindingPathProvider>();
            var connectedProvider = new Mock<ISolutionBindingPathProvider>();
            var slnDataReader = new Mock<ISolutionBindingDataReader>();

            _ = CreateTestSubject(legacyProvider.Object, connectedProvider.Object, slnDataReader.Object);

            legacyProvider.Invocations.Should().BeEmpty();
            connectedProvider.Invocations.Should().BeEmpty();
            slnDataReader.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetConfig_NoConfig_ReturnsStandalone()
        {
            // Arrange
            var legacyPathProvider = CreatePathProvider(null);
            var newPathProvider = CreatePathProvider(null);

            var testSubject = CreateTestSubject(legacyPathProvider, newPathProvider);

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
            var legacyPathProvider = CreatePathProvider(null);
            var newPathProvider = CreatePathProvider("c:\\new");
            
            var expectedProject = new BoundSonarQubeProject();
            var reader = CreateReader("c:\\new", expectedProject);

            var testSubject = CreateTestSubject(legacyPathProvider, newPathProvider, reader);

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
            var legacyPathProvider = CreatePathProvider("c:\\old");

            var expectedProject = new BoundSonarQubeProject();
            var reader = CreateReader("c:\\old", expectedProject);

            var testSubject = CreateTestSubject(legacyPathProvider, null, reader);

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
            var legacyPathProvider = CreatePathProvider("c:\\legacy");
            var newPathProvider = CreatePathProvider("c:\\new");

            var reader = new Mock<ISolutionBindingDataReader>();
            reader.Setup(x => x.Read("c:\\legacy")).Returns(null as BoundSonarQubeProject);
            reader.Setup(x => x.Read("c:\\new")).Returns(null as BoundSonarQubeProject);

            var testSubject = CreateTestSubject(legacyPathProvider, newPathProvider, reader.Object);

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
            var legacyPathProvider = CreatePathProvider("c:\\legacy");
            var newPathProvider = CreatePathProvider("c:\\new");

            var reader = new Mock<ISolutionBindingDataReader>();
            var expectedProject = new BoundSonarQubeProject();
            reader.Setup(x => x.Read("c:\\legacy")).Returns(null as BoundSonarQubeProject);
            reader.Setup(x => x.Read("c:\\new")).Returns(expectedProject);

            var testSubject = CreateTestSubject(legacyPathProvider, newPathProvider, reader.Object);

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
            var legacyPathProvider = CreatePathProvider("c:\\legacy");
            var newPathProvider = CreatePathProvider("c:\\new");

            var reader = new Mock<ISolutionBindingDataReader>();
            var legacyProject = new BoundSonarQubeProject();
            var newProject = new BoundSonarQubeProject();
            reader.Setup(x => x.Read("c:\\legacy")).Returns(legacyProject);
            reader.Setup(x => x.Read("c:\\new")).Returns(newProject);

            var testSubject = CreateTestSubject(legacyPathProvider, newPathProvider, reader.Object);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeSameAs(legacyProject);
            actual.Mode.Should().Be(SonarLintMode.LegacyConnected);
        }

        private static ISolutionBindingPathProvider CreatePathProvider(string pathToReturn)
        {
            var provider = new Mock<ISolutionBindingPathProvider>();
            provider.Setup(x => x.Get()).Returns((string)pathToReturn);
            return provider.Object;
        }

        private static ISolutionBindingDataReader CreateReader(string inputPath, BoundSonarQubeProject projectToReturn)
        {
            var reader = new Mock<ISolutionBindingDataReader>();
            reader.Setup(x => x.Read(inputPath)).Returns(projectToReturn);
            return reader.Object;
        }

        private static ObsoleteConfigurationProvider CreateTestSubject(ISolutionBindingPathProvider legacyProvider = null,
            ISolutionBindingPathProvider connectedModePathProvider = null,
            ISolutionBindingDataReader slnDataReader = null)
        {
            var testSubject = new ObsoleteConfigurationProvider(
                legacyProvider ?? Mock.Of<ISolutionBindingPathProvider>(),
                connectedModePathProvider ?? Mock.Of<ISolutionBindingPathProvider>(),
                slnDataReader ?? Mock.Of<ISolutionBindingDataReader>());
            return testSubject;
        }
    }
}
