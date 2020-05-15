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
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using Language = SonarLint.VisualStudio.Core.Language;

// This file contains tests for UnboundProjectFinder that relate to
// the project-level config files.
// The solution-level config tests are in another class.

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class UnboundProjectFinderTests_ProjectLevelConfig
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableConfigurationProvider configProvider;
        private ConfigurableRuleSetSerializer ruleSetSerializer;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetInfoProvider;
        private Mock<IFileSystem> fileMock;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.configProvider = new ConfigurableConfigurationProvider();
            this.serviceProvider.RegisterService(typeof(IConfigurationProvider), this.configProvider);

            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);

            this.ruleSetInfoProvider = new ConfigurableSolutionRuleSetsInformationProvider();
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), this.ruleSetInfoProvider);

            this.ruleSetSerializer = new ConfigurableRuleSetSerializer(new MockFileSystem());
            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), this.ruleSetSerializer);

            this.fileMock = new Mock<IFileSystem>();
            fileMock.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(false);
        }

        #region Tests

        [TestMethod]
        public void ArgCheck()
        {
            Action action = () => new UnboundProjectFinder(null, new ProjectBinderFactory(serviceProvider, Mock.Of<ILogger>()));
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");

            action = () => new UnboundProjectFinder(serviceProvider, projectBinderFactory: null);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("projectBinderFactory");
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void GetUnboundProjects_ValidSolution_ProjectRuleSetIsMissing(SonarLintMode mode)
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var expected = this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(mode);
            this.SetValidCSharpSolutionRuleSet(new RuleSet("SolutionRuleSet"));

            // Act
            var projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(expected, projects);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void GetUnboundProjects_ValidSolution_ProjectRuleSetNotIncludingSolutionRuleSet(SonarLintMode mode)
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var expected = this.SetValidFilteredProjects();
            this.SetValidProjectRuleSets((project, filePath) => new RuleSet("ProjectRuleSet") { FilePath = filePath });

            this.SetValidSolutionBinding(mode);
            this.SetValidCSharpSolutionRuleSet(new RuleSet("SolutionRuleSet"));

            // Act
            var projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(expected, projects);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void GetUnboundProjectsValidSolution_ProjectRuleSetIncludesSolutionRuleSet(SonarLintMode mode)
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var allProjects = this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(mode);
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets(mode);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(allProjects.Except(new Project[] { boundProject }), projects);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void GetUnboundProjects_ValidSolution_ProjectRuleSetIncludesSolutionRuleSet_RuleSetAggregation(SonarLintMode mode)
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var allProjects = this.SetValidFilteredProjects();
            // Duplicate the configurations, which will create duplicate rule sets
            allProjects.OfType<ProjectMock>().ToList().ForEach(p => this.SetValidProjectConfiguration(p, "AnotherConfiguration"));

            this.SetValidSolutionBinding(mode);
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets(mode);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(allProjects.Except(new Project[] { boundProject }), projects);
        }

        [TestMethod]
        public void GetUnboundProjects_SolutionNotBound_Standalone()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            this.configProvider.ModeToReturn = SonarLintMode.Standalone;

            // Act
            var projects = testSubject.GetUnboundProjects();

            // Assert
            AssertEmptyResult(projects);
            this.serviceProvider.AssertServiceNotUsed(typeof(IProjectSystemHelper));
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void GetUnboundProjects_HasBoundProjects(SonarLintMode mode)
        {
            // Arrange
            var testSubject = CreateTestSubject();
            this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(mode);
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets(mode);

            ProjectMock unboundProject = this.projectSystemHelper.FilteredProjects.OfType<ProjectMock>().Except(new[] { boundProject }).SingleOrDefault();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            projects.SingleOrDefault().Should().Be(unboundProject, "Unexpected unbound project");
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void GetUnboundProjects_HasNoBoundProjects(SonarLintMode mode)
        {
            // Arrange
            var testSubject = CreateTestSubject();

            this.SetValidSolutionBinding(mode);
            this.SetValidFilteredProjects();

            // Act
            var projects = testSubject.GetUnboundProjects();

            // Assert
            CollectionAssert.AreEquivalent(this.projectSystemHelper.FilteredProjects.ToArray(), projects.ToArray(), "Unexpected unbound projects");
        }

        #endregion Tests

        #region Helpers

        private UnboundProjectFinder CreateTestSubject() =>
            new UnboundProjectFinder(this.serviceProvider, new ProjectBinderFactory(serviceProvider, Mock.Of<ILogger>(), fileMock.Object));

        private IEnumerable<Project> SetValidFilteredProjects()
        {
            var project1 = new ProjectMock(@"c:\SolutionRoot\Project1\Project1.csproj");
            project1.SetCSProjectKind();

            var project2 = new ProjectMock(@"c:\SolutionRoot\Project2\project2.csproj");
            project2.SetCSProjectKind();

            this.projectSystemHelper.FilteredProjects = new Project[] { project1, project2 };
            this.projectSystemHelper.Projects = new Project[] { new ProjectMock(@"c:\SolutionRoot\excluded.csproj") };
            this.ruleSetInfoProvider.SolutionRootFolder = @"c:\SolutionRoot";

            this.SetValidProjectConfiguration(project1);
            this.SetValidProjectConfiguration(project2);

            return this.projectSystemHelper.FilteredProjects;
        }

        private void SetValidProjectConfiguration(ProjectMock project, string configurationName = "Configuration")
        {
            var configuration = new ConfigurationMock(configurationName);
            project.ConfigurationManager.Configurations.Add(configuration);

            PropertyMock ruleSetProperty = configuration.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
            ruleSetProperty.Value = project.FilePath.ToUpperInvariant(); // Catch cases where file paths are compared without OrdinalIgnoreCase

            this.ruleSetInfoProvider.RegisterProjectInfo(project, new RuleSetDeclaration[] {
                new RuleSetDeclaration(project, ruleSetProperty, (string)ruleSetProperty.Value, configurationName)
            });
        }

        private void SetValidProjectRuleSets(Func<ProjectMock, string, RuleSet> filePathToRuleSetFactory)
        {
            foreach (ProjectMock project in this.projectSystemHelper.FilteredProjects.OfType<ProjectMock>())
            {
                foreach (ConfigurationMock config in project.ConfigurationManager.Configurations)
                {
                    PropertyMock property = config.Properties
                        .OfType<PropertyMock>()
                        .SingleOrDefault(p => p.Name == Constants.CodeAnalysisRuleSetPropertyKey);

                    string ruleSetPath = property?.Value as string;
                    if (ruleSetPath != null)
                    {
                        this.ruleSetSerializer.RegisterRuleSet(filePathToRuleSetFactory.Invoke(project, ruleSetPath));
                    }
                }
            }
        }

        private void SetValidSolutionBinding(SonarLintMode bindingMode)
        {
            configProvider.ModeToReturn = bindingMode;
            configProvider.ProjectToReturn = new BoundSonarQubeProject { ProjectKey = "projectKey" };
            configProvider.FolderPathToReturn = "c:\\test";
        }

        private void SetValidCSharpSolutionRuleSet(RuleSet ruleSet)
        {
            var bindingConfiguration = configProvider.GetConfiguration();
            ruleSet.FilePath = SetValidSolutionRulesFile(bindingConfiguration, Language.CSharp);
            ruleSetSerializer.RegisterRuleSet(ruleSet);
        }

        private string SetValidSolutionRulesFile(BindingConfiguration bindingConfiguration, Language language)
        {
            var expectedSolutionRulesFile = bindingConfiguration.BuildPathUnderConfigDirectory(language.FileSuffixAndExtension);

            fileMock.Setup(x => x.File.Exists(expectedSolutionRulesFile)).Returns(true);
            return expectedSolutionRulesFile;
        }

        /// <summary>
        /// Sets configures the solution and project rule sets to have
        /// one rule set that will be considered as a rule set for a bound project
        /// </summary>
        /// <returns>The bound project for which the rule set was set</returns>
        private ProjectMock SetValidSolutionAndProjectRuleSets(SonarLintMode bindingMode)
        {
            var solutionRuleSet = new RuleSet("SolutionRuleSet");
            var boundProject = projectSystemHelper.FilteredProjects.OfType<ProjectMock>().First();

            SetValidCSharpSolutionRuleSet(solutionRuleSet);
            SetValidProjectRuleSets((project, filePath) =>
            {
                var ruleSet = new RuleSet("ProjectRuleSet") { FilePath = filePath };

                if (project == boundProject)
                {
                    ruleSet.RuleSetIncludes.Add(new RuleSetInclude(solutionRuleSet.FilePath, RuleAction.Default));
                }

                return ruleSet;
            });

            return boundProject;
        }

        private static void AssertEmptyResult(IEnumerable<Project> projects)
        {
            projects.Should().NotBeNull("Null are not expected");
            projects.Should().BeEmpty("Not expecting any results. Actual: {0}", GetString(projects));
        }

        private static string GetString(IEnumerable<Project> projects)
        {
            return string.Join(", ", projects.Select(p => p.FullName));
        }

        private void AssertExpectedProjects(IEnumerable<Project> expected, IEnumerable<Project> projects)
        {
            expected.Should().BeEquivalentTo(projects); // order is not important
        }

        #endregion Helpers
    }
}
