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
        public void SolutionBindingInformationProvider_ArgCheck()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingInformationProvider(null));
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_SolutionNotBound_StandaloneMode()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.configProvider.ModeToReturn = SonarLintMode.Standalone;
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Assert
            AssertEmptyResult(projects);
            this.serviceProvider.AssertServiceNotUsed(typeof(IProjectSystemHelper));
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_SolutionNotBound_NewConnectedMode()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.configProvider.ModeToReturn = SonarLintMode.Connected;
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Assert
            AssertEmptyResult(projects);
            this.serviceProvider.AssertServiceNotUsed(typeof(IProjectSystemHelper));
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_SolutionBound_EmptyFilteredProjects()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.projectSystemHelper.FilteredProjects = new Project[0];
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Assert
            AssertEmptyResult(projects);
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_ValidSolution_SolutionRuleSetIsMissing()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Assert
            AssertEmptyResult(projects);
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_ValidSolution_ProjectRuleSetIsMissing()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            this.SetValidSolutionRuleSet(new RuleSet("SolutionRuleSet"));

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Assert
            AssertEmptyResult(projects);
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_ValidSolution_ProjectRuleSetNotIncludingSolutionRuleSet()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            this.SetValidSolutionRuleSet(new RuleSet("SolutionRuleSet"));
            this.SetValidProjectRuleSets((project, filePath) => new RuleSet("ProjectRuleSet") { FilePath = filePath });

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Assert
            AssertEmptyResult(projects);
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_ValidSolution_ProjectRuleSetIncludesSolutionRuleSet()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Assert
            projects.SingleOrDefault().Should().Be(boundProject, "Unexpected bound project");
            this.ruleSetSerializer.AssertAllRegisteredRuleSetsLoadedExactlyOnce();
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_ValidSolution_ProjectRuleSetIncludesSolutionRuleSet_RuleSetAggregation()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            // Duplicate the configurations, which will create duplicate rule sets
            this.projectSystemHelper.FilteredProjects.OfType<ProjectMock>().ToList().ForEach(p => this.SetValidProjectConfiguration(p, "AnotherConfiguration"));
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Assert
            projects.SingleOrDefault().Should().Be(boundProject, "Unexpected bound project");
            this.ruleSetSerializer.AssertAllRegisteredRuleSetsLoadedExactlyOnce();
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetUnboundProjects_SolutionNotBound_Standalone()
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
        public void SolutionBindingInformationProvider_GetUnboundProjects_SolutionNotBound_NewConnectedMode()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.configProvider.ModeToReturn = SonarLintMode.Connected;
            this.configProvider.ProjectToReturn = new Persistence.BoundSonarQubeProject();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            AssertEmptyResult(projects);
            this.serviceProvider.AssertServiceNotUsed(typeof(IProjectSystemHelper));
        }


        [TestMethod]
        public void SolutionBindingInformationProvider_GetUnboundProjects_HasBoundProjects()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets();
            ProjectMock unboundProject = this.projectSystemHelper.FilteredProjects.OfType<ProjectMock>().Except(new[] { boundProject }).SingleOrDefault();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            projects.SingleOrDefault().Should().Be(unboundProject, "Unexpected unbound project");
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetUnboundProjects_HasNoBoundProjects()
        {
            // Arrange
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Assert
            CollectionAssert.AreEquivalent(this.projectSystemHelper.FilteredProjects.ToArray(), projects.ToArray(), "Unexpected unbound projects");
        }

        #endregion Tests

        #region Helpers

        private void SetValidSolutionBinding()
        {
            this.configProvider.ModeToReturn = SonarLintMode.LegacyConnected;
            this.configProvider.ProjectToReturn = new Persistence.BoundSonarQubeProject { ProjectKey = "projectKey" };
        }

        private void SetValidFilteredProjects()
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

        private void SetValidSolutionRuleSet(RuleSet ruleSet)
        {
            string expectedSolutionRuleSet = ((ISolutionRuleSetsInformationProvider)this.ruleSetInfoProvider).CalculateSolutionSonarQubeRuleSetFilePath("projectKey", Language.CSharp);
            ruleSet.FilePath = expectedSolutionRuleSet;

            this.ruleSetSerializer.RegisterRuleSet(ruleSet);
        }

        private void SetValidProjectRuleSets(Func<ProjectMock, string, RuleSet> filePathToRuleSetFactory)
        {
            foreach(ProjectMock project in this.projectSystemHelper.FilteredProjects.OfType<ProjectMock>())
            {
                foreach(ConfigurationMock config in project.ConfigurationManager.Configurations)
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

        /// <summary>
        /// Sets configures the solution and project rule sets to have
        /// one rule set that will be considered as a rule set for a bound project
        /// </summary>
        /// <returns>The bound project for which the rule set was set</returns>
        private ProjectMock SetValidSolutionAndProjectRuleSets()
        {
            var solutionRuleSet = new RuleSet("SolutionRuleSet");
            ProjectMock boundProject = this.projectSystemHelper.FilteredProjects.OfType<ProjectMock>().First();

            this.SetValidSolutionRuleSet(solutionRuleSet);
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

        #endregion Helpers
    }
}