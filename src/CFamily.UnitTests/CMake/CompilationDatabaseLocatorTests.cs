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

        // Macro evaluator that returns the input unchanged
        private static readonly IMacroEvaluationService PassthroughMacroEvaluator = CreatePassthroughMacroEvaluator();

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
                macroEvaluationService: PassthroughMacroEvaluator);

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
        public void Locate_MacroEvaluatorIsCalled_EvaluatedPropertyIsNull_Null()
        {
            var context = new MacroEvalContext(null, unevaluatedBuildRoot: "property that can't be evaluated");

            var result = context.TestSubject.Locate();

            result.Should().BeNull();
            context.MacroEvalService.Invocations.Count.Should().Be(1);
            context.Logger.AssertOutputStringExists(string.Format(Resources.UnableToEvaluateBuildRootProperty, "property that can't be evaluated"));
        }

        [TestMethod]
        [DataRow(null, null)]
        [DataRow("c:\\absolute", "c:\\absolute\\compile_commands.json")]
        [DataRow("c:\\absolute\\", "c:\\absolute\\compile_commands.json")]
        [DataRow("c:\\path", "c:\\path\\compile_commands.json")]
        [DataRow("c:\\path\\", "c:\\path\\compile_commands.json")]
        [DataRow("c:\\aaa\\..\\bbb", "c:\\bbb\\compile_commands.json")]  // non-canonical path
        public void Locate_MacroEvaluatorIsCalled_RootedPath_ExpectedValueIsReturn(string macroEvalReturnValue, string expectedResult)
        {
            var context = new MacroEvalContext(macroEvalReturnValue);

            var result = context.TestSubject.Locate();

            result.Should().Be(expectedResult);
            context.MacroEvalService.Invocations.Count.Should().Be(1);
        }

        [TestMethod]
        [DataRow("relative", "relative\\compile_commands.json")]
        [DataRow("relative\\", "relative\\compile_commands.json")]
        [DataRow("aaa\\bbb\\.\\..\\ccc\\", "aaa\\ccc\\compile_commands.json")]
        public void Locate_MacroEvaluatorIsCalled_RelativePath_ExpectedValueIsReturn(string macroEvalReturnValue, string partialExpectedResult)
        {
            var context = new MacroEvalContext(macroEvalReturnValue);
            var fullExpectedResult = Path.GetFullPath(partialExpectedResult);

            var result = context.TestSubject.Locate();

            result.Should().Be(fullExpectedResult);
            context.MacroEvalService.Invocations.Count.Should().Be(1);
        }

        private static CMakeSettings CreateCMakeSettings(string activeConfigurationName, string buildRoot) =>
            new()
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

        private static IMacroEvaluationService CreatePassthroughMacroEvaluator()
        {
            Func<string, string, string, string> passthrough = (input, _, _) => input;

            var macroEvaluator = new Mock<IMacroEvaluationService>();
            macroEvaluator.Setup(x => x.Evaluate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(passthrough);

            return macroEvaluator.Object;
        }

        /// <summary>
        /// Sets up the file and config mocks so that the test subject will call the
        /// macro evaluator to process property
        /// </summary>
        private class MacroEvalContext
        {
            public MacroEvalContext(string macroEvalReturnValue, string unevaluatedBuildRoot = "any")
            {
                const string ActiveConfig = "my-active-config";
                const string WorkspaceRootDir = "c:\\workspace root";

                var configProvider = CreateConfigProvider(ActiveConfig);
                var cmakeSettings = CreateCMakeSettings(ActiveConfig, unevaluatedBuildRoot);
                var cmakeSettingsProvider = CreateCMakeSettingsProvider(WorkspaceRootDir,
                    new CMakeSettingsSearchResult(cmakeSettings, "", ""));

                // Treat all files as existing
                var fileSystem = new Mock<IFileSystem>();
                Func<string, bool> nonNullFilesExist = x => x != null;
                fileSystem.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(nonNullFilesExist);
 
                MacroEvalService = new Mock<IMacroEvaluationService>();
                MacroEvalService.Setup(x => x.Evaluate(unevaluatedBuildRoot, ActiveConfig, WorkspaceRootDir))
                    .Returns(macroEvalReturnValue);

                Logger = new TestLogger(logToConsole: true);

                TestSubject = CreateTestSubject(WorkspaceRootDir, configProvider, cmakeSettingsProvider.Object,
                    fileSystem.Object, Logger, MacroEvalService.Object);
            }

            public CompilationDatabaseLocator TestSubject { get; }
            public Mock<IMacroEvaluationService> MacroEvalService { get; }
            public TestLogger Logger { get; }
        }
    }
}
