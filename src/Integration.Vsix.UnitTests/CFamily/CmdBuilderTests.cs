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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.VCProjectEngine;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;


namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CmdBuilderTests
    {

        [TestMethod]
        [DataRow("", "")]
        [DataRow("Default", "")]
        [DataRow("CompileAsC", "/TC")]
        [DataRow("CompileAsCpp", "/TP")]
        [DataRow("Invalid", "Unsupported CompileAs: Invalid")]
        public void ConvertCompileAsAndGetLanguage(string input, string output)
        {
            if ("Invalid".Equals(input))
            {
                Action action = () => CmdBuilder.ConvertCompileAsAndGetLanguage(input);
                action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                    .StartWith(output);
            }
            else
            {
                CmdBuilder.ConvertCompileAsAndGetLanguage(input).Should().Be(output);
            }
        }

        [TestMethod]
        [DataRow("", "")]
        [DataRow("false", "")]
        [DataRow("true", "/clr")]
        [DataRow("Pure", "/clr:pure")]
        [DataRow("Safe", "/clr:safe")]
        [DataRow("Invalid", "Unsupported CompileAsManaged: Invalid")]
        public void ConvertCompileAsManaged(string input, string output)
        {
            if ("Invalid".Equals(input))
            {
                Action action = () => CmdBuilder.ConvertCompileAsManaged(input);
                action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                    .StartWith(output);
            }
            else
            {
                CmdBuilder.ConvertCompileAsManaged(input).Should().Be(output);
            }
        }

        [TestMethod]
        [DataRow("", "")]
        [DataRow("MultiThreaded", "/MT")]
        [DataRow("MultiThreadedDebug", "/MTd")]
        [DataRow("MultiThreadedDLL", "/MD")]
        [DataRow("MultiThreadedDll", "/MD")]
        [DataRow("MultiThreadedDebugDLL", "/MDd")]
        [DataRow("MultiThreadedDebugDll", "/MDd")]
        [DataRow("Invalid", "Unsupported RuntimeLibrary: Invalid")]
        public void ConvertRuntimeLibrary(string input, string output)
        {
            if ("Invalid".Equals(input))
            {
                Action action = () => CmdBuilder.ConvertRuntimeLibrary(input);
                action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                    .StartWith(output);
            }
            else
            {
                CmdBuilder.ConvertRuntimeLibrary(input).Should().Be(output);
            }
        }

        [TestMethod]
        [DataRow("", "")]
        [DataRow("false", "")]
        [DataRow("Async", "/EHa")]
        [DataRow("Sync", "/EHsc")]
        [DataRow("SyncCThrow", "/EHs")]
        [DataRow("Invalid", "Unsupported ExceptionHandling: Invalid")]
        public void ConvertExceptionHandling(string input, string output)
        {
            if ("Invalid".Equals(input))
            {
                Action action = () => CmdBuilder.ConvertExceptionHandling(input);
                action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                    .StartWith(output);
            }
            else
            {
                CmdBuilder.ConvertExceptionHandling(input).Should().Be(output);
            }
        }

        [TestMethod]
        [DataRow("", "")]
        [DataRow("NotSet", "")]
        [DataRow("AdvancedVectorExtensions", "/arch:AVX")]
        [DataRow("AdvancedVectorExtensions2", "/arch:AVX2")]
        [DataRow("AdvancedVectorExtensions512", "/arch:AVX512")]
        [DataRow("StreamingSIMDExtensions", "/arch:SSE")]
        [DataRow("StreamingSIMDExtensions2", "/arch:SSE2")]
        [DataRow("NoExtensions", "/arch:IA32")]
        [DataRow("Invalid", "Unsupported EnableEnhancedInstructionSet: Invalid")]
        public void ConvertEnableEnhancedInstructionSet(string input, string output)
        {
            if ("Invalid".Equals(input))
            {
                Action action = () => CmdBuilder.ConvertEnableEnhancedInstructionSet(input);
                action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                    .StartWith(output);
            }
            else
            {
                CmdBuilder.ConvertEnableEnhancedInstructionSet(input).Should().Be(output);
            }
        }

        [TestMethod]
        [DataRow("", "")]
        [DataRow("Default", "")]
        [DataRow("StackFrameRuntimeCheck", "/RTCs")]
        [DataRow("UninitializedLocalUsageCheck", "/RTCu")]
        [DataRow("EnableFastChecks", "/RTC1")]
        [DataRow("Invalid", "Unsupported BasicRuntimeChecks: Invalid")]
        public void ConvertBasicRuntimeChecks(string input, string output)
        {
            if ("Invalid".Equals(input))
            {
                Action action = () => CmdBuilder.ConvertBasicRuntimeChecks(input);
                action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                    .StartWith(output);
            }
            else
            {
                CmdBuilder.ConvertBasicRuntimeChecks(input).Should().Be(output);
            }
        }

        [TestMethod]
        [DataRow("", "")]
        [DataRow("Default", "")]
        [DataRow(null, "")]
        [DataRow("stdcpplatest", "/std:c++latest")]
        [DataRow("stdcpp20", "/std:c++20")]
        [DataRow("stdcpp17", "/std:c++17")]
        [DataRow("stdcpp14", "/std:c++14")]
        [DataRow("Invalid", "Unsupported LanguageStandard: Invalid")]
        public void ConvertCppStandard(string input, string output)
        {
            if ("Invalid".Equals(input))
            {
                Action action = () => CmdBuilder.ConvertCppStandard(input);
                action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                    .StartWith(output);
            }
            else
            {
                CmdBuilder.ConvertCppStandard(input).Should().Be(output);
            }
        }

        [TestMethod]
        [DataRow("", "")]
        [DataRow("Default", "")]
        [DataRow("16Bytes", "/Zp16")]
        [DataRow("8Bytes", "/Zp8")]
        [DataRow("4Bytes", "/Zp4")]
        [DataRow("2Bytes", "/Zp2")]
        [DataRow("1Bytes", "/Zp1")]
        [DataRow("Invalid", "Unsupported StructMemberAlignment: Invalid")]
        public void ConvertStructMemberAlignment(string input, string output)
        {
            if ("Invalid".Equals(input))
            {
                Action action = () => CmdBuilder.ConvertStructMemberAlignment(input);
                action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                    .StartWith(output);
            }
            else
            {
                CmdBuilder.ConvertStructMemberAlignment(input).Should().Be(output);
            }
        }

        [TestMethod]
        [DataRow("", "", "")]
        [DataRow("CompileAsWinRT", "true", "/ZW ")]
        [DataRow("CompileAsWinRT", "false", "")]
        [DataRow("OpenMPSupport", "true", "/openmp ")]
        [DataRow("OpenMPSupport", "false", "")]
        [DataRow("RuntimeTypeInfo", "false", "/GR- ")]
        [DataRow("RuntimeTypeInfo", "true", "")]
        [DataRow("DisableLanguageExtensions", "true", "/Za ")]
        [DataRow("DisableLanguageExtensions", "false", "")]
        [DataRow("TreatWChar_tAsBuiltInType", "true", "")]
        [DataRow("TreatWChar_tAsBuiltInType", "false", "/Zc:wchar_t- ")]
        [DataRow("ForceConformanceInForLoopScope", "true", "")]
        [DataRow("ForceConformanceInForLoopScope", "false", "/Zc:forScope- ")]
        [DataRow("IgnoreStandardIncludePath", "true", "/X ")]
        [DataRow("IgnoreStandardIncludePath", "false", "")]
        [DataRow("OmitDefaultLibName", "true", "/Zl ")]
        [DataRow("OmitDefaultLibName", "false", "")]
        [DataRow("UndefineAllPreprocessorDefinitions", "true", "/u ")]
        [DataRow("UndefineAllPreprocessorDefinitions", "false", "")]
        [DataRow("UseStandardPreprocessor", "true", "/Zc:preprocessor ")]
        [DataRow("UseStandardPreprocessor", "false", "/Zc:preprocessor- ")]
        [DataRow("ConformanceMode", "true", "/permissive- ")]
        [DataRow("ConformanceMode", "false", "/permissive ")]
        [DataRow("LanguageStandard", "stdcpplatest", "/std:c++latest ")]
        [DataRow("LanguageStandard", "stdcpp20", "/std:c++20 ")]
        [DataRow("LanguageStandard", "stdcpp17", "/std:c++17 ")]
        [DataRow("LanguageStandard", "stdcpp14", "/std:c++14 ")]
        [DataRow("LanguageStandard", "Default", "")]
        [DataRow("ExceptionHandling", "Sync", "/EHsc ")]
        [DataRow("ExceptionHandling", "SyncCThrow", "/EHs ")]
        [DataRow("ExceptionHandling", "Async", "/EHa ")]
        [DataRow("ExceptionHandling", "false", "")]
        [DataRow("EnableEnhancedInstructionSet", "NotSet", "")]
        [DataRow("EnableEnhancedInstructionSet", "AdvancedVectorExtensions", "/arch:AVX ")]
        [DataRow("EnableEnhancedInstructionSet", "AdvancedVectorExtensions2", "/arch:AVX2 ")]
        [DataRow("EnableEnhancedInstructionSet", "AdvancedVectorExtensions512", "/arch:AVX512 ")]
        [DataRow("EnableEnhancedInstructionSet", "StreamingSIMDExtensions", "/arch:SSE ")]
        [DataRow("EnableEnhancedInstructionSet", "StreamingSIMDExtensions2", "/arch:SSE2 ")]
        [DataRow("EnableEnhancedInstructionSet", "NoExtensions", "/arch:IA32 ")]
        [DataRow("CompileAsManaged", "false", "")]
        [DataRow("CompileAsManaged", "true", "/clr ")]
        [DataRow("CompileAsManaged", "Pure", "/clr:pure ")]
        [DataRow("CompileAsManaged", "Safe", "/clr:safe ")]
        [DataRow("RuntimeLibrary", "MultiThreaded", "/MT ")]
        [DataRow("RuntimeLibrary", "MultiThreadedDebug", "/MTd ")]
        [DataRow("RuntimeLibrary", "MultiThreadedDLL", "/MD ")]
        [DataRow("RuntimeLibrary", "MultiThreadedDebugDLL", "/MDd ")]
        [DataRow("BasicRuntimeChecks", "Default", "")]
        [DataRow("BasicRuntimeChecks", "StackFrameRuntimeCheck", "/RTCs ")]
        [DataRow("BasicRuntimeChecks", "UninitializedLocalUsageCheck", "/RTCu ")]
        [DataRow("BasicRuntimeChecks", "EnableFastChecks", "/RTC1 ")]
        [DataRow("StructMemberAlignment", "Default", "")]
        [DataRow("StructMemberAlignment", "16Bytes", "/Zp16 ")]
        [DataRow("StructMemberAlignment", "8Bytes", "/Zp8 ")]
        [DataRow("StructMemberAlignment", "4Bytes", "/Zp4 ")]
        [DataRow("StructMemberAlignment", "2Bytes", "/Zp2 ")]
        [DataRow("StructMemberAlignment", "1Bytes", "/Zp1 ")]
        [DataRow("CompileAs", "Default", "")]
        [DataRow("CompileAs", "CompileAsC", "/TC ")]
        [DataRow("CompileAs", "CompileAsCpp", "/TP ")]
        [DataRow("AdditionalIncludeDirectories", "a;;b;c", "/I\"a\" /I\"b\" /I\"c\" ")]
        [DataRow("AdditionalIncludeDirectories", ";;;", "")]
        [DataRow("PreprocessorDefinitions", "a;;b;;c;", "/D\"a\" /D\"b\" /D\"c\" ")]
        [DataRow("UndefinePreprocessorDefinitions", ";a;;b;;c;", "/Ua /Ub /Uc ")]
        [DataRow("ForcedIncludeFiles", "a/;;b\\;c\\\\", "/FI\"a\" /FI\"b\" /FI\"c\" ")]
        [DataRow("ForcedIncludeFiles", "\"a\";;b;\"c\"", "/FI\"a\" /FI\"b\" /FI\"c\" ")]
        [DataRow("AdditionalOptions", "/DA /arch:AVX /std:c++17", "/DA /arch:AVX /std:c++17 ")]
        [DataRow("AdditionalOptions", "/MD /Zc:wchar_t-", "/MD /Zc:wchar_t- ")]
        public void AddOptFromProperties(string input, string output, string cmd)
        {
            var cmdBuilder = new CmdBuilder(false);
            var settingsMock = new Mock<IVCRulePropertyStorage>();
            settingsMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>())).Returns<string>(s => s == input ? output : "");

            cmdBuilder.AddOptFromProperties(settingsMock.Object);

            cmdBuilder.GetFullCmd().Should().Be(cmd);
            cmdBuilder.HeaderFileLang.Should().Be("");
        }

        [TestMethod]
        [DataRow("Default", "cpp")]
        [DataRow("CompileAsC", "c")]
        [DataRow("CompileAsCpp", "cpp")]
        public void HeaderFileLang(string compileAs, string lang)
        {
            var cmdBuilder = new CmdBuilder(true);
            var settingsMock = new Mock<IVCRulePropertyStorage>();
            settingsMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>())).Returns<string>(s => s == "CompileAs" ? compileAs : "");

            cmdBuilder.AddOptFromProperties(settingsMock.Object);

            cmdBuilder.GetFullCmd().Should().Be("");
            cmdBuilder.HeaderFileLang.Should().Be(lang);
        }

        [TestMethod]
        [DataRow("Create", false, "", "/Yc\"C:\\pch.h\" ")]
        [DataRow("Create", true, "", "/Yc\"C:\\pch.h\" ")]
        [DataRow("Create", true, "C:\\a.h", "/FI\"C:\\a.h\" /Yc\"C:\\pch.h\" ")]
        [DataRow("Use", false, "", "/Yu\"C:\\pch.h\" ")]
        [DataRow("Use", true, "", "/Yu\"C:\\pch.h\" /FI\"C:\\pch.h\" ")]
        [DataRow("Use", true, "C:\\a.h", "/FI\"C:\\a.h\" /Yu\"C:\\pch.h\" ")]
        public void PCHCreate(string mode, bool isHeader, string forceInclude, string command)
        {
            var cmdBuilder = new CmdBuilder(isHeader);
            var settingsMock = new Mock<IVCRulePropertyStorage>();
            settingsMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>())).Returns<string>(s =>
            {
                if (s == "PrecompiledHeader")
                {
                    return mode;
                }
                else if (s == "PrecompiledHeaderFile")
                {
                    return "C:\\pch.h";
                }
                else if (s == "ForcedIncludeFiles")
                {
                    return forceInclude;
                }
                else
                {
                    return "";
                }
            });

            cmdBuilder.AddOptFromProperties(settingsMock.Object);
            cmdBuilder.GetFullCmd().Should().Be(command);
        }

        [TestMethod]
        public void PCHUse()
        {
            var cmdBuilder = new CmdBuilder(false);
            var settingsMock = new Mock<IVCRulePropertyStorage>();
            settingsMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>())).Returns<string>(s =>
            {
                if (s == "PrecompiledHeader")
                {
                    return "Create";
                }
                else if (s == "PrecompiledHeaderFile")
                {
                    return "C:\\pch.h";
                }
                else
                {
                    return "";
                }
            });

            cmdBuilder.AddOptFromProperties(settingsMock.Object);
            cmdBuilder.GetFullCmd().Should().Be("/Yc\"C:\\pch.h\" ");
            cmdBuilder.HeaderFileLang.Should().Be("");
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
                CmdBuilder.GetPotentiallyUnsupportedPropertyValue(settingsMock.Object, "propertyName1",
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
                CmdBuilder.GetPotentiallyUnsupportedPropertyValue(settingsMock.Object, "propertyName1",
                    "default xxx");

            // Assert
            result.Should().Be("default xxx");
            methodCalled.Should().BeTrue(); // Sanity check that the test mock was invoked
        }

        [TestMethod]
        public void GetPotentiallyUnsupportedPropertyValue_CriticalException_IsNotSuppressed()
        {
            // Arrange
            var settingsMock = new Mock<IVCRulePropertyStorage>();
            settingsMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>()))
                .Throws(new StackOverflowException("foo"));

            // Act and Assert
            Action act = () =>
                CmdBuilder.GetPotentiallyUnsupportedPropertyValue(settingsMock.Object, "propertyName1",
                    "default xxx");

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("foo");
        }

        [TestMethod]
        [DataRow("a/b/c/", "\"a\\b\\c\"")]
        [DataRow("a\\b\\c\\", "\"a\\b\\c\"")]
        [DataRow("a\\b/c\\\\", "\"a\\b\\c\"")]
        [DataRow("\"a\\\\b/c\"", "\"a\\b\\c\"")]
        public void AdjustPath(string path, string adjustedPath)
        {
            CmdBuilder.AdjustPath(path).Should().Be(adjustedPath);
        }

        [TestMethod]
        [DataRow("a\\b\\c\\", "\"a\\b\\c\\\"")]
        [DataRow("a\\b\\\\c", "\"a\\b\\\\c\"")]
        [DataRow("\"a/b/c\"", "\"a/b/c\"")]
        public void AddQuote(string path, string quotedPath)
        {
            CmdBuilder.AddQuote(path).Should().Be(quotedPath);
        }
    }
}
