//-----------------------------------------------------------------------
// <copyright file="SolutionBindingInformationProviderTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionBindingInformationProviderTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSolutionBindingSerializer bindingSerializer;
        private ConfigurableRuleSetSerializer ruleSetSerializer;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetInfoProvider;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.bindingSerializer = new ConfigurableSolutionBindingSerializer();
            this.serviceProvider.RegisterService(typeof(Persistence.ISolutionBindingSerializer), this.bindingSerializer);

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
        public void SolutionBindingInformationProvider_GetBoundProjects_SolutionNotBound()
        {
            // Setup
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.bindingSerializer.CurrentBinding = null;
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Verify
            AssertEmptyResult(projects);

        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_SolutionBound_EmptyFilteredProjects()
        {
            // Setup
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.projectSystemHelper.FilteredProjects = new Project[0];
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Verify
            AssertEmptyResult(projects);
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_ValidSolution_SolutionRuleSetIsMissing()
        {
            // Setup
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Verify
            AssertEmptyResult(projects);
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_ValidSolution_ProjectRuleSetIsMissing()
        {
            // Setup
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            this.SetValidSolutionRuleSet(new RuleSet("SolutionRuleSet"));

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Verify
            AssertEmptyResult(projects);
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_ValidSolution_ProjectRuleSetNotIncludingSolutionRuleSet()
        {
            // Setup
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            this.SetValidSolutionRuleSet(new RuleSet("SolutionRuleSet"));
            this.SetValidProjectRuleSets((project, filePath) => new RuleSet("ProjectRuleSet") { FilePath = filePath });

            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Verify
            AssertEmptyResult(projects);
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetBoundProjects_ValidSolution_ProjectRuleSetncludsSolutionRuleSet()
        {
            // Setup
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetBoundProjects();

            // Verify
            Assert.AreSame(boundProject, projects.SingleOrDefault(), "Unexpected bound project");
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetUnboundProjects_HasBoundProjects()
        {
            // Setup
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            ProjectMock boundProject = SetValidSolutionAndProjectRuleSets();
            ProjectMock unboundProject = this.projectSystemHelper.FilteredProjects.OfType<ProjectMock>().Except(new[] { boundProject }).SingleOrDefault();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Verify
            Assert.AreSame(unboundProject, projects.SingleOrDefault(), "Unexpected unbound project");
        }

        [TestMethod]
        public void SolutionBindingInformationProvider_GetUnboundProjects_HasNoBoundProjects()
        {
            // Setup
            var testSubject = new SolutionBindingInformationProvider(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidFilteredProjects();
            IEnumerable<Project> projects;

            // Act
            projects = testSubject.GetUnboundProjects();

            // Verify
            CollectionAssert.AreEquivalent(this.projectSystemHelper.FilteredProjects.ToArray(), projects.ToArray(), "Unexpected unbound projects");
        }
        #endregion

        #region Helpers
        private void SetValidSolutionBinding()
        {
            this.bindingSerializer.CurrentBinding = new Persistence.BoundSonarQubeProject { ProjectKey = "projectKey" };
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
            string expectedSolutionRuleSet = ((ISolutionRuleSetsInformationProvider)this.ruleSetInfoProvider).CalculateSolutionSonarQubeRuleSetFilePath("projectKey", RuleSetGroup.CSharp);
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
            Assert.IsNotNull(projects, "Null are not expected");
            Assert.AreEqual(0, projects.Count(), "Not expecting any results. Actual: {0}", GetString(projects));
        }

        private static string GetString(IEnumerable<Project> projects)
        {
            return string.Join(", ", projects.Select(p => p.FullName));
        }
        #endregion
    }
}
