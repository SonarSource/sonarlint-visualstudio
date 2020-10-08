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
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.Integration.UnitTests.CFamily;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests.CFamilyTestUtility;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class CFamilyHelperTests
    {

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
            AssertMessageLogged(loggerMock, "Cannot analyze header files. File: 'c:\\dummy\\file.h'");
            request.Should().BeNull();
        }

        [TestMethod]
        public void CreateRequest_FileOutsideSolution_IsNotProcessed()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();

            var projectItemMock = CreateMockProjectItem("c:\\foo\\SingleFileISense\\xxx.vcxproj");
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

            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj");
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
            var projectItemMock = CreateMockProjectItem("c:\\foo\\SingleFileISense\\xxx.vcxproj");

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

        private Request GetSuccessfulRequest(IAnalyzerOptions analyzerOptions)
        {
            var loggerMock = new Mock<ILogger>();
            var rulesConfig = GetDummyRulesConfiguration();
            var rulesConfigProviderMock = new Mock<ICFamilyRulesConfigProvider>();
            rulesConfigProviderMock
                .Setup(x => x.GetRulesConfiguration(It.IsAny<string>()))
                .Returns(rulesConfig);

            var projectItemMock = CreateMockProjectItem("c:\\foo\\file.cpp");

            var request = CFamilyHelper.CreateRequest(loggerMock.Object, projectItemMock.Object, "c:\\foo\\file.cpp",
                rulesConfigProviderMock.Object, analyzerOptions);

            return request;
        }

        internal static void AssertMessageLogged(Mock<ILogger> loggerMock, string message)
        {
            loggerMock.Verify(x => x.WriteLine(It.Is<string>(
                s => s.Equals(message))), Times.Once);
        }

        internal static void AssertPartialMessageLogged(Mock<ILogger> loggerMock, string message)
        {
            loggerMock.Verify(x => x.WriteLine(It.Is<string>(
                s => s.Contains(message))), Times.Once);
        }
    }
}
