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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;

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

            testConfig.SetSolutionLevelFilePathForLanguage(Language.CSharp, "c:\\slnConfig.csharp", false);
            testConfig.SetSolutionLevelFilePathForLanguage(Language.VBNET, "c:\\slnConfig.vb", false);
            testConfig.SetSolutionLevelFilePathForLanguage(Language.Cpp, "c:\\slnConfig.cpp", false);

            var testSubject = testConfig.CreateTestSubject();

            // Act
            var result = testSubject.GetUnboundProjects();

            // Assert
            result.Should().BeEquivalentTo(project1, project2, project3);
            testConfig.AssertExistenceOfFileWasChecked("c:\\slnConfig.csharp");
            testConfig.AssertExistenceOfFileWasChecked("c:\\slnConfig.vb");
            testConfig.AssertExistenceOfFileWasChecked("c:\\slnConfig.cpp");
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

            testConfig.SetSolutionLevelFilePathForLanguage(Language.Cpp, "c:\\slnConfig.cpp", cppConfigExists);
            testConfig.SetSolutionLevelFilePathForLanguage(Language.C, "c:\\slnConfig.c", cConfigExists);

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
            testConfig.SetSolutionLevelFilePathForLanguage(Language.Cpp, "c:\\existingConfig.cpp", true);
            testConfig.SetSolutionLevelFilePathForLanguage(Language.C, "c:\\existingConfig.c", true);
            testConfig.AddFilteredProject(ProjectSystemHelper.CppProjectKind);

            var testSubject = testConfig.CreateTestSubject();

            // Act
            var projects = testSubject.GetUnboundProjects();

            // Assert
            projects.Should().BeEmpty();
            testConfig.AssertExistenceOfFileWasChecked("c:\\existingConfig.cpp");
            testConfig.AssertNoAttemptToLoadRulesetFile("c:\\existingConfig.cpp");
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
            private readonly SonarLintMode mode;
            private readonly string sqProjectKey;

            private readonly Mock<IFile> fileMock = new Mock<IFile>();
            private readonly List<ProjectMock> projects = new List<ProjectMock>();
            private readonly Mock<ISolutionRuleSetsInformationProvider> ruleSetsInfoProvider = new Mock<ISolutionRuleSetsInformationProvider>();
            private readonly Mock<IRuleSetSerializer> ruleSetSerializerMock = new Mock<IRuleSetSerializer>();

            public TestConfigurationBuilder(SonarLintMode bindingMode, string sqProjectKey)
            {
                mode = bindingMode;
                this.sqProjectKey = sqProjectKey;
            }

            public void SetSolutionLevelFilePathForLanguage(Language language, string filePathToReturn, bool fileExists)
            {
                ruleSetsInfoProvider.Setup(x => x.CalculateSolutionSonarQubeRuleSetFilePath(sqProjectKey, language, mode))
                    .Returns(filePathToReturn);

                fileMock.Setup(x => x.Exists(filePathToReturn)).Returns(fileExists);
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
                    .Returns(new BindingConfiguration(
                        new BoundSonarQubeProject(new Uri("http://localhost:8888"), sqProjectKey, "anySQProjectName"), mode));

                var sp = new ConfigurableServiceProvider();
                sp.RegisterService(typeof(ISolutionRuleSetsInformationProvider), ruleSetsInfoProvider.Object);
                sp.RegisterService(typeof(IProjectSystemHelper), projectSystemHelper.Object);
                sp.RegisterService(typeof(IConfigurationProvider), configProviderMock.Object);
                sp.RegisterService(typeof(IRuleSetSerializer), ruleSetSerializerMock.Object);

                var testSubject = new UnboundProjectFinder(sp, fileMock.Object);
                return testSubject;
            }

            public void AssertExistenceOfFileWasChecked(string filePath) =>
                fileMock.Verify(x => x.Exists(filePath), Times.Once);

            public void AssertNoAttemptToLoadRulesetFile(string filePath) =>
                ruleSetSerializerMock.Verify(x => x.LoadRuleSet(filePath), Times.Never);
        }
    }
}
