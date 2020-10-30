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
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.ProfileConflicts;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ConflictsManagerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectHelper;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetInfoProvider;
        private MockFileSystem fileSystem;
        private ConfigurableConfigurationProvider configProvider;
        private ConfigurableRuleSetInspector inspector;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private DTEMock dte;
        private ConflictsManager testSubject;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.projectHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectHelper);

            this.ruleSetInfoProvider = new ConfigurableSolutionRuleSetsInformationProvider
            {
                SolutionRootFolder = this.TestContext.TestRunDirectory
            };
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), this.ruleSetInfoProvider);

            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), Mock.Of<IRuleSetSerializer>());

            this.fileSystem = new MockFileSystem();

            this.configProvider = new ConfigurableConfigurationProvider {FolderPathToReturn = "c:\\test"};
            this.serviceProvider.RegisterService(typeof(IConfigurationProvider), this.configProvider);

            this.inspector = new ConfigurableRuleSetInspector();
            this.serviceProvider.RegisterService(typeof(IRuleSetInspector), this.inspector);

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.dte = new DTEMock();
            this.projectHelper.CurrentActiveSolution = new SolutionMock(dte);

            this.testSubject = new ConflictsManager(serviceProvider, new SonarLintOutputLogger(serviceProvider), new ProjectBinderFactory(serviceProvider, Mock.Of<ILogger>(), fileSystem), fileSystem);
        }

        [TestMethod]
        public void ConflictsManager_Ctor()
        {
            var logger = Mock.Of<ILogger>();
            var projectBinderFactory = new ProjectBinderFactory(serviceProvider, logger);
            Exceptions.Expect<ArgumentNullException>(() => new ConflictsManager(null, logger, projectBinderFactory, fileSystem));
            Exceptions.Expect<ArgumentNullException>(() => new ConflictsManager(serviceProvider, null, projectBinderFactory, fileSystem));
            Exceptions.Expect<ArgumentNullException>(() => new ConflictsManager(serviceProvider, logger, null, fileSystem));
            Exceptions.Expect<ArgumentNullException>(() => new ConflictsManager(serviceProvider, logger, projectBinderFactory, null));

            testSubject.Should().NotBeNull("Avoid code analysis warning when testSubject is unused");
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_NotBound()
        {
            // Arrange
            this.SetValidProjects();
            this.configProvider.ProjectToReturn = null;

            // Act + Assert
            testSubject.GetCurrentConflicts().Should().BeEmpty("Not expecting any conflicts since solution is not bound");
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_StandaloneMode_NoConflicts()
        {
            // Arrange
            this.SetValidProjects();
            this.configProvider.ModeToReturn = SonarLintMode.Standalone;
            this.configProvider.ProjectToReturn = null;

            // Act + Assert
            testSubject.GetCurrentConflicts().Should().BeEmpty("Not expecting any conflicts since solution is not bound");
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_NewConnectedMode_NoConflicts()
        {
            // Arrange
            this.SetValidProjects();
            this.configProvider.ModeToReturn = SonarLintMode.Connected;
            this.SetSolutionBinding(SonarLintMode.Connected);

            // Act + Assert
            testSubject.GetCurrentConflicts().Should().BeEmpty("Not expecting any conflicts since solution is not legacy bound");
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_NoValidProjects()
        {
            // Arrange
            this.SetSolutionBinding(SonarLintMode.LegacyConnected);

            // Act + Assert
            testSubject.GetCurrentConflicts().Should().BeEmpty("Not expecting any conflicts since there are no projects");
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_MissingBaselineFile()
        {
            // Arrange
            this.SetSolutionBinding(SonarLintMode.LegacyConnected);
            this.SetValidProjects();

            // Act + Assert
            testSubject.GetCurrentConflicts().Should().BeEmpty("Not expecting any conflicts since the solution baseline is missing");
            this.outputWindowPane.AssertOutputStrings(1);

            var expectedBaselineLocation = configProvider.GetConfiguration()
                .BuildPathUnderConfigDirectory(ProjectToLanguageMapper
                    .GetLanguageForProject(projectHelper.FilteredProjects.First()).FileSuffixAndExtension);

            outputWindowPane.AssertOutputStrings(1);
            this.outputWindowPane.AssertPartialOutputStrings(expectedBaselineLocation);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_NoRuleSetDeclaration()
        {
            // Arrange
            this.SetSolutionBinding(SonarLintMode.LegacyConnected);
            this.SetValidProjects();
            this.SetValidSolutionRuleSetPerProjectKind();

            // Act + Assert
            testSubject.GetCurrentConflicts().Should().BeEmpty("Not expecting any conflicts since there are no project rulesets specified");
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_NoConflicts()
        {
            // Arrange
            this.SetSolutionBinding(SonarLintMode.LegacyConnected);
            this.SetValidProjects();
            this.SetValidSolutionRuleSetPerProjectKind();
            IEnumerable<RuleSetDeclaration> knownDeclarations = this.SetSingleValidRuleSetDeclarationPerProject();

            bool findConflictsWasCalled = false;
            this.ConfigureInspectorWithConflicts(knownDeclarations, () =>
            {
                findConflictsWasCalled = true;
                return new RuleConflictInfo();
            });

            // Act + Assert
            testSubject.GetCurrentConflicts().Should().BeEmpty("Not expecting any conflicts since there are no conflicts");
            this.outputWindowPane.AssertOutputStrings(0);
            findConflictsWasCalled.Should().BeTrue();
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_ExceptionDuringFindConflicts()
        {
            // Arrange
            this.SetSolutionBinding(SonarLintMode.LegacyConnected);
            this.SetValidProjects(2);
            this.SetValidSolutionRuleSetPerProjectKind();
            IEnumerable<RuleSetDeclaration> knownDeclarations = this.SetSingleValidRuleSetDeclarationPerProject();
            int findConflictCalls = 0;
            this.ConfigureInspectorWithConflicts(knownDeclarations, () =>
            {
                findConflictCalls++;
                throw new Exception($"Hello world{findConflictCalls} ");
            });

            // Act + Assert
            int conflicts = 0;
            using (new AssertIgnoreScope()) // Ignore asserts on exception
            {
                conflicts = testSubject.GetCurrentConflicts().Count;
            }

            conflicts.Should().Be(0, "Expect 0 conflicts since failed each time");
            this.outputWindowPane.AssertOutputStrings(2);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(0, new[] { "Hello", "world1" });
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(1, new[] { "Hello", "world2" });
            findConflictCalls.Should().Be(2);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_HasConflicts()
        {
            // Arrange
            this.SetSolutionBinding(SonarLintMode.LegacyConnected);
            this.SetValidProjects(4);
            this.SetValidSolutionRuleSetPerProjectKind();
            IEnumerable<RuleSetDeclaration> knownDeclarations = this.SetSingleValidRuleSetDeclarationPerProject();
            int findConflictCalls = 0;
            this.ConfigureInspectorWithConflicts(knownDeclarations, () =>
            {
                findConflictCalls++;
                return CreateConflictsBasedOnNumberOfCalls(findConflictCalls);
            });

            // Act + Assert
            testSubject.GetCurrentConflicts().Should().HaveCount(3, "Expect 3 conflicts (1st call is not a conflict)");
            this.outputWindowPane.AssertOutputStrings(0);
            findConflictCalls.Should().Be(4);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_HasConflicts_WithoutAggregationApplied()
        {
            // Arrange
            this.SetSolutionBinding(SonarLintMode.LegacyConnected);
            this.SetValidProjects(3);
            this.SetValidSolutionRuleSetPerProjectKind();
            IEnumerable<RuleSetDeclaration> knownDeclarations = this.SetValidRuleSetDeclarationPerProjectAndConfiguration(
                useUniqueProjectRuleSets: true,
                configurationNames: new[] { "Debug", "Release" });

            this.ConfigureInspectorWithConflicts(knownDeclarations, () =>
            {
                RuleSet temp = TestRuleSetHelper.CreateTestRuleSet(1);
                return new RuleConflictInfo(temp.Rules); // Missing rules
            });

            // Act + Assert
            testSubject.GetCurrentConflicts().Should().HaveCount(6, "Expecting 6 conflicts (3 projects x 2 configuration)");
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_HasConflicts_WithAggregationApplied()
        {
            // Arrange
            this.SetSolutionBinding(SonarLintMode.LegacyConnected);
            this.SetValidProjects(3);
            this.SetValidSolutionRuleSetPerProjectKind();
            IEnumerable<RuleSetDeclaration> knownDeclarations = this.SetValidRuleSetDeclarationPerProjectAndConfiguration(
              useUniqueProjectRuleSets: false,
              configurationNames: new[] { "Debug", "Release" });

            this.ConfigureInspectorWithConflicts(knownDeclarations, () =>
            {
                RuleSet temp = TestRuleSetHelper.CreateTestRuleSet(1);
                return new RuleConflictInfo(temp.Rules); // Missing rules
            });

            // Act + Assert
            testSubject.GetCurrentConflicts().Should().HaveCount(3, "Expecting 3 conflicts (1 per project) since the ruleset declaration for project configuration are the same");
            this.outputWindowPane.AssertOutputStrings(0);
        }

        #region Helpers

        private void SetSolutionBinding(SonarLintMode mode)
        {
            this.configProvider.ModeToReturn = mode;
            this.configProvider.ProjectToReturn = mode == SonarLintMode.Standalone ? null : new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
        }

        private void SetValidProjects(int numberOfProjects = 1)
        {
            List<ProjectMock> projects = new List<ProjectMock>();
            for (int i = 0; i < numberOfProjects; i++)
            {
                ProjectMock project = this.dte.Solution.AddOrGetProject($@"X:\Solution\Project\Project{i}.csproj");
                project.SetCSProjectKind();
                projects.Add(project);
            }

            this.projectHelper.FilteredProjects = projects;
        }

        private void SetValidSolutionRuleSetPerProjectKind()
        {
            foreach (var project in this.projectHelper.FilteredProjects)
            {
                var bindingConfiguration = configProvider.GetConfiguration();

                var solutionRuleSet =
                    bindingConfiguration.BuildPathUnderConfigDirectory(ProjectToLanguageMapper
                        .GetLanguageForProject(project).FileSuffixAndExtension);

                fileSystem.AddFile(solutionRuleSet, new MockFileData(""));
            }
        }

        private IEnumerable<RuleSetDeclaration> SetSingleValidRuleSetDeclarationPerProject(string configurationName = "Configuration", bool useUniqueProjectRuleSets = false)
        {
            List<RuleSetDeclaration> declarations = new List<RuleSetDeclaration>();

            foreach (ProjectMock project in this.projectHelper.FilteredProjects.OfType<ProjectMock>())
            {
                var configuration = new ConfigurationMock(configurationName);
                project.ConfigurationManager.Configurations.Add(configuration);

                PropertyMock ruleSetProperty = configuration.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
                ruleSetProperty.Value = project.FilePath.ToUpperInvariant(); // Catch cases where file paths are compared without OrdinalIgnoreCase
                if (useUniqueProjectRuleSets)
                {
                    ruleSetProperty.Value = Path.ChangeExtension(project.FilePath, configurationName + ".ruleSet");
                }

                PropertyMock ruleSetDirectories = configuration.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);
                ruleSetDirectories.Value = ConflictsManager.CombineDirectories(new[] { this.TestContext.TestRunDirectory, "." });

                RuleSetDeclaration declaration = new RuleSetDeclaration(
                    project,
                    ruleSetProperty,
                    ruleSetProperty.Value.ToString().ToLowerInvariant(), // Catch cases where file paths are compared without OrdinalIgnoreCase
                    configuration.ConfigurationName,
                    ruleSetDirectories.Value.ToString().Split(';'));

                declarations.Add(declaration);

                this.ruleSetInfoProvider.RegisterProjectInfo(project, declaration);
            }

            return declarations;
        }

        private IEnumerable<RuleSetDeclaration> SetValidRuleSetDeclarationPerProjectAndConfiguration(bool useUniqueProjectRuleSets, params string[] configurationNames)
        {
            List<RuleSetDeclaration> declarations = new List<RuleSetDeclaration>();

            foreach (string configurationName in configurationNames)
            {
                declarations.AddRange(this.SetSingleValidRuleSetDeclarationPerProject(configurationName, useUniqueProjectRuleSets));
            }

            return declarations;
        }

        private void ConfigureInspectorWithConflicts(IEnumerable<RuleSetDeclaration> knownDeclarations, Func<RuleConflictInfo> conflictFactory)
        {
            this.inspector.FindConflictingRulesAction = (baseline, ruleset, directories) =>
            {
                baseline.Should().NotBeNull();
                ruleset.Should().NotBeNull();
                this.fileSystem.AllFiles.Should().Contain(baseline);

                knownDeclarations.Any(dec =>
                {
                    if (dec.RuleSetPath == ruleset)
                    {
                        return new HashSet<string>(dec.RuleSetDirectories).SetEquals(directories);
                    }

                    return false;
                }).Should().BeTrue();

                return conflictFactory.Invoke();
            };
        }

        private static RuleConflictInfo CreateConflictsBasedOnNumberOfCalls(int findConflictCalls)
        {
            RuleSet temp = TestRuleSetHelper.CreateTestRuleSet(2);
            Dictionary<RuleReference, RuleAction> map = new Dictionary<RuleReference, RuleAction>();

            switch (findConflictCalls)
            {
                case 1:
                    return new RuleConflictInfo(); // No conflicts

                case 2:
                    return new RuleConflictInfo(temp.Rules); // Missing rules

                case 3:
                    map[temp.Rules[0]] = RuleAction.Error;
                    return new RuleConflictInfo(map); // Weakened rules

                case 4:
                    map[temp.Rules[0]] = RuleAction.Error;
                    return new RuleConflictInfo(temp.Rules.Skip(1), map); // One of each

                default:
                    FluentAssertions.Execution.Execute.Assertion.FailWith("Called unexpected number of times");
                    return null;
            }
        }

        #endregion Helpers
    }
}
