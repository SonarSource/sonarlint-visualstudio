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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.VCProjectEngine;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class FileConfigTests
    {
        private const string FileName = @"C:\absolute\path\to\file.cpp";

        [TestMethod]
        public void TryGet_NoVCProject_ReturnsNull()
        {
            var dteProjectItemMock = new Mock<ProjectItem>();
            var dteProjectMock = new Mock<Project>();

            dteProjectMock.Setup(x => x.Object).Returns(null);
            dteProjectItemMock.Setup(x => x.Object).Returns(Mock.Of<VCFile>());
            dteProjectItemMock.Setup(x => x.ContainingProject).Returns(dteProjectMock.Object);

            CFamilyHelper.FileConfig.TryGet(dteProjectItemMock.Object, "c:\\path")
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

            CFamilyHelper.FileConfig.TryGet(dteProjectItemMock.Object, "c:\\path")
                .Should().BeNull();
        }

        [TestMethod]
        public void GetPotentiallyUnsupportedPropertyValue_PropertySupported_ReturnsValue()
        {
            // Arrange
            var settingsMock = new Mock<IVCRulePropertyStorage>();
            settingsMock.Setup(x => x.GetEvaluatedPropertyValue("propertyName1"))
                .Returns("propertyValue");

            // Act
            var result =
                CFamilyHelper.FileConfig.GetPotentiallyUnsupportedPropertyValue(settingsMock.Object, "propertyName1",
                    "default xxx");

            // Assert
            result.Should().Be("propertyValue");
        }

        [TestMethod]
        public void GetPotentiallyUnsupportedPropertyValue_PropertyUnsupported_ReturnsDefaultValue()
        {
            // Arrange
            var settingsMock = new Mock<IVCRulePropertyStorage>();
            var methodCalled = false;
            settingsMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>()))
                .Callback(() => methodCalled = true)
                .Throws(new InvalidCastException("xxx"));

            // Act - exception should be handled
            var result =
                CFamilyHelper.FileConfig.GetPotentiallyUnsupportedPropertyValue(settingsMock.Object, "propertyName1",
                    "default xxx");

            // Assert
            result.Should().Be("default xxx");
            methodCalled.Should().BeTrue(); // Sanity check that the test mock was invoked
        }

        [TestMethod]
        public void GetPotentiallyUnsuppertedPropertyValue_CriticalException_IsNotSuppressed()
        {
            // Arrange
            var settingsMock = new Mock<IVCRulePropertyStorage>();
            settingsMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>()))
                .Throws(new StackOverflowException("foo"));

            // Act and Assert
            Action act = () =>
                CFamilyHelper.FileConfig.GetPotentiallyUnsupportedPropertyValue(settingsMock.Object, "propertyName1",
                    "default xxx");

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("foo");
        }

        [TestMethod]
        public void PlatformName()
        {
            CFamilyHelper.FileConfig.IsPlatformX64("Win32").Should().Be(false);
            CFamilyHelper.FileConfig.IsPlatformX64("x64").Should().Be(true);

            Action action = () => CFamilyHelper.FileConfig.IsPlatformX64("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported PlatformName: foo");
        }

        [TestMethod]
        [DynamicData(nameof(AdditionalOptionsTestCases))]
        public void AdditionalOptions(dynamic testCase)
        {
            string optionsString = testCase.optionsString;
            string[] expectedOptions = testCase.expectedOptions;
            CFamilyHelper.FileConfig.GetAdditionalOptions(optionsString).Should().BeEquivalentTo(expectedOptions);
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
        public void PlatformToolset()
        {
            CFamilyHelper.FileConfig.GetCompilerVersion("v90", "").Should().Be("15.00.00");

            CFamilyHelper.FileConfig.GetCompilerVersion("v100", "").Should().Be("16.00.00");

            CFamilyHelper.FileConfig.GetCompilerVersion("v110", "").Should().Be("17.00.00");
            CFamilyHelper.FileConfig.GetCompilerVersion("v110_xp", "").Should().Be("17.00.00");

            CFamilyHelper.FileConfig.GetCompilerVersion("v120", "").Should().Be("18.00.00");
            CFamilyHelper.FileConfig.GetCompilerVersion("v120_xp", "").Should().Be("18.00.00");

            CFamilyHelper.FileConfig.GetCompilerVersion("v140", "").Should().Be("19.00.00");
            CFamilyHelper.FileConfig.GetCompilerVersion("v140_xp", "").Should().Be("19.00.00");

            CFamilyHelper.FileConfig.GetCompilerVersion("v141", "14.10.00").Should().Be("19.10.00");
            CFamilyHelper.FileConfig.GetCompilerVersion("v141_xp", "14.10.50").Should().Be("19.10.50");

            CFamilyHelper.FileConfig.GetCompilerVersion("v142", "14.25.28612").Should().Be("19.25.28612");

            Action action = () => CFamilyHelper.FileConfig.GetCompilerVersion("v142", "2132");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported VCToolsVersion: 2132");

            action = () => CFamilyHelper.FileConfig.GetCompilerVersion("v143", "14.30.0000");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported PlatformToolset: v143");

            action = () => CFamilyHelper.FileConfig.GetCompilerVersion("", "");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith
                ("The file cannot be analyzed because the platform toolset has not been specified.");
        }

        [TestMethod]
        public void ConvertCompileAsAndGetSqLanguage()
        {
            string cfamilyLanguage;
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("", FileName, out cfamilyLanguage).Should().Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("Default", FileName, out cfamilyLanguage).Should()
                .Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("Default", @"c:\Foo.cc", out cfamilyLanguage)
                .Should().Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("Default", @"c:\Foo.cxx", out cfamilyLanguage)
                .Should().Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("Default", @"c:\Foo.c", out cfamilyLanguage)
                .Should().Be("");
            cfamilyLanguage.Should().Be("c");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("CompileAsC", FileName, out cfamilyLanguage)
                .Should().Be("/TC");
            cfamilyLanguage.Should().Be("c");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("CompileAsCpp", FileName, out cfamilyLanguage)
                .Should().Be("/TP");
            cfamilyLanguage.Should().Be("cpp");

            Action action = () =>
                CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("foo", FileName, out cfamilyLanguage);
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported CompileAs: foo");
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
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported CompileAsManaged: foo");
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
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported RuntimeLibrary: foo");
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
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported ExceptionHandling: foo");
        }

        [TestMethod]
        public void EnhancedInstructionSet()
        {
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("").Should().Be("");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("NotSet").Should().Be("");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("AdvancedVectorExtensions").Should()
                .Be("/arch:AVX");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("AdvancedVectorExtensions2").Should()
                .Be("/arch:AVX2");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("StreamingSIMDExtensions").Should()
                .Be("/arch:SSE");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("StreamingSIMDExtensions2").Should()
                .Be("/arch:SSE2");
            CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("NoExtensions").Should().Be("/arch:IA32");

            Action action = () => CFamilyHelper.FileConfig.ConvertEnableEnhancedInstructionSet("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported EnableEnhancedInstructionSet: foo");
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
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported BasicRuntimeChecks: foo");
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
            CFamilyHelper.FileConfig.ConvertPrecompiledHeader("NotUsing", "XXX").Should().Be("");

            Action action = () => CFamilyHelper.FileConfig.ConvertPrecompiledHeader("foo", "");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported PrecompiledHeader: foo");
        }

        [TestMethod]
        public void LanguageStandard()
        {
            CFamilyHelper.FileConfig.ConvertLanguageStandard("").Should().Be("");
            CFamilyHelper.FileConfig.ConvertLanguageStandard("Default").Should().Be("");
            CFamilyHelper.FileConfig.ConvertLanguageStandard(null).Should().Be("");
            CFamilyHelper.FileConfig.ConvertLanguageStandard("stdcpplatest").Should().Be("/std:c++latest");
            CFamilyHelper.FileConfig.ConvertLanguageStandard("stdcpp17").Should().Be("/std:c++17");
            CFamilyHelper.FileConfig.ConvertLanguageStandard("stdcpp14").Should().Be("/std:c++14");

            Action action = () => CFamilyHelper.FileConfig.ConvertLanguageStandard("foo");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported LanguageStandard: foo");
        }


    }
}
