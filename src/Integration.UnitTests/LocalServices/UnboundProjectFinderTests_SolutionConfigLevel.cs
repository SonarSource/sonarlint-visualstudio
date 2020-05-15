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
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

// This file contains tests for UnboundProjectFinder that relate to
// the solution-level config files.
// The project-level config tests are in another class.

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class UnboundProjectFinderTests_SolutionConfigLevel
    {
        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void GetUnboundProjects_SolutionBound_EmptyFilteredProjects(SonarLintMode mode)
        {
            // Arrange - no projects created
            var testConfig = new TestConfigurationBuilder(mode, "sqKey1");

            var testSubject = testConfig.CreateTestSubject();

            // Act
            var result = testSubject.GetUnboundProjects();

            // Assert
            AssertEmptyResult(result);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void GetUnboundProjects_ValidSolution_SolutionRuleConfigIsMissing(SonarLintMode mode)
        {
            // If the solution ruleset is missing then all projects will be returned as unbound
            // Arrange - no projects created
            var testConfig = new TestConfigurationBuilder(mode, "sqKey1");
            var project1 = testConfig.AddFilteredProject(ProjectSystemHelper.CSharpProjectKind);
            var project2 = testConfig.AddFilteredProject(ProjectSystemHelper.VbCoreProjectKind);
            var project3 = testConfig.AddFilteredProject(ProjectSystemHelper.CppProjectKind);

            var csharpFilePath = testConfig.SetSolutionLevelFilePathForLanguage(Language.CSharp, false);
            var vbFilePath = testConfig.SetSolutionLevelFilePathForLanguage(Language.VBNET, false);
            var cppFilePath = testConfig.SetSolutionLevelFilePathForLanguage(Language.Cpp, false);

            var testSubject = testConfig.CreateTestSubject();

            // Act
            var result = testSubject.GetUnboundProjects();

            // Assert
            result.Should().BeEquivalentTo(project1, project2, project3);
            testConfig.AssertExistenceOfFileWasChecked(csharpFilePath);
            testConfig.AssertExistenceOfFileWasChecked(vbFilePath);
            testConfig.AssertExistenceOfFileWasChecked(cppFilePath);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected, /* cppConfig =*/ true, /* cConfig */ true)]
        [DataRow(SonarLintMode.Connected, /* cppConfig =*/ true, /* cConfig */ false)]
        [DataRow(SonarLintMode.Connected, /* cppConfig =*/ false, /* cConfig */ false)]
        [DataRow(SonarLintMode.Connected, /* cppConfig =*/ false, /* cConfig */ true)]
        [DataRow(SonarLintMode.LegacyConnected, /* cppConfig =*/ true, /* cConfig */ true)]
        [DataRow(SonarLintMode.LegacyConnected, /* cppConfig =*/ true, /* cConfig */ false)]
        [DataRow(SonarLintMode.LegacyConnected , /* cppConfig =*/ false, /* cConfig */ false)]
        [DataRow(SonarLintMode.LegacyConnected, /* cppConfig =*/ false, /* cConfig */ true)]
        public void GetUnboundProjects_ValidSolution_CFamily_RequiresBothCppAndCConfig(SonarLintMode mode,
            bool cppConfigExists, bool cConfigExists)
        {
            // Cpp projects should have both C++ and C solution-level rules config files
            var shouldBeBound = cppConfigExists && cConfigExists;

            // Arrange
            var testConfig = new TestConfigurationBuilder(mode, "sqKey1");
            var projectCpp = testConfig.AddFilteredProject(ProjectSystemHelper.CppProjectKind);

            testConfig.SetSolutionLevelFilePathForLanguage(Language.Cpp, cppConfigExists);
            testConfig.SetSolutionLevelFilePathForLanguage(Language.C, cConfigExists);

            var testSubject = testConfig.CreateTestSubject();

            // Act
            var result = testSubject.GetUnboundProjects();

            // Assert
            if (shouldBeBound)
            {
                result.Should().BeEmpty();
            }
            else
            {
                result.Should().BeEquivalentTo(projectCpp);
            }
        }

        [TestMethod]
        public void GetUnboundProjects_Connected_ValidSolution_ProjectLevelBindingIsNotRequired_ProjectsAreNotChecked()
        {
            // Arrange
            var testConfig = new TestConfigurationBuilder(SonarLintMode.Connected, "xxx_key");
            var cppFilePath = testConfig.SetSolutionLevelFilePathForLanguage(Language.Cpp, true);
            var cFilePath = testConfig.SetSolutionLevelFilePathForLanguage(Language.C, true);
            testConfig.AddFilteredProject(ProjectSystemHelper.CppProjectKind);

            var testSubject = testConfig.CreateTestSubject();

            // Act
            var projects = testSubject.GetUnboundProjects();

            // Assert
            projects.Should().BeEmpty();
            testConfig.AssertExistenceOfFileWasChecked(cppFilePath);
            testConfig.AssertNoAttemptToLoadRulesetFile(cppFilePath);

            testConfig.AssertExistenceOfFileWasChecked(cFilePath);
            testConfig.AssertNoAttemptToLoadRulesetFile(cFilePath);
        }

        private static void AssertEmptyResult(IEnumerable<EnvDTE.Project> projects)
        {
            projects.Should().NotBeNull("Null are not expected");
            projects.Should().BeEmpty("Not expecting any results. Actual: {0}", GetString(projects));
        }

        private static string GetString(IEnumerable<EnvDTE.Project> projects)
        {
            return string.Join(", ", projects.Select(p => p.FullName));
        }

        /// <summary>
        /// Builder that provides more declarative methods to set up the test environment 
        /// </summary>
        private class TestConfigurationBuilder
        {
            private readonly Mock<IFileSystem> fileSystemMock = new Mock<IFileSystem>();
            private readonly List<ProjectMock> projects = new List<ProjectMock>();
            private readonly Mock<IRuleSetSerializer> ruleSetSerializerMock = new Mock<IRuleSetSerializer>();
            private readonly BindingConfiguration bindingConfiguration;

            public TestConfigurationBuilder(SonarLintMode bindingMode, string sqProjectKey)
            {
                bindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(new Uri("http://localhost:8888"), sqProjectKey, "anySQProjectName"), bindingMode, "c:\\");
            }

            public string SetSolutionLevelFilePathForLanguage(Language language, bool fileExists)
            {
                var filePath = bindingConfiguration.BuildEscapedPathUnderProjectDirectory(language.FileSuffixAndExtension);

                fileSystemMock.Setup(x => x.File.Exists(filePath)).Returns(fileExists);

                return filePath;
            }

            public ProjectMock AddFilteredProject(string projectKind)
            {
                var project = new ProjectMock("any.proj");
                project.ProjectKind = projectKind;
                projects.Add(project);
                return project;
            }

            public UnboundProjectFinder CreateTestSubject()
            {
                var projectSystemHelper = new Mock<IProjectSystemHelper>();
                projectSystemHelper.Setup(x => x.GetFilteredSolutionProjects()).Returns(projects);

                var configProviderMock = new Mock<IConfigurationProvider>();
                configProviderMock.Setup(x => x.GetConfiguration())
                    .Returns(bindingConfiguration);

                var sp = new ConfigurableServiceProvider();
                sp.RegisterService(typeof(ISolutionRuleSetsInformationProvider), Mock.Of<ISolutionRuleSetsInformationProvider>);
                sp.RegisterService(typeof(IProjectSystemHelper), projectSystemHelper.Object);
                sp.RegisterService(typeof(IConfigurationProvider), configProviderMock.Object);
                sp.RegisterService(typeof(IRuleSetSerializer), ruleSetSerializerMock.Object);

                var testSubject = new UnboundProjectFinder(sp, new ProjectBinderFactory(sp, Mock.Of<ILogger>(), fileSystemMock.Object));
                return testSubject;
            }

            public void AssertExistenceOfFileWasChecked(string filePath) =>
                fileSystemMock.Verify(x => x.File.Exists(filePath), Times.Once);

            public void AssertNoAttemptToLoadRulesetFile(string filePath) =>
                ruleSetSerializerMock.Verify(x => x.LoadRuleSet(filePath), Times.Never);
        }
    }
}
