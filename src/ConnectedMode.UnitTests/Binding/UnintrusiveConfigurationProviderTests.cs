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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class UnintrusiveConfigurationProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<UnintrusiveConfigurationProvider, IConfigurationProvider>(
                MefTestHelpers.CreateExport<IUnintrusiveBindingPathProvider>(),
                MefTestHelpers.CreateExport<ISolutionBindingRepository>(),
                MefTestHelpers.CreateExport<ISolutionInfoProvider>());
        }
        
        [TestMethod]
        public void GetConfig_NoActiveSolution_ReturnsStandalone()
        {
            // Arrange
            var (pathProvider, solutionInfoProvider) = SetUpConfiguration(null,null);
            var configRepository = Substitute.For<ISolutionBindingRepository>();
            var testSubject = CreateTestSubject(pathProvider, solutionInfoProvider, configRepository);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeNull();
            actual.Mode.Should().Be(SonarLintMode.Standalone);
            configRepository.ReceivedCalls().Should().BeEmpty();
            pathProvider.ReceivedCalls().Should().BeEmpty();
            solutionInfoProvider.Received().GetSolutionName();
        }

        [TestMethod]
        public void GetConfig_NoConfig_ReturnsStandalone()
        {
            // Arrange
            var (pathProvider, solutionInfoProvider) = SetUpConfiguration("solution123",null);
            var configRepository = Substitute.For<ISolutionBindingRepository>();
            var testSubject = CreateTestSubject(pathProvider, solutionInfoProvider, configRepository);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            actual.Should().NotBeNull();
            actual.Project.Should().BeNull();
            actual.Mode.Should().Be(SonarLintMode.Standalone);
            configRepository.ReceivedCalls().Should().BeEmpty();
            solutionInfoProvider.Received().GetSolutionName();
            pathProvider.Received().GetBindingPath("solution123");
        }

        [TestMethod]
        public void GetConfig_Bound_ReturnsExpectedConfig()
        {
            // Arrange
            var expectedProject = new BoundServerProject("solution123", "project123", new ServerConnection.SonarCloud("org"));

            var (pathProvider, solutionInfoProvider) = SetUpConfiguration("solution123", "c:\\users\\foo\\bindings\\xxx.config");
            var bindingRepository = CreateRepo(expectedProject);
            var testSubject = CreateTestSubject(pathProvider, solutionInfoProvider, bindingRepository);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            CheckExpectedFileRead(bindingRepository, "c:\\users\\foo\\bindings\\xxx.config");
            actual.Should().NotBeNull();
            actual.Project.Should().BeEquivalentTo(expectedProject);
            actual.Mode.Should().Be(SonarLintMode.Connected);
            actual.BindingConfigDirectory.Should().Be("c:\\users\\foo\\bindings");
            Received.InOrder(() =>
            {
                solutionInfoProvider.GetSolutionName();
                pathProvider.GetBindingPath("solution123");
                bindingRepository.Read("c:\\users\\foo\\bindings\\xxx.config");
            });
        }

        [TestMethod]
        public void GetConfig_ConfigReaderReturnsNull_ReturnsStandalone()
        {
            // Arrange
            var expectedProject = new BoundServerProject("solution123", "project123", new ServerConnection.SonarCloud("org"));

            var (pathProvider, solutionInfoProvider) = SetUpConfiguration("solution123", "c:\\users\\foo\\bindings\\xxx.config");
            var bindingRepository = CreateRepo(null);
            var testSubject = CreateTestSubject(pathProvider, solutionInfoProvider, bindingRepository);

            // Act
            var actual = testSubject.GetConfiguration();

            // Assert
            CheckExpectedFileRead(bindingRepository, "c:\\users\\foo\\bindings\\xxx.config");
            actual.Should().BeSameAs(BindingConfiguration.Standalone);
        }

        private static UnintrusiveConfigurationProvider CreateTestSubject(IUnintrusiveBindingPathProvider pathProvider,
            ISolutionInfoProvider solutionInfoProvider = null,
            ISolutionBindingRepository configRepo = null) =>
            new(pathProvider,
                solutionInfoProvider ?? Substitute.For<ISolutionInfoProvider>(),
                configRepo ?? Substitute.For<ISolutionBindingRepository>());

        private static (IUnintrusiveBindingPathProvider, ISolutionInfoProvider) SetUpConfiguration(string localBindingKey, string pathToReturn)
        {
            var solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
            solutionInfoProvider.GetSolutionName().Returns(localBindingKey);
            var pathProvider = Substitute.For<IUnintrusiveBindingPathProvider>();
            pathProvider.GetBindingPath(localBindingKey).Returns(pathToReturn);
            return (pathProvider, solutionInfoProvider);
        }

        private static ISolutionBindingRepository CreateRepo(BoundServerProject projectToReturn)
        {
            var repo = Substitute.For<ISolutionBindingRepository>();
            repo.Read(Arg.Any<string>()).Returns(projectToReturn);
            return repo;
        }

        private static void CheckExpectedFileRead(ISolutionBindingRepository configReader, string expectedFilePath)
            => configReader.Received().Read(expectedFilePath);
    }
}
