/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator.Locators;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.NodeJSLocator.Locators
{
    [TestClass]
    public class EnvironmentVariableNodeLocatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<EnvironmentVariableNodeLocator, IEnvironmentVariableNodeLocator>(null, new[]
            {
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public void Locate_EnvironmentVariableDoesNotExist_Null()
        {
            var fileSystem = new Mock<IFileSystem>();
            var envSettings = SetupEnvironmentSettings(null);

            var testSubject = CreateTestSubject(fileSystem.Object, envSettings);
            var result = testSubject.Locate();

            result.Should().BeNull();
            fileSystem.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Locate_EnvironmentVariableHasValue_FileDoesNotExist_Null()
        {
            const string filePath = "test";
            var envSettings = SetupEnvironmentSettings(filePath);
            var fileSystem = SetupFileSystem(filePath, false);

            var testSubject = CreateTestSubject(fileSystem.Object, envSettings);
            var result = testSubject.Locate();

            result.Should().BeNull();
            fileSystem.VerifyAll();
        }

        [TestMethod]
        public void Locate_EnvironmentVariableHasValue_FileExists_FilePath()
        {
            const string filePath = "test";
            var envSettings = SetupEnvironmentSettings(filePath);
            var fileSystem = SetupFileSystem(filePath, true);

            var testSubject = CreateTestSubject(fileSystem.Object, envSettings);
            var result = testSubject.Locate();

            result.Should().Be(filePath);
            fileSystem.VerifyAll();
        }

        private static Mock<IFileSystem> SetupFileSystem(string filePath, bool fileExists)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(filePath)).Returns(fileExists);

            return fileSystem;
        }

        private IEnvironmentSettings SetupEnvironmentSettings(string nodeExePath)
        {
            var settings = new Mock<IEnvironmentSettings>();
            settings.Setup(x => x.NodeJsExeFilePath()).Returns(nodeExePath);

            return settings.Object;
        }

        private EnvironmentVariableNodeLocator CreateTestSubject(IFileSystem fileSystem, IEnvironmentSettings environmentSettings)
        {
            return new EnvironmentVariableNodeLocator(fileSystem, environmentSettings, Mock.Of<ILogger>());
        }
    }
}
