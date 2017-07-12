/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using System.Collections.Generic;
using FluentAssertions;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class CFamilyTest
    {
        private const string FileName = @"C:\absolute\path\to\file.cpp";
        [TestMethod]
        public void FileConfig_Test1()
        {
            string sqLanguage;
            CFamily.Capture[] captures = new CFamily.FileConfig()
            {
                PlatformName = "Win32",
                PlatformToolset = "v140",
                IncludeDirectories = "sys1;sys2;",
                AdditionalIncludeDirectories = "dir1;dir2;",
                PreprocessorDefinitions = "D1;D2;",
                UndefinePreprocessorDefinitions = "U1;U2;",
                ForcedIncludeFiles = "h1;h2;",
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
            CFamily.Capture p = captures[0];
            CFamily.Capture c = captures[1];

            p.Compiler.Should().Be("msvc-cl");
            p.StdErr.Should().Be("19.00.00 for x86");
            p.Cwd.Should().Be(@"C:\absolute\path\to");
            p.Executable.Should().Be("cl.exe");

            c.Compiler.Should().Be("msvc-cl");
            c.StdErr.Should().BeNull("otherwise will be considered as probe");
            c.Cwd.Should().Be(p.Cwd);
            c.Executable.Should().BeSameAs(p.Executable, "otherwise won't be associated with probe");
            c.Env.Should().Equal(new List<string>()
            {
                "INCLUDE=sys1;sys2;"
            });
            c.Cmd.Should().Equal(new List<string>() {
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
            CFamily.Capture[] captures = new CFamily.FileConfig()
            {
                PlatformName = "x64",
                PlatformToolset = "v140",
                AdditionalIncludeDirectories = "",
                PreprocessorDefinitions = "",
                UndefinePreprocessorDefinitions = "",
                ForcedIncludeFiles = "",
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
            CFamily.Capture p = captures[0];
            CFamily.Capture c = captures[1];

            p.StdErr.Should().Be("19.00.00 for x64");

            c.Cmd.Should().Equal(new List<string>() {
                "cl.exe",
                "/X",
                "/Yu", "stdafx.h",
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
            CFamily.FileConfig.ConvertPlatformName("Win32").Should().Be("x86");
            CFamily.FileConfig.ConvertPlatformName("x64").Should().Be("x64");

            Action action = () => CFamily.FileConfig.ConvertPlatformName("foo");
            action.ShouldThrow<Exception>().WithMessage("Unsupported PlatformName: foo");
        }

        [TestMethod]
        public void PlatformToolset()
        {
            CFamily.FileConfig.ConvertPlatformToolset("v141").Should().Be("19.10.00");
            CFamily.FileConfig.ConvertPlatformToolset("v141_xp").Should().Be("19.10.00");

            CFamily.FileConfig.ConvertPlatformToolset("v140").Should().Be("19.00.00");
            CFamily.FileConfig.ConvertPlatformToolset("v140_xp").Should().Be("19.00.00");

            CFamily.FileConfig.ConvertPlatformToolset("v120").Should().Be("18.00.00");
            CFamily.FileConfig.ConvertPlatformToolset("v120_xp").Should().Be("18.00.00");

            CFamily.FileConfig.ConvertPlatformToolset("v110").Should().Be("17.00.00");
            CFamily.FileConfig.ConvertPlatformToolset("v110_xp").Should().Be("17.00.00");

            CFamily.FileConfig.ConvertPlatformToolset("v100").Should().Be("16.00.00");

            CFamily.FileConfig.ConvertPlatformToolset("v90").Should().Be("15.00.00");

            Action action = () => CFamily.FileConfig.ConvertPlatformToolset("v142");
            action.ShouldThrow<Exception>().WithMessage("Unsupported PlatformToolset: v142");
        }

        [TestMethod]
        public void ConvertCompileAsAndGetSqLanguage()
        {
            string sqLanguage;
            CFamily.FileConfig.ConvertCompileAsAndGetSqLanguage("Default", FileName, out sqLanguage).Should().Be("");
            sqLanguage.Should().Be("cpp");
            CFamily.FileConfig.ConvertCompileAsAndGetSqLanguage("Default", @"c:\Foo.cc", out sqLanguage).Should().Be("");
            sqLanguage.Should().Be("cpp");
            CFamily.FileConfig.ConvertCompileAsAndGetSqLanguage("Default", @"c:\Foo.cxx", out sqLanguage).Should().Be("");
            sqLanguage.Should().Be("cpp");
            CFamily.FileConfig.ConvertCompileAsAndGetSqLanguage("Default", @"c:\Foo.c", out sqLanguage).Should().Be("");
            sqLanguage.Should().Be("c");
            CFamily.FileConfig.ConvertCompileAsAndGetSqLanguage("CompileAsC", FileName, out sqLanguage).Should().Be("/TC");
            sqLanguage.Should().Be("c");
            CFamily.FileConfig.ConvertCompileAsAndGetSqLanguage("CompileAsCpp", FileName, out sqLanguage).Should().Be("/TP");
            sqLanguage.Should().Be("cpp");

            Action action = () => CFamily.FileConfig.ConvertCompileAsAndGetSqLanguage("foo", FileName, out sqLanguage);
            action.ShouldThrow<Exception>().WithMessage("Unsupported CompileAs: foo");
        }

        [TestMethod]
        public void CompileAsManaged()
        {
            CFamily.FileConfig.ConvertCompileAsManaged("").Should().Be("");
            CFamily.FileConfig.ConvertCompileAsManaged("false").Should().Be("");
            CFamily.FileConfig.ConvertCompileAsManaged("true").Should().Be("/clr");
            CFamily.FileConfig.ConvertCompileAsManaged("Pure").Should().Be("/clr:pure");
            CFamily.FileConfig.ConvertCompileAsManaged("Safe").Should().Be("/clr:safe");

            Action action = () => CFamily.FileConfig.ConvertCompileAsManaged("foo");
            action.ShouldThrow<Exception>().WithMessage("Unsupported CompileAsManaged: foo");
        }

        [TestMethod]
        public void RuntimeLibrary()
        {
            CFamily.FileConfig.ConvertRuntimeLibrary("MultiThreaded").Should().Be("/MT");

            CFamily.FileConfig.ConvertRuntimeLibrary("MultiThreadedDebug").Should().Be("/MTd");

            CFamily.FileConfig.ConvertRuntimeLibrary("MultiThreadedDLL").Should().Be("/MD");
            CFamily.FileConfig.ConvertRuntimeLibrary("MultiThreadedDll").Should().Be("/MD");

            CFamily.FileConfig.ConvertRuntimeLibrary("MultiThreadedDebugDLL").Should().Be("/MDd");
            CFamily.FileConfig.ConvertRuntimeLibrary("MultiThreadedDebugDll").Should().Be("/MDd");

            Action action = () => CFamily.FileConfig.ConvertRuntimeLibrary("foo");
            action.ShouldThrow<Exception>().WithMessage("Unsupported RuntimeLibrary: foo");
        }

        [TestMethod]
        public void ExceptionHandling()
        {
            CFamily.FileConfig.ConvertExceptionHandling("false").Should().Be("");
            CFamily.FileConfig.ConvertExceptionHandling("Async").Should().Be("/EHa");
            CFamily.FileConfig.ConvertExceptionHandling("Sync").Should().Be("/EHsc");
            CFamily.FileConfig.ConvertExceptionHandling("SyncCThrow").Should().Be("/EHs");

            Action action = () => CFamily.FileConfig.ConvertExceptionHandling("foo");
            action.ShouldThrow<Exception>().WithMessage("Unsupported ExceptionHandling: foo");
        }

        [TestMethod]
        public void EnhancedInstructionSet()
        {
            CFamily.FileConfig.ConvertEnableEnhancedInstructionSet("NotSet").Should().Be("");
            CFamily.FileConfig.ConvertEnableEnhancedInstructionSet("AdvancedVectorExtensions").Should().Be("/arch:AVX");
            CFamily.FileConfig.ConvertEnableEnhancedInstructionSet("AdvancedVectorExtensions2").Should().Be("/arch:AVX2");
            CFamily.FileConfig.ConvertEnableEnhancedInstructionSet("StreamingSIMDExtensions").Should().Be("/arch:SSE");
            CFamily.FileConfig.ConvertEnableEnhancedInstructionSet("StreamingSIMDExtensions2").Should().Be("/arch:SSE2");
            CFamily.FileConfig.ConvertEnableEnhancedInstructionSet("NoExtensions").Should().Be("/arch:IA32");

            Action action = () => CFamily.FileConfig.ConvertEnableEnhancedInstructionSet("foo");
            action.ShouldThrow<Exception>().WithMessage("Unsupported EnableEnhancedInstructionSet: foo");
        }

        [TestMethod]
        public void BasicRuntimeChecks()
        {
            CFamily.FileConfig.ConvertBasicRuntimeChecks("Default").Should().Be("");
            CFamily.FileConfig.ConvertBasicRuntimeChecks("StackFrameRuntimeCheck").Should().Be("/RTCs");
            CFamily.FileConfig.ConvertBasicRuntimeChecks("UninitializedLocalUsageCheck").Should().Be("/RTCu");
            CFamily.FileConfig.ConvertBasicRuntimeChecks("EnableFastChecks").Should().Be("/RTC1");

            Action action = () => CFamily.FileConfig.ConvertBasicRuntimeChecks("foo");
            action.ShouldThrow<Exception>().WithMessage("Unsupported BasicRuntimeChecks: foo");
        }
    }
}
