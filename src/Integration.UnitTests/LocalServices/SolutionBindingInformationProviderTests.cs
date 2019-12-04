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
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionBindingInformationProviderTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableConfigurationProvider configProvider;
        private ConfigurableRuleSetSerializer ruleSetSerializer;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetInfoProvider;

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

            this.ruleSetSerializer = new ConfigurableRuleSetSerializer();
            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), this.ruleSetSerializer);
        }

        #region Tests

        [TestMethod]
        public void ArgCheck()
        {
            // Arrange
            Action action = () => new SolutionBindingInformationProvider(null);

            // Act & Assert
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void GetUnboundProjects_Legacy_SolutionBound_EmptyFilteredProjects()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.projectSystemHelper.FilteredProjects = new Project[0];
            IEnumerable<Project> projects;

            this.SetValidSolutionBinding(SonarLintMode.LegacyConnected);

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertEmptyResult(projects);
        }

        [TestMethod]
        public void GetUnboundProjects_Connected_SolutionBound_EmptyFilteredProjects()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.projectSystemHelper.FilteredProjects = new Project[0];
            IEnumerable<Project> projects;

            this.SetValidSolutionBinding(SonarLintMode.Connected);

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertEmptyResult(projects);
        }

        [TestMethod]
        public void GetUnboundProjects_Legacy_ValidSolution_SolutionRuleSetIsMissing()
        {
            // If the solution ruleset is missing then all projects will be returned as unbound

            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            var expected = this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(SonarLintMode.LegacyConnected);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(expected, projects);
        }

        [TestMethod]
        public void GetUnboundProjects_Connected_ValidSolution_SolutionRuleSetIsMissing()
        {
            // If the solution ruleset is missing then all projects will be returned as unbound

            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            var expected = this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(SonarLintMode.Connected);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(expected, projects);
        }

        [TestMethod]
        public void GetUnboundProjects_Legacy_ValidSolution_ProjectRuleSetIsMissing()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            var expected = this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(SonarLintMode.LegacyConnected);
            this.SetValidSolutionRuleSet(new RuleSet("SolutionRuleSet"), SonarLintMode.LegacyConnected);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(expected, projects);
        }

        [TestMethod]
        public void GetUnboundProjects_Connected_ValidSolution_ProjectRuleSetIsMissing()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            var expected = this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(SonarLintMode.Connected);
            this.SetValidSolutionRuleSet(new RuleSet("SolutionRuleSet"), SonarLintMode.Connected);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(expected, projects);
        }

        [TestMethod]
        public void GetUnboundProjects_Legacy_ValidSolution_ProjectRuleSetNotIncludingSolutionRuleSet()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            var expected = this.SetValidFilteredProjects();
            this.SetValidProjectRuleSets((project, filePath) => new RuleSet("ProjectRuleSet") { FilePath = filePath });

            this.SetValidSolutionBinding(SonarLintMode.LegacyConnected);
            this.SetValidSolutionRuleSet(new RuleSet("SolutionRuleSet"), SonarLintMode.LegacyConnected);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(expected, projects);
        }

        [TestMethod]
        public void GetUnboundProjects_Connected_ValidSolution_ProjectRuleSetNotIncludingSolutionRuleSet()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            var expected = this.SetValidFilteredProjects();
            this.SetValidProjectRuleSets((project, filePath) => new RuleSet("ProjectRuleSet") { FilePath = filePath });

            this.SetValidSolutionBinding(SonarLintMode.Connected);
            this.SetValidSolutionRuleSet(new RuleSet("SolutionRuleSet"), SonarLintMode.Connected);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(expected, projects);
        }

        [TestMethod]
        public void GetUnboundProjects_LegacyValidSolution_ProjectRuleSetIncludesSolutionRuleSet()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            var allProjects = this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(SonarLintMode.LegacyConnected);
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets(SonarLintMode.LegacyConnected);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(allProjects.Except(new Project[] { boundProject }), projects);
            this.ruleSetSerializer.AssertAllRegisteredRuleSetsLoadedExactlyOnce();
        }

        [TestMethod]
        public void GetUnboundProjects_Connected_ValidSolution_ProjectRuleSetIncludesSolutionRuleSet()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            var allProjects = this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(SonarLintMode.Connected);
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets(SonarLintMode.Connected);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(allProjects.Except(new Project[] { boundProject }), projects);
            this.ruleSetSerializer.AssertAllRegisteredRuleSetsLoadedExactlyOnce();
        }

        [TestMethod]
        public void GetUnboundProjects_Legacy_ValidSolution_ProjectRuleSetIncludesSolutionRuleSet_RuleSetAggregation()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            var allProjects = this.SetValidFilteredProjects();
            // Duplicate the configurations, which will create duplicate rule sets
            allProjects.OfType<ProjectMock>().ToList().ForEach(p => this.SetValidProjectConfiguration(p, "AnotherConfiguration"));

            this.SetValidSolutionBinding(SonarLintMode.LegacyConnected);
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets(SonarLintMode.LegacyConnected);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(allProjects.Except(new Project[] { boundProject }), projects);
            this.ruleSetSerializer.AssertAllRegisteredRuleSetsLoadedExactlyOnce();
        }

        [TestMethod]
        public void GetUnboundProjects_Connected_ValidSolution_ProjectRuleSetIncludesSolutionRuleSet_RuleSetAggregation()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            var allProjects = this.SetValidFilteredProjects();
            // Duplicate the configurations, which will create duplicate rule sets
            allProjects.OfType<ProjectMock>().ToList().ForEach(p => this.SetValidProjectConfiguration(p, "AnotherConfiguration"));

            this.SetValidSolutionBinding(SonarLintMode.Connected);
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets(SonarLintMode.Connected);

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertExpectedProjects(allProjects.Except(new Project[] { boundProject }), projects);
            this.ruleSetSerializer.AssertAllRegisteredRuleSetsLoadedExactlyOnce();
        }

        [TestMethod]
        public void GetUnboundProjects_SolutionNotBound_Standalone()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.configProvider.ModeToReturn = SonarLintMode.Standalone;
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertEmptyResult(projects);
            this.serviceProvider.AssertServiceNotUsed(typeof(IProjectSystemHelper));
        }

        [TestMethod]
        public void GetUnboundProjects_Legacy_HasBoundProjects()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(SonarLintMode.LegacyConnected);
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets(SonarLintMode.LegacyConnected);

            ProjectMock unboundProject = this.projectSystemHelper.FilteredProjects.OfType<ProjectMock>().Except(new[] { boundProject }).SingleOrDefault();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            projects.SingleOrDefault().Should().Be(unboundProject, "Unexpected unbound project");
        }

        [TestMethod]
        public void GetUnboundProjects_Connected_HasBoundProjects()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidFilteredProjects();

            this.SetValidSolutionBinding(SonarLintMode.Connected);
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets(SonarLintMode.Connected);

            ProjectMock unboundProject = this.projectSystemHelper.FilteredProjects.OfType<ProjectMock>().Except(new[] { boundProject }).SingleOrDefault();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            projects.SingleOrDefault().Should().Be(unboundProject, "Unexpected unbound project");
        }

        [TestMethod]
        public void GetUnboundProjects_Legacy_HasNoBoundProjects()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);

            this.SetValidSolutionBinding(SonarLintMode.LegacyConnected);
            this.SetValidFilteredProjects();

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            CollectionAssert.AreEquivalent(this.projectSystemHelper.FilteredProjects.ToArray(), projects.ToArray(), "Unexpected unbound projects");
        }


        [TestMethod]
        public void GetUnboundProjects_Connected_HasNoBoundProjects()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);

            this.SetValidSolutionBinding(SonarLintMode.Connected);
            this.SetValidFilteredProjects();

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            CollectionAssert.AreEquivalent(this.projectSystemHelper.FilteredProjects.ToArray(), projects.ToArray(), "Unexpected unbound projects");
        }

        #endregion Tests

        #region Helpers

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
            this.configProvider.ModeToReturn = bindingMode;
            this.configProvider.ProjectToReturn = new BoundSonarQubeProject { ProjectKey = "projectKey" };
        }

        private void SetValidSolutionRuleSet(RuleSet ruleSet, SonarLintMode bindingMode)
        {
            string expectedSolutionRuleSet = ((ISolutionRuleSetsInformationProvider)this.ruleSetInfoProvider)
                .CalculateSolutionSonarQubeRuleSetFilePath("projectKey", Language.CSharp, bindingMode);
            ruleSet.FilePath = expectedSolutionRuleSet;

            this.ruleSetSerializer.RegisterRuleSet(ruleSet);
        }

        /// <summary>
        /// Sets configures the solution and project rule sets to have
        /// one rule set that will be considered as a rule set for a bound project
        /// </summary>
        /// <returns>The bound project for which the rule set was set</returns>
        private ProjectMock SetValidSolutionAndProjectRuleSets(SonarLintMode bindingMode)
        {
            var solutionRuleSet = new RuleSet("SolutionRuleSet");
            ProjectMock boundProject = this.projectSystemHelper.FilteredProjects.OfType<ProjectMock>().First();

            this.SetValidSolutionRuleSet(solutionRuleSet, bindingMode);
            this.SetValidProjectRuleSets((project, filePath) =>
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
