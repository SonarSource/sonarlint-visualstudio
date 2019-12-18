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
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using static Sonarlint.Issue.Types;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyHelperTests
    {
        private const string FileName = @"C:\absolute\path\to\file.cpp";
        [TestMethod]
        public void FileConfig_Test1()
        {
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
            }.ToCaptures(FileName, out _);
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
            }.ToCaptures(FileName, out _);
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
            string cfamilyLanguage;
            // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("", FileName, out cfamilyLanguage).Should().Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("Default", FileName, out cfamilyLanguage).Should().Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("Default", @"c:\Foo.cc", out cfamilyLanguage).Should().Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("Default", @"c:\Foo.cxx", out cfamilyLanguage).Should().Be("");
            cfamilyLanguage.Should().Be("cpp");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("Default", @"c:\Foo.c", out cfamilyLanguage).Should().Be("");
            cfamilyLanguage.Should().Be("c");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("CompileAsC", FileName, out cfamilyLanguage).Should().Be("/TC");
            cfamilyLanguage.Should().Be("c");
            CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("CompileAsCpp", FileName, out cfamilyLanguage).Should().Be("/TP");
            cfamilyLanguage.Should().Be("cpp");

            Action action = () => CFamilyHelper.FileConfig.ConvertCompileAsAndGetLanguage("foo", FileName, out cfamilyLanguage);
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
            CFamilyHelper.FileConfig.ConvertPrecompiledHeader("NotUsing", "XXX").Should().Be("");

            Action action = () => CFamilyHelper.FileConfig.ConvertPrecompiledHeader("foo", "");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported PrecompiledHeader: foo");
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
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith("Unsupported LanguageStandard: foo");
        }

        [TestMethod]
        public void CreateRequest_HeaderFile_IsNotProcessed()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();

            var projectItemMock = new Mock<ProjectItem>();

            // Act
            var request = CFamilyHelper.CreateRequest(loggerMock.Object, projectItemMock.Object, "c:\\dummy\\file.h", DummyCFamilyRulesConfig.CreateValidRulesConfig);

            // Assert
            AssertMessageLogged(loggerMock, "Cannot analyze header files. File: 'c:\\dummy\\file.h'");
            request.Should().BeNull();
        }

        [TestMethod]
        public void CreateRequest_FileOutsideSolution_IsNotProcessed()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();

            var projectItemMock = CreateProjectItemWithProject("c:\\foo\\SingleFileISense\\xxx.vcxproj");

            // Act
            var request = CFamilyHelper.CreateRequest(loggerMock.Object, projectItemMock.Object, "c:\\dummy\\file.cpp", DummyCFamilyRulesConfig.CreateValidRulesConfig);

            // Assert
            AssertMessageLogged(loggerMock,
                "Unable to retrieve the configuration for file 'c:\\dummy\\file.cpp'. Check the file is part of a project in the current solution.");
            request.Should().BeNull();
        }

        [TestMethod]
        public void CreateRequest_ErrorGetting_IsHandled()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();

            var projectItemMock = CreateProjectItemWithProject("c:\\foo\\xxx.vcxproj");

            // Act
            var request = CFamilyHelper.CreateRequest(loggerMock.Object, projectItemMock.Object, "c:\\dummy\\file.cpp", DummyCFamilyRulesConfig.CreateValidRulesConfig);

            // Assert
            AssertPartialMessageLogged(loggerMock,
                "Unable to collect C/C++ configuration for c:\\dummy\\file.cpp: ");
            request.Should().BeNull();
        }

        [TestMethod]
        public void TryGetConfig_ErrorsAreLogged()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();

            // Act
            using (new AssertIgnoreScope())
            {
                var request = CFamilyHelper.TryGetConfig(loggerMock.Object, null, "c:\\dummy");

                // Assert
                AssertPartialMessageLogged(loggerMock,
                    "Unable to collect C/C++ configuration for c:\\dummy: ");
                request.Should().BeNull();
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

        [TestMethod]
        public void GetKeyValueOptionsList_UsingEmbeddedRulesJson()
        {
            var options = CFamilyHelper.GetKeyValueOptionsList(CFamilyHelper.DefaultRulesCache.GetSettings("cpp"));

            // QP option
            CheckHasOption("internal.qualityProfile=");

            // Check a few known rules with parameters
            CheckHasOption("ClassComplexity.maximumClassComplexityThreshold=80");
            CheckHasOption("S1142.max=3");
            CheckHasOption("S1578.format=^[A-Za-z_-][A-Za-z0-9_-]+\\.(c|m|cpp|cc|cxx)$");

            options.Count().Should().Be(38);

            string CheckHasOption(string optionName)
            {
                var matches = options.Where(x => x.StartsWith(optionName, StringComparison.InvariantCulture));
                matches.Count().Should().Be(1);
                return matches.First();
            }
        }

        [TestMethod]
        public void GetKeyValueOptionsList_WithKnownConfig()
        {
            var rulesConfig = GetDummyRulesConfiguration();
            var options = CFamilyHelper.GetKeyValueOptionsList(rulesConfig);

            // QP option
            CheckHasExactOption("internal.qualityProfile=rule2,rule3"); // only active rules

            // Check a few known rules with parameters
            CheckHasExactOption("rule1.rule1 Param1=rule1 Value1");
            CheckHasExactOption("rule1.rule1 Param2=rule1 Value2");

            CheckHasExactOption("rule2.rule2 Param1=rule2 Value1");
            CheckHasExactOption("rule2.rule2 Param2=rule2 Value2");

            CheckHasExactOption("rule3.rule3 Param1=rule3 Value1");
            CheckHasExactOption("rule3.rule3 Param2=rule3 Value2");

            options.Count().Should().Be(7);

            string CheckHasExactOption(string expected)
            {
                var matches = options.Where(x => string.Equals(x, expected, StringComparison.InvariantCulture));
                matches.Count().Should().Be(1);
                return matches.First();
            }
        }

        [TestMethod]
        public void ToSonarLintIssue_EndLineIsNotZero()
        {
            var ruleConfig = GetDummyRulesConfiguration();
            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, null);

            // Act
            var issue = CFamilyHelper.ToSonarLintIssue(message, "lang1", ruleConfig);

            //Assert
            issue.StartLine.Should().Be(4);
            issue.StartLineOffset.Should().Be(3 - 1);

            issue.EndLine.Should().Be(2);
            issue.EndLineOffset.Should().Be(1 - 1);

            issue.RuleKey.Should().Be("lang1:rule2");
            issue.FilePath.Should().Be("file");
            issue.Message.Should().Be("test endline is not zero");
        }

        [TestMethod]
        public void ToSonarLintIssue_EndLineIsZero()
        {
            // Special case: ignore column offsets if EndLine is zero
            var ruleConfig = GetDummyRulesConfiguration();
            var message = new Message("rule3", "ff", 101, 1, 0, 3, "test endline is zero", true, null);

            // Act
            var issue = CFamilyHelper.ToSonarLintIssue(message, "cpp", ruleConfig);

            //Assert
            issue.StartLine.Should().Be(101);

            issue.EndLine.Should().Be(0);
            issue.StartLineOffset.Should().Be(0);
            issue.EndLineOffset.Should().Be(0);

            issue.RuleKey.Should().Be("cpp:rule3");
            issue.FilePath.Should().Be("ff");
            issue.Message.Should().Be("test endline is zero");
        }

        [TestMethod]
        public void ToSonarLintIssue_SeverityAndTypeLookup()
        {
            var ruleConfig = GetDummyRulesConfiguration();

            // 1. Check rule2
            var message = new Message("rule2", "any", 4, 3, 2, 1, "message", false, null);
            var issue = CFamilyHelper.ToSonarLintIssue(message, "lang1", ruleConfig);

            issue.RuleKey.Should().Be("lang1:rule2");
            issue.Severity.Should().Be(Severity.Info);
            issue.Type.Should().Be(Sonarlint.Issue.Types.Type.CodeSmell);

            // 2. Check rule3
            message = new Message("rule3", "any", 4, 3, 2, 1, "message", false, null);
            issue = CFamilyHelper.ToSonarLintIssue(message, "lang1", ruleConfig);

            issue.RuleKey.Should().Be("lang1:rule3");
            issue.Severity.Should().Be(Severity.Critical);
            issue.Type.Should().Be(Sonarlint.Issue.Types.Type.Vulnerability);
        }

        [TestMethod]
        public void ConvertFromIssueSeverity()
        {
            CFamilyHelper.Convert(IssueSeverity.Blocker).Should().Be(Severity.Blocker);
            CFamilyHelper.Convert(IssueSeverity.Critical).Should().Be(Severity.Critical);
            CFamilyHelper.Convert(IssueSeverity.Info).Should().Be(Severity.Info);
            CFamilyHelper.Convert(IssueSeverity.Major).Should().Be(Severity.Major);
            CFamilyHelper.Convert(IssueSeverity.Minor).Should().Be(Severity.Minor);

            Action act = () => CFamilyHelper.Convert((IssueSeverity)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueSeverity");
        }

        [TestMethod]
        public void ConvertFromIssueType()
        {
            CFamilyHelper.Convert(IssueType.Bug).Should().Be(Sonarlint.Issue.Types.Type.Bug);
            CFamilyHelper.Convert(IssueType.CodeSmell).Should().Be(Sonarlint.Issue.Types.Type.CodeSmell);
            CFamilyHelper.Convert(IssueType.Vulnerability).Should().Be(Sonarlint.Issue.Types.Type.Vulnerability);

            Action act = () => CFamilyHelper.Convert((IssueType)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueType");
        }

        [TestMethod]
        public void SubProcessTimeout()
        {
            SetTimeoutAndCheckCalculatedTimeout("", 10000); // not set -> default

            SetTimeoutAndCheckCalculatedTimeout("222", 222); // valid -> used
            SetTimeoutAndCheckCalculatedTimeout("200000", 200000); // valid -> used

            SetTimeoutAndCheckCalculatedTimeout("-111", 10000); // invalid -> default
            SetTimeoutAndCheckCalculatedTimeout("not an integer", 10000);
            SetTimeoutAndCheckCalculatedTimeout("1.23", 10000);
            SetTimeoutAndCheckCalculatedTimeout("2,000", 10000);
            SetTimeoutAndCheckCalculatedTimeout("2.001", 10000);

            void SetTimeoutAndCheckCalculatedTimeout(string valueToSet, int expectedTimeout)
            {
                using (new EnvironmentVariableScope())
                {
                    Environment.SetEnvironmentVariable("SONAR_INTERNAL_CFAMILY_ANALYSIS_TIMEOUT_MS", valueToSet);

                    CFamilyHelper.GetTimeoutInMs().Should().Be(expectedTimeout);
                }
            }
        }

        private static ICFamilyRulesConfig GetDummyRulesConfiguration()
        {
            var config = new DummyCFamilyRulesConfig
            {
                RuleKeyToActiveMap = new Dictionary<string, bool>
                {
                    { "rule1", false },
                    { "rule2", true },
                    { "rule3", true }
                },

                RulesParameters = new Dictionary<string, IDictionary<string, string>>
                {
                    { "rule1", new Dictionary<string, string> { { "rule1 Param1", "rule1 Value1" }, { "rule1 Param2", "rule1 Value2" } } },
                    { "rule2", new Dictionary<string, string> { { "rule2 Param1", "rule2 Value1" }, { "rule2 Param2", "rule2 Value2" } } },
                    { "rule3", new Dictionary<string, string> { { "rule3 Param1", "rule3 Value1" }, { "rule3 Param2", "rule3 Value2" } } }
                },

                RulesMetadata = new Dictionary<string, RuleMetadata>
                {
                    { "rule1", new RuleMetadata { Title = "rule1 title", DefaultSeverity = IssueSeverity.Blocker, Type = IssueType.Bug } },
                    { "rule2", new RuleMetadata { Title = "rule2 title", DefaultSeverity = IssueSeverity.Info, Type = IssueType.CodeSmell } },
                    { "rule3", new RuleMetadata { Title = "rule3 title", DefaultSeverity = IssueSeverity.Critical, Type = IssueType.Vulnerability } },
                }
            };
            return config;
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
