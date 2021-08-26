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

using System;
using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class CompilationDatabaseLocatorTests
    {
        private const string RootDirectory = "dummy root";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CompilationDatabaseLocator, ICompilationDatabaseLocator>(null, new[]
            {
                MefTestHelpers.CreateExport<IFolderWorkspaceService>(Mock.Of<IFolderWorkspaceService>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Locate_NoRootDirectory_Null(string rootDirectory)
        {
            var logger = new TestLogger();
            var testSubject = CreateTestSubject(rootDirectory, logger: logger);

            var result = testSubject.Locate();

            result.Should().BeNull();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Locate_NoCMakeSettingsFile_ReturnsDefaultLocationIfFileExists(bool fileExists)
        {
            var activeConfiguration = "any";
            var configProvider = CreateConfigProvider(activeConfiguration);
            var defaultLocation = GetDefaultDatabaseFileLocation(activeConfiguration);
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(cmakeSettingsLocation)).Returns(false);

            fileSystem.Setup(x => x.File.Exists(defaultLocation)).Returns(fileExists);

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object, logger);

            var result = testSubject.Locate();

            if (fileExists)
            {
                result.Should().Be(defaultLocation);
            }
            else
            {
                result.Should().BeNull();
            }

            fileSystem.Verify(x=> x.File.Exists(cmakeSettingsLocation), Times.Once);
        }

        [TestMethod]
        public void Locate_FailedToReadCMakeSettings_NonCriticalException_Null()
        {
            var configProvider = CreateConfigProvider("my config");
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(cmakeSettingsLocation)).Returns(true);
            fileSystem
                .Setup(x => x.File.ReadAllText(cmakeSettingsLocation))
                .Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object, logger);

            var result = testSubject.Locate();
            result.Should().BeNull();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void Locate_FailedToParseCMakeSettings_Null()
        {
            var configProvider = CreateConfigProvider("my config");
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(cmakeSettingsLocation)).Returns(true);
            fileSystem
                .Setup(x => x.File.ReadAllText(cmakeSettingsLocation))
                .Returns("invalid json");

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object, logger);

            var result = testSubject.Locate();
            result.Should().BeNull();

            logger.AssertPartialOutputStringExists("JsonReaderException");
        }

        [TestMethod]
        public void Locate_FailedToReadCMakeSettings_CriticalException_ExceptionThrown()
        {
            var configProvider = CreateConfigProvider("my config");
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(cmakeSettingsLocation)).Returns(true);
            fileSystem
                .Setup(x => x.File.ReadAllText(cmakeSettingsLocation))
                .Throws(new StackOverflowException());

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object, logger);

            Action act =() => testSubject.Locate();

            act.Should().Throw<StackOverflowException>();
        }

        [TestMethod]
        public void Locate_HasCMakeSettingsFile_ActiveConfigurationDoesNotExist_Null()
        {
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);
            var configProvider = CreateConfigProvider("my-config");

            var fileSystem = new Mock<IFileSystem>();
            SetupCMakeSettingsFileExists(fileSystem, cmakeSettingsLocation, new CMakeSettings());

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object, logger);

            var result = testSubject.Locate();

            result.Should().BeNull();
            logger.AssertOutputStringExists(string.Format(Resources.NoBuildConfigInCMakeSettings,
                "my-config", 
                CompilationDatabaseLocator.CMakeSettingsFileName));

            fileSystem.Verify(x=> x.File.ReadAllText(cmakeSettingsLocation), Times.Once);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Locate_HasCMakeSettingsFile_ReturnsConfiguredPathIfItExists(bool fileExists)
        {
            var configProvider = CreateConfigProvider("my-config");
            var cmakeSettings = CreateCMakeSettings("my-config", "folder");
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>();
            SetupCMakeSettingsFileExists(fileSystem, cmakeSettingsLocation, cmakeSettings);

            var compilationDatabaseFullLocation = Path.GetFullPath(
                Path.Combine("folder", CompilationDatabaseLocator.CompilationDatabaseFileName));
            fileSystem.Setup(x => x.File.Exists(compilationDatabaseFullLocation)).Returns(fileExists);

            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object);

            var result = testSubject.Locate();

            if (fileExists)
            {
                result.Should().Be(compilationDatabaseFullLocation);
            }
            else
            {
                result.Should().BeNull();
            }
        }

        [TestMethod]
        [DataRow("c:\\absolute\\", "c:\\absolute\\")]
        [DataRow("relative", "relative")]
        [DataRow("c:\\absolute\\${name}\\${projectDir}", "c:\\absolute\\my-config\\dummy root")]
        [DataRow("c:\\${projectDir}\\somefolder\\${name}\\sub", "c:\\dummy root\\somefolder\\my-config\\sub")]
        [DataRow("${projectDir}\\somefolder\\${name}\\sub", "dummy root\\somefolder\\my-config\\sub")]
        public void Locate_HasCMakeSettingsFile_ConfiguredPathHasParameters_ParametersReplaced(string configuredPath, string expectedPath)
        {
            var configProvider = CreateConfigProvider("my-config");
            var cmakeSettings = CreateCMakeSettings("my-config", configuredPath);
            var cmakeSettingsLocation = Path.GetFullPath(Path.Combine(RootDirectory, CompilationDatabaseLocator.CMakeSettingsFileName));

            var fileSystem = new Mock<IFileSystem>();
            SetupCMakeSettingsFileExists(fileSystem, cmakeSettingsLocation, cmakeSettings);

            var compilationDatabaseFullLocation = Path.GetFullPath(Path.Combine(expectedPath, CompilationDatabaseLocator.CompilationDatabaseFileName));
            fileSystem.Setup(x => x.File.Exists(compilationDatabaseFullLocation)).Returns(true);

            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object);

            var result = testSubject.Locate();

            result.Should().Be(compilationDatabaseFullLocation);
        }

        private static CMakeSettings CreateCMakeSettings(string activeConfigurationName, string buildRoot) =>
            new CMakeSettings
            {
                Configurations = new[]
                {
                    new CMakeBuildConfiguration
                    {
                        Name = activeConfigurationName,
                        BuildRoot = buildRoot
                    }
                }
            };

        private static string GetCmakeSettingsLocation(string rootDirectory) => 
            Path.GetFullPath(Path.Combine(rootDirectory, CompilationDatabaseLocator.CMakeSettingsFileName));

        private static string GetDefaultDatabaseFileLocation(string activeBuildConfiguration) =>
            Path.GetFullPath(Path.Combine(
                string.Format(CompilationDatabaseLocator.DefaultLocationFormat,
                    RootDirectory,
                    activeBuildConfiguration),
                CompilationDatabaseLocator.CompilationDatabaseFileName));

        private void SetupCMakeSettingsFileExists(Mock<IFileSystem> fileSystem, string cmakeSettingsLocation, CMakeSettings cmakeSettings)
        {
            fileSystem.Setup(x => x.File.Exists(cmakeSettingsLocation)).Returns(true);
            fileSystem.Setup(x => x.File.ReadAllText(cmakeSettingsLocation)).Returns(JsonConvert.SerializeObject(cmakeSettings));
        }
        private CompilationDatabaseLocator CreateTestSubject(string rootDirectory, 
            IBuildConfigProvider buildConfigProvider = null,
            IFileSystem fileSystem = null, 
            ILogger logger = null)
        {
            var folderWorkspaceService = new Mock<IFolderWorkspaceService>();
            folderWorkspaceService.Setup(x => x.FindRootDirectory()).Returns(rootDirectory);

            buildConfigProvider ??= Mock.Of<IBuildConfigProvider>();
            logger ??= Mock.Of<ILogger>();

            return new CompilationDatabaseLocator(folderWorkspaceService.Object, buildConfigProvider, fileSystem, logger);
        }

        private IBuildConfigProvider CreateConfigProvider(string activeConfiguration)
        {
            var provider = new Mock<IBuildConfigProvider>();
            provider.Setup(x => x.GetActiveConfig(It.IsAny<string>())).Returns(activeConfiguration);
            return provider.Object;
        }
    }
}
