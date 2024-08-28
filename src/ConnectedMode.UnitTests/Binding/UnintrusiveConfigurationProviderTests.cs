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

using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    [TestClass]
    public class UnintrusiveConfigurationProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<UnintrusiveConfigurationProvider, IConfigurationProvider>(
                MefTestHelpers.CreateExport<IUnintrusiveBindingPathProvider>(),
                MefTestHelpers.CreateExport<ISolutionBindingRepository>());
        }

        [TestMethod]
        public void GetConfig_NoConfig_ReturnsStandalone()
        {
            // Arrange
            var pathProvider = CreatePathProvider(null);
            var configRepository = new Mock<ISolutionBindingRepository>();
            var testSubject = CreateTestSubject(pathProvider, configRepository.Object);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeNull();
            actual.Mode.Should().Be(SonarLintMode.Standalone);
            configRepository.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetConfig_Bound_ReturnsExpectedConfig()
        {
            // Arrange
            var expectedProject = new BoundSonarQubeProject(new Uri("http://localhost"), "any project", null);

            var pathProvider = CreatePathProvider("c:\\users\\foo\\bindings\\xxx.config");
            var configReader = CreateRepo(expectedProject);
            var testSubject = CreateTestSubject(pathProvider, configReader.Object);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            CheckExpectedFileRead(configReader, "c:\\users\\foo\\bindings\\xxx.config");
            actual.Should().NotBeNull();
            actual.Project.Should().BeEquivalentTo(BoundServerProject.FromBoundSonarQubeProject(expectedProject));
            actual.Mode.Should().Be(SonarLintMode.Connected);
            actual.BindingConfigDirectory.Should().Be("c:\\users\\foo\\bindings");
        }

        [TestMethod]
        public void GetConfig_ConfigReaderReturnsNull_ReturnsStandalone()
        {
            // Arrange
            var pathProvider = CreatePathProvider("c:\\users\\foo\\bindings\\xxx.config");
            var configReader = CreateRepo(null);
            var testSubject = CreateTestSubject(pathProvider, configReader.Object);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            CheckExpectedFileRead(configReader, "c:\\users\\foo\\bindings\\xxx.config");
            actual.Should().BeSameAs(BindingConfiguration.Standalone);
        }

        private static UnintrusiveConfigurationProvider CreateTestSubject(IUnintrusiveBindingPathProvider pathProvider,
            ISolutionBindingRepository configRepo = null)
        {
            configRepo ??= Mock.Of<ISolutionBindingRepository>();
            return new UnintrusiveConfigurationProvider(pathProvider, configRepo);
        }

        private static IUnintrusiveBindingPathProvider CreatePathProvider(string pathToReturn)
        {
            var pathProvider = new Mock<IUnintrusiveBindingPathProvider>();
            pathProvider.Setup(x => x.GetCurrentBindingPath()).Returns(() => pathToReturn);
            return pathProvider.Object;
        }

        private static Mock<ISolutionBindingRepository> CreateRepo(BoundSonarQubeProject projectToReturn)
        {
            var repo = new Mock<ISolutionBindingRepository>();
            repo.Setup(x => x.Read(It.IsAny<string>())).Returns(projectToReturn);
            return repo;
        }

        private static void CheckExpectedFileRead(Mock<ISolutionBindingRepository> configReader, string expectedFilePath)
            => configReader.Verify(x => x.Read(expectedFilePath), Times.Once);
    }
}
