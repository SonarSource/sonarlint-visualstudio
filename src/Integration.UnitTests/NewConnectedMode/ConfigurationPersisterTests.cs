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
    public class ConfigurationPersisterTests
    {
        private Mock<ISolutionBindingPathProvider> legacyPathProvider;
        private Mock<ISolutionBindingPathProvider> newPathProvider;
        private Mock<ISolutionBindingDataWriter> solutionBindingDataWriter;
        private Mock<ILegacyConfigFolderItemAdder> legacyItemAdderMock;
        private ConfigurationPersister testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            legacyPathProvider = new Mock<ISolutionBindingPathProvider>();
            newPathProvider = new Mock<ISolutionBindingPathProvider>();
            solutionBindingDataWriter = new Mock<ISolutionBindingDataWriter>();
            legacyItemAdderMock = new Mock<ILegacyConfigFolderItemAdder>();

            testSubject = new ConfigurationPersister(legacyPathProvider.Object,
                newPathProvider.Object,
                solutionBindingDataWriter.Object,
                legacyItemAdderMock.Object);
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullLegacySerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationPersister(null, newPathProvider.Object, null, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("legacyPathProvider");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullConnectedModeSerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationPersister(legacyPathProvider.Object, null, null, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("connectedModePathProvider");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullSolutionBindingSerializer_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationPersister(legacyPathProvider.Object, newPathProvider.Object, null, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingDataWriter");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullPostSaveAction_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationPersister(legacyPathProvider.Object, newPathProvider.Object, solutionBindingDataWriter.Object, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("legacyConfigFolderItemAdder");
        }

        [TestMethod]
        public void Persist_NullProject_Throws()
        {
            // Act
            Action act = () => testSubject.Persist(null, SonarLintMode.Connected);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("project");
        }

        [TestMethod]
        public void Persist_StandaloneMode_Throws()
        {
            // Act
            Action act = () => testSubject.Persist(new BoundSonarQubeProject(), SonarLintMode.Standalone);

            // Assert
            act.Should().ThrowExactly<InvalidOperationException>();
        }

        [TestMethod]
        public void Persist_LegacyConfig_SavesLegacyConfig()
        {
            // Arrange
            var project = new BoundSonarQubeProject();
            legacyPathProvider.Setup(x => x.Get()).Returns("c:\\old.txt");

            solutionBindingDataWriter
                .Setup(x => x.Write("c:\\old.txt", project, legacyItemAdderMock.Object.AddToFolder))
                .Returns(true);

            // Act
            var actual = testSubject.Persist(project, SonarLintMode.LegacyConnected);

            // Assert
            actual.Should().NotBe(null);

            solutionBindingDataWriter.Verify(x =>
                    x.Write("c:\\old.txt", project, legacyItemAdderMock.Object.AddToFolder),
                Times.Once);
        }

        [TestMethod]
        public void Persist_NewConnectedModeConfig_SaveNewConfig()
        {
            var projectToWrite = new BoundSonarQubeProject();
            newPathProvider.Setup(x => x.Get()).Returns("c:\\new.txt");

            solutionBindingDataWriter
                .Setup(x => x.Write("c:\\new.txt", projectToWrite, null))
                .Returns(true);

            // Act
            var actual = testSubject.Persist(projectToWrite, SonarLintMode.Connected);

            // Assert
            actual.Should().NotBe(null);

            solutionBindingDataWriter.Verify(x =>
                    x.Write("c:\\new.txt", projectToWrite, null),
                Times.Once);
        }
    }
}
