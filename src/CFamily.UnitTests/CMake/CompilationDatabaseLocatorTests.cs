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
using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.TestInfrastructure;
using static SonarLint.VisualStudio.TestInfrastructure.Extensions.FileSystemExtensions;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class CompilationDatabaseLocatorTests
    {
        private const string RootDirectory = "dummy root";

        // Macro evaluator that returns the input unchanged
        private static readonly IMacroEvaluationService PassthroughMacroService = CreatePassthroughMacroService();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CompilationDatabaseLocator, ICompilationDatabaseLocator>(
                MefTestHelpers.CreateExport<IFolderWorkspaceService>(),
                MefTestHelpers.CreateExport<ILogger>());
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
        public void Locate_NoCMakeSettings_ReturnsDefaultLocationIfFileExists(bool fileExists)
        {
            var activeConfiguration = "any";
            var configProvider = CreateConfigProvider(activeConfiguration);
            var cmakeSettingsProvider = CreateCMakeSettingsProvider(RootDirectory, null);

            var defaultLocation = GetDefaultDatabaseFileLocation(activeConfiguration);

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetFileExists(defaultLocation, fileExists);

            var testSubject = CreateTestSubject(RootDirectory, configProvider, cmakeSettingsProvider.Object, fileSystem.Object);

            var result = testSubject.Locate();

            if (fileExists)
            {
                result.Should().Be(defaultLocation);
            }
            else
            {
                result.Should().BeNull();
            }

            fileSystem.VerifyFileExistsCalledOnce(defaultLocation);
            cmakeSettingsProvider.Verify(x => x.Find(RootDirectory), Times.Once);
        }

        [TestMethod]
        public void Locate_HasCMakeSettingsFile_ActiveConfigurationDoesNotExist_Null()
        {
            var configProvider = CreateConfigProvider("my-config");
            var cmakeSettingsProvider = CreateCMakeSettingsProvider(RootDirectory,
                new CMakeSettingsSearchResult(new CMakeSettings(), "", ""));

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, configProvider, cmakeSettingsProvider.Object, logger: logger);

            var result = testSubject.Locate();

            result.Should().BeNull();
            logger.AssertOutputStringExists(string.Format(Resources.NoBuildConfigInCMakeSettings, "my-config"));
        }

        [TestMethod]
        public void Locate_HasCMakeSettingsFile_NoBuildRootParameter_Null()
        {
            var configProvider = CreateConfigProvider("my-config");
            var cmakeSettings = CreateCMakeSettings("my-config", buildRoot: null);
            var cmakeSettingsProvider = CreateCMakeSettingsProvider(RootDirectory,
                new CMakeSettingsSearchResult(cmakeSettings, "", ""));

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(RootDirectory, configProvider, cmakeSettingsProvider.Object, logger: logger);

            var result = testSubject.Locate();

            result.Should().BeNull();
            logger.AssertOutputStringExists(string.Format(Resources.NoBuildRootInCMakeSettings, "my-config"));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Locate_HasCMakeSettingsFile_ReturnsConfiguredPathIfItExists(bool fileExists)
        {
            var configProvider = CreateConfigProvider("my-config");
            var cmakeSettings = CreateCMakeSettings("my-config", "folder");
            var cmakeSettingsProvider = CreateCMakeSettingsProvider(RootDirectory, 
                new CMakeSettingsSearchResult(cmakeSettings, "", ""));

            var compilationDatabaseFullLocation = Path.GetFullPath(
                Path.Combine("folder", CompilationDatabaseLocator.CompilationDatabaseFileName));

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetFileExists(compilationDatabaseFullLocation, fileExists);

            var testSubject = CreateTestSubject(RootDirectory, configProvider, cmakeSettingsProvider.Object, fileSystem.Object,
                macroEvaluationService: PassthroughMacroService);

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
        public void Locate_MacroServiceIsCalled_EvaluatedPropertyIsNull_Null()
        {
            var context = new MacroEvalContext(null, unevaluatedBuildRoot: "property that can't be evaluated");

            var result = context.TestSubject.Locate();

            result.Should().BeNull();
            context.MacroEvalService.Invocations.Count.Should().Be(1);
            context.Logger.AssertOutputStringExists(string.Format(Resources.UnableToEvaluateBuildRootProperty, "property that can't be evaluated"));
        }

        [TestMethod]
        [DataRow("c:\\absolute", "c:\\absolute\\compile_commands.json")]
        [DataRow("c:\\absolute\\", "c:\\absolute\\compile_commands.json")]
        [DataRow("c:\\path", "c:\\path\\compile_commands.json")]
        [DataRow("c:\\path\\", "c:\\path\\compile_commands.json")]
        [DataRow("c:\\aaa\\..\\bbb", "c:\\bbb\\compile_commands.json")]  // non-canonical path
        public void Locate_MacroServiceIsCalled_RootedPath_ExpectedValueIsReturn(string macroServiceReturnValue, string expectedResult)
        {
            var context = new MacroEvalContext(macroServiceReturnValue);

            var result = context.TestSubject.Locate();

            result.Should().Be(expectedResult);
            context.MacroEvalService.Invocations.Count.Should().Be(1);
        }

        [TestMethod]
        [DataRow("relative", "relative\\compile_commands.json")]
        [DataRow("relative\\", "relative\\compile_commands.json")]
        [DataRow("aaa\\bbb\\.\\..\\ccc\\", "aaa\\ccc\\compile_commands.json")]
        public void Locate_MacroServiceIsCalled_RelativePath_ExpectedValueIsReturn(string macroServiceReturnValue, string partialExpectedResult)
        {
            var context = new MacroEvalContext(macroServiceReturnValue);
            var fullExpectedResult = Path.GetFullPath(partialExpectedResult);

            var result = context.TestSubject.Locate();

            result.Should().Be(fullExpectedResult);
            context.MacroEvalService.Invocations.Count.Should().Be(1);
        }

        private static CMakeSettings CreateCMakeSettings(string activeConfigurationName, 
            string buildRoot, 
            string generator = "generator") =>
            new()
            {
                Configurations = new[]
                {
                    new CMakeBuildConfiguration
                    {
                        Name = activeConfigurationName,
                        BuildRoot = buildRoot,
                        Generator = generator
                    }
                }
            };

        private static string GetDefaultDatabaseFileLocation(string activeBuildConfiguration) =>
            Path.GetFullPath(Path.Combine(
                string.Format(CompilationDatabaseLocator.DefaultLocationFormat,
                    RootDirectory,
                    activeBuildConfiguration),
                CompilationDatabaseLocator.CompilationDatabaseFileName));

        private static CompilationDatabaseLocator CreateTestSubject(string rootDirectory, 
            IBuildConfigProvider buildConfigProvider = null,
            ICMakeSettingsProvider cMakeSettingsProvider = null,
            IFileSystem fileSystem = null, 
            ILogger logger = null,
            IMacroEvaluationService macroEvaluationService = null)
        {
            var folderWorkspaceService = new Mock<IFolderWorkspaceService>();
            folderWorkspaceService.Setup(x => x.FindRootDirectory()).Returns(rootDirectory);

            cMakeSettingsProvider ??= Mock.Of<ICMakeSettingsProvider>();
            buildConfigProvider ??= Mock.Of<IBuildConfigProvider>();
            logger ??= Mock.Of<ILogger>();
            fileSystem ??= new FileSystem();
            macroEvaluationService ??= Mock.Of<IMacroEvaluationService>();

            return new CompilationDatabaseLocator(folderWorkspaceService.Object, buildConfigProvider, cMakeSettingsProvider, macroEvaluationService, fileSystem, logger);
        }

        private static IBuildConfigProvider CreateConfigProvider(string activeConfiguration)
        {
            var provider = new Mock<IBuildConfigProvider>();
            provider.Setup(x => x.GetActiveConfig(It.IsAny<string>())).Returns(activeConfiguration);
            
            return provider.Object;
        }

        private static Mock<ICMakeSettingsProvider> CreateCMakeSettingsProvider(string rootDirectory, CMakeSettingsSearchResult result)
        {
            var cmakeSettingsProvider = new Mock<ICMakeSettingsProvider>();
            cmakeSettingsProvider.Setup(x => x.Find(rootDirectory)).Returns(result);

            return cmakeSettingsProvider;
        }

        private static IMacroEvaluationService CreatePassthroughMacroService()
        {
            Func<string, IEvaluationContext, string> passthrough = (input, _) => input;

            var macroService = new Mock<IMacroEvaluationService>();
            macroService.Setup(x => x.Evaluate(It.IsAny<string>(), It.IsAny<IEvaluationContext>()))
                .Returns(passthrough);

            return macroService.Object;
        }

        /// <summary>
        /// Sets up the file and config mocks so that the test subject will call the
        /// macro evaluation service to process property
        /// </summary>
        private class MacroEvalContext
        {
            public MacroEvalContext(string macroServiceReturnValue, string unevaluatedBuildRoot = "any")
            {
                const string activeConfig = "my-active-config";
                const string workspaceRootDir = "c:\\workspace root";
                const string cmakeSettingsFilePath = "some path to cmake settings";
                const string cmakeListsFilePath = "some path to cmake lists";
                const string generator = "dummy generator";

                var configProvider = CreateConfigProvider(activeConfig);
                var cmakeSettings = CreateCMakeSettings(activeConfig, unevaluatedBuildRoot, generator);
                var cmakeSettingsProvider = CreateCMakeSettingsProvider(workspaceRootDir,
                    new CMakeSettingsSearchResult(cmakeSettings, cmakeSettingsFilePath, cmakeListsFilePath));

                // Treat all files as existing
                var fileSystem = new Mock<IFileSystem>();
                Func<string, bool> nonNullFilesExist = x => x != null;
                fileSystem.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(nonNullFilesExist);
 
                MacroEvalService = new Mock<IMacroEvaluationService>();
                MacroEvalService.Setup(x =>
                    x.Evaluate(unevaluatedBuildRoot,
                            It.Is((IEvaluationContext context) =>
                                context != null &&
                                context.ActiveConfiguration == activeConfig &&
                                context.RootDirectory == workspaceRootDir &&
                                context.Generator == generator &&
                                context.RootCMakeListsFilePath == cmakeListsFilePath &&
                                context.CMakeSettingsFilePath == cmakeSettingsFilePath)))
                        .Returns(macroServiceReturnValue);

                Logger = new TestLogger(logToConsole: true);

                TestSubject = CreateTestSubject(workspaceRootDir, configProvider, cmakeSettingsProvider.Object,
                    fileSystem.Object, Logger, MacroEvalService.Object);
            }

            public CompilationDatabaseLocator TestSubject { get; }
            public Mock<IMacroEvaluationService> MacroEvalService { get; }
            public TestLogger Logger { get; }
        }
    }
}
