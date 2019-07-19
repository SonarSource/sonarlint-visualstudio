/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyTest
    {
        private const string FileName = @"C:\absolute\path\to\file.cpp";
        [TestMethod]
        public void FileConfig_Test1()
        {
            string sqLanguage;
            CFamilyHelper.Capture[] captures = new CFamilyHelper.FileConfig()
            {
                PlatformName = "Win32",
                PlatformToolset = "v140",
                IncludeDirectories = "sys1;sys2;",
                AdditionalIncludeDirectories = "dir1;dir2;",
                PreprocessorDefinitions = "D1;D2;",
                UndefinePreprocessorDefinitions = "U1;U2;",
                ForcedIncludeFiles = "h1;h2;",
                PrecompiledHeader = "",
                CompileAs = "Default",
                CompileAsManaged = "",
                RuntimeLibrary = "MultiThreaded",
                ExceptionHandling = "false",
                EnableEnhancedInstructionSet = "NotSet",
                RuntimeTypeInfo = "true",
                BasicRuntimeChecks = "Default",
                AdditionalOptions = "/a1 /a2",
                AbsoluteFilePath = FileName,
            }.ToCaptures(FileName, out sqLanguage);
            CFamilyHelper.Capture p = captures[0];
            CFamilyHelper.Capture c = captures[1];

            p.Compiler.Should().Be("msvc-cl");
            p.StdErr.Should().Be("19.00.00 for x86");
            p.Cwd.Should().Be(@"C:\absolute\path\to");
            p.Executable.Should().Be("cl.exe");

            c.Compiler.Should().Be("msvc-cl");
            c.StdErr.Should().BeNull("otherwise will be considered as probe");
            c.Cwd.Should().Be(p.Cwd);
            c.Executable.Should().BeSameAs(p.Executable, "otherwise won't be associated with probe");
            c.Env.Should().Equal(new[] { "INCLUDE=sys1;sys2;" });
            c.Cmd.Should().Equal(new[] {
                "cl.exe",
                "/I", "dir1", "/I", "dir2",
                "/FI", "h1", "/FI", "h2",
                "/D", "D1", "/D", "D2",
                "/U", "U1", "/U", "U2",
                "/MT",
                "/a1", "/a2",
                FileName,
            });
        }

        [TestMethod]
        public void FileConfig_Test2()
        {
            string sqLanguage;
            CFamilyHelper.Capture[] captures = new CFamilyHelper.FileConfig()
            {
                PlatformName = "x64",
                PlatformToolset = "v140",
                AdditionalIncludeDirectories = "",
                PreprocessorDefinitions = "",
                UndefinePreprocessorDefinitions = "",
                ForcedIncludeFiles = "",
                PrecompiledHeader = "Use",
                PrecompiledHeaderFile = "stdafx.h",
                UndefineAllPreprocessorDefinitions = "true",
                IgnoreStandardIncludePath = "true",
                CompileAs = "CompileAsCpp",
                DisableLanguageExtensions = "true",
                CompileAsManaged = "true",
                CompileAsWinRT = "true",
                TreatWChar_tAsBuiltInType = "false",
                ForceConformanceInForLoopScope = "false",
                OpenMPSupport = "true",
                RuntimeLibrary = "MultiThreadedDebugDLL",
                RuntimeTypeInfo = "false",
                ExceptionHandling = "Async",
                EnableEnhancedInstructionSet = "NoExtensions",
                BasicRuntimeChecks = "EnableFastChecks",
                AdditionalOptions = "",
                AbsoluteFilePath = FileName,
            }.ToCaptures(FileName, out sqLanguage);
            CFamilyHelper.Capture p = captures[0];
            CFamilyHelper.Capture c = captures[1];

            p.StdErr.Should().Be("19.00.00 for x64");

            c.Cmd.Should().Equal(new[] {
                "cl.exe",
                "/X",
                "/Yustdafx.h",
                "/u",
                "/TP",
                "/clr",
                "/ZW",
                "/Za",
                "/Zc:wchar_t-",
                "/Zc:forScope-",
                "/openmp",
                "/MDd",
                "/EHa",
                "/arch:IA32",
                "/GR-",
                "/RTC1",
                FileName,
            });
        }

        [TestMethod]
        public void PlatformName()
        {
            CFamilyHelper.FileConfig.ConvertPlatformName("Win32").Should().Be("x86");
            CFamilyHelper.FileConfig.ConvertPlatformName("x64").Should().Be("x64");

            Action action = () => CFamilyHelper.FileConfig.ConvertPlatformName("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported PlatformName: foo");
        }

        [TestMethod]
        public void PlatformToolset()
        {
            CFamilyHelper.FileConfig.ConvertPlatformToolset("v90").Should().Be("15.00.00");

            CFamilyHelper.FileConfig.ConvertPlatformToolset("v100").Should().Be("16.00.00");

            CFamilyHelper.FileConfig.ConvertPlatformToolset("v110").Should().Be("17.00.00");
            CFamilyHelper.FileConfig.ConvertPlatformToolset("v110_xp").Should().Be("17.00.00");

            CFamilyHelper.FileConfig.ConvertPlatformToolset("v120").Should().Be("18.00.00");
            CFamilyHelper.FileConfig.ConvertPlatformToolset("v120_xp").Should().Be("18.00.00");

            CFamilyHelper.FileConfig.ConvertPlatformToolset("v140").Should().Be("19.00.00");
            CFamilyHelper.FileConfig.ConvertPlatformToolset("v140_xp").Should().Be("19.00.00");

            CFamilyHelper.FileConfig.ConvertPlatformToolset("v141").Should().Be("19.10.00");
            CFamilyHelper.FileConfig.ConvertPlatformToolset("v141_xp").Should().Be("19.10.00");

            CFamilyHelper.FileConfig.ConvertPlatformToolset("v142").Should().Be("19.20.00");

            Action action = () => CFamilyHelper.FileConfig.ConvertPlatformToolset("v143");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported PlatformToolset: v143");

            action = () => CFamilyHelper.FileConfig.ConvertPlatformToolset("");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith
                ("The file cannot be analyzed because the platform toolset has not been specified.");
        }

        [TestMethod]
        public void ConvertCompileAsAndGetSqLanguage()
        {
            string sqLanguage;
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetSqLanguage("", FileName, out sqLanguage).Should().Be("");
            sqLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetSqLanguage("Default", FileName, out sqLanguage).Should().Be("");
            sqLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetSqLanguage("Default", @"c:\Foo.cc", out sqLanguage).Should().Be("");
            sqLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetSqLanguage("Default", @"c:\Foo.cxx", out sqLanguage).Should().Be("");
            sqLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetSqLanguage("Default", @"c:\Foo.c", out sqLanguage).Should().Be("");
            sqLanguage.Should().Be("c");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetSqLanguage("CompileAsC", FileName, out sqLanguage).Should().Be("/TC");
            sqLanguage.Should().Be("c");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetSqLanguage("CompileAsCpp", FileName, out sqLanguage).Should().Be("/TP");
            sqLanguage.Should().Be("cpp");

            Action action = () => CFamilyHelper.FileConfig.ConvertCompileAsAndGetSqLanguage("foo", FileName, out sqLanguage);
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported CompileAs: foo");
        }

        [TestMethod]
        public void CompileAsManaged()
        {
            CFamilyHelper.FileConfig.ConvertCompileAsManaged("").Should().Be("");
            CFamilyHelper.FileConfig.ConvertCompileAsManaged("false").Should().Be("");
            CFamilyHelper.FileConfig.ConvertCompileAsManaged("true").Should().Be("/clr");
            CFamilyHelper.FileConfig.ConvertCompileAsManaged("Pure").Should().Be("/clr:pure");
            CFamilyHelper.FileConfig.ConvertCompileAsManaged("Safe").Should().Be("/clr:safe");

            Action action = () => CFamilyHelper.FileConfig.ConvertCompileAsManaged("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported CompileAsManaged: foo");
        }

        [TestMethod]
        public void RuntimeLibrary()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.FileConfig.ConvertRuntimeLibrary("").Should().Be("");

            CFamilyHelper.FileConfig.ConvertRuntimeLibrary("MultiThreaded").Should().Be("/MT");

            CFamilyHelper.FileConfig.ConvertRuntimeLibrary("MultiThreadedDebug").Should().Be("/MTd");

            CFamilyHelper.FileConfig.ConvertRuntimeLibrary("MultiThreadedDLL").Should().Be("/MD");
            CFamilyHelper.FileConfig.ConvertRuntimeLibrary("MultiThreadedDll").Should().Be("/MD");

            CFamilyHelper.FileConfig.ConvertRuntimeLibrary("MultiThreadedDebugDLL").Should().Be("/MDd");
            CFamilyHelper.FileConfig.ConvertRuntimeLibrary("MultiThreadedDebugDll").Should().Be("/MDd");

            Action action = () => CFamilyHelper.FileConfig.ConvertRuntimeLibrary("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported RuntimeLibrary: foo");
        }

        [TestMethod]
        public void ExceptionHandling()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.FileConfig.ConvertExceptionHandling("").Should().Be("");
            CFamilyHelper.FileConfig.ConvertExceptionHandling("false").Should().Be("");
            CFamilyHelper.FileConfig.ConvertExceptionHandling("Async").Should().Be("/EHa");
            CFamilyHelper.FileConfig.ConvertExceptionHandling("Sync").Should().Be("/EHsc");
            CFamilyHelper.FileConfig.ConvertExceptionHandling("SyncCThrow").Should().Be("/EHs");

            Action action = () => CFamilyHelper.FileConfig.ConvertExceptionHandling("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported ExceptionHandling: foo");
        }

        [TestMethod]
        public void EnhancedInstructionSet()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("").Should().Be("");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("NotSet").Should().Be("");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("AdvancedVectorExtensions").Should().Be("/arch:AVX");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("AdvancedVectorExtensions2").Should().Be("/arch:AVX2");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("StreamingSIMDExtensions").Should().Be("/arch:SSE");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("StreamingSIMDExtensions2").Should().Be("/arch:SSE2");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("NoExtensions").Should().Be("/arch:IA32");

            Action action = () => CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported EnableEnhancedInstructionSet: foo");
        }

        [TestMethod]
        public void BasicRuntimeChecks()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.FileConfig.ConvertBasicRuntimeChecks("").Should().Be("");
            CFamilyHelper.FileConfig.ConvertBasicRuntimeChecks("Default").Should().Be("");
            CFamilyHelper.FileConfig.ConvertBasicRuntimeChecks("StackFrameRuntimeCheck").Should().Be("/RTCs");
            CFamilyHelper.FileConfig.ConvertBasicRuntimeChecks("UninitializedLocalUsageCheck").Should().Be("/RTCu");
            CFamilyHelper.FileConfig.ConvertBasicRuntimeChecks("EnableFastChecks").Should().Be("/RTC1");

            Action action = () => CFamilyHelper.FileConfig.ConvertBasicRuntimeChecks("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported BasicRuntimeChecks: foo");
        }

        [TestMethod]
        public void PrecompiledHeader()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.FileConfig.ConvertPrecompiledHeader("", "stdafx.h").Should().Be("");
            CFamilyHelper.FileConfig.ConvertPrecompiledHeader("Use", "stdafx.h").Should().Be("/Yustdafx.h");
            CFamilyHelper.FileConfig.ConvertPrecompiledHeader("Create", "stdafx.h").Should().Be("/Ycstdafx.h");
            CFamilyHelper.FileConfig.ConvertPrecompiledHeader("Use", "").Should().Be("/Yu");
            CFamilyHelper.FileConfig.ConvertPrecompiledHeader("Create", "").Should().Be("/Yc");

            Action action = () => CFamilyHelper.FileConfig.ConvertPrecompiledHeader("foo", "");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported PrecompiledHeader: foo");
        }

        [TestMethod]
        public void ProcessFile_HeaderFile_IsNotProcessed()
        {
            // Arrange
            var daemonMock = new Mock<ISonarLintDaemon>();
            var issueConsumerMock = new Mock<IIssueConsumer>();
            var loggerMock = new Mock<ILogger>();

            var projectItemMock = new Mock<ProjectItem>();

            // Act
            CFamilyHelper.ProcessFile(daemonMock.Object, issueConsumerMock.Object,
                loggerMock.Object, projectItemMock.Object, "c:\\dummy\\file.h", "charset");

            // Assert
            AssertMessageLogged(loggerMock, "Cannot analyze header files. File: 'c:\\dummy\\file.h'");
            AssertFileNotAnalysed(daemonMock);
        }

        [TestMethod]
        public void ProcessFile_FileOutsideSolution_IsNotProcessed()
        {
            // Arrange
            var daemonMock = new Mock<ISonarLintDaemon>();
            var issueConsumerMock = new Mock<IIssueConsumer>();
            var loggerMock = new Mock<ILogger>();

            var projectItemMock = CreateProjectItemWithProject("c:\\foo\\SingleFileISense\\xxx.vcxproj");

            // Act
            CFamilyHelper.ProcessFile(daemonMock.Object, issueConsumerMock.Object,
                loggerMock.Object, projectItemMock.Object, "c:\\dummy\\file.cpp", "charset");

            // Assert
            AssertMessageLogged(loggerMock,
                "Unable to retrieve the configuration for file 'c:\\dummy\\file.cpp'. Check the file is part of a project in the current solution.");
            AssertFileNotAnalysed(daemonMock);
        }

        [TestMethod]
        public void ProcessFile_ErrorGetting_IsHandled()
        {
            // Arrange
            var daemonMock = new Mock<ISonarLintDaemon>();
            var issueConsumerMock = new Mock<IIssueConsumer>();
            var loggerMock = new Mock<ILogger>();

            var projectItemMock = CreateProjectItemWithProject("c:\\foo\\xxx.vcxproj");

            // Act
            CFamilyHelper.ProcessFile(daemonMock.Object, issueConsumerMock.Object,
                loggerMock.Object, projectItemMock.Object, "c:\\dummy\\file.cpp", "charset");

            // Assert
            AssertPartialMessageLogged(loggerMock,
                "Unable to collect C/C++ configuration for c:\\dummy\\file.cpp: ");
            AssertFileNotAnalysed(daemonMock);
        }

        [TestMethod]
        public void TryGetConfig_ErrorsAreLogged()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            string sqLanguage;

            // Act
            using (new AssertIgnoreScope())
            {
                string json = CFamilyHelper.TryGetConfig(loggerMock.Object, null, "c:\\dummy", out sqLanguage);

                // Assert
                AssertPartialMessageLogged(loggerMock,
                    "Unable to collect C/C++ configuration for c:\\dummy: ");
                json.Should().BeNull();
                sqLanguage.Should().BeNull();
            }
        }

        [TestMethod]
        public void IsFileInSolution_NullItem_ReturnsFalse()
        {
            // Arrange and Act
            var result = CFamilyHelper.IsFileInSolution(null);

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsFileInSolution_SingleFileIntelliSense_ReturnsFalse()
        {
            // Arrange
            var projectItemMock = CreateProjectItemWithProject("c:\\foo\\SingleFileISense\\xxx.vcxproj");

            // Act
            var result = CFamilyHelper.IsFileInSolution(projectItemMock.Object);

            // Assert
            result.Should().BeFalse();
            projectItemMock.Verify(x => x.ContainingProject, Times.Once); // check the test hit the expected path
        }

        [TestMethod]
        public void IsFileInSolution_ExceptionThrown_ReturnsFalse()
        {
            // Arrange
            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(i => i.ContainingProject).Throws<System.Runtime.InteropServices.COMException>();

            // Act
            var result = CFamilyHelper.IsFileInSolution(projectItemMock.Object);

            // Assert
            result.Should().BeFalse();
            projectItemMock.Verify(x => x.ContainingProject, Times.Once); // check the test hit the expected path
        }

        [TestMethod]
        public void IsHeaderFile_DotHExtension_ReturnsTrue()
        {
            // Act and Assert
            CFamilyHelper.IsHeaderFile("c:\\aaa\\bbbb\\file.h").Should().Be(true);
            CFamilyHelper.IsHeaderFile("c:\\aaa\\bbbb\\FILE.H").Should().Be(true);
        }

        [TestMethod]
        public void IsHeaderFile_NotDotHExtension_ReturnsFalse()
        {
            // Act and Assert
            CFamilyHelper.IsHeaderFile("c:\\aaa\\bbbb\\file.hh").Should().Be(false);
            CFamilyHelper.IsHeaderFile("c:\\aaa\\bbbb\\FILE.cpp").Should().Be(false);
            CFamilyHelper.IsHeaderFile("c:\\aaa\\bbbb\\noextension").Should().Be(false);
        }

        private Mock<ProjectItem> CreateProjectItemWithProject(string projectName)
        {
            var projectItemMock = new Mock<ProjectItem>();
            var projectMock = new ProjectMock(projectName);
            projectItemMock.Setup(i => i.ContainingProject).Returns(projectMock);
            projectItemMock.Setup(i => i.ConfigurationManager).Returns(projectMock.ConfigurationManager);
            projectMock.ConfigurationManager.ActiveConfiguration = new ConfigurationMock("dummy config");

            return projectItemMock;
        }

        private static void AssertFileNotAnalysed(Mock<ISonarLintDaemon> daemonMock)
        {
            daemonMock.Verify(d => d.RequestAnalysis(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IIssueConsumer>()),
                Times.Never);
        }

        private static void AssertMessageLogged(Mock<ILogger> loggerMock, string message)
        {
            loggerMock.Verify(x => x.WriteLine(It.Is<string>(
                s => s.Equals(message))), Times.Once);
        }
        private static void AssertPartialMessageLogged(Mock<ILogger> loggerMock, string message)
        {
            loggerMock.Verify(x => x.WriteLine(It.Is<string>(
                s => s.Contains(message))), Times.Once);
        }
    }
}
