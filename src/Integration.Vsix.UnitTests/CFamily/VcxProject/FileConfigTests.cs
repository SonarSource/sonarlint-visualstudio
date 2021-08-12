﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
        public void GetPotentiallyUnsupportedPropertyValue_PropertySupported_ReturnsValue()
        {
            // Arrange
            var settingsMock = new Mock<IVCRulePropertyStorage>();
            settingsMock.Setup(x => x.GetEvaluatedPropertyValue("propertyName1"))
                .Returns("propertyValue");

            // Act
            var result =
                FileConfig.GetPotentiallyUnsupportedPropertyValue(settingsMock.Object, "propertyName1",
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
                FileConfig.GetPotentiallyUnsupportedPropertyValue(settingsMock.Object, "propertyName1",
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
                FileConfig.GetPotentiallyUnsupportedPropertyValue(settingsMock.Object, "propertyName1",
                    "default xxx");

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("foo");
        }

        [TestMethod]
        public void GetCompilerVersion()
        {
            FileConfig.GetCompilerVersion("v90", "").Should().Be("15.00.00");

            FileConfig.GetCompilerVersion("v100", "").Should().Be("16.00.00");

            FileConfig.GetCompilerVersion("v110", "").Should().Be("17.00.00");
            FileConfig.GetCompilerVersion("v110_xp", "").Should().Be("17.00.00");

            FileConfig.GetCompilerVersion("v120", "").Should().Be("18.00.00");
            FileConfig.GetCompilerVersion("v120_xp", "").Should().Be("18.00.00");

            FileConfig.GetCompilerVersion("v140", "").Should().Be("19.00.00");
            FileConfig.GetCompilerVersion("v140_xp", "").Should().Be("19.00.00");

            FileConfig.GetCompilerVersion("v141", "14.10.00").Should().Be("19.10.00");
            FileConfig.GetCompilerVersion("v141_xp", "14.10.50").Should().Be("19.10.50");

            FileConfig.GetCompilerVersion("v142", "14.25.28612").Should().Be("19.25.28612");

            Action action = () => FileConfig.GetCompilerVersion("v142", "2132");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported VCToolsVersion: 2132");

            action = () => FileConfig.GetCompilerVersion("v143", "14.30.0000");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should()
                .StartWith("Unsupported PlatformToolset: v143");

            action = () => FileConfig.GetCompilerVersion("", "");
            action.Should().ThrowExactly<ArgumentException>().And.Message.Should().StartWith
                ("The file cannot be analyzed because the platform toolset has not been specified.");
        }

        [TestMethod]
        public void TryGet_UnsupportedItemType_ReturnsNull()
        {
            // Arrange
            var projectItemConfig = new ProjectItemConfig {ItemType = "None"};
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
            var projectItemConfig = new ProjectItemConfig {ConfigurationType = ConfigurationTypes.typeUnknown};
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
            var projectItemConfig = new ProjectItemConfig {IsVCCLCompilerTool = false};
            var projectItemMock = CreateMockProjectItem("c:\\foo\\xxx.vcxproj", projectItemConfig);

            // Act
            var fileConfig = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.cpp");

            // Assert
            fileConfig.Should().BeNull();
            testLogger.AssertOutputStringExists("Custom build tools aren't supported. Custom-built file: 'c:\\dummy\\file.cpp'");
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
                    ["PrecompiledHeader"] = "NotUsing",
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
            Assert.AreEqual("pch.h", request.ForcedIncludeFiles);
            Assert.AreEqual("CompileAsCpp", request.CompileAs);

            // Arrange
            projectItemConfig.FileConfigProperties["CompileAs"] = "CompileAsC";
            projectItemConfig.FileConfigProperties["ForcedIncludeFiles"] = "FHeader.h";

            // Act
            request = FileConfig.TryGet(testLogger, projectItemMock.Object, "c:\\dummy\\file.h");

            // Assert
            Assert.AreEqual("FHeader.h", request.ForcedIncludeFiles);
            Assert.AreEqual("CompileAsC", request.CompileAs);
        }
    }
}
