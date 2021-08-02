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

using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.CFamily.CMake;
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
            logger.AssertOutputStringExists(Resources.NoRootDirectory);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Locate_NoCMakeSettingsFile_ReturnsDefaultLocationIfItExists(bool defaultFileExists)
        {
            var activeConfiguration = "any";
            var configProvider = CreateConfigProvider(activeConfiguration);
            var defaultLocation = GetDefaultDatabaseFileLocation(activeConfiguration);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(false);
            fileSystem.Setup(x => x.File.Exists(defaultLocation)).Returns(defaultFileExists);

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, fileSystem.Object, logger, configProvider);

            var result = testSubject.Locate();

            if (defaultFileExists)
            {
                result.Should().Be(defaultLocation);
                logger.AssertOutputStringExists(string.Format(Resources.FoundCompilationDatabaseFile, defaultLocation));
            }
            else
            {
                result.Should().BeNull();
                logger.AssertOutputStringExists(string.Format(Resources.NoCompilationDatabaseFile, defaultLocation));
            }

            fileSystem.Verify(x=> x.File.Exists(defaultLocation), Times.Once);
        }

        [TestMethod]
        public void Locate_HasCMakeSettingsFile_ActiveConfigurationDoesNotExist_Null()
        {
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);
            var configProvider = CreateConfigProvider("my-config");

            var fileSystem = new Mock<IFileSystem>();
            SetupCMakeSettingsFileExists(fileSystem, cmakeSettingsLocation, new CMakeSettings());

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, fileSystem.Object, logger, configProvider);

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
        public void Locate_HasCMakeSettingsFile_ReturnsConfiguredPathIfItExists(bool configuredPathExists)
        {
            var activeConfiguration = "my config";
            var configProvider = CreateConfigProvider(activeConfiguration);
            var cmakeSettings = CreateCMakeSettings(activeConfiguration, "folder");
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>();
            SetupCMakeSettingsFileExists(fileSystem, cmakeSettingsLocation, cmakeSettings);

            var compilationDatabaseFullLocation = Path.GetFullPath(
                Path.Combine("folder", CompilationDatabaseLocator.CompilationDatabaseFileName));

            fileSystem.Setup(x => x.File.Exists(compilationDatabaseFullLocation)).Returns(configuredPathExists);

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, fileSystem.Object, logger, configProvider);

            var result = testSubject.Locate();

            if (configuredPathExists)
            {
                result.Should().Be(compilationDatabaseFullLocation);
                logger.AssertOutputStringExists(string.Format(Resources.FoundCompilationDatabaseFile, compilationDatabaseFullLocation));
            }
            else
            {
                result.Should().BeNull();
                logger.AssertOutputStringExists(string.Format(Resources.NoCompilationDatabaseFile, compilationDatabaseFullLocation));
            }

            fileSystem.Verify(x => x.File.Exists(compilationDatabaseFullLocation), Times.Once);
        }

        [TestMethod]
        [DataRow("c:\\absolute\\", "c:\\absolute\\")]
        [DataRow("relative", "relative")]
        [DataRow("c:\\absolute\\${name}\\${projectDir}", "c:\\absolute\\my-config\\dummy root")]
        [DataRow("c:\\${projectDir}\\somefolder\\${name}\\sub", "c:\\dummy root\\somefolder\\my-config\\sub")]
        [DataRow("${projectDir}\\somefolder\\${name}\\sub", "dummy root\\somefolder\\my-config\\sub")]
        public void Locate_HasCMakeSettingsFile_ConfiguredPathHasParameters_ParametersReplaced(string configuredPath, string expectedPath)
        {
            var activeConfig = "my-config";
            var configProvider = CreateConfigProvider(activeConfig);
            var cmakeSettings = CreateCMakeSettings(activeConfig, configuredPath);
            var cmakeSettingsLocation = Path.GetFullPath(Path.Combine(RootDirectory, CompilationDatabaseLocator.CMakeSettingsFileName));

            var fileSystem = new Mock<IFileSystem>();
            SetupCMakeSettingsFileExists(fileSystem, cmakeSettingsLocation, cmakeSettings);

            var compilationDatabaseFullLocation = Path.GetFullPath(Path.Combine(expectedPath, CompilationDatabaseLocator.CompilationDatabaseFileName));
            fileSystem.Setup(x => x.File.Exists(compilationDatabaseFullLocation)).Returns(true);

            var testSubject = CreateTestSubject(RootDirectory, fileSystem.Object, buildConfigProvider: configProvider);

            var result = testSubject.Locate();

            result.Should().Be(compilationDatabaseFullLocation);

            fileSystem.Verify(x => x.File.Exists(compilationDatabaseFullLocation), Times.Once);
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
        private CompilationDatabaseLocator CreateTestSubject(string rootDirectory, IFileSystem fileSystem = null, 
            ILogger logger = null, IBuildConfigProvider buildConfigProvider = null)
        {
            var folderWorkspaceService = new Mock<IFolderWorkspaceService>();
            folderWorkspaceService.Setup(x => x.FindRootDirectory()).Returns(rootDirectory);

            logger ??= Mock.Of<ILogger>();
            buildConfigProvider ??= Mock.Of<IBuildConfigProvider>();

            return new CompilationDatabaseLocator(folderWorkspaceService.Object, fileSystem, buildConfigProvider, logger);
        }

        private IBuildConfigProvider CreateConfigProvider(string activeConfiguration)
        {
            var provider = new Mock<IBuildConfigProvider>();
            provider.Setup(x => x.GetActiveConfig(It.IsAny<string>())).Returns(activeConfiguration);
            return provider.Object;
        }

    }
}
