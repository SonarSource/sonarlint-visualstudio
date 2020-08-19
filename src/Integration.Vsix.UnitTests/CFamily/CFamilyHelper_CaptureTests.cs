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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyHelper_CaptureTests
    {
        private const string FileName = @"C:\absolute\file.cpp";
        private const string ProjectPath = @"C:\absolute\path\project.vcxproj";

        [TestMethod]
        public void ToCaptures_Test1()
        {
            var fileConfig = new CFamilyHelper.FileConfig
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
                AbsoluteProjectPath = ProjectPath,
                CompilerVersion = "19.00.00",
            };

            CFamilyHelper.Capture[] captures = CFamilyHelper.Capture.ToCaptures(fileConfig, FileName, out _);
            CFamilyHelper.Capture p = captures[0];
            CFamilyHelper.Capture c = captures[1];

            p.Compiler.Should().Be("msvc-cl");
            p.CompilerVersion.Should().Be("19.00.00");
            p.X64.Should().Be(false);
            p.Cwd.Should().Be(@"C:\absolute\path");
            p.Executable.Should().Be("cl.exe");

            c.Compiler.Should().Be("msvc-cl");
            c.CompilerVersion.Should().BeNull("otherwise will be considered as probe");
            c.Cwd.Should().Be(p.Cwd);
            c.Executable.Should().BeSameAs(p.Executable, "otherwise won't be associated with probe");
            c.Env.Should().Equal(new[] { "INCLUDE=sys1;sys2;" });
            c.Cmd.Should().Equal(new[]
            {
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
        public void ToCaptures_Test2()
        {
            var fileConfig = new CFamilyHelper.FileConfig()
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
                CompilerVersion = "19.00.00",
            };

            CFamilyHelper.Capture[] captures = CFamilyHelper.Capture.ToCaptures(fileConfig, FileName, out _);
            CFamilyHelper.Capture p = captures[0];
            CFamilyHelper.Capture c = captures[1];

            p.CompilerVersion.Should().Be("19.00.00");
            p.X64.Should().Be(true);

            c.Cmd.Should().Equal(new[]
            {
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
            CFamilyHelper.Capture.IsPlatformX64("Win32").Should().Be(false);
            CFamilyHelper.Capture.IsPlatformX64("x64").Should().Be(true);

            Action action = () => CFamilyHelper.Capture.IsPlatformX64("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported PlatformName: foo");
        }

        [TestMethod]
        [DynamicData(nameof(AdditionalOptionsTestCases))]
        public void AdditionalOptions(dynamic testCase)
        {
            string optionsString = testCase.optionsString;
            string[] expectedOptions = testCase.expectedOptions;
            CFamilyHelper.Capture.GetAdditionalOptions(optionsString).Should().BeEquivalentTo(expectedOptions);
        }

        public static IEnumerable<object[]> AdditionalOptionsTestCases
        {
            get
            {
                return new[]
                {
                    new object[] {new {optionsString = "/arch:\"IA32\"", expectedOptions = new[] { "/arch:\"IA32\"" } }},
                    new object[] {new {optionsString = "/D A", expectedOptions = new[] {"/D", "A"}}},
                    new object[] {new {optionsString = "/D \"A\"", expectedOptions = new[] {"/D", "\"A\"" } }},
                    new object[] {new {optionsString = "/D \"A= str\"", expectedOptions = new[] {"/D", "\"A= str\"" } }},
                    new object[] {new {optionsString = "/U \"A\"", expectedOptions = new[] {"/U", "\"A\"" } }},
                    new object[] {new {optionsString = "/U \" A\"", expectedOptions = new[] {"/U", "\" A\"" } }},
                    new object[] {new {optionsString = "/D \"A# str\"", expectedOptions = new[] {"/D", "\"A# str\"" } }},
                    new object[] {new {optionsString = "/FI \"C:\\Repos\\a.h\"", expectedOptions = new[] {"/FI", "\"C:\\Repos\\a.h\"" } }},
                    new object[]
                    {
                        new
                        {
                            optionsString = "/D \"my test\" /D test",
                            expectedOptions = new[] {"/D", "\"my test\"", "/D", "test"}
                        }
                    },
                    new object[]
                    {
                        new
                        {
                            optionsString = "/D test /D test",
                            expectedOptions = new[] {"/D", "test", "/D", "test"}
                        }
                    },
                    new object[]
                    {
                        new
                        {
                            optionsString = "/D test /D \"my test\"",
                            expectedOptions = new[] {"/D", "test", "/D", "\"my test\"" }
                        }
                    },
                    new object[]
                    {
                        new
                        {
                            optionsString = "/D \"my test\" /D \"my test\"",
                            expectedOptions = new[] {"/D", "\"my test\"", "/D", "\"my test\"" }
                        }
                    },
                    new object[]
                    {
                        new
                        {
                            optionsString = "/D \"my test\" /D \"my test\" /D test",
                            expectedOptions = new[] {"/D", "\"my test\"", "/D", "\"my test\"", "/D", "test"}
                        }
                    }
                };
            }
        }

        [TestMethod]
        public void ConvertCompileAsAndGetSqLanguage()
        {
            string cfamilyLanguage;
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.Capture.ConvertCompileAsAndGetLanguage("", FileName, out cfamilyLanguage).Should().Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.Capture.ConvertCompileAsAndGetLanguage("Default", FileName, out cfamilyLanguage).Should()
                .Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.Capture.ConvertCompileAsAndGetLanguage("Default", @"c:\Foo.cc", out cfamilyLanguage)
                .Should().Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.Capture.ConvertCompileAsAndGetLanguage("Default", @"c:\Foo.cxx", out cfamilyLanguage)
                .Should().Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.Capture.ConvertCompileAsAndGetLanguage("Default", @"c:\Foo.c", out cfamilyLanguage)
                .Should().Be("");
            cfamilyLanguage.Should().Be("c");
            CFamilyHelper.Capture.ConvertCompileAsAndGetLanguage("CompileAsC", FileName, out cfamilyLanguage)
                .Should().Be("/TC");
            cfamilyLanguage.Should().Be("c");
            CFamilyHelper.Capture.ConvertCompileAsAndGetLanguage("CompileAsCpp", FileName, out cfamilyLanguage)
                .Should().Be("/TP");
            cfamilyLanguage.Should().Be("cpp");

            Action action = () =>
                CFamilyHelper.Capture.ConvertCompileAsAndGetLanguage("foo", FileName, out cfamilyLanguage);
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported CompileAs: foo");
        }

        [TestMethod]
        public void CompileAsManaged()
        {
            CFamilyHelper.Capture.ConvertCompileAsManaged("").Should().Be("");
            CFamilyHelper.Capture.ConvertCompileAsManaged("false").Should().Be("");
            CFamilyHelper.Capture.ConvertCompileAsManaged("true").Should().Be("/clr");
            CFamilyHelper.Capture.ConvertCompileAsManaged("Pure").Should().Be("/clr:pure");
            CFamilyHelper.Capture.ConvertCompileAsManaged("Safe").Should().Be("/clr:safe");

            Action action = () => CFamilyHelper.Capture.ConvertCompileAsManaged("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported CompileAsManaged: foo");
        }

        [TestMethod]
        public void RuntimeLibrary()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.Capture.ConvertRuntimeLibrary("").Should().Be("");

            CFamilyHelper.Capture.ConvertRuntimeLibrary("MultiThreaded").Should().Be("/MT");

            CFamilyHelper.Capture.ConvertRuntimeLibrary("MultiThreadedDebug").Should().Be("/MTd");

            CFamilyHelper.Capture.ConvertRuntimeLibrary("MultiThreadedDLL").Should().Be("/MD");
            CFamilyHelper.Capture.ConvertRuntimeLibrary("MultiThreadedDll").Should().Be("/MD");

            CFamilyHelper.Capture.ConvertRuntimeLibrary("MultiThreadedDebugDLL").Should().Be("/MDd");
            CFamilyHelper.Capture.ConvertRuntimeLibrary("MultiThreadedDebugDll").Should().Be("/MDd");

            Action action = () => CFamilyHelper.Capture.ConvertRuntimeLibrary("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported RuntimeLibrary: foo");
        }

        [TestMethod]
        public void ExceptionHandling()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.Capture.ConvertExceptionHandling("").Should().Be("");
            CFamilyHelper.Capture.ConvertExceptionHandling("false").Should().Be("");
            CFamilyHelper.Capture.ConvertExceptionHandling("Async").Should().Be("/EHa");
            CFamilyHelper.Capture.ConvertExceptionHandling("Sync").Should().Be("/EHsc");
            CFamilyHelper.Capture.ConvertExceptionHandling("SyncCThrow").Should().Be("/EHs");

            Action action = () => CFamilyHelper.Capture.ConvertExceptionHandling("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported ExceptionHandling: foo");
        }

        [TestMethod]
        public void EnhancedInstructionSet()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.Capture.ConvertEnableEnhancedInstructionSet("").Should().Be("");
            CFamilyHelper.Capture.ConvertEnableEnhancedInstructionSet("NotSet").Should().Be("");
            CFamilyHelper.Capture.ConvertEnableEnhancedInstructionSet("AdvancedVectorExtensions").Should()
                .Be("/arch:AVX");
            CFamilyHelper.Capture.ConvertEnableEnhancedInstructionSet("AdvancedVectorExtensions2").Should()
                .Be("/arch:AVX2");
            CFamilyHelper.Capture.ConvertEnableEnhancedInstructionSet("StreamingSIMDExtensions").Should()
                .Be("/arch:SSE");
            CFamilyHelper.Capture.ConvertEnableEnhancedInstructionSet("StreamingSIMDExtensions2").Should()
                .Be("/arch:SSE2");
            CFamilyHelper.Capture.ConvertEnableEnhancedInstructionSet("NoExtensions").Should().Be("/arch:IA32");

            Action action = () => CFamilyHelper.Capture.ConvertEnableEnhancedInstructionSet("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported EnableEnhancedInstructionSet: foo");
        }

        [TestMethod]
        public void BasicRuntimeChecks()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.Capture.ConvertBasicRuntimeChecks("").Should().Be("");
            CFamilyHelper.Capture.ConvertBasicRuntimeChecks("Default").Should().Be("");
            CFamilyHelper.Capture.ConvertBasicRuntimeChecks("StackFrameRuntimeCheck").Should().Be("/RTCs");
            CFamilyHelper.Capture.ConvertBasicRuntimeChecks("UninitializedLocalUsageCheck").Should().Be("/RTCu");
            CFamilyHelper.Capture.ConvertBasicRuntimeChecks("EnableFastChecks").Should().Be("/RTC1");

            Action action = () => CFamilyHelper.Capture.ConvertBasicRuntimeChecks("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported BasicRuntimeChecks: foo");
        }

        [TestMethod]
        public void PrecompiledHeader()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.Capture.ConvertPrecompiledHeader("", "stdafx.h").Should().Be("");
            CFamilyHelper.Capture.ConvertPrecompiledHeader("Use", "stdafx.h").Should().Be("/Yustdafx.h");
            CFamilyHelper.Capture.ConvertPrecompiledHeader("Create", "stdafx.h").Should().Be("/Ycstdafx.h");
            CFamilyHelper.Capture.ConvertPrecompiledHeader("Use", "").Should().Be("/Yu");
            CFamilyHelper.Capture.ConvertPrecompiledHeader("Create", "").Should().Be("/Yc");
            CFamilyHelper.Capture.ConvertPrecompiledHeader("NotUsing", "XXX").Should().Be("");

            Action action = () => CFamilyHelper.Capture.ConvertPrecompiledHeader("foo", "");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported PrecompiledHeader: foo");
        }

        [TestMethod]
        public void LanguageStandard()
        {
            CFamilyHelper.Capture.ConvertLanguageStandard("").Should().Be("");
            CFamilyHelper.Capture.ConvertLanguageStandard("Default").Should().Be("");
            CFamilyHelper.Capture.ConvertLanguageStandard(null).Should().Be("");
            CFamilyHelper.Capture.ConvertLanguageStandard("stdcpplatest").Should().Be("/std:c++latest");
            CFamilyHelper.Capture.ConvertLanguageStandard("stdcpp17").Should().Be("/std:c++17");
            CFamilyHelper.Capture.ConvertLanguageStandard("stdcpp14").Should().Be("/std:c++14");

            Action action = () => CFamilyHelper.Capture.ConvertLanguageStandard("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported LanguageStandard: foo");
        }
    }
}
