/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests.CFamilyTestUtility;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject
{
    [TestClass]
    public class FileConfigTests
    {
        private readonly TestLogger testLogger = new TestLogger();
        private const string ClFilePath = "C:\\path\\cl.exe";
        private const string ClangClFilePath = "C:\\path\\clang-cl.exe";

        private static IFileSystem CreateFileSystemWithNoFiles()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.File.Exists(Arg.Any<string>()).Returns(false);
            return fileSystem;
        }

        private static IFileSystem CreateFileSystemWithExistingFile(string fullPath)
        {
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.File.Exists(fullPath).Returns(true);
            return fileSystem;
        }

        private static IFileSystem CreateFileSystemWithClCompiler() => CreateFileSystemWithExistingFile(ClFilePath);
        private static IFileSystem CreateFileSystemWithClangClCompiler() => CreateFileSystemWithExistingFile(ClangClFilePath);

        [TestMethod]
        public void TryGet_NoVCProject_ReturnsNull()
        {
            var dteProjectItemMock = new Mock<ProjectItem>();
            var dteProjectMock = new Mock<Project>();

            dteProjectMock.Setup(x => x.Object).Returns(null);
            dteProjectItemMock.Setup(x => x.Object).Returns(Mock.Of<VCFile>());
            dteProjectItemMock.Setup(x => x.ContainingProject).Returns(dteProjectMock.Object);

            FileConfig.TryGet(testLogger, dteProjectItemMock.Object, "c:\\path", CreateFileSystemWithClCompiler())
                .Should().BeNull();
        }

        [TestMethod]
        public void TryGet_NoVCFile_ReturnsNull()
        {
            var dteProjectItemMock = new Mock<ProjectItem>();
            var dteProjectMock = new Mock<Project>();

            dteProjectMock.Setup(x => x.Object).Returns(Mock.Of<VCProject>());
            dteProjectItemMock.Setup(x => x.Object).Returns(null);
            dteProjectItemMock.Setup(x => x.ContainingProject).Returns(dteProjectMock.Object);

            FileConfig.TryGet(testLogger, dteProjectItemMock.Object, "c:\\path", CreateFileSystemWithClCompiler())
                .Should().BeNull();
        }

        [TestMethod]
        public void TryGet_UnsupportedItemType_ReturnsNull()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig { ItemType = "None" };
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var fileConfig = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithClCompiler());

            // Assert
            fileConfig.Should().BeNull();
            testLogger.AssertOutputStringExists("File's \"Item type\" is not supported. File: 'c:\\dummy\\file.cpp'");
        }

        [TestMethod]
        public void TryGet_UnsupportedConfigurationType_ReturnsNull()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig { ConfigurationType = ConfigurationTypes.typeUnknown };
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var fileConfig = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithClCompiler());

            // Assert
            fileConfig.Should().BeNull();
            testLogger.AssertOutputStringExists("Project's \"Configuration type\" is not supported.");
        }

        [TestMethod]
        public void TryGet_UnsupportedCustomBuild_ReturnsNull()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig { IsVCCLCompilerTool = false };
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var fileConfig = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithClCompiler());

            // Assert
            fileConfig.Should().BeNull();
            testLogger.AssertOutputStringExists("Custom build tools aren't supported. Custom-built file: 'c:\\dummy\\file.cpp'");
        }

        [TestMethod]
        public void TryGet_Full_Cmd_ForCompileAsCppFile()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig
            {
                ItemType = "ClCompile",
                FileConfigProperties = CreateDefaultClCompileFileProperties(new Dictionary<string, string>
                {
                    ["CompileAs"] = "CompileAsCpp"
                })
            };

            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithClCompiler());

            // Assert
            request.Should().NotBeNull();
            Assert.AreEqual("\"C:\\path\\cl.exe\" /permissive- /TP /std:c++17 /EHsc /arch:AVX512 /MT /RTCu /Zp8 /DA \"c:\\dummy\\file.cpp\"", request.CDCommand);
            Assert.AreEqual("C:\\path\\includeDir1;C:\\path\\includeDir2;C:\\path\\includeDir3;", request.EnvInclude);
            Assert.AreEqual("c:\\dummy\\file.cpp", request.CDFile);
            Assert.AreEqual("c:\\foo", request.CDDirectory);
            Assert.IsFalse(request.IsHeaderFile);
        }

        [TestMethod]
        public void TryGet_Full_Cmd_ForCompileAsCFile()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig
            {
                ItemType = "ClCompile",
                FileConfigProperties = CreateDefaultClCompileFileProperties(new Dictionary<string, string>
                {
                    ["CompileAs"] = "CompileAsC"
                })
            };

            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithClCompiler());

            // Assert
            request.Should().NotBeNull();
            Assert.AreEqual("\"C:\\path\\cl.exe\" /permissive- /TC /std:c17 /EHsc /arch:AVX512 /MT /RTCu /Zp8 /DA \"c:\\dummy\\file.cpp\"", request.CDCommand);
            Assert.AreEqual("C:\\path\\includeDir1;C:\\path\\includeDir2;C:\\path\\includeDir3;", request.EnvInclude);
            Assert.AreEqual("c:\\dummy\\file.cpp", request.CDFile);
            Assert.AreEqual("c:\\foo", request.CDDirectory);
            Assert.IsFalse(request.IsHeaderFile);
        }


        [DataTestMethod]
        public void TryGet_Full_Cmd_ForNonCFile()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig
            {
                ItemType = "ClCompile",
                FileConfigProperties = CreateDefaultClCompileFileProperties(new Dictionary<string, string>
                {
                    ["CompileAs"] = "Default"
                })
            };

            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig, "ANY_NON_CCODE");

            // Act
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithClCompiler());

            // Assert
            request.Should().NotBeNull();
            Assert.AreEqual("\"C:\\path\\cl.exe\" /permissive- /TP /std:c++17 /EHsc /arch:AVX512 /MT /RTCu /Zp8 /DA \"c:\\dummy\\file.cpp\"", request.CDCommand);
            Assert.AreEqual("C:\\path\\includeDir1;C:\\path\\includeDir2;C:\\path\\includeDir3;", request.EnvInclude);
            Assert.AreEqual("c:\\dummy\\file.cpp", request.CDFile);
            Assert.AreEqual("c:\\foo", request.CDDirectory);
            Assert.IsFalse(request.IsHeaderFile);
        }

        [TestMethod]
        public void TryGet_Full_Cmd_ForCCodeFile()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig
            {
                ItemType = "ClCompile",
                FileConfigProperties = CreateDefaultClCompileFileProperties(new Dictionary<string, string>
                {
                    ["LanguageStandard"] = "stdcpp17",
                    ["LanguageStandard_C"] = "stdc17",
                })
            };

            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig, "CCode");

            // Act
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithClCompiler());

            // Assert
            request.Should().NotBeNull();
            Assert.AreEqual("\"C:\\path\\cl.exe\" /permissive- /TC /std:c17 /EHsc /arch:AVX512 /MT /RTCu /Zp8 /DA \"c:\\dummy\\file.cpp\"", request.CDCommand);
            Assert.AreEqual("C:\\path\\includeDir1;C:\\path\\includeDir2;C:\\path\\includeDir3;", request.EnvInclude);
            Assert.AreEqual("c:\\dummy\\file.cpp", request.CDFile);
            Assert.AreEqual("c:\\foo", request.CDDirectory);
            Assert.IsFalse(request.IsHeaderFile);
        }

        [TestMethod]
        public void TryGet_HeaderFileOptions_ReturnsValidConfig()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig
            {
                ItemType = "ClInclude",
                FileConfigProperties = new Dictionary<string, string>
                {
                    ["CompileAs"] = "Default",
                    ["CompileAsManaged"] = "false",
                    ["EnableEnhancedInstructionSet"] = "",
                    ["RuntimeLibrary"] = "",
                    ["LanguageStandard"] = "",
                    ["ExceptionHandling"] = "Sync",
                    ["BasicRuntimeChecks"] = "UninitializedLocalUsageCheck",
                    ["ForcedIncludeFiles"] = "",
                    ["PrecompiledHeader"] = "Use",
                    ["PrecompiledHeaderFile"] = "pch.h",
                }
            };

            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.h", CreateFileSystemWithClCompiler());

            // Assert
            request.Should().NotBeNull();
            Assert.AreEqual("\"C:\\path\\cl.exe\" /Yu\"pch.h\" /FI\"pch.h\" /TP /EHsc /RTCu \"c:\\dummy\\file.h\"", request.CDCommand);
            Assert.AreEqual("c:\\dummy\\file.h", request.CDFile);
            Assert.IsTrue(request.IsHeaderFile);

            // Arrange
            projectItemConfig.FileConfigProperties["CompileAs"] = "CompileAsC";
            projectItemConfig.FileConfigProperties["ForcedIncludeFiles"] = "FHeader.h";

            // Act
            request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.h", CreateFileSystemWithClCompiler());

            // Assert
            Assert.AreEqual("\"C:\\path\\cl.exe\" /FI\"FHeader.h\" /Yu\"pch.h\" /TC /EHsc /RTCu \"c:\\dummy\\file.h\"", request.CDCommand);
            Assert.AreEqual("c:\\dummy\\file.h", request.CDFile);
            Assert.IsTrue(request.IsHeaderFile);

            // Arrange
            projectItemConfig.FileConfigProperties["CompileAs"] = "CompileAsCpp";

            // Act
            request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.h", CreateFileSystemWithClCompiler());

            // Assert
            Assert.AreEqual("\"C:\\path\\cl.exe\" /FI\"FHeader.h\" /Yu\"pch.h\" /TP /EHsc /RTCu \"c:\\dummy\\file.h\"", request.CDCommand);
            Assert.AreEqual("c:\\dummy\\file.h", request.CDFile);
            Assert.IsTrue(request.IsHeaderFile);
        }

        [TestMethod]
        public void TryGet_CompilerName_VS2017()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig
            {
                ProjectConfigProperties = new Dictionary<string, string>
                {
                    ["ClCompilerPath"] = null,
                    ["IncludePath"] = "C:\\path\\includeDir1;C:\\path\\includeDir2;C:\\path\\includeDir3;",
                    ["VC_ExecutablePath_x86"] = "C:\\path\\x86",
                    ["VC_ExecutablePath_x64"] = "C:\\path\\x64",
                }
            };

            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithExistingFile("C:\\path\\x86\\cl.exe"));

            // Assert
            request.Should().NotBeNull();
            Assert.IsTrue(request.CDCommand.StartsWith("\"C:\\path\\x86\\cl.exe\""));

            // Arrange
            projectItemConfig.PlatformName = "x64";
            projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);
            // Act
            request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithExistingFile("C:\\path\\x64\\cl.exe"));

            // Assert
            request.Should().NotBeNull();
            Assert.IsTrue(request.CDCommand.StartsWith("\"C:\\path\\x64\\cl.exe\""));
        }

        [TestMethod]
        public void TryGet_CompilerName_ClangCL()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig
            {
                ProjectConfigProperties = new Dictionary<string, string>
                {
                    ["ClCompilerPath"] = null,
                    ["IncludePath"] = "C:\\path\\includeDir1;C:\\path\\includeDir2;C:\\path\\includeDir3;",
                    ["ExecutablePath"] = "C:\\path\\no-compiler\\;C:\\path",
                    ["CLToolExe"] = "clang-cl.exe",
                    ["VC_ExecutablePath_x86"] = "C:\\path\\x86",
                    ["VC_ExecutablePath_x64"] = "C:\\path\\x64",
                }
            };

            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithClangClCompiler());

            // Assert
            request.Should().NotBeNull();
            Assert.IsTrue(request.CDCommand.StartsWith("\"C:\\path\\clang-cl.exe\""));

            // Arrange
            projectItemConfig.ProjectConfigProperties["ClCompilerPath"] = "\\clang-cl.exe";

            // Act
            request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithClangClCompiler());

            // Assert
            request.Should().NotBeNull();
            Assert.IsTrue(request.CDCommand.StartsWith("\"C:\\path\\clang-cl.exe\""));

            // Act
            request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithNoFiles());

            // Assert
            request.Should().BeNull();
            testLogger.AssertOutputStringExists("Compiler is not supported.");

            // Arrange
            projectItemConfig.ProjectConfigProperties["ClToolExe"] = null;

            // Act
            request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithExistingFile("C:\\path\\x86\\cl.exe"));

            // Assert
            request.Should().NotBeNull();
            Assert.IsTrue(request.CDCommand.StartsWith("\"C:\\path\\x86\\cl.exe\""));

            // Arrange
            projectItemConfig.ProjectConfigProperties["ClToolExe"] = null;

            // Act
            request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithNoFiles());

            // Assert
            request.Should().BeNull();
            testLogger.AssertOutputStringExists("Compiler is not supported.");
        }

        [TestMethod]
        public void TryGet_CompilerName_CL_No_ClCompilerPath_NoCLToolExe()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig
            {
                ProjectConfigProperties = new Dictionary<string, string>
                {
                    ["ClCompilerPath"] = null,
                    ["IncludePath"] = "C:\\path\\includeDir1;C:\\path\\includeDir2;C:\\path\\includeDir3;",
                    ["ExecutablePath"] = "C:\\path\\no-compiler\\;C:\\path",
                    ["CLToolExe"] = null,
                    ["VC_ExecutablePath_x86"] = "C:\\path\\x86",
                    ["VC_ExecutablePath_x64"] = "C:\\path\\x64",
                }
            };

            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp", CreateFileSystemWithClCompiler());

            // Assert
            request.Should().NotBeNull();
            Assert.IsTrue(request.CDCommand.StartsWith("\"C:\\path\\cl.exe\""));
        }

        private Dictionary<string, string> CreateDefaultClCompileFileProperties(Dictionary<string, string> overrides)
        {
            var defaultProperties = new Dictionary<string, string>
            {
                ["PrecompiledHeader"] = "NotUsing",
                ["CompileAsManaged"] = "false",
                ["EnableEnhancedInstructionSet"] = "AdvancedVectorExtensions512",
                ["RuntimeLibrary"] = "MultiThreaded",
                ["LanguageStandard"] = "stdcpp17",
                ["LanguageStandard_C"] = "stdc17",
                ["ExceptionHandling"] = "Sync",
                ["BasicRuntimeChecks"] = "UninitializedLocalUsageCheck",
                ["ConformanceMode"] = "true",
                ["StructMemberAlignment"] = "8Bytes",
                ["AdditionalOptions"] = "/DA",
            };

            foreach (var overrideProperties in overrides)
            {
                defaultProperties[overrideProperties.Key] = overrideProperties.Value;
            }

            return defaultProperties;
        }

    }
}
