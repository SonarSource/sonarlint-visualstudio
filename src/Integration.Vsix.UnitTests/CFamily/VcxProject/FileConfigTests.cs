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

using System.Collections.Generic;
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

        [TestMethod]
        public void TryGet_NoVCProject_ReturnsNull()
        {
            var dteProjectItemMock = new Mock<ProjectItem>();
            var dteProjectMock = new Mock<Project>();

            dteProjectMock.Setup(x => x.Object).Returns(null);
            dteProjectItemMock.Setup(x => x.Object).Returns(Mock.Of<VCFile>());
            dteProjectItemMock.Setup(x => x.ContainingProject).Returns(dteProjectMock.Object);

            FileConfig.TryGet(testLogger, dteProjectItemMock.Object, "c:\\path")
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

            FileConfig.TryGet(testLogger, dteProjectItemMock.Object, "c:\\path")
                .Should().BeNull();
        }

        [TestMethod]
        public void TryGet_UnsupportedItemType_ReturnsNull()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig { ItemType = "None" };
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var fileConfig = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp");

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
            var fileConfig = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp");

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
            var fileConfig = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp");

            // Assert
            fileConfig.Should().BeNull();
            testLogger.AssertOutputStringExists("Custom build tools aren't supported. Custom-built file: 'c:\\dummy\\file.cpp'");
        }

        [TestMethod]
        public void TryGet_Full_Cmd()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig
            {
                ItemType = "ClCompile",
                FileConfigProperties = new Dictionary<string, string>
                {
                    ["PrecompiledHeader"] = "NotUsing",
                    ["CompileAs"] = "CompileAsCpp",
                    ["CompileAsManaged"] = "false",
                    ["EnableEnhancedInstructionSet"] = "AdvancedVectorExtensions512",
                    ["RuntimeLibrary"] = "MultiThreaded",
                    ["LanguageStandard"] = "stdcpp17",
                    ["ExceptionHandling"] = "Sync",
                    ["BasicRuntimeChecks"] = "UninitializedLocalUsageCheck",
                    ["ConformanceMode"] = "true",
                    ["StructMemberAlignment"] = "8Bytes",
                    ["AdditionalOptions"] = "/DA",
                }
            };

            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp");

            // Assert
            request.Should().NotBeNull();
            Assert.AreEqual("\"C:\\path\\cl.exe\" /permissive- /std:c++17 /EHsc /arch:AVX512 /MT /RTCu /Zp8 /TP /DA \"c:\\dummy\\file.cpp\"", request.CDCommand);
            Assert.AreEqual("", request.HeaderFileLanguage);
            Assert.AreEqual("C:\\path\\includeDir1;C:\\path\\includeDir2;C:\\path\\includeDir3;", request.EnvInclude);
            Assert.AreEqual("c:\\dummy\\file.cpp", request.CDFile);
            Assert.AreEqual("c:\\foo", request.CDDirectory);
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
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.h");

            // Assert
            request.Should().NotBeNull();
            Assert.AreEqual("\"C:\\path\\cl.exe\" /Yu\"pch.h\" /FI\"pch.h\" /EHsc /RTCu \"c:\\dummy\\file.h\"", request.CDCommand);
            Assert.AreEqual("cpp", request.HeaderFileLanguage);

            // Arrange
            projectItemConfig.FileConfigProperties["CompileAs"] = "CompileAsC";
            projectItemConfig.FileConfigProperties["ForcedIncludeFiles"] = "FHeader.h";

            // Act
            request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.h");

            // Assert
            Assert.AreEqual("\"C:\\path\\cl.exe\" /FI\"FHeader.h\" /Yu\"pch.h\" /EHsc /RTCu \"c:\\dummy\\file.h\"", request.CDCommand);
            Assert.AreEqual("c", request.HeaderFileLanguage);
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
            var request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp");

            // Assert
            request.Should().NotBeNull();
            Assert.IsTrue(request.CDCommand.StartsWith("\"C:\\path\\x86\\cl.exe\""));

            // Arrange
            projectItemConfig.PlatformName = "x64";
            projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);
            // Act
            request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp");

            // Assert
            request.Should().NotBeNull();
            Assert.IsTrue(request.CDCommand.StartsWith("\"C:\\path\\x64\\cl.exe\""));
        }

    }
}
