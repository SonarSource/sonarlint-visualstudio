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

using System;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence
{
    [TestClass]
    public class ConfigurationPersisterTests
    {
        private Mock<IUnintrusiveBindingPathProvider> configFilePathProvider;
        private Mock<ISolutionBindingRepository> solutionBindingRepository;
        private ConfigurationPersister testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            configFilePathProvider = new Mock<IUnintrusiveBindingPathProvider>();
            solutionBindingRepository = new Mock<ISolutionBindingRepository>();

            testSubject = new ConfigurationPersister(
                configFilePathProvider.Object,
                solutionBindingRepository.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ConfigurationPersister, IConfigurationPersister>(
                MefTestHelpers.CreateExport<IUnintrusiveBindingPathProvider>(),
                MefTestHelpers.CreateExport<ISolutionBindingRepository>());
        }

        [TestMethod]
        public void Persist_NullProject_Throws()
        {
            // Act
            Action act = () => testSubject.Persist(null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [TestMethod]
        public void Persist_SaveNewConfig()
        {
            var projectToWrite = new BoundSonarQubeProject();
            configFilePathProvider.Setup(x => x.GetCurrentBindingPath()).Returns("c:\\new.txt");

            solutionBindingRepository
                .Setup(x => x.Write("c:\\new.txt", projectToWrite))
                .Returns(true);

            // Act
            var actual = testSubject.Persist(projectToWrite);

            // Assert
            actual.Should().NotBe(null);

            solutionBindingRepository.Verify(x =>
                    x.Write("c:\\new.txt", projectToWrite),
                Times.Once);
        }
    }
}
