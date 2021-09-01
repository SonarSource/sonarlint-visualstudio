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
using static SonarLint.VisualStudio.Integration.UnitTests.Extensions.FileSystemExtensions;

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

            var fileSystem = new Mock<IFileSystem>()
                .FileDoesNotExist(cmakeSettingsLocation)
                .SetFileExists(defaultLocation, fileExists);

            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object);

            var result = testSubject.Locate();

            if (fileExists)
            {
                result.Should().Be(defaultLocation);
            }
            else
            {
                result.Should().BeNull();
            }

            fileSystem.VerifyFileExistsCalledOnce(cmakeSettingsLocation);
        }

        [TestMethod]
        public void Locate_FailedToReadCMakeSettings_NonCriticalException_Null()
        {
            var configProvider = CreateConfigProvider("my config");
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>()
                .FileExists(cmakeSettingsLocation);

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
            const string invalidJson = "invalid json";
            var expectedMessage = GetExpectedDeserializationMessage();
            var configProvider = CreateConfigProvider("my config");
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>()
                .SetFileReadAllText(cmakeSettingsLocation, invalidJson);

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object, logger);

            var result = testSubject.Locate();
            result.Should().BeNull();

            logger.AssertPartialOutputStringExists(expectedMessage);

            string GetExpectedDeserializationMessage()
            {
                try
                {
                    JsonConvert.DeserializeObject(invalidJson);
                }
                catch (JsonReaderException ex)
                {
                    return ex.Message;
                }
                return null;
            }
        }

        [TestMethod]
        public void Locate_FailedToReadCMakeSettings_CriticalException_ExceptionThrown()
        {
            var configProvider = CreateConfigProvider("my config");
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);

            var fileSystem = new Mock<IFileSystem>()
                .FileExists(cmakeSettingsLocation);

            fileSystem
                .Setup(x => x.File.ReadAllText(cmakeSettingsLocation))
                .Throws(new StackOverflowException());

            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object);

            Action act = () => testSubject.Locate();

            act.Should().Throw<StackOverflowException>();
        }

        [TestMethod]
        public void Locate_HasCMakeSettingsFile_ActiveConfigurationDoesNotExist_Null()
        {
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);
            var configProvider = CreateConfigProvider("my-config");

            var fileSystem = new Mock<IFileSystem>()
                .SetupCMakeSettingsFileExists(cmakeSettingsLocation, new CMakeSettings());

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object, logger);

            var result = testSubject.Locate();

            result.Should().BeNull();
            logger.AssertOutputStringExists(string.Format(Resources.NoBuildConfigInCMakeSettings,
                "my-config",
                CompilationDatabaseLocator.CMakeSettingsFileName));

            fileSystem.VerifyFileReadAllTextCalledOnce(cmakeSettingsLocation);
        }

        [TestMethod]
        public void Locate_HasCMakeSettingsFile_NoBuildRootParameter_Null()
        {
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);
            var configProvider = CreateConfigProvider("my-config");

            var cMakeSettings = CreateCMakeSettings("my-config", buildRoot: null);

            var fileSystem = new Mock<IFileSystem>()
                .SetupCMakeSettingsFileExists(cmakeSettingsLocation, cMakeSettings);

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, configProvider, fileSystem.Object, logger);

            var result = testSubject.Locate();

            result.Should().BeNull();
            logger.AssertOutputStringExists(string.Format(Resources.NoBuildRootInCMakeSettings,
                "my-config",
                cmakeSettingsLocation));

            fileSystem.VerifyFileReadAllTextCalledOnce(cmakeSettingsLocation);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Locate_HasCMakeSettingsFile_ReturnsConfiguredPathIfItExists(bool fileExists)
        {
            var configProvider = CreateConfigProvider("my-config");
            var cmakeSettings = CreateCMakeSettings("my-config", "folder");
            var cmakeSettingsLocation = GetCmakeSettingsLocation(RootDirectory);
            var compilationDatabaseFullLocation = GetCompilationDatabaseFilePath("folder");

            var fileSystem = new Mock<IFileSystem>()
                .SetupCMakeSettingsFileExists(cmakeSettingsLocation, cmakeSettings)
                .SetFileExists(compilationDatabaseFullLocation, fileExists);

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
            var compilationDatabaseFullLocation = GetCompilationDatabaseFilePath(expectedPath);

            var fileSystem = new Mock<IFileSystem>()
                .SetupCMakeSettingsFileExists(cmakeSettingsLocation, cmakeSettings)
                .FileExists(compilationDatabaseFullLocation);

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
            Path.GetFullPath(Path.Combine(rootDirectory, "CMakeSettings.json"));

        private static string GetCompilationDatabaseFilePath(string rootDirectory) =>
            Path.GetFullPath(Path.Combine(rootDirectory, "compile_commands.json"));

        private static string GetDefaultDatabaseFileLocation(string activeBuildConfiguration) =>
            Path.GetFullPath(Path.Combine(
                string.Format(CompilationDatabaseLocator.DefaultLocationFormat,
                    RootDirectory,
                    activeBuildConfiguration),
                CompilationDatabaseLocator.CompilationDatabaseFileName));

        private static CompilationDatabaseLocator CreateTestSubject(string rootDirectory,
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

        private static IBuildConfigProvider CreateConfigProvider(string activeConfiguration)
        {
            var provider = new Mock<IBuildConfigProvider>();
            provider.Setup(x => x.GetActiveConfig(It.IsAny<string>())).Returns(activeConfiguration);
            return provider.Object;
        }
    }
 
    internal static class CompilationDatabaseLocationTestsExtensions
    {
        public static Mock<IFileSystem> SetupCMakeSettingsFileExists(this Mock<IFileSystem> fileSystem, string cmakeSettingsLocation, CMakeSettings cmakeSettings) =>
            fileSystem.SetFileReadAllText(cmakeSettingsLocation, JsonConvert.SerializeObject(cmakeSettings));
    }
}
