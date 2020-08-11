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
using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.VCProjectEngine;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.Integration.UnitTests.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class CFamilyHelperTests
    {
        private const string ValidPlatformName = "Win32";

        private static readonly IDictionary<string, string> MandatoryProjectConfigProperties = new Dictionary<string, string>
        {
            ["PlatformToolset"] = "v140_xp"
        };

        private static readonly IDictionary<string, string> MandatoryFileConfigProperties = new Dictionary<string, string>
        {
            ["PrecompiledHeader"] = "NotUsing",
            ["CompileAs"] = "CompileAsCpp",
            ["CompileAsManaged"] = "false",
            ["EnableEnhancedInstructionSet"] = "",
            ["RuntimeLibrary"] = "",
            ["LanguageStandard"] = "",
            ["ExceptionHandling"] = "Sync",
            ["BasicRuntimeChecks"] = "UninitializedLocalUsageCheck",
        };

        [TestMethod]
        public void CreateRequest_HeaderFile_IsNotProcessed()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();

            var projectItemMock = new Mock<ProjectItem>();
            var rulesConfigProviderMock = new Mock<ICFamilyRulesConfigProvider>();

            // Act
            var request = CFamilyHelper.CreateRequest(loggerMock.Object, projectItemMock.Object, "c:\\dummy\\file.h",
                rulesConfigProviderMock.Object, null);

            // Assert
            request.Should().BeNull();
        }

        [TestMethod]
        public void CreateRequest_FileOutsideSolution_IsNotProcessed()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();

            var projectItemMock = CreateProjectItemWithProject("c:\\foo\\SingleFileISense\\xxx.vcxproj");
            var rulesConfigProviderMock = new Mock<ICFamilyRulesConfigProvider>();

            // Act
            var request = CFamilyHelper.CreateRequest(loggerMock.Object, projectItemMock.Object, "c:\\dummy\\file.cpp",
                rulesConfigProviderMock.Object, null);

            // Assert
            AssertMessageLogged(loggerMock,
                "Unable to retrieve the configuration for file 'c:\\dummy\\file.cpp'. Check the file is part of a project in the current solution.");
            request.Should().BeNull();
        }

        [TestMethod]
        public void CreateRequest_ErrorInFileConfigTryGet_IsHandled()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();

            var projectItemMock = CreateProjectItemWithProject("c:\\foo\\xxx.vcxproj");
            // Note: we want the exception to be thrown from inside the FileConfig::TryGet
            projectItemMock.Setup(x => x.Object).Throws(new InvalidOperationException("xxx"));

            var rulesConfigProviderMock = new Mock<ICFamilyRulesConfigProvider>();

            // Act
            var request = CFamilyHelper.CreateRequest(loggerMock.Object, projectItemMock.Object, "c:\\dummy\\file.cpp",
                rulesConfigProviderMock.Object, null);

            // Assert
            AssertPartialMessageLogged(loggerMock,
                "Unable to collect C/C++ configuration for c:\\dummy\\file.cpp: ");
            request.Should().BeNull();
        }

        [TestMethod]
        public void CreateRequest_NoAnalyzerOptions_RequestCreatedWithoutOptions()
        {
            var request = GetSuccessfulRequest(null);
            request.Should().NotBeNull();

            (request.Flags & Request.CreateReproducer).Should().Be(0);
            (request.Flags & Request.BuildPreamble).Should().Be(0);
            request.RulesConfiguration.Should().NotBeNull();
            request.Options.Should().NotBeEmpty();
        }

        [TestMethod]
        public void CreateRequest_AnalyzerOptionsAreNotCFamilyOptions_RequestCreatedWithoutOptions()
        {
            var request = GetSuccessfulRequest(Mock.Of<IAnalyzerOptions>());
            request.Should().NotBeNull();

            (request.Flags & Request.CreateReproducer).Should().Be(0);
            (request.Flags & Request.BuildPreamble).Should().Be(0);
            request.RulesConfiguration.Should().NotBeNull();
            request.Options.Should().NotBeEmpty();
        }

        [TestMethod]
        public void CreateRequest_AnalyzerOptionsWithReproducerEnabled_RequestCreatedWithReproducerFlag()
        {
            var request = GetSuccessfulRequest(new CFamilyAnalyzerOptions {CreateReproducer = true});
            request.Should().NotBeNull();

            (request.Flags & Request.CreateReproducer).Should().NotBe(0);
        }

        [TestMethod]
        public void CreateRequest_AnalyzerOptionsWithoutReproducerEnabled_RequestCreatedWithoutReproducerFlag()
        {
            var request = GetSuccessfulRequest(new CFamilyAnalyzerOptions {CreateReproducer = false});
            request.Should().NotBeNull();

            (request.Flags & Request.CreateReproducer).Should().Be(0);
        }

        [TestMethod]
        public void CreateRequest_AnalyzerOptionsWithPCH_RequestCreatedWithPCHFlag()
        {
            var request = GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = true });
            request.Should().NotBeNull();

            (request.Flags & Request.BuildPreamble).Should().NotBe(0);
            request.RulesConfiguration.Should().BeNull();
            request.Options.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateRequest_AnalyzerOptionsWithoutPCH_RequestCreatedWithoutPCHFlag()
        {
            var request = GetSuccessfulRequest(new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = false });
            request.Should().NotBeNull();

            (request.Flags & Request.BuildPreamble).Should().Be(0);
            request.RulesConfiguration.Should().NotBeNull();
            request.Options.Should().NotBeEmpty();
        }

        [TestMethod]
        public void CreateRequest_RequestCreatedWithPCHFilePath()
        {
            var request = GetSuccessfulRequest(null);
            request.Should().NotBeNull();

            request.PchFile.Should().Be(CFamilyHelper.PchFilePath);
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
        public void GetKeyValueOptionsList_UsingRealEmbeddedRulesJson()
        {
            var sonarWayProvider = new CFamilySonarWayRulesConfigProvider(CFamilyShared.CFamilyFilesDirectory);
            var options = CFamilyHelper.GetKeyValueOptionsList(sonarWayProvider.GetRulesConfiguration("cpp"));

            // QP option
            CheckHasOption("internal.qualityProfile=");

            // Check a few known rules with parameters
            CheckHasOption("ClassComplexity.maximumClassComplexityThreshold=80");
            CheckHasOption("S1142.max=3");
            CheckHasOption("S1578.format=^[A-Za-z_-][A-Za-z0-9_-]+\\.(c|m|cpp|cc|cxx)$");

            options.Count().Should()
                .BeGreaterOrEqualTo(39); // basic sanity check: v6.6 has 39 - not expecting options to be removed

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
        public void ToSonarLintIssue_HasMessageParts_IssueWithSecondaryLocations()
        {
            var ruleConfig = GetDummyRulesConfiguration();
            var messageParts = new List<MessagePart>
            {
                new MessagePart("test1.cpp", 1, 2, 3, 4, "this is a test 1"),
                new MessagePart("test2.cpp", 5, 6, 7, 8, "this is a test 2")
            };

            var expectedLocations = new List<AnalysisIssueLocation>
            {
                new AnalysisIssueLocation("this is a test 2", "test2.cpp", 5, 7, 6, 8),
                new AnalysisIssueLocation("this is a test 1", "test1.cpp", 1, 3, 2, 4)
            };

            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, messageParts.ToArray());

            // Act
            var issue = CFamilyHelper.ToSonarLintIssue(message, "lang1", ruleConfig);

            //Assert
            issue.Locations.Should().BeEquivalentTo(expectedLocations);
        }

        [TestMethod]
        public void ToSonarLintIssue_EndLineIsNotZero()
        {
            var ruleConfig = GetDummyRulesConfiguration();
            var message = new Message("rule2", "file", 4, 3, 2, 1, "test endline is not zero", false, new MessagePart[0]);

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
            var message = new Message("rule3", "ff", 101, 1, 0, 3, "test endline is zero", true, new MessagePart[0]);

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
        [DataRow("rule2", AnalysisIssueSeverity.Info, AnalysisIssueType.CodeSmell)]
        [DataRow("rule3", AnalysisIssueSeverity.Critical, AnalysisIssueType.Vulnerability)]
        public void ToSonarLintIssue_SeverityAndTypeLookup(string ruleKey, AnalysisIssueSeverity expectedSeverity, AnalysisIssueType expectedType)
        {
            var ruleConfig = GetDummyRulesConfiguration();

            var message = new Message(ruleKey, "any", 4, 3, 2, 1, "message", false, new MessagePart[0]);
            var issue = CFamilyHelper.ToSonarLintIssue(message, "lang1", ruleConfig);

            issue.RuleKey.Should().Be($"lang1:{ruleKey}");
            issue.Severity.Should().Be(expectedSeverity);
            issue.Type.Should().Be(expectedType);
        }

        [TestMethod]
        [DataRow(IssueSeverity.Blocker, AnalysisIssueSeverity.Blocker)]
        [DataRow(IssueSeverity.Critical, AnalysisIssueSeverity.Critical)]
        [DataRow(IssueSeverity.Info, AnalysisIssueSeverity.Info)]
        [DataRow(IssueSeverity.Major, AnalysisIssueSeverity.Major)]
        [DataRow(IssueSeverity.Minor, AnalysisIssueSeverity.Minor)]
        public void ConvertFromIssueSeverity(IssueSeverity cfamilySeverity, AnalysisIssueSeverity analysisIssueSeverity)
        {
            CFamilyHelper.Convert(cfamilySeverity).Should().Be(analysisIssueSeverity);
        }

        [TestMethod]
        public void ConvertFromIssueSeverity_InvalidValue_Throws()
        {
            Action act = () => CFamilyHelper.Convert((IssueSeverity)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueSeverity");
        }

        [TestMethod]
        [DataRow(IssueType.Bug, AnalysisIssueType.Bug)]
        [DataRow(IssueType.CodeSmell, AnalysisIssueType.CodeSmell)]
        [DataRow(IssueType.Vulnerability, AnalysisIssueType.Vulnerability)]
        public void ConvertFromIssueType(IssueType cfamilyIssueType, AnalysisIssueType analysisIssueType)
        {
            CFamilyHelper.Convert(cfamilyIssueType).Should().Be(analysisIssueType);

            Action act = () => CFamilyHelper.Convert((IssueType) (-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueType");
        }

        [TestMethod]
        public void ConvertFromIssueType_InvalidValue_Throws()
        {
            Action act = () => CFamilyHelper.Convert((IssueType)(-1));
            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("issueType");
        }

        private static ICFamilyRulesConfig GetDummyRulesConfiguration()
        {
            var config = new DummyCFamilyRulesConfig("any")
                .AddRule("rule1", IssueSeverity.Blocker, isActive: false,
                    parameters: new Dictionary<string, string>
                        {{"rule1 Param1", "rule1 Value1"}, {"rule1 Param2", "rule1 Value2"}})
                .AddRule("rule2", IssueSeverity.Info, isActive: true,
                    parameters: new Dictionary<string, string>
                        {{"rule2 Param1", "rule2 Value1"}, {"rule2 Param2", "rule2 Value2"}})
                .AddRule("rule3", IssueSeverity.Critical, isActive: true,
                    parameters: new Dictionary<string, string>
                        {{"rule3 Param1", "rule3 Value1"}, {"rule3 Param2", "rule3 Value2"}});

            config.RulesMetadata["rule1"].Type = IssueType.Bug;
            config.RulesMetadata["rule2"].Type = IssueType.CodeSmell;
            config.RulesMetadata["rule3"].Type = IssueType.Vulnerability;

            return config;
        }

        private Mock<ProjectItem> CreateProjectItemWithProject(string projectName)
        {
            var vcProjectMock = new Mock<VCProject>();
            var vcConfig = CreateVCConfigurationWithProperties(ValidPlatformName, MandatoryProjectConfigProperties);
            vcProjectMock.SetupGet(x => x.ActiveConfiguration).Returns(vcConfig);

            var projectMock = new ProjectMock(projectName) {Project = vcProjectMock.Object};

            var vcFileMock = new Mock<VCFile>();
            var vcFileConfig = CreateVCFileConfigurationWithToolProperties(MandatoryFileConfigProperties);
            vcFileMock.Setup(x => x.GetFileConfigurationForProjectConfiguration(vcConfig)).Returns(vcFileConfig);

            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(i => i.ContainingProject).Returns(projectMock);
            projectItemMock.Setup(i => i.Object).Returns(vcFileMock.Object);

            // Set the project item to have a valid DTE configuration
            // - used to check whether the project item is in a solution or not
            var dteConfigManagerMock = new Mock<ConfigurationManager>();
            var dteConfigMock = new Mock<Configuration>();
            dteConfigManagerMock.Setup(x => x.ActiveConfiguration).Returns(dteConfigMock.Object);
            projectItemMock.Setup(i => i.ConfigurationManager).Returns(dteConfigManagerMock.Object);

            return projectItemMock;
        }

        private Request GetSuccessfulRequest(IAnalyzerOptions analyzerOptions)
        {
            var loggerMock = new Mock<ILogger>();
            var rulesConfig = GetDummyRulesConfiguration();
            var rulesConfigProviderMock = new Mock<ICFamilyRulesConfigProvider>();
            rulesConfigProviderMock
                .Setup(x => x.GetRulesConfiguration(It.IsAny<string>()))
                .Returns(rulesConfig);

            var projectItemMock = CreateProjectItemWithProject("c:\\foo\\file.cpp");

            var request = CFamilyHelper.CreateRequest(loggerMock.Object, projectItemMock.Object, "c:\\foo\\file.cpp",
                rulesConfigProviderMock.Object, analyzerOptions);

            return request;
        }

        private static VCConfiguration CreateVCConfigurationWithProperties(string platformName, IDictionary<string, string> propertyMap = null)
        {
            var vcPlatformMock = new Mock<VCPlatform>();
            vcPlatformMock.SetupGet(x => x.Name).Returns(platformName);

            var vcConfigMock = new Mock<VCConfiguration>();
            vcConfigMock.SetupGet(x => x.Platform).Returns(vcPlatformMock.Object);

            vcConfigMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>()))
                .Returns<string>(s =>
                {
                    string propertyValue = null;
                    propertyMap?.TryGetValue(s, out propertyValue);
                    return propertyValue ?? string.Empty;
                });

            return vcConfigMock.Object;
        }

        private static VCFileConfiguration CreateVCFileConfigurationWithToolProperties(IDictionary<string, string> toolPropertyMap = null)
        {
            var toolPropertiesMock = new Mock<IVCRulePropertyStorage>();

            toolPropertiesMock.Setup(x => x.GetEvaluatedPropertyValue(It.IsAny<string>()))
                .Returns<string>(s =>
                {
                    string propertyValue = null;
                    toolPropertyMap?.TryGetValue(s, out propertyValue);
                    return propertyValue ?? string.Empty;
                });

            var vcFileConfigMock = new Mock<VCFileConfiguration>();
            vcFileConfigMock.SetupGet(x => x.Tool).Returns(toolPropertiesMock.Object);

            return vcFileConfigMock.Object;
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
